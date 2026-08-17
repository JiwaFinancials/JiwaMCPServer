using System.Text.Json;

namespace JiwaMcpServer.ToolRouting;

public sealed record DomainScore(string Name, double Confidence);

public sealed record DomainClassificationResult(IReadOnlyList<DomainScore> Domains)
{
    public static DomainClassificationResult Empty { get; } = new([]);
}

public sealed record ToolCatalogEntry(
    string Name,
    string Description,
    string? Domain,
    string? Category,
    string? BusinessCapability,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> Tags);

public sealed record ToolCandidate(string Name, double Similarity, ToolCatalogEntry Tool);

public sealed record RerankedTool(string Name, double Confidence, string Reason, ToolCatalogEntry Tool);

public sealed record ToolSchemaDescriptor(string Name, string Description, JsonElement InputSchema, string SourceType);

public sealed record ToolRoutingResult(
    string UserPrompt,
    IReadOnlyList<DomainScore> ClassifiedDomains,
    IReadOnlyList<ToolCandidate> RetrievedTools,
    IReadOnlyList<RerankedTool> SelectedTools,
    TimeSpan RetrievalLatency,
    TimeSpan RoutingLatency,
    TimeSpan TotalLatency)
{
    public IReadOnlyList<string> SelectedToolNames => SelectedTools.Select(x => x.Name).ToArray();
}

public sealed record CloudAgentToolContext(string UserPrompt, IReadOnlyList<ToolSchemaDescriptor> SelectedToolSchemas);

public interface IDomainCatalog
{
    IReadOnlyList<string> GetDomains();
}

public interface IDomainClassifier
{
    Task<DomainClassificationResult> ClassifyAsync(string userPrompt, CancellationToken cancellationToken = default);
}

public interface IToolMetadataProvider
{
    Task<IReadOnlyList<ToolCatalogEntry>> GetToolMetadataAsync(CancellationToken cancellationToken = default);
}

public interface IToolCatalog
{
    Task<IReadOnlyList<ToolCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ToolCatalogEntry?> GetByNameAsync(string toolName, CancellationToken cancellationToken = default);
}

public interface IEmbeddingService
{
    Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public interface IVectorStore
{
    Task UpsertAsync(IEnumerable<(ToolCatalogEntry Tool, IReadOnlyList<float> Embedding)> vectors, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolCandidate>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        IReadOnlySet<string>? allowedDomains,
        int topK,
        double similarityThreshold,
        CancellationToken cancellationToken = default);
}

public interface IToolRetriever
{
    Task<IReadOnlyList<ToolCandidate>> RetrieveAsync(
        string userPrompt,
        IReadOnlyList<DomainScore> domains,
        CancellationToken cancellationToken = default);
}

public interface IToolReranker
{
    Task<IReadOnlyList<RerankedTool>> RerankAsync(
        string userPrompt,
        IReadOnlyList<ToolCandidate> candidates,
        int maxSelectedTools,
        CancellationToken cancellationToken = default);
}

public interface ILocalToolRouter
{
    Task<ToolRoutingResult> RouteAsync(string userPrompt, CancellationToken cancellationToken = default);
}

public interface IToolSchemaProvider
{
    Task<IReadOnlyList<ToolSchemaDescriptor>> LoadSchemasAsync(IReadOnlyList<string> selectedTools, CancellationToken cancellationToken = default);
}

public interface ICloudAgentToolContextBuilder
{
    Task<CloudAgentToolContext> BuildAsync(string userPrompt, CancellationToken cancellationToken = default);
}
