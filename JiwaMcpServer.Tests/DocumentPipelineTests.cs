using JiwaMcpServer.Services.DocumentIntelligence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Integration-style tests for the document intelligence pipeline (in-memory infrastructure).
/// </summary>
public class DocumentPipelineTests
{
    private static IDocumentPipeline BuildPipeline(DocumentIntelligenceOptions? options = null)
    {
        options ??= new DocumentIntelligenceOptions
        {
            ChunkTargetTokens = 200,
            ChunkMinTokens = 80,
            ChunkMaxTokens = 400,
            ChunkOverlapTokens = 40,
            EmbeddingDimensions = 64,
            MinimumExtractedCharactersBeforeOcr = 5
        };

        var repo = new InMemoryDocumentRepository();
        var vectorStore = new InMemoryVectorStore();
        var deterministic = new DeterministicEmbeddingService(options);
        var httpFactory = new ServiceCollection().AddHttpClient().BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var embeddingService = new OpenAiEmbeddingService(options, httpFactory, NullLogger<OpenAiEmbeddingService>.Instance, deterministic);
        var chunker = new SemanticChunker(options);
        var queue = new DocumentProcessingQueue();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var scanner = new PassThroughMalwareScanner();
        var audit = new LoggerAuditLogger(NullLogger<LoggerAuditLogger>.Instance);

        var extractors = new IDocumentExtractor[]
        {
            new PdfDocumentExtractor(),
            new DocxDocumentExtractor(),
            new SpreadsheetDocumentExtractor(),
            new PlainTextDocumentExtractor(),
            new ImageDocumentExtractor()
        };
        IDocumentExtractor composite = new CompositeDocumentExtractor(extractors);

        var ocrServices = Array.Empty<IOcrService>();
        IOcrService ocrComposite = new CompositeOcrService(ocrServices);

        return new DocumentPipeline(
            repo, queue, composite, ocrComposite, embeddingService,
            vectorStore, chunker, scanner, audit, cache, options,
            NullLogger<DocumentPipeline>.Instance);
    }

    private static string MakeBase64(string text) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task IngestPlainText_ReturnsIndexedStatus()
    {
        var pipeline = BuildPipeline();
        var response = await pipeline.IngestAsync(new DocumentIngestRequest(
            TenantId: "tenant1",
            FileName: "hello.txt",
            MimeType: "text/plain",
            ContentBase64: MakeBase64("Hello world document text for testing chunking."),
            Title: "Test Document",
            ProcessAsync: false,
            ExternalDocumentId: null,
            ForceReindex: false,
            SourceFileId: null),
            CancellationToken.None);

        Assert.Equal(DocumentProcessingStatus.Indexed, response.Status);
        Assert.NotEmpty(response.DocumentId);
        Assert.NotNull(response.UpdatedAt);
    }

    [Fact]
    public async Task IngestAndSearch_ReturnsRelevantChunk()
    {
        var pipeline = BuildPipeline();
        const string text = "The invoice total is $408.86 due on 15/01/2025. Tax amount GST $37.17. Vendor: Acme Corp. Invoice number INV-2025-001.";

        var ingest = await pipeline.IngestAsync(new DocumentIngestRequest(
            TenantId: "tenant1",
            FileName: "invoice.txt",
            MimeType: "text/plain",
            ContentBase64: MakeBase64(text),
            Title: "Invoice",
            ProcessAsync: false,
            ExternalDocumentId: "doc-inv-001",
            ForceReindex: false,
            SourceFileId: null),
            CancellationToken.None);

        Assert.Equal(DocumentProcessingStatus.Indexed, ingest.Status);

        var search = await pipeline.SearchAsync(new DocumentSearchRequest(
            TenantId: "tenant1",
            Query: "invoice total",
            DocumentIds: null,
            TopK: 3,
            IncludeChunkText: true),
            CancellationToken.None);

        Assert.NotEmpty(search.Results);
        Assert.NotEmpty(search.SuggestedAnswer);
    }

