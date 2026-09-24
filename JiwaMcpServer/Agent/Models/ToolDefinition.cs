namespace JiwaMcpServer.Agent.Models;

/// <summary>
/// Lightweight metadata for a single MCP tool shown to the planner.
/// Keywords help the planner match user intent without seeing the full JSON schema.
/// </summary>
public sealed class ToolDefinition
{
    /// <summary>Exact tool name as registered with the MCP server.</summary>
    public required string Name { get; init; }

    /// <summary>Short description included in the planner prompt.</summary>
    public required string Description { get; init; }

    /// <summary>Parent domain name (must match a <see cref="DomainDefinition.Name"/>).</summary>
    public required string Domain { get; init; }

    /// <summary>Primary action suggested by the tool metadata.</summary>
    public string? Action { get; init; }

    /// <summary>Primary resource or artifact acted on by the tool.</summary>
    public string? Resource { get; init; }

    /// <summary>Short prerequisite summary shown to the planner.</summary>
    public string? Prerequisites { get; init; }

    /// <summary>Compact summary of the likely inputs.</summary>
    public string? InputSummary { get; init; }

    /// <summary>Compact summary of the likely output/result.</summary>
    public string? OutputSummary { get; init; }

    /// <summary>Structured routing metadata used before ranking.</summary>
    public ToolMetadata Metadata { get; init; } = new();

    /// <summary>Keywords that help the planner recognise when this tool is relevant.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];
}
