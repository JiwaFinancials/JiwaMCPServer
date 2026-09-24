using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Catalog;

/// <summary>
/// Registry of business domains and the tools that belong to each one.
/// Only lightweight metadata is stored here — no JSON schemas.
/// </summary>
public interface IToolCatalog
{
    /// <summary>All business domains registered in the catalog.</summary>
    IReadOnlyList<DomainDefinition> GetDomains();

    /// <summary>All tool definitions across every domain.</summary>
    IReadOnlyList<ToolDefinition> GetAllTools();

    /// <summary>Returns tools belonging to the specified domains.</summary>
    IReadOnlyList<ToolDefinition> GetToolsForDomains(IEnumerable<string> domainNames);
}
