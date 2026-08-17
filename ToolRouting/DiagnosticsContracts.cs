namespace JiwaMcpServer.ToolRouting;

public sealed class ToolRoutingExecutionContext
{
    public ToolRoutingExecutionContext(string routingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingId);
        RoutingId = routingId;
    }

    public string RoutingId { get; }

    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

    public string? Prompt { get; set; }

    public int CatalogSize { get; set; }

    public IReadOnlyList<DomainScore> ClassifiedDomains { get; set; } = [];

    public IReadOnlyList<ToolCandidate> RetrievedTools { get; set; } = [];

    public IReadOnlyList<RerankedTool> SelectedTools { get; set; } = [];

    public IReadOnlyList<string> ToolsSentToCloud { get; set; } = [];

    public int SchemaCount { get; set; }

    public TimeSpan DomainClassificationLatency { get; set; }

    public TimeSpan RetrievalLatency { get; set; }

    public TimeSpan RerankingLatency { get; set; }

    public TimeSpan SchemaLoadingLatency { get; set; }

    public TimeSpan CloudAgentLatency { get; set; }

    public TimeSpan TotalLatency { get; set; }

    public int CandidateCount => RetrievedTools.Count;

    public int SelectedCount => SelectedTools.Count;

    public double ReductionPercent => CatalogSize <= 0
        ? 0
        : Math.Round((1d - (double)SelectedCount / CatalogSize) * 100d, 2, MidpointRounding.AwayFromZero);
}

public interface IToolRoutingContextAccessor
{
    ToolRoutingExecutionContext? Current { get; set; }
}

public sealed class ToolRoutingContextAccessor : IToolRoutingContextAccessor
{
    private static readonly AsyncLocal<ToolRoutingExecutionContext?> CurrentContext = new();

    public ToolRoutingExecutionContext? Current
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }
}

public sealed record ToolRetrievalDiagnosticItem(string Name, double Similarity);

public sealed record ToolSelectionDiagnosticItem(string Name, double Confidence, string Reason);

public sealed record ToolRoutingLatestDiagnostics(
    string RoutingId,
    string? Prompt,
    int CatalogSize,
    IReadOnlyList<DomainScore> Domains,
    IReadOnlyList<ToolRetrievalDiagnosticItem> RetrievedTools,
    IReadOnlyList<ToolSelectionDiagnosticItem> SelectedTools,
    IReadOnlyList<string> ToolsSentToCloud,
    int SchemaCount,
    double DomainClassificationLatencyMs,
    double RetrievalLatencyMs,
    double RerankingLatencyMs,
    double SchemaLoadingLatencyMs,
    double CloudAgentLatencyMs,
    double TotalLatencyMs,
    double ReductionPercent,
    DateTimeOffset ObservedAtUtc);

public interface IToolRoutingDiagnosticsStore
{
    ToolRoutingLatestDiagnostics? GetLatest();

    void Capture(ToolRoutingExecutionContext context, bool includePrompt);
}

public sealed class InMemoryToolRoutingDiagnosticsStore : IToolRoutingDiagnosticsStore
{
    private readonly Lock _gate = new();
    private ToolRoutingLatestDiagnostics? _latest;

    public ToolRoutingLatestDiagnostics? GetLatest()
    {
        lock (_gate)
        {
            return _latest;
        }
    }

    public void Capture(ToolRoutingExecutionContext context, bool includePrompt)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = new ToolRoutingLatestDiagnostics(
            context.RoutingId,
            includePrompt ? context.Prompt : null,
            context.CatalogSize,
            context.ClassifiedDomains.ToArray(),
            context.RetrievedTools.Select(x => new ToolRetrievalDiagnosticItem(x.Name, x.Similarity)).ToArray(),
            context.SelectedTools.Select(x => new ToolSelectionDiagnosticItem(x.Name, x.Confidence, x.Reason)).ToArray(),
            context.ToolsSentToCloud.ToArray(),
            context.SchemaCount,
            context.DomainClassificationLatency.TotalMilliseconds,
            context.RetrievalLatency.TotalMilliseconds,
            context.RerankingLatency.TotalMilliseconds,
            context.SchemaLoadingLatency.TotalMilliseconds,
            context.CloudAgentLatency.TotalMilliseconds,
            context.TotalLatency.TotalMilliseconds,
            context.ReductionPercent,
            DateTimeOffset.UtcNow);

        lock (_gate)
        {
            _latest = snapshot;
        }
    }
}

public static class ToolRoutingCorrelation
{
    public const string ResponseHeaderName = "X-Tool-Routing-Id";

    public static string CreateRoutingId() => Guid.NewGuid().ToString("n");
}
