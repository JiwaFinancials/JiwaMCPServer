using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace JiwaMcpServer.ToolMetadata;

public static class BusinessToolMetadataServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessToolMetadata(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var toolAssemblies = assemblies
            .Where(a => a is not null)
            .Distinct()
            .ToArray();

        services.TryAddSingleton<BusinessEntityRegistry>();
        services.TryAddSingleton<BusinessToolMetadataBuilder>();
        services.TryAddSingleton<ToolMetadataExtractor>();
        services.TryAddSingleton<BusinessToolMetadataCatalog>(sp =>
        {
            var extractor = sp.GetRequiredService<ToolMetadataExtractor>();
            var descriptors = extractor.ExtractFromAssemblies(toolAssemblies);
            return new BusinessToolMetadataCatalog(descriptors);
        });

        return services;
    }
}
