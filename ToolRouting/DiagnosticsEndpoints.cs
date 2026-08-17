using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public static class ToolRoutingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapToolRoutingDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/tool-routing", new HealthCheckOptions
        {
            Predicate = check => string.Equals(check.Name, "tool-routing", StringComparison.OrdinalIgnoreCase),
            ResponseWriter = WriteHealthResponseAsync
        });

        endpoints.MapGet("/diagnostics/tool-routing/latest", (
            HttpContext httpContext,
            IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions,
            IToolRoutingDiagnosticsStore diagnosticsStore) =>
        {
            var options = diagnosticsOptions.Value;
            if (!options.EnableDiagnosticEndpoint)
            {
                return Results.NotFound();
            }

            if (!IsAuthorized(httpContext, options))
            {
                return Results.Unauthorized();
            }

            var latest = diagnosticsStore.GetLatest();
            return latest is null ? Results.NotFound() : Results.Ok(latest);
        });

        return endpoints;
    }

    private static bool IsAuthorized(HttpContext httpContext, ToolRoutingDiagnosticsOptions options)
    {
        if (options.RestrictDiagnosticEndpointToLocalhost)
        {
            var remoteIp = httpContext.Connection.RemoteIpAddress;
            if (remoteIp is not null && !IPAddress.IsLoopback(remoteIp))
            {
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(options.DiagnosticApiKey))
        {
            return true;
        }

        var providedKey = httpContext.Request.Headers[options.DiagnosticApiKeyHeaderName].FirstOrDefault();
        return string.Equals(providedKey, options.DiagnosticApiKey, StringComparison.Ordinal);
    }

    private static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var entry = report.Entries.TryGetValue("tool-routing", out var toolRoutingEntry)
            ? toolRoutingEntry
            : default;

        var payload = new
        {
            status = report.Status.ToString(),
            routingEnabled = TryReadHealthData(entry, "routingEnabled"),
            embeddingService = TryReadHealthData(entry, "embeddingService"),
            vectorStore = TryReadHealthData(entry, "vectorStore")
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private static object? TryReadHealthData(HealthReportEntry entry, string key)
    {
        return entry.Data.TryGetValue(key, out var value)
            ? value
            : null;
    }
}
