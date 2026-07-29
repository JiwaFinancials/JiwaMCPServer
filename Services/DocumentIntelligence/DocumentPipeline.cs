using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class DocumentPipeline(
    IDocumentRepository repository,
    IDocumentProcessingQueue queue,
    IDocumentExtractor extractor,
    IOcrService ocrService,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    SemanticChunker chunker,
    IMalwareScanner malwareScanner,
    IDocumentAuditLogger auditLogger,
    IMemoryCache cache,
    DocumentIntelligenceOptions options,
    ILogger<DocumentPipeline> logger) : IDocumentPipeline
{
    public async Task<DocumentIngestResponse> IngestAsync(DocumentIngestRequest request, CancellationToken cancellationToken)
    {
        ValidateIngestRequest(request);

        var content = Convert.FromBase64String(request.ContentBase64);
        if (content.Length > options.MaxDocumentSizeBytes)
        {
            throw new InvalidOperationException($"Document size exceeds maximum allowed size ({options.MaxDocumentSizeBytes} bytes).");
        }

        var extension = Path.GetExtension(request.FileName);
        if (!options.AllowedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            || !options.AllowedMimeTypes.Contains(request.MimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Document type is not allowed ({request.MimeType}, {extension}).");
        }

        var scanResult = await malwareScanner.ScanAsync(request.FileName, request.MimeType, content, cancellationToken);
        if (!scanResult.IsClean)
        {
            throw new InvalidOperationException($"Malware scan failed: {scanResult.Reason ?? "unknown reason"}.");
        }

        var hash = ComputeHash(content);
        var duplicate = await repository.FindByHashAsync(request.TenantId, hash, cancellationToken);
        if (duplicate is not null && !request.ForceReindex)
        {
            return new DocumentIngestResponse(
                duplicate.DocumentId,
                request.TenantId,
                duplicate.ProcessingStatus,
                "Duplicate upload detected. Existing indexed document returned.",
                duplicate.PageCount,
                duplicate.DocumentType,
                duplicate.Invoice,
                duplicate.UpdatedAt);
        }

        var documentId = request.ExternalDocumentId?.Trim();
        if (string.IsNullOrWhiteSpace(documentId))
        {
            documentId = Guid.NewGuid().ToString("N");
        }

        var now = DateTimeOffset.UtcNow;
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? Path.GetFileNameWithoutExtension(request.FileName)
            : request.Title.Trim();

        var document = new DocumentRecord(
            DocumentId: documentId,
            TenantId: request.TenantId,
            Title: title,
            FileName: request.FileName,
            MimeType: request.MimeType,
            FileSizeBytes: content.Length,
            FileHash: hash,
            PageCount: 0,
            DocumentType: DocumentType.Unknown,
            CreatedAt: now,
            UpdatedAt: now,
            ProcessingStatus: DocumentProcessingStatus.Queued,
            ProcessingError: null,
            Invoice: null,
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceFileId"] = request.SourceFileId ?? string.Empty,
                ["uploadedAt"] = now.ToString("O")
            });

        await repository.UpsertDocumentAsync(document, content, cancellationToken);
        await auditLogger.WriteAsync(request.TenantId, "document.ingest", documentId, "Document accepted", cancellationToken);

        if (request.ProcessAsync && options.EnableBackgroundProcessing)
        {
            queue.Enqueue(new DocumentProcessingJob(request.TenantId, documentId, request.ForceReindex));
            return new DocumentIngestResponse(documentId, request.TenantId, DocumentProcessingStatus.Queued, "Document queued for background processing.", null, null, null, now);
        }

        await ProcessDocumentAsync(request.TenantId, documentId, request.ForceReindex, cancellationToken);
        var processed = await repository.GetDocumentAsync(request.TenantId, documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document disappeared after processing.");

        return new DocumentIngestResponse(processed.DocumentId, processed.TenantId, processed.ProcessingStatus, "Document processed.", processed.PageCount, processed.DocumentType, processed.Invoice, processed.UpdatedAt);
    }

    public async Task ProcessDocumentAsync(string tenantId, string documentId, bool forceReindex, CancellationToken cancellationToken)
    {
        var doc = await repository.GetDocumentAsync(tenantId, documentId, cancellationToken)
            ?? throw new InvalidOperationException($"Document '{documentId}' was not found for tenant '{tenantId}'.");

        if (doc.ProcessingStatus == DocumentProcessingStatus.Indexed && !forceReindex)
        {
            return;
        }

        await repository.UpdateStatusAsync(tenantId, documentId, DocumentProcessingStatus.Extracting, null, cancellationToken);

        try
        {
            var content = await repository.GetDocumentContentAsync(tenantId, documentId, cancellationToken)
                ?? throw new InvalidOperationException("Document content is unavailable.");

            var extracted = await extractor.ExtractAsync(doc.FileName, doc.MimeType, content, cancellationToken);
            if (extracted.Pages.Count > options.MaxPages)
            {
                throw new InvalidOperationException($"Document has {extracted.Pages.Count} pages which exceeds configured limit ({options.MaxPages}).");
            }

            var pages = extracted.Pages
                .Select(page => page with { DocumentId = doc.DocumentId })
                .ToArray();

            if (extracted.NeedsOcr || extracted.ExtractedCharacterCount < options.MinimumExtractedCharactersBeforeOcr)
            {
                await repository.UpdateStatusAsync(tenantId, documentId, DocumentProcessingStatus.RunningOcr, null, cancellationToken);
                var ocrResult = await ocrService.ReadAsync(doc.FileName, doc.MimeType, content, cancellationToken);
                if (ocrResult.Success)
                {
                    pages = MergeOcr(pages, ocrResult.Pages, doc.DocumentId);
                }
                else
                {
                    logger.LogWarning("OCR failed for document {DocumentId}: {Error}", documentId, ocrResult.Error);
                }
            }

            var pageText = string.Join(Environment.NewLine + Environment.NewLine, pages.Select(p => p.Text));
            var detectedType = DetectDocumentType(doc.FileName, pageText);
            var invoice = ExtractInvoice(pageText);
            var metadata = BuildMetadata(doc, pageText, pages, extracted.Metadata, detectedType, invoice);

            await repository.UpsertPagesAsync(tenantId, documentId, pages, cancellationToken);
            await repository.UpdateStatusAsync(tenantId, documentId, DocumentProcessingStatus.Chunking, null, cancellationToken);
            var chunks = chunker.Chunk(documentId, pages);
            await repository.UpsertChunksAsync(tenantId, documentId, chunks, cancellationToken);

            await repository.UpdateStatusAsync(tenantId, documentId, DocumentProcessingStatus.Embedding, null, cancellationToken);
            var embeddedChunks = new EmbeddedChunk[chunks.Count];
            await Parallel.ForEachAsync(chunks.Select((chunk, i) => (chunk, i)), cancellationToken, async (item, ct) =>
            {
                var embedding = await embeddingService.CreateEmbeddingAsync(item.chunk.Text, ct);
                embeddedChunks[item.i] = new EmbeddedChunk(item.chunk, embedding);
            });

            await vectorStore.UpsertEmbeddingsAsync(tenantId, documentId, embeddedChunks, cancellationToken);

            var updated = doc with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                PageCount = pages.Length,
                DocumentType = detectedType,
                Invoice = invoice,
                ProcessingStatus = DocumentProcessingStatus.Indexed,
                ProcessingError = null,
                Metadata = metadata
            };

            await repository.UpsertDocumentAsync(updated, content, cancellationToken);
            await repository.UpdateStatusAsync(tenantId, documentId, DocumentProcessingStatus.Indexed, null, cancellationToken);
            await auditLogger.WriteAsync(tenantId, "document.indexed", documentId, $"Indexed {chunks.Count} chunks across {pages.Length} pages", cancellationToken);
        }
        catch (Exception ex)
        {
            await repository.UpdateStatusAsync(tenantId, documentId, DocumentProcessingStatus.Failed, ex.Message, cancellationToken);
            await auditLogger.WriteAsync(tenantId, "document.failed", documentId, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new InvalidOperationException("query is required.");
        }

        var queryEmbedding = await embeddingService.CreateEmbeddingAsync(request.Query, cancellationToken);
        var topK = Math.Min(options.MaxSearchResults, Math.Max(1, request.TopK));
        var results = await vectorStore.SearchAsync(request.TenantId, queryEmbedding, request.DocumentIds, topK, cancellationToken);

        var projected = request.IncludeChunkText
            ? results
            : results.Select(r => r with { Text = string.Empty }).ToArray();

        var answer = BuildSuggestedAnswer(request.Query, results);
        return new DocumentSearchResponse(request.Query, projected, answer);
    }

    public async Task<DocumentSummaryResponse?> GetSummaryAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        var cacheKey = $"summary:{tenantId}:{documentId}";
        if (cache.TryGetValue<DocumentSummaryResponse>(cacheKey, out var cached))
        {
            return cached;
        }

        var document = await repository.GetDocumentAsync(tenantId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var pages = await repository.GetPagesAsync(tenantId, documentId, cancellationToken);
        var text = string.Join(Environment.NewLine + Environment.NewLine, pages.Take(12).Select(p => p.Text));
        var keyDates = ExtractDates(text);
        var keyEntities = ExtractEntities(text);
        var summary = BuildSummarySnippet(text);

        var response = new DocumentSummaryResponse(
            document.DocumentId,
            document.TenantId,
            document.Title,
            document.DocumentType,
            document.PageCount,
            document.ProcessingStatus,
            document.Invoice,
            keyDates,
            keyEntities,
            summary,
            new Dictionary<string, string>(document.Metadata));

        cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));
        return response;
    }

    public async Task<DocumentInvoiceResponse?> ExtractInvoiceAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        var document = await repository.GetDocumentAsync(tenantId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var pages = await repository.GetPagesAsync(tenantId, documentId, cancellationToken);
        var text = string.Join(Environment.NewLine, pages.Select(p => p.Text));
        var invoice = document.Invoice ?? ExtractInvoice(text);
        var evidence = new List<string>();

        if (!string.IsNullOrWhiteSpace(invoice?.InvoiceNumber))
        {
            evidence.Add($"invoiceNumber={invoice.InvoiceNumber}");
        }

        if (!string.IsNullOrWhiteSpace(invoice?.Vendor))
        {
            evidence.Add($"vendor={invoice.Vendor}");
        }

        if (invoice is not null && invoice.TotalAmount > 0)
        {
            evidence.Add($"totalAmount={invoice.TotalAmount}");
        }

        return new DocumentInvoiceResponse(documentId, tenantId, invoice is not null, invoice, evidence);
    }

    public async Task<DocumentTablesResponse?> ExtractTablesAsync(string tenantId, string documentId, int? pageNumber, CancellationToken cancellationToken)
    {
        var document = await repository.GetDocumentAsync(tenantId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var pages = await repository.GetPagesAsync(tenantId, documentId, cancellationToken);
        var tables = pages
            .Where(p => !pageNumber.HasValue || p.PageNumber == pageNumber.Value)
            .SelectMany(p => p.Tables)
            .ToArray();

        return new DocumentTablesResponse(documentId, tenantId, tables.Length, tables);
    }

    public async Task<DeleteDocumentResponse> DeleteAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        await repository.DeleteDocumentAsync(tenantId, documentId, cancellationToken);
        await vectorStore.DeleteDocumentAsync(tenantId, documentId, cancellationToken);
        await auditLogger.WriteAsync(tenantId, "document.delete", documentId, "Document deleted", cancellationToken);
        cache.Remove($"summary:{tenantId}:{documentId}");
        return new DeleteDocumentResponse(documentId, tenantId, true, "Document deleted.");
    }

    public async Task<DocumentIngestResponse> ReindexAsync(string tenantId, string documentId, bool forceOcr, CancellationToken cancellationToken)
    {
        await ProcessDocumentAsync(tenantId, documentId, forceOcr, cancellationToken);
        var doc = await repository.GetDocumentAsync(tenantId, documentId, cancellationToken)
            ?? throw new InvalidOperationException($"Document '{documentId}' was not found.");

        return new DocumentIngestResponse(doc.DocumentId, doc.TenantId, doc.ProcessingStatus, "Document reindexed.", doc.PageCount, doc.DocumentType, doc.Invoice, doc.UpdatedAt);
    }

    private static void ValidateIngestRequest(DocumentIngestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new InvalidOperationException("tenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new InvalidOperationException("fileName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.MimeType))
        {
            throw new InvalidOperationException("mimeType is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentBase64))
        {
            throw new InvalidOperationException("contentBase64 is required.");
        }
    }

    private static string ComputeHash(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static DocumentPage[] MergeOcr(IReadOnlyList<DocumentPage> pages, IReadOnlyList<OcrPage> ocrPages, string documentId)
    {
        var byPage = pages.ToDictionary(x => x.PageNumber);

        foreach (var ocrPage in ocrPages)
        {
            if (!byPage.TryGetValue(ocrPage.PageNumber, out var page))
            {
                byPage[ocrPage.PageNumber] = new DocumentPage(documentId, ocrPage.PageNumber, ocrPage.Text, true, Array.Empty<string>(), Array.Empty<DocumentTable>());
                continue;
            }

            var mergedText = string.IsNullOrWhiteSpace(page.Text)
                ? ocrPage.Text
                : $"{page.Text}{Environment.NewLine}{Environment.NewLine}[OCR]{Environment.NewLine}{ocrPage.Text}";

            byPage[ocrPage.PageNumber] = page with
            {
                DocumentId = documentId,
                Text = mergedText,
                UsedOcr = true
            };
        }

        return byPage.Values.OrderBy(x => x.PageNumber).ToArray();
    }

    private static DocumentType DetectDocumentType(string fileName, string text)
    {
        var sample = (fileName + "\n" + text).ToLowerInvariant();
        if (sample.Contains("invoice") || sample.Contains("tax invoice") || sample.Contains("bill to"))
        {
            return DocumentType.Invoice;
        }

        if (sample.Contains("statement") || sample.Contains("balance brought forward"))
        {
            return DocumentType.Statement;
        }

        if (sample.Contains("agreement") || sample.Contains("terms and conditions") || sample.Contains("party"))
        {
            return DocumentType.Contract;
        }

        if (sample.Contains("user guide") || sample.Contains("manual") || sample.Contains("troubleshooting"))
        {
            return DocumentType.Manual;
        }

        if (sample.Contains("report") || sample.Contains("executive summary"))
        {
            return DocumentType.Report;
        }

        return DocumentType.Unknown;
    }

    private static InvoiceData? ExtractInvoice(string text)
    {
        var invoiceNo = MatchValue(text, @"invoice\s*(number|no|#)\s*[:\-]?\s*([A-Z0-9\-\/]+)", 2);
        var vendor = MatchValue(text, @"from\s*[:\-]?\s*([^\r\n]{2,80})", 1)
                     ?? MatchValue(text, @"vendor\s*[:\-]?\s*([^\r\n]{2,80})", 1);
        var invoiceDate = MatchValue(text, @"invoice\s*date\s*[:\-]?\s*([0-9]{1,2}[\/\-][0-9]{1,2}[\/\-][0-9]{2,4})", 1)
                          ?? MatchValue(text, @"date\s*[:\-]?\s*([0-9]{1,2}[\/\-][0-9]{1,2}[\/\-][0-9]{2,4})", 1);

        var total = MatchDecimal(text, @"(invoice\s*)?total\s*(due)?\s*[:\-]?\s*([$€£]?\s?[0-9,]+\.?[0-9]{0,2})");
        var tax = MatchDecimal(text, @"(gst|vat|tax)\s*[:\-]?\s*([$€£]?\s?[0-9,]+\.?[0-9]{0,2})");
        var currency = MatchValue(text, @"\b(AUD|USD|EUR|GBP|NZD|CAD)\b", 1);

        if (string.IsNullOrWhiteSpace(invoiceNo) && total <= 0)
        {
            return null;
        }

        return new InvoiceData(
            InvoiceNumber: invoiceNo ?? string.Empty,
            Vendor: vendor ?? string.Empty,
            InvoiceDate: invoiceDate ?? string.Empty,
            TotalAmount: total,
            TaxAmount: tax,
            Currency: currency ?? string.Empty);
    }

    private static Dictionary<string, string> BuildMetadata(DocumentRecord doc, string text, IReadOnlyList<DocumentPage> pages, IReadOnlyDictionary<string, string> extractedMetadata, DocumentType type, InvoiceData? invoice)
    {
        var metadata = new Dictionary<string, string>(doc.Metadata, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in extractedMetadata)
        {
            metadata[kv.Key] = kv.Value;
        }

        metadata["documentId"] = doc.DocumentId;
        metadata["title"] = doc.Title;
        metadata["fileName"] = doc.FileName;
        metadata["pageCount"] = pages.Count.ToString();
        metadata["documentType"] = type.ToString();
        metadata["createdDate"] = doc.CreatedAt.ToString("O");
        metadata["keyDates"] = string.Join(";", ExtractDates(text));
        metadata["keyEntities"] = string.Join(";", ExtractEntities(text));

        if (invoice is not null)
        {
            metadata["invoiceNumber"] = invoice.InvoiceNumber;
            metadata["vendor"] = invoice.Vendor;
            metadata["invoiceDate"] = invoice.InvoiceDate;
            metadata["totalAmount"] = invoice.TotalAmount.ToString("0.00");
            metadata["taxAmount"] = invoice.TaxAmount.ToString("0.00");
            metadata["currency"] = invoice.Currency;
        }

        return metadata;
    }

    private static string BuildSuggestedAnswer(string query, IReadOnlyList<SearchResultChunk> results)
    {
        if (results.Count == 0)
        {
            return "No relevant document chunks were found.";
        }

        var best = results[0];
        var snippet = best.Text.Length > 240 ? best.Text[..240] + "..." : best.Text;
        return $"Top match (Page {best.StartPage}-{best.EndPage}): {snippet}";
    }

    private static string BuildSummarySnippet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "No extractable text was found.";
        }

        var normalized = Regex.Replace(text, "\\s+", " ").Trim();
        if (normalized.Length <= 1200)
        {
            return normalized;
        }

        return normalized[..1200] + "...";
    }

    private static IReadOnlyList<string> ExtractDates(string text)
    {
        return Regex.Matches(text, @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractEntities(string text)
    {
        return Regex.Matches(text, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+){0,3}\b")
            .Select(m => m.Value)
            .Where(v => v.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToArray();
    }

    private static string? MatchValue(string text, string pattern, int group)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!match.Success || match.Groups.Count <= group)
        {
            return null;
        }

        return match.Groups[group].Value.Trim();
    }

    private static decimal MatchDecimal(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return 0;
        }

        var value = match.Groups[^1].Value.Replace("$", string.Empty).Replace("€", string.Empty).Replace("£", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(value, out var amount) ? amount : 0;
    }
}

public sealed class DocumentProcessingWorker(
    IDocumentProcessingQueue queue,
    IServiceProvider serviceProvider,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.DequeueAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<IDocumentPipeline>();
                await pipeline.ProcessDocumentAsync(job.TenantId, job.DocumentId, job.ForceReindex, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background document processing failed for tenant={TenantId}, document={DocumentId}", job.TenantId, job.DocumentId);
            }
        }
    }
}
