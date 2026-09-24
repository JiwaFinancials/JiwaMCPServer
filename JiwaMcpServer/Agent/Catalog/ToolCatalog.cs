using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Catalog;

/// <summary>
/// Immutable in-memory tool catalog built from <see cref="DomainDefinition"/> and
/// <see cref="ToolDefinition"/> lists supplied at construction time.
/// </summary>
public sealed class ToolCatalog : IToolCatalog
{
    private static readonly IReadOnlyDictionary<string, string> DomainAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Inventory"] = "InventoryTools",
        ["Documents"] = "DocumentTools"
    };

    private readonly IReadOnlyList<DomainDefinition> _domains;
    private readonly IReadOnlyList<ToolDefinition> _tools;

    public ToolCatalog(IReadOnlyList<DomainDefinition> domains, IReadOnlyList<ToolDefinition> tools)
    {
        _domains = domains;
        _tools = tools;
    }

    public IReadOnlyList<DomainDefinition> GetDomains() => _domains;

    public IReadOnlyList<ToolDefinition> GetAllTools() => _tools;

    public IReadOnlyList<ToolDefinition> GetToolsForDomains(IEnumerable<string> domainNames)
    {
        var resolvedDomains = domainNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(ResolveDomainAlias)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (resolvedDomains.Count == 0)
            return [];

        return _tools.Where(tool => resolvedDomains.Contains(ResolveDomainAlias(tool.Domain))).ToList();
    }

    private static string ResolveDomainAlias(string domain)
        => DomainAliases.TryGetValue(domain, out var canonical) ? canonical : domain;
}
