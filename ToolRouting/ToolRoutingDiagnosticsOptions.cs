using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public sealed class ToolRoutingDiagnosticsOptions
{
    public const string SectionName = "ToolRoutingDiagnostics";

    public bool EnableDetailedLogging { get; set; } = true;

    public bool EnablePromptLogging { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public bool EnableDiagnosticEndpoint { get; set; } = true;

    public bool RestrictDiagnosticEndpointToLocalhost { get; set; } = true;

    public string DiagnosticApiKeyHeaderName { get; set; } = "X-Tool-Routing-Diagnostics-Key";

    public string DiagnosticApiKey { get; set; } = string.Empty;
}

public sealed class ToolRoutingDiagnosticsOptionsValidator : IValidateOptions<ToolRoutingDiagnosticsOptions>
{
    public ValidateOptionsResult Validate(string? name, ToolRoutingDiagnosticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.EnableDiagnosticEndpoint)
        {
            return ValidateOptionsResult.Success;
        }

        if (!options.RestrictDiagnosticEndpointToLocalhost && string.IsNullOrWhiteSpace(options.DiagnosticApiKey))
        {
            return ValidateOptionsResult.Fail(
                "ToolRoutingDiagnostics:DiagnosticApiKey is required when the diagnostics endpoint is enabled without localhost restriction.");
        }

        if (string.IsNullOrWhiteSpace(options.DiagnosticApiKeyHeaderName))
        {
            return ValidateOptionsResult.Fail("ToolRoutingDiagnostics:DiagnosticApiKeyHeaderName is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