    [Fact]
    public async Task GetSummary_ReturnsDocumentMetadata()
    {
        var pipeline = BuildPipeline();
        const string docText = "CONTRACT AGREEMENT\n\nThis agreement between Party A and Party B is dated 20/06/2024.";

        var ingest = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "contract.txt", "text/plain", MakeBase64(docText), "Contract", false, null, false, null),
            CancellationToken.None);

        var summary = await pipeline.GetSummaryAsync("t1", ingest.DocumentId, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(ingest.DocumentId, summary.DocumentId);
        Assert.NotEmpty(summary.SummaryText);
    }

    [Fact]
    public async Task ExtractInvoice_DetectsInvoiceFields()
    {
        var pipeline = BuildPipeline();
        const string invoiceText =
            "TAX INVOICE\nInvoice Number: INV-5001\nVendor: Widget Supplies Pty Ltd\nInvoice Date: 10/07/2025\nTotal: $1,250.00\nGST: $113.64\nCurrency: AUD";

        var ingest = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "inv.txt", "text/plain", MakeBase64(invoiceText), null, false, null, false, null),
            CancellationToken.None);

        var result = await pipeline.ExtractInvoiceAsync("t1", ingest.DocumentId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsInvoice);
        Assert.NotNull(result.Invoice);
        Assert.Contains("INV-5001", result.Invoice!.InvoiceNumber);
        Assert.True(result.Invoice.TotalAmount > 0);
    }

    [Fact]
    public async Task ExtractTables_ReturnsCsvTable()
    {
        var pipeline = BuildPipeline();
        const string csv = "Name,Age,City\nAlice,30,Melbourne\nBob,25,Sydney";

        var ingest = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "data.csv", "text/csv", MakeBase64(csv), null, false, null, false, null),
            CancellationToken.None);

        var tables = await pipeline.ExtractTablesAsync("t1", ingest.DocumentId, null, CancellationToken.None);

        Assert.NotNull(tables);
        Assert.True(tables.TableCount > 0);
    }

    [Fact]
    public async Task DeleteDocument_RemovesDocument()
    {
        var pipeline = BuildPipeline();
        var ingest = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "temp.txt", "text/plain", MakeBase64("Temp content for deletion test."), null, false, null, false, null),
            CancellationToken.None);

        var delete = await pipeline.DeleteAsync("t1", ingest.DocumentId, CancellationToken.None);

        Assert.True(delete.Deleted);

        var summary = await pipeline.GetSummaryAsync("t1", ingest.DocumentId, CancellationToken.None);
        Assert.Null(summary);
    }

    [Fact]
    public async Task Ingest_DuplicateHash_ReturnsExistingDocument()
    {
        var pipeline = BuildPipeline();
        const string content = "Unique content for duplicate detection test.";
        var b64 = MakeBase64(content);

        var first = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "dup.txt", "text/plain", b64, null, false, null, false, null),
            CancellationToken.None);

        var second = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "dup.txt", "text/plain", b64, null, false, null, false, null),
            CancellationToken.None);

        Assert.Equal(first.DocumentId, second.DocumentId);
        Assert.Contains("Duplicate", second.Message);
    }

    [Fact]
    public async Task Reindex_ReprocessesDocument()
    {
        var pipeline = BuildPipeline();
        const string content = "Original content for reindex test with enough tokens to chunk.";
        var ingest = await pipeline.IngestAsync(new DocumentIngestRequest(
            "t1", "reindex.txt", "text/plain", MakeBase64(content), null, false, null, false, null),
            CancellationToken.None);

        var reindex = await pipeline.ReindexAsync("t1", ingest.DocumentId, false, CancellationToken.None);

        Assert.Equal(DocumentProcessingStatus.Indexed, reindex.Status);
    }

    [Fact]
    public async Task Ingest_InvalidMimeType_ThrowsException()
    {
        var pipeline = BuildPipeline();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.IngestAsync(new DocumentIngestRequest(
                "t1", "bad.exe", "application/x-msdownload",
                MakeBase64("malicious"), null, false, null, false, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Ingest_OversizedContent_ThrowsException()
    {
        var options = new DocumentIntelligenceOptions { MaxDocumentSizeBytes = 50 };
        var pipeline = BuildPipeline(options);
        var bigContent = new string('x', 100);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.IngestAsync(new DocumentIngestRequest(
                "t1", "huge.txt", "text/plain", MakeBase64(bigContent), null, false, null, false, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Search_NoEmbeddingsIndexed_ReturnsEmpty()
    {
        var pipeline = BuildPipeline();
        var result = await pipeline.SearchAsync(new DocumentSearchRequest(
            TenantId: "empty-tenant",
            Query: "anything",
            DocumentIds: null,
            TopK: 5,
            IncludeChunkText: true),
            CancellationToken.None);

        Assert.Empty(result.Results);
    }

    [Fact]
    public void SemanticChunker_ProducesChunks_ForMultiPageDocument()
    {
        var options = new DocumentIntelligenceOptions { ChunkTargetTokens = 60, ChunkMinTokens = 20, ChunkMaxTokens = 120, ChunkOverlapTokens = 10 };
        var chunker = new SemanticChunker(options);
        var pages = Enumerable.Range(1, 5).Select(i =>
            new DocumentPage("doc1", i, $"Page {i} content. " + string.Join(". ", Enumerable.Range(1, 8).Select(j => $"Sentence {j} on page {i}")), false, Array.Empty<string>(), Array.Empty<DocumentTable>()))
            .ToArray();

        var chunks = chunker.Chunk("doc1", pages);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, c =>
        {
            Assert.NotEmpty(c.ChunkId);
            Assert.NotEmpty(c.Text);
            Assert.True(c.StartPage >= 1);
            Assert.True(c.EndPage >= c.StartPage);
        });
    }

    [Fact]
    public void DeterministicEmbedding_ProducesDeterministicOutput()
    {
        var options = new DocumentIntelligenceOptions { EmbeddingDimensions = 64 };
        var svc = new DeterministicEmbeddingService(options);
        const string text = "same input text for both calls";

        var first = svc.CreateEmbeddingAsync(text, CancellationToken.None).GetAwaiter().GetResult();
        var second = svc.CreateEmbeddingAsync(text, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task DocumentTools_Contract_ReturnsSchemas()
    {
        var pipeline = BuildPipeline();
        var fileStorage = new JiwaMcpServer.Services.FileStorageService();
        var tools = new JiwaMcpServer.Tools.DocumentTools(pipeline, fileStorage);

        var result = await tools.DocumentContracts();

        Assert.Contains("document.ingest", result);
        Assert.Contains("document.search", result);
        Assert.Contains("document.getSummary", result);
        Assert.Contains("document.extractInvoice", result);
        Assert.Contains("document.extractTables", result);
    }

    // -----------------------------------------------------------------------
    // PDF extraction tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PdfExtractor_UncompressedPdf_ExtractsText()
    {
        // Minimal valid PDF with an uncompressed content stream containing BT/ET text
        var pdf = BuildMinimalUncompressedPdf("Invoice Total $408.86 GST $37.17 Vendor Acme Corp");
        var extractor = new PdfDocumentExtractor();

        var result = await extractor.ExtractAsync("test.pdf", "application/pdf", pdf, CancellationToken.None);

        Assert.False(result.NeedsOcr, "Uncompressed text PDF should not need OCR");
        Assert.True(result.ExtractedCharacterCount > 0, "Should extract some text");
        var allText = string.Join(" ", result.Pages.Select(p => p.Text));
        Assert.Contains("408.86", allText);
        Assert.Contains("Acme", allText);
    }

    [Fact]
    public async Task PdfExtractor_FlateDecodePdf_ExtractsText()
    {
        // Build a PDF with a FlateDecode-compressed content stream
        var pdf = BuildFlateDecodePdf("INVOICE Tax Invoice Number INV-5001 Total $1250.00");
        var extractor = new PdfDocumentExtractor();

        var result = await extractor.ExtractAsync("invoice.pdf", "application/pdf", pdf, CancellationToken.None);

        Assert.False(result.NeedsOcr, "FlateDecode PDF with text should not need OCR");
        Assert.True(result.ExtractedCharacterCount > 0, "Should decompress and extract text");
        var allText = string.Join(" ", result.Pages.Select(p => p.Text));
        Assert.Contains("1250", allText);
    }

    [Fact]
    public async Task PdfExtractor_ImageOnlyPdf_SetsNeedsOcr()
    {
        // Minimal PDF with no text streams — simulates a scanned document
        var pdf = BuildImageOnlyPdf();
        var extractor = new PdfDocumentExtractor();

        var result = await extractor.ExtractAsync("scan.pdf", "application/pdf", pdf, CancellationToken.None);

        Assert.True(result.NeedsOcr, "Image-only PDF should set NeedsOcr");
        Assert.Equal(0, result.ExtractedCharacterCount);
    }

    [Fact]
    public async Task FileTools_ReadUploadedFile_PdfReturnsExtractedText()
    {
        var fileStorage = new JiwaMcpServer.Services.FileStorageService();
        JiwaMcpServer.Services.FileStorageService.SetSessionId("test-pdf-session");

        var pdf = BuildFlateDecodePdf("Purchase Order Amount Due $750.00 Supplier Widgets Ltd");
        var b64 = Convert.ToBase64String(pdf);
        var uploadResult = fileStorage.UploadFile("order.pdf", "application/pdf", b64);
        Assert.True(uploadResult.IsSuccess);

        var extractor = new CompositeDocumentExtractor(new IDocumentExtractor[]
        {
            new PdfDocumentExtractor(), new PlainTextDocumentExtractor()
        });
        var tools = new JiwaMcpServer.Tools.FileTools(fileStorage, null, extractor);

        var result = await tools.ReadUploadedFile(uploadResult.FileId!);

        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("pageCount", result);
        Assert.Contains("750", result);
    }

    [Fact]
    public async Task DocumentIngest_SourceFileIdFromDifferentSession_Succeeds()
    {
        var pipeline = BuildPipeline();
        var fileStorage = new JiwaMcpServer.Services.FileStorageService();

        // Upload in one session
        JiwaMcpServer.Services.FileStorageService.SetSessionId("upload-session");
        var pdf = BuildFlateDecodePdf("INVOICE INV-77 Total $408.86 Vendor ACME");
        var upload = fileStorage.UploadFile("invoice.pdf", "application/pdf", Convert.ToBase64String(pdf));
        Assert.True(upload.IsSuccess);
        Assert.NotNull(upload.FileId);

        // Ingest from a different session (simulating a different MCP connection)
        JiwaMcpServer.Services.FileStorageService.SetSessionId("ingest-session");
        var tools = new JiwaMcpServer.Tools.DocumentTools(pipeline, fileStorage);

        var ingestResponse = await tools.DocumentIngest(sourceFileId: upload.FileId!, tenantId: "tenant-a", processAsync: false);

        Assert.DoesNotContain("\"error\"", ingestResponse, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("documentId", ingestResponse, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Indexed", ingestResponse, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // PDF builder helpers
    // -----------------------------------------------------------------------

    private static byte[] BuildMinimalUncompressedPdf(string text)
    {
        // Build a minimal 1-page uncompressed PDF whose content stream holds a BT/ET block
        // with the text split into Tj tokens.
        var contentStream = BuildPdfContentStream(text);
        var contentBytes = System.Text.Encoding.Latin1.GetBytes(contentStream);
        return BuildPdfBytes(contentBytes, isCompressed: false);
    }

    private static byte[] BuildFlateDecodePdf(string text)
    {
        var contentStream = BuildPdfContentStream(text);
        var contentBytes = System.Text.Encoding.Latin1.GetBytes(contentStream);
        // Compress with zlib (ZLibStream)
        using var ms = new System.IO.MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionMode.Compress, leaveOpen: true))
            zlib.Write(contentBytes, 0, contentBytes.Length);
        return BuildPdfBytes(ms.ToArray(), isCompressed: true);
    }

    private static byte[] BuildImageOnlyPdf()
    {
        // PDF with no content stream at all - just the bare structure
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("1 0 obj<</Type /Catalog /Pages 2 0 R>>endobj");
        sb.AppendLine("2 0 obj<</Type /Pages /Kids[3 0 R]/Count 1>>endobj");
        sb.AppendLine("3 0 obj<</Type /Page /Parent 2 0 R /MediaBox[0 0 612 792]>>endobj");
        sb.AppendLine("xref\n0 4");
        sb.AppendLine("trailer<</Size 4/Root 1 0 R>>");
        sb.AppendLine("%%EOF");
        return System.Text.Encoding.Latin1.GetBytes(sb.ToString());
    }

    private static string BuildPdfContentStream(string text)
    {
        // Write text words as individual Tj tokens so the extractor can pick them up
        var tokens = new System.Text.StringBuilder();
        tokens.Append("BT\n/F1 12 Tf\n");
        int y = 700;
        foreach (var word in text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
        {
            var safe = word.Replace("(", "\\(").Replace(")", "\\)").Replace("\\", "\\\\");
            tokens.Append($"100 {y} Td ({safe}) Tj\n");
            y -= 14;
        }
        tokens.Append("ET");
        return tokens.ToString();
    }

    private static byte[] BuildPdfBytes(byte[] streamContent, bool isCompressed)
    {
        var streamLen = streamContent.Length;
        var filterLine = isCompressed ? "/Filter /FlateDecode " : "";

        // We'll build the PDF as a string where the stream placeholder is replaced
        var header = System.Text.Encoding.Latin1.GetBytes(
            $"%PDF-1.4\n" +
            $"1 0 obj<</Type /Catalog /Pages 2 0 R>>endobj\n" +
            $"2 0 obj<</Type /Pages /Kids[3 0 R]/Count 1>>endobj\n" +
            $"3 0 obj<</Type /Page /Parent 2 0 R /MediaBox[0 0 612 792] /Contents 4 0 R>>endobj\n" +
            $"4 0 obj<</Length {streamLen} {filterLine}>>\nstream\n");

        var footer = System.Text.Encoding.Latin1.GetBytes(
            $"\nendstream\nendobj\n" +
            $"xref\n0 5\n" +
            $"trailer<</Size 5/Root 1 0 R>>\n" +
            $"%%EOF\n");

        var result = new byte[header.Length + streamContent.Length + footer.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(streamContent, 0, result, header.Length, streamContent.Length);
        Buffer.BlockCopy(footer, 0, result, header.Length + streamContent.Length, footer.Length);
        return result;
    }
}
