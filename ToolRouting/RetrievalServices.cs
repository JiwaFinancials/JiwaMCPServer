using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public sealed class LocalHashEmbeddingService(IOptions<ToolRoutingOptions> options) : IEmbeddingService
{
    private readonly int _dimensions = options.Value.EmbeddingDimensions;

    public Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = new float[_dimensions];

        foreach (var token in Tokenize(text))
        {
            var index = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(token)) % _dimensions;
            vector[index] += 1f;
        }

        Normalize(vector);
        return Task.FromResult<IReadOnlyList<float>>(vector);
    }

    private static IEnumerable<string> Tokenize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var token in value
                     .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ':', ';', '-', '_', '/', '\\', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalized = token.Trim().ToLowerInvariant();
            if (normalized.Length > 1)
            {
                yield return normalized;
            }
        }
    }

    private static void Normalize(float[] vector)
    {
        double norm = 0;
        for (var i = 0; i < vector.Length; i++)
        {
            norm += vector[i] * vector[i];
        }

        if (norm <= 0)
        {
            return;
        }

        var scale = 1d / Math.Sqrt(norm);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] * scale);
        }
    }
}

public sealed class InMemoryToolVectorStore : IVectorStore
{
    private sealed record VectorItem(ToolCatalogEntry Tool, IReadOnlyList<float> Embedding);

    private readonly Dictionary<string, VectorItem> _vectors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();

    public Task UpsertAsync(IEnumerable<(ToolCatalogEntry Tool, IReadOnlyList<float> Embedding)> vectors, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            foreach (var vector in vectors)
            {
                _vectors[vector.Tool.Name] = new VectorItem(vector.Tool, vector.Embedding);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ToolCandidate>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        IReadOnlySet<string>? allowedDomains,
        int topK,
        double similarityThreshold,
        CancellationToken cancellationToken = default)
    {
        List<ToolCandidate> ranked;

        lock (_gate)
        {
            ranked = _vectors.Values
                .Where(x => allowedDomains is null || allowedDomains.Count == 0 || (x.Tool.Domain is not null && allowedDomains.Contains(x.Tool.Domain)))
                .Select(x =>
                {
                    var similarity = CosineSimilarity(queryEmbedding, x.Embedding);
                    return new ToolCandidate(x.Tool.Name, similarity, x.Tool);
                })
                .Where(x => x.Similarity >= similarityThreshold)
                .OrderByDescending(x => x.Similarity)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(topK, 1))
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<ToolCandidate>>(ranked);
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var count = Math.Min(left.Count, right.Count);
        if (count == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < count; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        if (leftNorm <= 0 || rightNorm <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}

public sealed class ToolCatalogIndexer(
    IToolCatalog toolCatalog,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILogger<ToolCatalogIndexer> logger)
{
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexed;

    public async Task EnsureIndexedAsync(CancellationToken cancellationToken = default)
    {
        if (_indexed)
        {
            return;
        }

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexed)
            {
                return;
            }

            var tools = await toolCatalog.GetAllAsync(cancellationToken);
            var vectors = new List<(ToolCatalogEntry Tool, IReadOnlyList<float> Embedding)>(tools.Count);

            foreach (var tool in tools)
            {
                var searchable = BuildSearchText(tool);
                var embedding = await embeddingService.CreateEmbeddingAsync(searchable, cancellationToken);
                vectors.Add((tool, embedding));
            }

            await vectorStore.UpsertAsync(vectors, cancellationToken);
            _indexed = true;
            logger.LogInformation("Indexed {ToolCount} tools for semantic retrieval.", tools.Count);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static string BuildSearchText(ToolCatalogEntry tool)
    {
        return string.Join(' ',
        [
            tool.Name,
            tool.Description,
            tool.Domain ?? string.Empty,
            tool.Category ?? string.Empty,
            tool.BusinessCapability ?? string.Empty,
            string.Join(' ', tool.Keywords),
            string.Join(' ', tool.Tags)
        ]);
    }
}

public sealed class SemanticToolRetriever(
    ToolCatalogIndexer indexer,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IOptions<ToolRoutingOptions> options,
    IToolRoutingContextAccessor contextAccessor,
    IOptions<ToolRoutingDiagnosticsOptions> diagnosticsOptions) : IToolRetriever
{
    private readonly ToolRoutingOptions _options = options.Value;
    private readonly ToolRoutingDiagnosticsOptions _diagnosticsOptions = diagnosticsOptions.Value;

    public async Task<IReadOnlyList<ToolCandidate>> RetrieveAsync(
        string userPrompt,
        IReadOnlyList<DomainScore> domains,
        CancellationToken cancellationToken = default)
    {
        var routingContext = contextAccessor.Current;
        using var activity = ToolRoutingStructuredLogger.StartStageActivity("ToolRetrieval", routingContext);
        var stopwatch = Stopwatch.StartNew();

        await indexer.EnsureIndexedAsync(cancellationToken);
        var embedding = await embeddingService.CreateEmbeddingAsync(userPrompt, cancellationToken);
        var domainSet = domains.Count == 0
            ? null
            : domains.Where(x => x.Confidence >= 0.5).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = await vectorStore.SearchAsync(
            embedding,
            domainSet,
            _options.RetrievalTopK,
            _options.SimilarityThreshold,
            cancellationToken);

        stopwatch.Stop();

        if (routingContext is not null)
        {
            routingContext.RetrievedTools = candidates;
            routingContext.RetrievalLatency = stopwatch.Elapsed;
            activity?.SetTag("tool.candidate.count", candidates.Count);
            activity?.SetTag("retrieval.latency.ms", stopwatch.Elapsed.TotalMilliseconds);

            if (_diagnosticsOptions.EnableMetrics)
            {
                ToolRoutingTelemetry.ToolRetrievalDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, ToolRoutingTelemetry.CreateCommonTags(routingContext));
            }
        }

        return candidates;
    }
}