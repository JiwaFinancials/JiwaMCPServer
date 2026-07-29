using System.Collections.Concurrent;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class InMemoryVectorStore : IVectorStore
{
    private sealed record StoredVector(string TenantId, EmbeddedChunk EmbeddedChunk);

    private readonly ConcurrentDictionary<string, StoredVector> _vectors = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertEmbeddingsAsync(string tenantId, string documentId, IReadOnlyList<EmbeddedChunk> chunks, CancellationToken cancellationToken)
    {
        foreach (var chunk in chunks)
        {
            var key = BuildKey(tenantId, documentId, chunk.Chunk.ChunkId);
            _vectors[key] = new StoredVector(tenantId, chunk);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchResultChunk>> SearchAsync(string tenantId, IReadOnlyList<float> queryEmbedding, IReadOnlyList<string>? documentIds, int topK, CancellationToken cancellationToken)
    {
        var docFilter = documentIds is { Count: > 0 }
            ? new HashSet<string>(documentIds, StringComparer.OrdinalIgnoreCase)
            : null;

        var candidates = _vectors.Values
            .Where(v => string.Equals(v.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
            .Where(v => docFilter is null || docFilter.Contains(v.EmbeddedChunk.Chunk.DocumentId))
            .Select(v =>
            {
                var score = CosineSimilarity(queryEmbedding, v.EmbeddedChunk.Embedding);
                return new SearchResultChunk(
                    v.EmbeddedChunk.Chunk.DocumentId,
                    v.EmbeddedChunk.Chunk.ChunkId,
                    score,
                    v.EmbeddedChunk.Chunk.StartPage,
                    v.EmbeddedChunk.Chunk.EndPage,
                    v.EmbeddedChunk.Chunk.Text,
                    new Dictionary<string, string>(v.EmbeddedChunk.Chunk.Metadata));
            })
            .OrderByDescending(x => x.Score)
            .Take(Math.Max(topK, 1))
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResultChunk>>(candidates);
    }

    public Task DeleteDocumentAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        var prefix = $"{tenantId}:{documentId}:";
        var keys = _vectors.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray();

        foreach (var key in keys)
        {
            _vectors.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private static string BuildKey(string tenantId, string documentId, string chunkId) => $"{tenantId}:{documentId}:{chunkId}";

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var max = Math.Min(left.Count, right.Count);
        if (max == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < max; i++)
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
