using System.Diagnostics;
using System.Text.RegularExpressions;
using JiwaMcpServer.ToolMetadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public sealed class StaticDomainCatalog(IOptions<ToolRoutingOptions> options) : IDomainCatalog
{
    private readonly ToolRoutingOptions _options = options.Value;

    public IReadOnlyList<string> GetDomains() => _options.Domains;
}

public sealed class HeuristicDomainClassifier(
    IOptions<ToolRoutingOptions> options,
    IDomainCatalog domainCatalog,
    IToolRoutingContextAccessor contextAccessor,
    IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions,
    ILogger<HeuristicDomainClassifier> logger) : IDomainClassifier
{
    private static readonly Dictionary<string, string[]> DomainKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Procurement"] = ["purchase", "purchase order", "po", "supplier", "vendor", "procurement", "requisition", "creditor"],
        ["Finance"] = ["invoice", "payment", "gst", "tax", "bill", "accounts payable", "accounts receivable", "debtor", "receipt"],
        ["CRM"] = ["customer", "client", "lead", "contact", "sales", "opportunity"],
        ["Documents"] = ["document", "file", "pdf", "docx", "xlsx", "ocr", "attachment", "upload"],
        ["Reporting"] = ["report", "reporting", "analytics", "dashboard", "summary", "metric"],
        ["HR"] = ["employee", "staff", "payroll", "leave", "timesheet"],
        ["Projects"] = ["project", "task", "milestone", "resource", "job costing"],
        ["Knowledge Base"] = ["knowledge", "knowledge base", "faq", "article", "documentation"],
        ["Email"] = ["email", "mail", "inbox", "message"],
        ["Calendar"] = ["calendar", "meeting", "event", "appointment", "schedule"],
        ["Customer Support"] = ["support", "ticket", "case", "incident"],
        ["ERP"] = ["inventory", "warehouse", "stock", "product", "item", "form", "screen"]
    };

    private readonly ToolRoutingOptions _options = options.Value;
    private readonly ToolRoutingDiagnosticsOptions _diagnosticsOptions = diagnosticsOptions.Value;

    public Task<DomainClassificationResult> ClassifyAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableDomainClassification || string.IsNullOrWhiteSpace(userPrompt))
        {
            return Task.FromResult(DomainClassificationResult.Empty);
        }

        var routingContext = contextAccessor.Current;
        using var activity = ToolRoutingStructuredLogger.StartStageActivity("DomainClassification", routingContext);
        var stopwatch = Stopwatch.StartNew();

        var normalizedPrompt = Normalize(userPrompt);
        var filtered = domainCatalog.GetDomains()
            .Select(domain => ScoreDomain(domain, normalizedPrompt))
            .Where(result => result is not null)
            .Select(result => result!)
            .OrderByDescending(result => result.Confidence)
            .Take(3)
            .ToArray();

        stopwatch.Stop();

        if (routingContext is not null)
        {
            routingContext.ClassifiedDomains = filtered;
            routingContext.DomainClassificationLatency = stopwatch.Elapsed;
            activity?.SetTag("domain.count", filtered.Length);
            activity?.SetTag("domain.latency.ms", stopwatch.Elapsed.TotalMilliseconds);

            if (_diagnosticsOptions.EnableDetailedLogging)
            {
                ToolRoutingStructuredLogger.LogDomainClassificationResult(logger, routingContext);
            }
        }

        return Task.FromResult(new DomainClassificationResult(filtered));
    }

    private static DomainScore? ScoreDomain(string domain, string normalizedPrompt)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var keywords = DomainKeywords.TryGetValue(domain, out var configuredKeywords)
            ? configuredKeywords
            : Tokenize(domain);

        var score = ContainsPhrase(normalizedPrompt, domain) ? 3 : 0;
        foreach (var keyword in keywords)
        {
            if (ContainsPhrase(normalizedPrompt, keyword))
            {
                score++;
            }
        }

        if (score == 0)
        {
            return null;
        }

        var confidence = Math.Min(0.99, 0.45 + score * 0.12);
        return new DomainScore(domain, Math.Round(confidence, 2, MidpointRounding.AwayFromZero));
    }

    private static bool ContainsPhrase(string normalizedText, string phrase)
    {
        var normalizedPhrase = Normalize(phrase);
        if (normalizedPhrase.Length == 0)
        {
            return false;
        }

        if (normalizedPhrase.Contains(' ', StringComparison.Ordinal))
        {
            return normalizedText == normalizedPhrase
                || normalizedText.StartsWith(normalizedPhrase + ' ', StringComparison.Ordinal)
                || normalizedText.EndsWith(' ' + normalizedPhrase, StringComparison.Ordinal)
                || normalizedText.Contains(' ' + normalizedPhrase + ' ', StringComparison.Ordinal);
        }

        return normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => token.Equals(normalizedPhrase, StringComparison.Ordinal)
                || token.StartsWith(normalizedPhrase, StringComparison.Ordinal)
                || normalizedPhrase.StartsWith(token, StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value, "[^a-z0-9]+", " ", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))
            .Trim()
            .ToLowerInvariant();
    }

    private static string[] Tokenize(string value) =>
        Normalize(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class BusinessToolMetadataProvider(
    BusinessToolMetadataCatalog catalog,
    ToolDescriptionComposer descriptionComposer) : IToolMetadataProvider
{
    public Task<IReadOnlyList<ToolCatalogEntry>> GetToolMetadataAsync(CancellationToken cancellationToken = default)
    {
        var entries = catalog.Descriptors
            .Where(descriptor => !ToolAliasRegistry.IsAlias(descriptor.ToolName))
            .Select(descriptor =>
            {
                var category = descriptor.Metadata.EntityType;
                var domain = InferDomain(descriptor.Metadata.EntityType, descriptor.Metadata.Tags);
                var capability = BuildCapability(descriptor.Metadata.ActionType, descriptor.Metadata.EntityType, descriptor.ToolName);

                var keywords = descriptor.Metadata.Aliases
                    .Concat(descriptor.Metadata.Tags)
                    .Concat(descriptor.Metadata.IntentPhrases)
                    .Concat(Tokenize(descriptor.ToolName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var tags = descriptor.Metadata.Tags
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new ToolCatalogEntry(
                    descriptor.ToolName,
                    descriptionComposer.Compose(descriptor),
                    domain,
                    category,
                    capability,
                    keywords,
                    tags);
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<ToolCatalogEntry>>(entries);
    }

    private static string BuildCapability(string? action, string? entity, string fallback)
    {
        var normalizedAction = string.IsNullOrWhiteSpace(action) ? "Use" : action.Trim();
        var normalizedEntity = string.IsNullOrWhiteSpace(entity) ? fallback : entity.Trim();
        return $"{normalizedAction} {normalizedEntity}";
    }

    private static string? InferDomain(string? entityType, IReadOnlyList<string> tags)
    {
        var terms = new[] { entityType ?? string.Empty }.Concat(tags).ToArray();
        if (ContainsAny(terms, "purchase", "supplier", "creditor", "procurement", "vendor")) return "Procurement";
        if (ContainsAny(terms, "invoice", "finance", "payment", "debtor", "creditor purchase")) return "Finance";
        if (ContainsAny(terms, "customer", "crm", "sales", "debtor")) return "CRM";
        if (ContainsAny(terms, "document", "ocr", "file", "pdf", "docx", "xlsx")) return "Documents";
        if (ContainsAny(terms, "report", "analytics", "summary", "search")) return "Reporting";
        return "ERP";
    }

    private static bool ContainsAny(IEnumerable<string> terms, params string[] needles)
    {
        var all = string.Join(' ', terms).ToLowerInvariant();
        return needles.Any(x => all.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> Tokenize(string value)
    {
        var normalized = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2", RegexOptions.None, TimeSpan.FromSeconds(1));
        return normalized
            .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed class InMemoryToolCatalog(IToolMetadataProvider metadataProvider) : IToolCatalog
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IReadOnlyList<ToolCatalogEntry>? _cache;

    public async Task<IReadOnlyList<ToolCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is null)
            {
                _cache = await metadataProvider.GetToolMetadataAsync(cancellationToken);
            }

            return _cache;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<ToolCatalogEntry?> GetByNameAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return null;
        }

        var tools = await GetAllAsync(cancellationToken);
        return tools.FirstOrDefault(x => string.Equals(x.Name, toolName.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
