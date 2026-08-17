using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public sealed class ToolRoutingHealthCheck(
    IOptions<ToolRoutingOptions> routingOptions,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var routingEnabled = routingOptions.Value.EnableRouting;
        var embeddingAvailable = embeddingService is not null;
        var vectorStoreAvailable = vectorStore is not null;

        var data = new Dictionary<string, object>
        {
            ["routingEnabled"] = routingEnabled,
            ["embeddingService"] = embeddingAvailable,
            ["vectorStore"] = vectorStoreAvailable
        };

        var status = !routingEnabled || embeddingAvailable && vectorStoreAvailable
            ? HealthStatus.Healthy
            : HealthStatus.Unhealthy;

        return Task.FromResult(new HealthCheckResult(status, description: "Tool routing health", data: data));
    }
}

internal static class ToolRoutingStructuredLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static void LogRequestReceived(ILogger logger, ToolRoutingExecutionContext context, ToolRoutingDiagnosticsOptions options)
    {
        logger.LogInformation(
            "REQUEST RECEIVED RoutingId={RoutingId} Prompt={Prompt}",
            context.RoutingId,
            options.EnablePromptLogging ? context.Prompt : null);
    }

    public static void LogDomainClassificationResult(ILogger logger, ToolRoutingExecutionContext context)
    {
        logger.LogInformation(
            "DOMAIN CLASSIFICATION RESULT RoutingId={RoutingId} Domains={Domains} DomainClassificationLatencyMs={DomainClassificationLatencyMs}",
            context.RoutingId,
            Serialize(context.ClassifiedDomains.Select(x => new { name = x.Name, confidence = x.Confidence })),
            context.DomainClassificationLatency.TotalMilliseconds);
    }

    public static void LogToolRetrievalResult(ILogger logger, ToolRoutingExecutionContext context)
    {
        logger.LogInformation(
            "TOOL RETRIEVAL RESULT RoutingId={RoutingId} ToolCatalogCount={ToolCatalogCount} RetrievedCount={RetrievedCount} RetrievedTools={RetrievedTools} RetrievalLatencyMs={RetrievalLatencyMs}",
            context.RoutingId,
            context.CatalogSize,
            context.CandidateCount,
            Serialize(context.RetrievedTools.Select(x => new { name = x.Name, similarity = x.Similarity })),
            context.RetrievalLatency.TotalMilliseconds);
    }

    public static void LogToolSelectionResult(ILogger logger, ToolRoutingExecutionContext context)
    {
        logger.LogInformation(
            "TOOL SELECTION RESULT RoutingId={RoutingId} SelectedCount={SelectedCount} SelectedTools={SelectedTools} RerankingLatencyMs={RerankingLatencyMs}",
            context.RoutingId,
            context.SelectedCount,
            Serialize(context.SelectedTools.Select(x => new { name = x.Name, confidence = x.Confidence, reason = x.Reason })),
            context.RerankingLatency.TotalMilliseconds);
    }

    public static void LogSchemaLoading(ILogger logger, ToolRoutingExecutionContext context)
    {
        logger.LogInformation(
            "SCHEMA LOADING RoutingId={RoutingId} SchemaCount={SchemaCount} SchemaLoadingLatencyMs={SchemaLoadingLatencyMs}",
            context.RoutingId,
            context.SchemaCount,
            context.SchemaLoadingLatency.TotalMilliseconds);
    }

    public static void LogCloudAgentInvocation(ILogger logger, ToolRoutingExecutionContext context)
    {
        logger.LogInformation(
            "CLOUD AGENT INVOCATION RoutingId={RoutingId} ToolCount={ToolCount} ToolsSentToCloud={ToolsSentToCloud} CloudAgentLatencyMs={CloudAgentLatencyMs}",
            context.RoutingId,
            context.ToolsSentToCloud.Count,
            Serialize(context.ToolsSentToCloud),
            context.CloudAgentLatency.TotalMilliseconds);
    }

    public static void LogReductionMetrics(ILogger logger, ToolRoutingExecutionContext context)
    {
        logger.LogInformation(
            "TOOL REDUCTION METRICS RoutingId={RoutingId} CatalogSize={CatalogSize} CandidateCount={CandidateCount} SelectedCount={SelectedCount} ReductionPercent={ReductionPercent}",
            context.RoutingId,
            context.CatalogSize,
            context.CandidateCount,
            context.SelectedCount,
            context.ReductionPercent);
    }

    public static IDisposable BeginRoutingScope(ILogger logger, ToolRoutingExecutionContext context)
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["RoutingId"] = context.RoutingId
        }) ?? NullScope.Instance;
    }

    public static Activity? StartStageActivity(string stageName, ToolRoutingExecutionContext? context)
    {
        var activity = ToolRoutingTelemetry.ActivitySource.StartActivity(stageName, ActivityKind.Internal);
        if (activity is not null && context is not null)
        {
            activity.SetTag("routing.id", context.RoutingId);
        }

        return activity;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
