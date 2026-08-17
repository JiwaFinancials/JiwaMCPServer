using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public sealed class DeterministicToolReranker(
    IToolRoutingContextAccessor contextAccessor,
    IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions,
    ILogger<DeterministicToolReranker> logger,
    FileStorageService? fileStorage = null) : IToolReranker
{
    private enum UploadedAttachmentKind
    {
        Unknown,
        Csv,
        Excel,
        Document
    }

    private sealed record UploadedFileRoutingContext(string FileName, string MimeType, UploadedAttachmentKind Kind);

    private static readonly HashSet<string> PromptStopWords =
    [
        "a", "an", "and", "create", "find", "for", "from", "get", "in", "me", "of", "on", "open", "search", "show", "the", "to", "with"
    ];

    private readonly ToolRoutingDiagnosticsOptions _diagnosticsOptions = diagnosticsOptions.Value;
    private readonly FileStorageService? _fileStorage = fileStorage;

    public Task<IReadOnlyList<RerankedTool>> RerankAsync(
        string userPrompt,
        IReadOnlyList<ToolCandidate> candidates,
        int maxSelectedTools,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<RerankedTool>>([]);
        }

        var routingContext = contextAccessor.Current;
        using var activity = ToolRoutingStructuredLogger.StartStageActivity("ToolReranking", routingContext);
        var stopwatch = Stopwatch.StartNew();

        var normalizedPrompt = Normalize(userPrompt);
        var promptTerms = normalizedPrompt
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => !PromptStopWords.Contains(term))
            .ToArray();
        var uploadedFileContext = TryGetUploadedFileContext();
        var referencesUploadedFile = uploadedFileContext is not null || PromptReferencesUploadedFile(normalizedPrompt);

        var prefersPurchaseOrder = ContainsPhrase(normalizedPrompt, "po") || ContainsPhrase(normalizedPrompt, "purchase order");
        var prefersCreditorPurchase = ContainsAny(normalizedPrompt, "supplier invoice", "vendor bill", "ap invoice", "accounts payable", "creditor purchase");

        var rankedCandidates = candidates
            .Select(candidate => ScoreCandidate(candidate, normalizedPrompt, promptTerms, prefersPurchaseOrder, prefersCreditorPurchase, referencesUploadedFile, uploadedFileContext))
            .OrderByDescending(candidate => candidate.RawScore)
            .ThenByDescending(candidate => candidate.Candidate.Similarity)
            .ThenBy(candidate => candidate.Candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var candidatePool = rankedCandidates.Where(candidate => !candidate.HasAttachmentMismatch).ToArray();
        if (candidatePool.Length == 0)
        {
            candidatePool = rankedCandidates;
        }

        var reranked = candidatePool
            .Where((candidate, index) => index == 0 || candidate.Confidence >= 0.25)
            .Take(maxSelectedTools)
            .Select(candidate => new RerankedTool(candidate.Candidate.Name, candidate.Confidence, candidate.Reason, candidate.Candidate.Tool))
            .ToList();

        AppendRequiredAttachmentTools(reranked, candidatePool, normalizedPrompt, uploadedFileContext);
        var rerankedArray = reranked.ToArray();

        stopwatch.Stop();

        if (routingContext is not null)
        {
            routingContext.SelectedTools = rerankedArray;
            routingContext.RerankingLatency = stopwatch.Elapsed;
            activity?.SetTag("tool.selected.count", rerankedArray.Length);
            activity?.SetTag("reranking.latency.ms", stopwatch.Elapsed.TotalMilliseconds);

            if (_diagnosticsOptions.EnableDetailedLogging)
            {
                ToolRoutingStructuredLogger.LogToolSelectionResult(logger, routingContext);
            }

            if (_diagnosticsOptions.EnableMetrics)
            {
                ToolRoutingTelemetry.ToolRerankingDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, ToolRoutingTelemetry.CreateCommonTags(routingContext));
            }
        }

        return Task.FromResult<IReadOnlyList<RerankedTool>>(rerankedArray);
    }

    private (ToolCandidate Candidate, double RawScore, double Confidence, string Reason, bool HasAttachmentMismatch) ScoreCandidate(
        ToolCandidate candidate,
        string normalizedPrompt,
        IReadOnlyList<string> promptTerms,
        bool prefersPurchaseOrder,
        bool prefersCreditorPurchase,
        bool referencesUploadedFile,
        UploadedFileRoutingContext? uploadedFileContext)
    {
        var toolText = string.Join(' ',
            candidate.Name,
            candidate.Tool.Description,
            candidate.Tool.Domain,
            candidate.Tool.Category,
            candidate.Tool.BusinessCapability,
            string.Join(' ', candidate.Tool.Keywords),
            string.Join(' ', candidate.Tool.Tags));
        var normalizedToolText = Normalize(toolText);

        var score = candidate.Similarity;
        var lexicalHits = promptTerms.Count(term => ContainsPhrase(normalizedToolText, term));
        score += Math.Min(0.24, lexicalHits * 0.04);

        var phraseMatch = candidate.Tool.Keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(Normalize)
            .FirstOrDefault(keyword => keyword.Length > 2 && ContainsPhrase(normalizedPrompt, keyword));
        if (!string.IsNullOrWhiteSpace(phraseMatch))
        {
            score += 0.12;
        }

        if (!string.IsNullOrWhiteSpace(candidate.Tool.Domain) && ContainsPhrase(normalizedPrompt, candidate.Tool.Domain))
        {
            score += 0.08;
        }

        var isPurchaseOrderTool = ContainsAny(normalizedToolText, "purchase order", " create po ", " po ");
        var isCreditorPurchaseTool = ContainsAny(normalizedToolText, "creditor purchase", "supplier invoice", "vendor bill", "accounts payable", "ap invoice");
        var isSupplierTool = string.Equals(candidate.Tool.Category, "Supplier", StringComparison.OrdinalIgnoreCase);
        var isCsvTool = ContainsAny(normalizedToolText, "csv", "comma separated");
        var isExcelTool = ContainsAny(normalizedToolText, "excel", "xlsx", "spreadsheet");
        var isDocumentTool = ContainsAny(normalizedToolText, "document", "pdf", "ocr", "image", "docx", "invoice extraction");
        var isAttachmentAwareTool = ContainsAny(normalizedToolText, "attachment", "attached file", "uploaded file", "uploaded attachment", "uploaded document", "source file", "filename", "file name", "sourcefileid");
        var promptMentionsCsv = ContainsAny(normalizedPrompt, "csv", "comma separated");
        var promptMentionsExcel = ContainsAny(normalizedPrompt, "excel", "xlsx", "spreadsheet", "workbook");
        var attachmentReason = string.Empty;
        var hasAttachmentMismatch = false;

        if (prefersPurchaseOrder && !prefersCreditorPurchase)
        {
            if (isPurchaseOrderTool)
            {
                score += 0.20;
            }

            if (isCreditorPurchaseTool)
            {
                score -= 0.18;
            }
        }

        if (prefersCreditorPurchase)
        {
            if (isCreditorPurchaseTool)
            {
                score += 0.20;
            }

            if (isPurchaseOrderTool)
            {
                score -= 0.18;
            }

            if (isSupplierTool)
            {
                score -= 0.24;
            }
        }

        if (referencesUploadedFile && uploadedFileContext is null && !promptMentionsCsv && !promptMentionsExcel)
        {
            if (isCsvTool)
            {
                score -= 0.32;
                hasAttachmentMismatch = true;
                attachmentReason = "Prompt references an uploaded attachment without indicating CSV input";
            }
            else if (isExcelTool)
            {
                score -= 0.26;
                hasAttachmentMismatch = true;
                attachmentReason = "Prompt references an uploaded attachment without indicating spreadsheet input";
            }
            else if (isDocumentTool)
            {
                score += 0.10;
                attachmentReason = "Prompt references an uploaded attachment";
            }
            else if (isAttachmentAwareTool)
            {
                score += 0.06;
                attachmentReason = "Prompt references an uploaded attachment";
            }
        }

        if (uploadedFileContext is not null)
        {
            switch (uploadedFileContext.Kind)
            {
                case UploadedAttachmentKind.Csv:
                    if (isCsvTool)
                    {
                        score += 0.30;
                        attachmentReason = "Uploaded attachment is a CSV file";
                    }
                    else
                    {
                        score -= 0.10;
                        if (isDocumentTool)
                        {
                            score -= 0.06;
                        }
                    }
                    break;
                case UploadedAttachmentKind.Excel:
                    attachmentReason = "Uploaded attachment is an Excel workbook";
                    if (isCsvTool)
                    {
                        score -= 0.18;
                        hasAttachmentMismatch = true;
                    }
                    else if (isExcelTool)
                    {
                        score += 0.18;
                    }
                    break;
                case UploadedAttachmentKind.Document:
                {
                    var attachmentLabel = DescribeAttachment(uploadedFileContext);
                    attachmentReason = $"Uploaded attachment '{uploadedFileContext.FileName}' is {attachmentLabel}";
                    if (isCsvTool)
                    {
                        score -= 0.45;
                        hasAttachmentMismatch = true;
                        attachmentReason = $"Uploaded attachment '{uploadedFileContext.FileName}' is {attachmentLabel}, not a CSV file";
                    }
                    else if (isExcelTool)
                    {
                        score -= 0.32;
                        hasAttachmentMismatch = true;
                        attachmentReason = $"Uploaded attachment '{uploadedFileContext.FileName}' is {attachmentLabel}, not a spreadsheet";
                    }
                    else
                    {
                        if (isDocumentTool)
                        {
                            score += 0.18;
                        }

                        if (isAttachmentAwareTool)
                        {
                            score += 0.08;
                        }
                    }

                    break;
                }
            }
        }

        var rawScore = score;
        var confidence = Math.Round(Math.Clamp(rawScore, 0, 1), 2, MidpointRounding.AwayFromZero);
        var reason = lexicalHits > 0 || !string.IsNullOrWhiteSpace(phraseMatch)
            ? "Semantic similarity and prompt term alignment"
            : "Semantic similarity";

        if (prefersPurchaseOrder && isPurchaseOrderTool && !prefersCreditorPurchase)
        {
            reason = "Prompt indicates purchase-order intent";
        }
        else if (prefersCreditorPurchase && isCreditorPurchaseTool)
        {
            reason = "Prompt indicates supplier-invoice intent";
        }

        reason = CombineReasons(reason, attachmentReason);
        return (candidate, rawScore, confidence, reason, hasAttachmentMismatch);
    }

    private UploadedFileRoutingContext? TryGetUploadedFileContext()
    {
        if (_fileStorage is null)
        {
            return null;
        }

        var latestFile = _fileStorage.GetLastUploadedFileInfo()
            ?? _fileStorage.GetLastUploadedFileInfoAcrossSessions();
        if (latestFile is null)
        {
            return null;
        }

        return new UploadedFileRoutingContext(
            latestFile.FileName,
            latestFile.MimeType,
            ClassifyAttachment(latestFile.FileName, latestFile.MimeType));
    }

    private static UploadedAttachmentKind ClassifyAttachment(string fileName, string mimeType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            return UploadedAttachmentKind.Csv;
        }

        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("excel", StringComparison.OrdinalIgnoreCase))
        {
            return UploadedAttachmentKind.Excel;
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".doc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("msword", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
            || mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return UploadedAttachmentKind.Document;
        }

        return UploadedAttachmentKind.Unknown;
    }

    private static string DescribeAttachment(UploadedFileRoutingContext uploadedFileContext)
    {
        var extension = Path.GetExtension(uploadedFileContext.FileName ?? string.Empty);
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
            || uploadedFileContext.MimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "a PDF document";
        }

        if (string.Equals(extension, ".doc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
            || uploadedFileContext.MimeType.Contains("msword", StringComparison.OrdinalIgnoreCase)
            || uploadedFileContext.MimeType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase))
        {
            return "a Word document";
        }

        if (uploadedFileContext.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
        {
            return "an image document";
        }

        return "a document";
    }

    private static void AppendRequiredAttachmentTools(
        List<RerankedTool> selectedTools,
        IReadOnlyList<(ToolCandidate Candidate, double RawScore, double Confidence, string Reason, bool HasAttachmentMismatch)> rankedCandidates,
        string normalizedPrompt,
        UploadedFileRoutingContext? uploadedFileContext)
    {
        var selectedToolNames = selectedTools
            .Select(tool => tool.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredToolNames = GetRequiredAttachmentToolNames(normalizedPrompt, selectedToolNames, uploadedFileContext);
        if (requiredToolNames.Count == 0)
        {
            return;
        }

        var candidateLookup = rankedCandidates
            .Where(candidate => !candidate.HasAttachmentMismatch)
            .GroupBy(candidate => candidate.Candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var toolName in requiredToolNames)
        {
            if (!candidateLookup.TryGetValue(toolName, out var candidate))
            {
                continue;
            }

            selectedTools.Add(new RerankedTool(
                candidate.Candidate.Name,
                Math.Max(candidate.Confidence, 0.9),
                CombineReasons(candidate.Reason, "Attachment workflow requirement"),
                candidate.Candidate.Tool));
        }
    }

    private static IReadOnlyList<string> GetRequiredAttachmentToolNames(
        string normalizedPrompt,
        IReadOnlySet<string> selectedToolNames,
        UploadedFileRoutingContext? uploadedFileContext)
    {
        if (uploadedFileContext is null && !PromptReferencesUploadedFile(normalizedPrompt))
        {
            return [];
        }

        var promptMentionsCsv = ContainsAny(normalizedPrompt, "csv", "comma separated");
        var promptMentionsExcel = ContainsAny(normalizedPrompt, "excel", "xlsx", "spreadsheet", "workbook");

        string[] toolNames = uploadedFileContext?.Kind switch
        {
            UploadedAttachmentKind.Csv => ["QueryCsv"],
            UploadedAttachmentKind.Excel => ["DescribeExcel", "ReadExcelRows", "QueryExcel"],
            UploadedAttachmentKind.Document => ["DocumentIngest", "DocumentExtractInvoice"],
            _ when promptMentionsCsv => ["QueryCsv"],
            _ when promptMentionsExcel => ["DescribeExcel", "ReadExcelRows", "QueryExcel"],
            _ => ["DocumentIngest", "DocumentExtractInvoice"]
        };

        return toolNames
            .Where(toolName => !selectedToolNames.Contains(toolName, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool PromptReferencesUploadedFile(string normalizedPrompt) =>
        ContainsAny(normalizedPrompt, "attached file", "attachment", "uploaded file", "uploaded document", "source file", "invoice attachment", "this file", "this document", "this pdf", "this spreadsheet", "this workbook", "this csv");

    private static string CombineReasons(string reason, string attachmentReason)
    {
        if (string.IsNullOrWhiteSpace(attachmentReason))
        {
            return reason;
        }

        return string.Equals(reason, "Semantic similarity", StringComparison.OrdinalIgnoreCase)
            ? attachmentReason
            : $"{reason}; {attachmentReason}";
    }

    private static bool ContainsAny(string value, params string[] phrases) => phrases.Any(phrase => ContainsPhrase(value, phrase));

    private static bool ContainsPhrase(string normalizedText, string phrase)
    {
        var normalizedPhrase = Normalize(phrase);
        return normalizedPhrase.Length > 0
            && (normalizedText == normalizedPhrase
                || normalizedText.StartsWith(normalizedPhrase + ' ', StringComparison.Ordinal)
                || normalizedText.EndsWith(' ' + normalizedPhrase, StringComparison.Ordinal)
                || normalizedText.Contains(' ' + normalizedPhrase + ' ', StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        return string.Join(' ', value
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '-', '_', '/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}

public sealed class RetrievalAugmentedToolRouter(
    IOptions<ToolRoutingOptions> options,
    IDomainClassifier domainClassifier,
    IToolRetriever retriever,
    IToolReranker reranker,
    IToolCatalog toolCatalog,
    BusinessToolRoutingPolicy routingPolicy,
    IToolRoutingContextAccessor contextAccessor,
    IToolRoutingDiagnosticsStore diagnosticsStore,
    IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions,
    ILogger<RetrievalAugmentedToolRouter> logger,
    FileStorageService? fileStorage = null) : ILocalToolRouter
{
    private enum UploadedAttachmentKind
    {
        Unknown,
        Csv,
        Excel,
        Document
    }

    private sealed record UploadedFileRoutingContext(string FileName, string MimeType, UploadedAttachmentKind Kind);

    private static readonly Regex ActionVerbRegex = new(
        "\\b(create|add|update|edit|change|delete|remove|get|show|view|list|search|find|open|activate|import|read|query|save|describe|extract|reindex|raise)\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private readonly ToolRoutingOptions _options = options.Value;
    private readonly ToolRoutingDiagnosticsOptions _diagnosticsOptions = diagnosticsOptions.Value;
    private readonly FileStorageService? _fileStorage = fileStorage;

    public async Task<ToolRoutingResult> RouteAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        var originalContext = contextAccessor.Current;
        var createdContext = false;
        var routingContext = originalContext;

        if (routingContext is null)
        {
            routingContext = new ToolRoutingExecutionContext(ToolRoutingCorrelation.CreateRoutingId())
            {
                Prompt = userPrompt
            };
            contextAccessor.Current = routingContext;
            createdContext = true;
        }
        else if (string.IsNullOrWhiteSpace(routingContext.Prompt))
        {
            routingContext.Prompt = userPrompt;
        }

        using var scope = ToolRoutingStructuredLogger.BeginRoutingScope(logger, routingContext);
        using var activity = ToolRoutingStructuredLogger.StartStageActivity("ToolRouting", routingContext);
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            if (_diagnosticsOptions.EnableMetrics)
            {
                ToolRoutingTelemetry.RequestsTotal.Add(1, ToolRoutingTelemetry.CreateCommonTags(routingContext));
            }

            var catalog = await toolCatalog.GetAllAsync(cancellationToken);
            routingContext.CatalogSize = catalog.Count;

            activity?.SetTag("tool.catalog.count", catalog.Count);
            activity?.SetTag("toolrouting.prompt", userPrompt);

            var domains = _options.EnableDomainClassification
                ? await domainClassifier.ClassifyAsync(userPrompt, cancellationToken)
                : DomainClassificationResult.Empty;

            var actionPrompts = ExtractActionPrompts(userPrompt);
            activity?.SetTag("tool.action.count", actionPrompts.Count);

            var aggregatedCandidates = new Dictionary<string, ToolCandidate>(StringComparer.OrdinalIgnoreCase);
            var segmentCandidates = new List<(string Prompt, IReadOnlyList<ToolCandidate> RetrievedCandidates)>(actionPrompts.Count);
            var retrievalStopwatch = Stopwatch.StartNew();
            foreach (var actionPrompt in actionPrompts)
            {
                var retrievedCandidates = await retriever.RetrieveAsync(actionPrompt, domains.Domains, cancellationToken);
                segmentCandidates.Add((actionPrompt, retrievedCandidates));
                MergeCandidates(aggregatedCandidates, retrievedCandidates);
            }
            retrievalStopwatch.Stop();

            var rankedAggregatedCandidates = aggregatedCandidates.Values
                .OrderByDescending(candidate => candidate.Similarity)
                .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var candidates = rankedAggregatedCandidates
                .Take(Math.Max(_options.RetrievalTopK, actionPrompts.Count))
                .ToArray();
            routingContext.RetrievedTools = candidates;
            routingContext.RetrievalLatency = retrievalStopwatch.Elapsed;

            if (_diagnosticsOptions.EnableDetailedLogging)
            {
                ToolRoutingStructuredLogger.LogToolRetrievalResult(logger, routingContext);
            }

            var aggregatedSelected = new Dictionary<string, RerankedTool>(StringComparer.OrdinalIgnoreCase);
            var rerankingStopwatch = Stopwatch.StartNew();
            foreach (var (actionPrompt, retrievedCandidates) in segmentCandidates)
            {
                var rerankerCandidates = EnsureRerankerCandidateFloor(
                    retrievedCandidates,
                    rankedAggregatedCandidates,
                    catalog,
                    _options.RetrievalTopK);

                var rerankedTools = await reranker.RerankAsync(actionPrompt, rerankerCandidates, _options.MaxSelectedTools, cancellationToken);
                MergeSelectedTools(aggregatedSelected, rerankedTools, actionPrompt, actionPrompts.Count);
            }
            rerankingStopwatch.Stop();

            var selected = aggregatedSelected.Values
                .OrderByDescending(tool => tool.Confidence)
                .ThenByDescending(tool => aggregatedCandidates.TryGetValue(tool.Name, out var candidate) ? candidate.Similarity : 0)
                .ThenBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(_options.MaxSelectedTools, actionPrompts.Count))
                .ToArray();

            var availableToolNames = catalog
                .Select(x => x.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var excludedToolNames = routingPolicy.GetExcludedTools(userPrompt, availableToolNames);
            if (excludedToolNames.Count > 0)
            {
                selected = selected
                    .Where(x => !excludedToolNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            var selectedToolNames = selected
                .Select(x => x.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var candidateLookup = candidates
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var catalogLookup = catalog
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var augmented = selected.ToList();
            var requiredToolNames = routingPolicy.GetRequiredAdditionalTools(userPrompt, selectedToolNames, availableToolNames);
            AppendAdditionalTools(augmented, requiredToolNames, candidateLookup, catalogLookup, "Semantic workflow requirement");

            selectedToolNames = augmented
                .Select(x => x.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var attachmentToolNames = GetRequiredAttachmentTools(userPrompt, selectedToolNames, availableToolNames);
            AppendAdditionalTools(augmented, attachmentToolNames, candidateLookup, catalogLookup, "Attachment workflow requirement");

            selected = augmented.ToArray();

            routingContext.SelectedTools = selected;
            routingContext.RerankingLatency = rerankingStopwatch.Elapsed;

            totalStopwatch.Stop();
            routingContext.TotalLatency = totalStopwatch.Elapsed;

            activity?.SetTag("tool.candidate.count", candidates.Length);
            activity?.SetTag("tool.selected.count", selected.Length);
            activity?.SetTag("retrieval.latency.ms", routingContext.RetrievalLatency.TotalMilliseconds);
            activity?.SetTag("reranking.latency.ms", routingContext.RerankingLatency.TotalMilliseconds);

            if (_diagnosticsOptions.EnableDetailedLogging)
            {
                ToolRoutingStructuredLogger.LogReductionMetrics(logger, routingContext);
            }

            diagnosticsStore.Capture(routingContext, _diagnosticsOptions.EnablePromptLogging);
            ToolRoutingTelemetry.UpdateGaugeValues(routingContext);

            return new ToolRoutingResult(userPrompt, domains.Domains, candidates, selected, routingContext.RetrievalLatency, routingContext.RerankingLatency, routingContext.TotalLatency);
        }
        catch
        {
            if (_diagnosticsOptions.EnableMetrics)
            {
                ToolRoutingTelemetry.FailuresTotal.Add(1, ToolRoutingTelemetry.CreateCommonTags(routingContext));
            }

            throw;
        }
        finally
        {
            if (createdContext)
            {
                contextAccessor.Current = originalContext;
            }
        }
    }

    private IReadOnlyList<string> GetRequiredAttachmentTools(
        string userPrompt,
        IReadOnlySet<string> selectedToolNames,
        IReadOnlySet<string> availableToolNames)
    {
        var normalizedPrompt = Normalize(userPrompt);
        var uploadedFileContext = TryGetUploadedFileContext();
        if (uploadedFileContext is null && !PromptReferencesUploadedFile(normalizedPrompt))
        {
            return [];
        }

        var promptMentionsCsv = ContainsAny(normalizedPrompt, "csv", "comma separated");
        var promptMentionsExcel = ContainsAny(normalizedPrompt, "excel", "xlsx", "spreadsheet", "workbook");

        string[] toolNames = uploadedFileContext?.Kind switch
        {
            UploadedAttachmentKind.Csv => ["QueryCsv"],
            UploadedAttachmentKind.Excel => ["DescribeExcel", "ReadExcelRows", "QueryExcel"],
            UploadedAttachmentKind.Document => ["DocumentIngest", "DocumentExtractInvoice"],
            _ when promptMentionsCsv => ["QueryCsv"],
            _ when promptMentionsExcel => ["DescribeExcel", "ReadExcelRows", "QueryExcel"],
            _ => ["DocumentIngest", "DocumentExtractInvoice"]
        };

        return toolNames
            .Where(toolName => availableToolNames.Contains(toolName, StringComparer.OrdinalIgnoreCase)
                && !selectedToolNames.Contains(toolName, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private UploadedFileRoutingContext? TryGetUploadedFileContext()
    {
        if (_fileStorage is null)
        {
            return null;
        }

        var latestFile = _fileStorage.GetLastUploadedFileInfo()
            ?? _fileStorage.GetLastUploadedFileInfoAcrossSessions();
        if (latestFile is null)
        {
            return null;
        }

        return new UploadedFileRoutingContext(
            latestFile.FileName,
            latestFile.MimeType,
            ClassifyAttachment(latestFile.FileName, latestFile.MimeType));
    }

    private static UploadedAttachmentKind ClassifyAttachment(string fileName, string mimeType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            return UploadedAttachmentKind.Csv;
        }

        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("excel", StringComparison.OrdinalIgnoreCase))
        {
            return UploadedAttachmentKind.Excel;
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".doc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("msword", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
            || mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return UploadedAttachmentKind.Document;
        }

        return UploadedAttachmentKind.Unknown;
    }

    private static bool PromptReferencesUploadedFile(string normalizedPrompt) =>
        ContainsAny(normalizedPrompt, "attached file", "attachment", "uploaded file", "uploaded document", "source file", "invoice attachment", "this file", "this document", "this pdf", "this spreadsheet", "this workbook", "this csv");

    private static bool ContainsAny(string value, params string[] phrases) => phrases.Any(phrase => ContainsPhrase(value, phrase));

    private static bool ContainsPhrase(string normalizedText, string phrase)
    {
        var normalizedPhrase = Normalize(phrase);
        return normalizedPhrase.Length > 0
            && (normalizedText == normalizedPhrase
                || normalizedText.StartsWith(normalizedPhrase + ' ', StringComparison.Ordinal)
                || normalizedText.EndsWith(' ' + normalizedPhrase, StringComparison.Ordinal)
                || normalizedText.Contains(' ' + normalizedPhrase + ' ', StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        return string.Join(' ', value
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '-', '_', '/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static void AppendAdditionalTools(
        List<RerankedTool> selectedTools,
        IReadOnlyList<string> additionalToolNames,
        IReadOnlyDictionary<string, ToolCandidate> candidateLookup,
        IReadOnlyDictionary<string, ToolCatalogEntry> catalogLookup,
        string reason)
    {
        foreach (var toolName in additionalToolNames)
        {
            if (selectedTools.Any(existing => string.Equals(existing.Name, toolName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (candidateLookup.TryGetValue(toolName, out var candidate))
            {
                selectedTools.Add(new RerankedTool(candidate.Name, Math.Max(candidate.Similarity, 0.9), reason, candidate.Tool));
                continue;
            }

            if (catalogLookup.TryGetValue(toolName, out var tool))
            {
                selectedTools.Add(new RerankedTool(tool.Name, 0.9, reason, tool));
            }
        }
    }

    private static IReadOnlyList<string> ExtractActionPrompts(string userPrompt)
    {
        var normalizedPrompt = Regex.Replace(userPrompt, "\\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPrompt))
        {
            return [userPrompt];
        }

        var matches = ActionVerbRegex.Matches(normalizedPrompt);
        if (matches.Count < 2)
        {
            return [normalizedPrompt];
        }

        var prompts = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : normalizedPrompt.Length;
            var segment = normalizedPrompt[start..end]
                .Trim()
                .TrimStart(',', ';', ':')
                .TrimEnd(',', ';', ':');
            segment = Regex.Replace(segment, "^(and|then)\\s+", string.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).Trim();
            if (segment.Length < 3)
            {
                continue;
            }

            if (seen.Add(segment))
            {
                prompts.Add(segment);
            }
        }

        return prompts.Count > 0 ? prompts : [normalizedPrompt];
    }

    private static void MergeCandidates(IDictionary<string, ToolCandidate> aggregate, IEnumerable<ToolCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!aggregate.TryGetValue(candidate.Name, out var existingCandidate) || candidate.Similarity > existingCandidate.Similarity)
            {
                aggregate[candidate.Name] = candidate;
            }
        }
    }

    private static void MergeSelectedTools(
        IDictionary<string, RerankedTool> aggregate,
        IEnumerable<RerankedTool> selectedTools,
        string actionPrompt,
        int actionPromptCount)
    {
        foreach (var selectedTool in selectedTools)
        {
            var annotatedTool = actionPromptCount > 1
                ? selectedTool with { Reason = AppendActionPromptReason(selectedTool.Reason, actionPrompt) }
                : selectedTool;

            if (!aggregate.TryGetValue(selectedTool.Name, out var existingTool) || annotatedTool.Confidence > existingTool.Confidence)
            {
                aggregate[selectedTool.Name] = annotatedTool;
            }
        }
    }

    private static IReadOnlyList<ToolCandidate> EnsureRerankerCandidateFloor(
        IReadOnlyList<ToolCandidate> retrievedCandidates,
        IReadOnlyList<ToolCandidate> rankedAggregatedCandidates,
        IReadOnlyList<ToolCatalogEntry> catalog,
        int retrievalTopK)
    {
        if (retrievedCandidates.Count == 0)
        {
            return retrievedCandidates;
        }

        var targetCount = Math.Max(retrievalTopK, 1);
        if (retrievedCandidates.Count >= targetCount)
        {
            return retrievedCandidates
                .Take(targetCount)
                .ToArray();
        }

        var output = new List<ToolCandidate>(targetCount);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in retrievedCandidates)
        {
            if (seen.Add(candidate.Name))
            {
                output.Add(candidate);
            }
        }

        foreach (var candidate in rankedAggregatedCandidates)
        {
            if (output.Count >= targetCount)
            {
                break;
            }

            if (seen.Add(candidate.Name))
            {
                output.Add(candidate);
            }
        }

        if (output.Count < targetCount)
        {
            foreach (var tool in catalog.OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (output.Count >= targetCount)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(tool.Name) || !seen.Add(tool.Name))
                {
                    continue;
                }

                output.Add(new ToolCandidate(tool.Name, 0, tool));
            }
        }

        return output;
    }

    private static string AppendActionPromptReason(string reason, string actionPrompt)
    {
        var trimmedPrompt = actionPrompt.Trim();
        var promptSnippet = trimmedPrompt.Length <= 80
            ? trimmedPrompt
            : trimmedPrompt[..77] + "...";
        return $"{reason} (matched action: {promptSnippet})";
    }
}

public sealed class ReflectionToolSchemaProvider(
    BusinessToolMetadataCatalog catalog,
    ToolDescriptionComposer descriptionComposer,
    IToolRoutingContextAccessor contextAccessor,
    IToolRoutingDiagnosticsStore diagnosticsStore,
    IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions,
    ILogger<ReflectionToolSchemaProvider> logger) : IToolSchemaProvider
{
    private readonly ToolRoutingDiagnosticsOptions _diagnosticsOptions = diagnosticsOptions.Value;

    public Task<IReadOnlyList<ToolSchemaDescriptor>> LoadSchemasAsync(IReadOnlyList<string> selectedTools, CancellationToken cancellationToken = default)
    {
        var routingContext = contextAccessor.Current;
        using var activity = ToolRoutingStructuredLogger.StartStageActivity("SchemaLoading", routingContext);
        var stopwatch = Stopwatch.StartNew();
        var output = new List<ToolSchemaDescriptor>(selectedTools.Count);

        foreach (var toolName in selectedTools)
        {
            if (!catalog.TryGetByToolName(toolName, out var descriptor) || descriptor is null)
            {
                continue;
            }

            var schema = BuildInputSchema(descriptor);
            var description = descriptionComposer.Compose(descriptor);
            output.Add(new ToolSchemaDescriptor(descriptor.ToolName, description, schema, descriptor.ToolType.FullName ?? descriptor.ToolType.Name));
        }

        stopwatch.Stop();

        if (routingContext is not null)
        {
            routingContext.SchemaCount = output.Count;
            routingContext.SchemaLoadingLatency = stopwatch.Elapsed;
            activity?.SetTag("tool.selected.count", routingContext.SelectedCount);
            activity?.SetTag("schema.count", output.Count);
            diagnosticsStore.Capture(routingContext, _diagnosticsOptions.EnablePromptLogging);

            if (_diagnosticsOptions.EnableDetailedLogging)
            {
                ToolRoutingStructuredLogger.LogSchemaLoading(logger, routingContext);
            }
        }

        return Task.FromResult<IReadOnlyList<ToolSchemaDescriptor>>(output);
    }

    private static JsonElement BuildInputSchema(BusinessToolDescriptor descriptor)
    {
        var properties = descriptor.Method
            .GetParameters()
            .Where(p => p.ParameterType != typeof(CancellationToken))
            .ToDictionary(
                p => p.Name ?? "arg",
                p => new Dictionary<string, object?>
                {
                    ["type"] = MapJsonType(p.ParameterType),
                    ["description"] = p.Name
                });

        var schemaObject = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = descriptor.Method
                .GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken) && !p.HasDefaultValue && Nullable.GetUnderlyingType(p.ParameterType) is null && !IsNullableReferenceType(p))
                .Select(p => p.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray()
        };

        var json = JsonSerializer.SerializeToElement(schemaObject);
        return json;
    }

    private static bool IsNullableReferenceType(System.Reflection.ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsClass)
        {
            return false;
        }

        foreach (var attribute in parameter.CustomAttributes)
        {
            if (attribute.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static string MapJsonType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string) || type.IsEnum) return "string";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string)) return "array";
        return "object";
    }
}

public sealed class CloudAgentToolContextBuilder(
    ILocalToolRouter toolRouter,
    IToolSchemaProvider schemaProvider,
    IToolRoutingContextAccessor contextAccessor,
    IToolRoutingDiagnosticsStore diagnosticsStore,
    IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions,
    ILogger<CloudAgentToolContextBuilder> logger) : ICloudAgentToolContextBuilder
{
    private readonly ToolRoutingDiagnosticsOptions _diagnosticsOptions = diagnosticsOptions.Value;

    public async Task<CloudAgentToolContext> BuildAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        var originalContext = contextAccessor.Current;
        var createdContext = false;
        var routingContext = originalContext;

        if (routingContext is null)
        {
            routingContext = new ToolRoutingExecutionContext(ToolRoutingCorrelation.CreateRoutingId())
            {
                Prompt = userPrompt
            };
            contextAccessor.Current = routingContext;
            createdContext = true;
        }
        else if (string.IsNullOrWhiteSpace(routingContext.Prompt))
        {
            routingContext.Prompt = userPrompt;
        }

        try
        {
            var routing = await toolRouter.RouteAsync(userPrompt, cancellationToken);
            var selectedNames = routing.SelectedToolNames;
            var stopwatch = Stopwatch.StartNew();
            var schemas = await schemaProvider.LoadSchemasAsync(selectedNames, cancellationToken);
            stopwatch.Stop();

            using var activity = ToolRoutingStructuredLogger.StartStageActivity("CloudAgentInvocation", routingContext);
            routingContext.ToolsSentToCloud = schemas.Select(x => x.Name).ToArray();
            routingContext.CloudAgentLatency = stopwatch.Elapsed;
            activity?.SetTag("tool.selected.count", schemas.Count);
            activity?.SetTag("cloud_agent.latency.ms", stopwatch.Elapsed.TotalMilliseconds);
            diagnosticsStore.Capture(routingContext, _diagnosticsOptions.EnablePromptLogging);

            if (_diagnosticsOptions.EnableDetailedLogging)
            {
                ToolRoutingStructuredLogger.LogCloudAgentInvocation(logger, routingContext);
            }

            if (_diagnosticsOptions.EnableMetrics)
            {
                ToolRoutingTelemetry.CloudAgentDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, ToolRoutingTelemetry.CreateCommonTags(routingContext));
            }

            return new CloudAgentToolContext(userPrompt, schemas);
        }
        finally
        {
            if (createdContext)
            {
                contextAccessor.Current = originalContext;
            }
        }
    }
}