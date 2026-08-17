using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace JiwaMcpServer.ToolRouting;

public static class McpServerToolRoutingExtensions
{
    public static IMcpServerBuilder WithRetrievalAugmentedToolRouting(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (request, cancellationToken) =>
            {
                var services = request.Services!;
                var options = services.GetRequiredService<IOptions<ToolRoutingOptions>>().Value;
                if (!options.EnableRouting)
                {
                    return await next(request, cancellationToken);
                }

                var diagnosticsOptions = services.GetRequiredService<IOptions<ToolRoutingDiagnosticsOptions>>().Value;
                var contextAccessor = services.GetRequiredService<IToolRoutingContextAccessor>();
                var diagnosticsStore = services.GetRequiredService<IToolRoutingDiagnosticsStore>();
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ToolRouting.McpServer");
                var httpContextAccessor = services.GetService<IHttpContextAccessor>();
                var prompt = httpContextAccessor?.HttpContext?.Request.Headers["X-User-Prompt"].FirstOrDefault()
                    ?? httpContextAccessor?.HttpContext?.Request.Headers["X-Tool-Query"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return await next(request, cancellationToken);
                }

                var routingContext = contextAccessor.Current;
                var createdContext = false;
                if (routingContext is null)
                {
                    routingContext = new ToolRoutingExecutionContext(ToolRoutingCorrelation.CreateRoutingId())
                    {
                        Prompt = prompt
                    };
                    contextAccessor.Current = routingContext;
                    createdContext = true;
                }
                else if (string.IsNullOrWhiteSpace(routingContext.Prompt))
                {
                    routingContext.Prompt = prompt;
                }

                using var scope = ToolRoutingStructuredLogger.BeginRoutingScope(logger, routingContext);

                try
                {
                    if (diagnosticsOptions.EnableDetailedLogging)
                    {
                        ToolRoutingStructuredLogger.LogRequestReceived(logger, routingContext, diagnosticsOptions);
                    }

                    using var schemaActivity = ToolRoutingStructuredLogger.StartStageActivity("SchemaLoading", routingContext);
                    var schemaStopwatch = Stopwatch.StartNew();
                    var result = await next(request, cancellationToken);
                    schemaStopwatch.Stop();

                    routingContext.SchemaCount = result.Tools.Count;
                    routingContext.SchemaLoadingLatency = schemaStopwatch.Elapsed;
                    schemaActivity?.SetTag("schema.count", result.Tools.Count);
                    schemaActivity?.SetTag("schema.latency.ms", schemaStopwatch.Elapsed.TotalMilliseconds);

                    if (diagnosticsOptions.EnableDetailedLogging)
                    {
                        ToolRoutingStructuredLogger.LogSchemaLoading(logger, routingContext);
                    }

                    var router = services.GetRequiredService<ILocalToolRouter>();
                    var cloudStopwatch = Stopwatch.StartNew();
                    var routingResult = await router.RouteAsync(prompt, cancellationToken);
                    var selected = routingResult.SelectedToolNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var originalTools = result.Tools;
                    var filteredTools = originalTools
                        .Where(tool => selected.Contains(tool.Name))
                        .ToArray();

                    if (filteredTools.Length == 0 && originalTools.Count > 0)
                    {
                        logger.LogWarning(
                            "Tool routing selected no matching tools. Preserving original tool list to avoid empty tool context. RoutingId={RoutingId} SelectedCount={SelectedCount} OriginalCount={OriginalCount}",
                            routingContext.RoutingId,
                            selected.Count,
                            originalTools.Count);
                        result.Tools = originalTools;
                    }
                    else
                    {
                        result.Tools = filteredTools;
                    }

                    var selectedToolNames = result.Tools
                        .Select(tool => tool.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var availableToolNames = originalTools
                        .Select(tool => tool.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var policy = services.GetRequiredService<BusinessToolRoutingPolicy>();
                    var requiredToolNames = policy.GetRequiredAdditionalTools(prompt, selectedToolNames, availableToolNames);
                    if (requiredToolNames.Count > 0)
                    {
                        var lookup = originalTools
                            .Where(tool => !string.IsNullOrWhiteSpace(tool.Name))
                            .ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);

                        var augmentedTools = result.Tools.ToList();
                        foreach (var toolName in requiredToolNames)
                        {
                            if (lookup.TryGetValue(toolName, out var tool) &&
                                !augmentedTools.Any(existing => string.Equals(existing.Name, toolName, StringComparison.OrdinalIgnoreCase)))
                            {
                                augmentedTools.Add(tool);
                            }
                        }

                        result.Tools = augmentedTools;
                    }

                    var excludedToolNames = policy.GetExcludedTools(prompt, availableToolNames);
                    if (excludedToolNames.Count > 0)
                    {
                        result.Tools = result.Tools
                            .Where(tool => !excludedToolNames.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    cloudStopwatch.Stop();

                    using var activity = ToolRoutingStructuredLogger.StartStageActivity("CloudAgentInvocation", routingContext);
                    routingContext.ToolsSentToCloud = result.Tools.Select(x => x.Name).ToArray();
                    routingContext.CloudAgentLatency = cloudStopwatch.Elapsed;
                    activity?.SetTag("tool.selected.count", result.Tools.Count);
                    activity?.SetTag("cloud_agent.latency.ms", cloudStopwatch.Elapsed.TotalMilliseconds);

                    diagnosticsStore.Capture(routingContext, diagnosticsOptions.EnablePromptLogging);
                    ToolRoutingTelemetry.UpdateGaugeValues(routingContext);

                    if (diagnosticsOptions.EnableDetailedLogging)
                    {
                        ToolRoutingStructuredLogger.LogCloudAgentInvocation(logger, routingContext);
                    }

                    if (diagnosticsOptions.EnableMetrics)
                    {
                        ToolRoutingTelemetry.CloudAgentDurationMs.Record(cloudStopwatch.Elapsed.TotalMilliseconds, ToolRoutingTelemetry.CreateCommonTags(routingContext));
                    }

                    return result;
                }
                finally
                {
                    if (createdContext)
                    {
                        contextAccessor.Current = null;
                    }
                }
            });
        });

        return builder;
    }
}