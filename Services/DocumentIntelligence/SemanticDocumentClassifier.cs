using Microsoft.Extensions.Logging;

namespace JiwaMcpServer.Services.DocumentIntelligence;

/// <summary>
/// Classifies document types using deterministic document heuristics.
/// </summary>
public sealed class SemanticDocumentClassifier(ILogger<SemanticDocumentClassifier> logger)
{
    private static readonly (DocumentType Type, string[] Keywords)[] ClassificationRules =
    [
        (DocumentType.Invoice, ["tax invoice", "invoice number", "amount due", "remit", "gst", "vendor"]),
        (DocumentType.Statement, ["statement", "opening balance", "closing balance", "transactions"]),
        (DocumentType.Contract, ["agreement", "contract", "terms and conditions", "party a", "party b"]),
        (DocumentType.Manual, ["manual", "guide", "instructions", "step 1", "troubleshooting"]),
        (DocumentType.Report, ["report", "executive summary", "analysis", "findings", "recommendation"])
    ];

    public Task<DocumentType> ClassifyAsync(string fileName, string documentText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentText);

        var sample = TruncateForAnalysis(documentText, 4000).ToLowerInvariant();
        var bestMatch = ClassificationRules
            .Select(rule => new
            {
                rule.Type,
                Score = rule.Keywords.Count(keyword => sample.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(rule => rule.Score)
            .FirstOrDefault();

        var detectedType = bestMatch is { Score: > 0 }
            ? bestMatch.Type
            : DocumentType.Unknown;

        logger.LogInformation("Document '{FileName}' classified as {DocumentType}", fileName, detectedType);
        return Task.FromResult(detectedType);
    }

    private static string TruncateForAnalysis(string text, int maxChars)
    {
        if (text.Length <= maxChars)
        {
            return text;
        }

        var truncated = text[..maxChars];
        var lastDot = truncated.LastIndexOf('.');
        var lastNewline = truncated.LastIndexOf('\n');
        var cutPoint = Math.Max(lastDot, lastNewline);

        return cutPoint > maxChars * 0.7
            ? truncated[..cutPoint]
            : truncated;
    }
}
