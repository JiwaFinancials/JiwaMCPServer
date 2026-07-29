using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DocumentIntelligenceOptions>(configuration.GetSection(DocumentIntelligenceOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DocumentIntelligenceOptions>>().Value);

        services.AddMemoryCache();
        services.AddHttpClient();

        services.TryAddSingleton<IMalwareScanner, PassThroughMalwareScanner>();
        services.TryAddSingleton<IDocumentAuditLogger, LoggerAuditLogger>();

        services.TryAddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.TryAddSingleton<IVectorStore, InMemoryVectorStore>();

        services.TryAddSingleton<DeterministicEmbeddingService>();
        services.TryAddSingleton<IEmbeddingService, OpenAiEmbeddingService>();

        services.TryAddSingleton<IDocumentExtractor, PdfDocumentExtractor>();
        services.TryAddSingleton<IDocumentExtractor, DocxDocumentExtractor>();
        services.TryAddSingleton<IDocumentExtractor, SpreadsheetDocumentExtractor>();
        services.TryAddSingleton<IDocumentExtractor, PlainTextDocumentExtractor>();
        services.TryAddSingleton<IDocumentExtractor, ImageDocumentExtractor>();
        services.TryAddSingleton<IDocumentExtractor, CompositeDocumentExtractor>();

        services.TryAddSingleton<IOcrService, AzureDocumentIntelligenceOcrService>();
        services.TryAddSingleton<IOcrService, TesseractOcrService>();
        services.TryAddSingleton<IOcrService, CompositeOcrService>();

        services.TryAddSingleton<SemanticChunker>();
        services.TryAddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.TryAddSingleton<IDocumentPipeline, DocumentPipeline>();

        services.AddHostedService<DocumentProcessingWorker>();
        return services;
    }
}
