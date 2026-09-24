using JiwaMcpServer.Agent.SchemaResolver;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Agent.ToolInvoker;
using System.Reflection;

namespace JiwaMcpServer.Agent;

/// <summary>
/// Extension methods for wiring the deterministic domain-router tool registry into the DI container.
/// </summary>
public static class AgentServiceExtensions
{
    /// <summary>
    /// Registers the domain-router supporting services.
    /// Call after <c>WithToolsFromAssembly()</c> so that the ToolRegistry can discover
    /// all registered tool classes.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">App configuration (not used for AI model selection).</param>
    /// <param name="pluginAssemblies">
    /// Any additional assemblies loaded at runtime that contain <c>[McpServerToolType]</c> classes.
    /// </param>
    public static IServiceCollection AddJiwaAgentPipeline(
        this IServiceCollection services,
        IConfiguration configuration,
        IEnumerable<Assembly>? pluginAssemblies = null)
    {
        var assemblies = new List<Assembly> { typeof(AgentServiceExtensions).Assembly };
        if (pluginAssemblies != null)
            assemblies.AddRange(pluginAssemblies);

        services.AddSingleton(sp => new ToolRegistry(
            assemblies,
            sp.GetRequiredService<ILogger<ToolRegistry>>()));

        RegisterToolSelectionOverrides(services, assemblies);

        services.AddSingleton<IToolCatalog>(sp => JiwaToolCatalogFactory.Create(sp.GetRequiredService<ToolRegistry>()));
        services.AddSingleton<IToolSchemaResolver, ReflectionToolSchemaResolver>();
        services.AddSingleton<IToolInvoker, ReflectionToolInvoker>();
        services.AddSingleton<IIntentExtractor, IntentExtractor>();
        services.AddSingleton<IToolRoutingContextService, ToolRoutingContextService>();
        services.AddSingleton<IDomainRouter, DomainRouter>();
        services.AddSingleton<IToolShortlistService, ToolShortlistService>();
        services.AddSingleton<IToolRouter, ToolRouter>();

        return services;
    }

    private static void RegisterToolSelectionOverrides(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var overrideType in assembly
                .GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IToolSelectionOverride).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase))
            {
                services.AddSingleton(typeof(IToolSelectionOverride), overrideType);
            }
        }
    }
}
