using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.ToolMetadata;

public static class BusinessToolMetadataMcpExtensions
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IMcpServerBuilder WithBusinessToolMetadata(this IMcpServerBuilder builder, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var toolAssemblies = assemblies
            .Where(a => a is not null)
            .Distinct()
            .ToArray();

        builder.Services.AddBusinessToolMetadata(toolAssemblies);

        builder.WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (request, cancellationToken) =>
            {
                var result = await next(request, cancellationToken);
                var catalog = request.Services!.GetRequiredService<BusinessToolMetadataCatalog>();
                var descriptionComposer = request.Services!.GetRequiredService<ToolDescriptionComposer>();
                result.ApplyBusinessToolMetadata(catalog, descriptionComposer);
                return result;
            });
        });

        return builder;
    }

    public static ListToolsResult ApplyBusinessToolMetadata(
        this ListToolsResult result,
        BusinessToolMetadataCatalog catalog,
        ToolDescriptionComposer descriptionComposer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(descriptionComposer);

        // Create a new list to avoid concurrent modification of shared collection
        var filteredTools = new List<Tool>(result.Tools.Count);

        foreach (var tool in result.Tools)
        {
            // Skip alias tools
            if (ToolAliasRegistry.IsAlias(tool.Name))
            {
                continue;
            }

            if (!catalog.TryGetByToolName(tool.Name, out var descriptor) || descriptor is null)
            {
                filteredTools.Add(tool);
                continue;
            }

            // Create new metadata to avoid concurrent modification of shared JsonObject
            var meta = new JsonObject
            {
                ["entityType"] = JsonValue.Create(descriptor.Metadata.EntityType),
                ["actionType"] = JsonValue.Create(descriptor.Metadata.ActionType),
                ["aliases"] = new JsonArray(descriptor.Metadata.Aliases.Select(a => JsonValue.Create(a)).ToArray()),
                ["tags"] = new JsonArray(descriptor.Metadata.Tags.Select(t => JsonValue.Create(t)).ToArray()),
                ["intentPhrases"] = new JsonArray(descriptor.Metadata.IntentPhrases.Select(p => JsonValue.Create(p)).ToArray()),
                ["searchText"] = JsonValue.Create(descriptor.Metadata.SearchText)
            };

            // Create a new Tool instance with updated description and metadata
            // to avoid mutating the shared Tool instance across concurrent requests
            var updatedTool = new Tool
            {
                Name = tool.Name,
                Description = descriptionComposer.Compose(descriptor),
                InputSchema = tool.InputSchema,
                Meta = meta
            };

            filteredTools.Add(updatedTool);
        }

        result.Tools = filteredTools;
        return result;
    }
}
