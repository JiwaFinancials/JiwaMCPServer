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
                var catalog = request.Services.GetRequiredService<BusinessToolMetadataCatalog>();
                result.ApplyBusinessToolMetadata(catalog);
                return result;
            });
        });

        return builder;
    }

    public static ListToolsResult ApplyBusinessToolMetadata(this ListToolsResult result, BusinessToolMetadataCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(catalog);

        foreach (var tool in result.Tools)
        {
            if (!catalog.TryGetByToolName(tool.Name, out var descriptor) || descriptor is null)
            {
                continue;
            }

            tool.Meta ??= new JsonObject();
            tool.Meta["entityType"] = descriptor.Metadata.EntityType;
            tool.Meta["actionType"] = descriptor.Metadata.ActionType;
            tool.Meta["aliases"] = JsonSerializer.SerializeToNode(descriptor.Metadata.Aliases, MetadataJsonOptions);
            tool.Meta["tags"] = JsonSerializer.SerializeToNode(descriptor.Metadata.Tags, MetadataJsonOptions);
            tool.Meta["intentPhrases"] = JsonSerializer.SerializeToNode(descriptor.Metadata.IntentPhrases, MetadataJsonOptions);
            tool.Meta["searchText"] = descriptor.Metadata.SearchText;
        }

        return result;
    }
}
