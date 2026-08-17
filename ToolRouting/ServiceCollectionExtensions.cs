using System.Reflection;
using JiwaMcpServer.ToolMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRetrievalAugmentedToolRouting(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var toolAssemblies = assemblies
            .Where(a => a is not null)
            .Distinct()
            .ToArray();

        services.Configure<ToolRoutingOptions>(configuration.GetSection(ToolRoutingOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ToolRoutingOptions>>().Value);

        services.Configure<ToolRoutingDiagnosticsOptions>(configuration.GetSection(ToolRoutingDiagnosticsOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ToolRoutingDiagnosticsOptions>>().Value);

        services.TryAddSingleton<IValidateOptions<ToolRoutingOptions>, ToolRoutingOptionsValidator>();
        services.TryAddSingleton<IValidateOptions<ToolRoutingDiagnosticsOptions>, ToolRoutingDiagnosticsOptionsValidator>();

        services.AddOptions<ToolRoutingOptions>().ValidateOnStart();
        services.AddOptions<ToolRoutingDiagnosticsOptions>().ValidateOnStart();

        services.TryAddSingleton<IToolRoutingContextAccessor, ToolRoutingContextAccessor>();
        services.TryAddSingleton<IToolRoutingDiagnosticsStore, InMemoryToolRoutingDiagnosticsStore>();

        services.TryAddSingleton<IDomainCatalog, StaticDomainCatalog>();
        services.TryAddSingleton<IDomainClassifier, HeuristicDomainClassifier>();

        services.TryAddSingleton<BusinessEntityRegistry>();
        services.TryAddSingleton<BusinessTerminologyRegistry>();
        services.TryAddSingleton<BusinessToolMetadataBuilder>();
        services.TryAddSingleton<ToolDescriptionComposer>();
        services.TryAddSingleton<BusinessToolRoutingPolicy>();
        services.TryAddSingleton<ToolMetadataExtractor>();
        services.TryAddSingleton<BusinessToolMetadataCatalog>(sp =>
        {
            var extractor = sp.GetRequiredService<ToolMetadataExtractor>();
            var descriptors = extractor.ExtractFromAssemblies(toolAssemblies);
            return new BusinessToolMetadataCatalog(descriptors);
        });

        services.TryAddSingleton<IToolMetadataProvider, BusinessToolMetadataProvider>();
        services.TryAddSingleton<IToolCatalog, InMemoryToolCatalog>();

        services.TryAddSingleton<IEmbeddingService, LocalHashEmbeddingService>();
        services.TryAddSingleton<IVectorStore, InMemoryToolVectorStore>();
        services.TryAddSingleton<ToolCatalogIndexer>();
        services.TryAddSingleton<IToolRetriever, SemanticToolRetriever>();

        services.TryAddSingleton<IToolReranker, DeterministicToolReranker>();
        services.TryAddSingleton<ILocalToolRouter, RetrievalAugmentedToolRouter>();

        services.TryAddSingleton<IToolSchemaProvider, ReflectionToolSchemaProvider>();
        services.TryAddSingleton<ICloudAgentToolContextBuilder, CloudAgentToolContextBuilder>();

        services.AddHealthChecks()
            .AddCheck<ToolRoutingHealthCheck>("tool-routing");

        return services;
    }
}
