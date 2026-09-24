using JiwaMcpServer.Agent.LlmClient;

namespace JiwaMcpServer.Agent.SchemaResolver;

/// <summary>
/// Resolves full JSON schemas for selected MCP tools.
/// Results are cached so each schema is generated at most once per process lifetime.
/// </summary>
public interface IToolSchemaResolver
{
    /// <summary>Total number of tools registered across all assemblies.</summary>
    int TotalToolCount { get; }

    /// <summary>All registered tool names discovered by the MCP registry.</summary>
    IReadOnlyList<string> RegisteredToolNames { get; }

    /// <summary>
    /// Resolves schemas for the named tools only.
    /// Unknown tool names are logged and skipped.
    /// </summary>
    Task<IReadOnlyDictionary<string, LlmToolDefinition>> ResolveAsync(
        IEnumerable<string> toolNames,
        CancellationToken ct = default);

    /// <summary>Resolves schemas for every registered tool (used for baseline comparison / logging).</summary>
    Task<IReadOnlyDictionary<string, LlmToolDefinition>> ResolveAllAsync(CancellationToken ct = default);
}
