using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

namespace JiwaMcpServer.ToolRouting;

public static class ToolRoutingTelemetry
{
    public const string ActivitySourceName = "Mcp.ToolRouting";
    public const string MeterName = "Mcp.ToolRouting";

    private static int _toolCatalogSize;
    private static int _toolCandidateCount;
    private static int _toolSelectedCount;

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>("tool_routing_requests_total");
    public static readonly Counter<long> FailuresTotal = Meter.CreateCounter<long>("tool_routing_failures_total");
    public static readonly Histogram<double> ToolRetrievalDurationMs = Meter.CreateHistogram<double>("tool_retrieval_duration_ms");
    public static readonly Histogram<double> ToolRerankingDurationMs = Meter.CreateHistogram<double>("tool_reranking_duration_ms");
    public static readonly Histogram<double> CloudAgentDurationMs = Meter.CreateHistogram<double>("cloud_agent_duration_ms");
    public static readonly ObservableGauge<int> ToolCatalogSize = Meter.CreateObservableGauge("tool_catalog_size", () => new Measurement<int>(Volatile.Read(ref _toolCatalogSize)));
    public static readonly ObservableGauge<int> ToolCandidateCount = Meter.CreateObservableGauge("tool_candidate_count", () => new Measurement<int>(Volatile.Read(ref _toolCandidateCount)));
    public static readonly ObservableGauge<int> ToolSelectedCount = Meter.CreateObservableGauge("tool_selected_count", () => new Measurement<int>(Volatile.Read(ref _toolSelectedCount)));

    public static TagList CreateCommonTags(ToolRoutingExecutionContext? context)
    {
        var tags = new TagList();

        if (context is not null)
        {
            tags.Add("routing.id", context.RoutingId);
            tags.Add("tool.catalog.count", context.CatalogSize);
            tags.Add("tool.candidate.count", context.CandidateCount);
            tags.Add("tool.selected.count", context.SelectedCount);
            tags.Add("retrieval.latency.ms", context.RetrievalLatency.TotalMilliseconds);
            tags.Add("reranking.latency.ms", context.RerankingLatency.TotalMilliseconds);
        }

        return tags;
    }

    public static void UpdateGaugeValues(ToolRoutingExecutionContext? context)
    {
        if (context is null)
        {
            return;
        }

        Interlocked.Exchange(ref _toolCatalogSize, context.CatalogSize);
        Interlocked.Exchange(ref _toolCandidateCount, context.CandidateCount);
        Interlocked.Exchange(ref _toolSelectedCount, context.SelectedCount);
    }
}
