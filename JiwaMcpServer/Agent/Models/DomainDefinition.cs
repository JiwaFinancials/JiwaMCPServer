namespace JiwaMcpServer.Agent.Models;

/// <summary>
/// Lightweight metadata describing a business domain exposed by the tool catalog.
/// Only this summary is sent to the planner — no full tool schemas.
/// </summary>
public sealed class DomainDefinition
{
    /// <summary>Short, unique domain name (e.g. "InventoryTools", "CustomerTools").</summary>
    public required string Name { get; init; }

    /// <summary>One-line description included in the planner prompt.</summary>
    public required string Description { get; init; }

    /// <summary>High-level capability bullets shown to the planner.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}
