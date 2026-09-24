using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

namespace JiwaMcpServer.Agent.SchemaResolver;

/// <summary>
/// Metadata for a single discovered MCP tool method.
/// </summary>
public sealed class ToolEntry
{
    public required string Name { get; init; }
    public required Type ClassType { get; init; }
    public required MethodInfo Method { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Discovers all <c>[McpServerTool]</c>-decorated methods at startup via reflection
/// and makes them available for schema generation and invocation.
/// Register as a singleton.
/// </summary>
public sealed class ToolRegistry
{
    private readonly IReadOnlyDictionary<string, ToolEntry> _tools;

    /// <summary>All discovered tools keyed by (case-insensitive) tool name.</summary>
    public IReadOnlyDictionary<string, ToolEntry> Tools => _tools;

    /// <summary>Total number of registered tools.</summary>
    public int Count => _tools.Count;

    public ToolRegistry(IEnumerable<Assembly> assemblies, ILogger<ToolRegistry>? logger = null)
    {
        var tools = new Dictionary<string, ToolEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<McpServerToolTypeAttribute>() is null)
                    continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                    if (toolAttr is null)
                        continue;

                    // Name defaults to the method name when not explicitly set on the attribute
                    var toolName = string.IsNullOrWhiteSpace(toolAttr.Name) ? method.Name : toolAttr.Name;
                    var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

                    tools[toolName] = new ToolEntry
                    {
                        Name = toolName,
                        ClassType = type,
                        Method = method,
                        Description = description
                    };
                }
            }
        }

        _tools = tools;

        logger?.LogInformation(
            "Initial Registered Tools ({ToolCount}): {Tools}",
            _tools.Count,
            string.Join(", ", _tools.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)));
    }

    public bool TryGetTool(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ToolEntry? entry)
        => _tools.TryGetValue(name, out entry);
}
