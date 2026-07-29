using System.Collections.Concurrent;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private sealed record StoredDocument(DocumentRecord Document, byte[] Content);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, StoredDocument>> _documentsByTenant = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyList<DocumentPage>>> _pagesByTenant = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyList<DocumentChunk>>> _chunksByTenant = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertDocumentAsync(DocumentRecord document, byte[] content, CancellationToken cancellationToken)
    {
        var tenantDocs = _documentsByTenant.GetOrAdd(document.TenantId, _ => new ConcurrentDictionary<string, StoredDocument>(StringComparer.OrdinalIgnoreCase));
        tenantDocs[document.DocumentId] = new StoredDocument(document, content);
        return Task.CompletedTask;
    }

    public Task<DocumentRecord?> GetDocumentAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        if (_documentsByTenant.TryGetValue(tenantId, out var tenantDocs) && tenantDocs.TryGetValue(documentId, out var stored))
        {
            return Task.FromResult<DocumentRecord?>(stored.Document);
        }

        return Task.FromResult<DocumentRecord?>(null);
    }

    public Task<byte[]?> GetDocumentContentAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        if (_documentsByTenant.TryGetValue(tenantId, out var tenantDocs) && tenantDocs.TryGetValue(documentId, out var stored))
        {
            return Task.FromResult<byte[]?>(stored.Content);
        }

        return Task.FromResult<byte[]?>(null);
    }

    public Task<DocumentRecord?> FindByHashAsync(string tenantId, string fileHash, CancellationToken cancellationToken)
    {
        if (!_documentsByTenant.TryGetValue(tenantId, out var tenantDocs))
        {
            return Task.FromResult<DocumentRecord?>(null);
        }

        var existing = tenantDocs.Values.Select(x => x.Document).FirstOrDefault(d => string.Equals(d.FileHash, fileHash, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(existing);
    }

    public Task UpsertPagesAsync(string tenantId, string documentId, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken)
    {
        var tenantPages = _pagesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, IReadOnlyList<DocumentPage>>(StringComparer.OrdinalIgnoreCase));
        tenantPages[documentId] = pages;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentPage>> GetPagesAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        if (_pagesByTenant.TryGetValue(tenantId, out var tenantPages) && tenantPages.TryGetValue(documentId, out var pages))
        {
            return Task.FromResult(pages);
        }

        return Task.FromResult<IReadOnlyList<DocumentPage>>(Array.Empty<DocumentPage>());
    }

    public Task UpsertChunksAsync(string tenantId, string documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        var tenantChunks = _chunksByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, IReadOnlyList<DocumentChunk>>(StringComparer.OrdinalIgnoreCase));
        tenantChunks[documentId] = chunks;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        if (_chunksByTenant.TryGetValue(tenantId, out var tenantChunks) && tenantChunks.TryGetValue(documentId, out var chunks))
        {
            return Task.FromResult(chunks);
        }

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(Array.Empty<DocumentChunk>());
    }

    public async Task UpdateStatusAsync(string tenantId, string documentId, DocumentProcessingStatus status, string? error, CancellationToken cancellationToken)
    {
        var doc = await GetDocumentAsync(tenantId, documentId, cancellationToken);
        if (doc is null)
        {
            return;
        }

        var updated = doc with
        {
            ProcessingStatus = status,
            ProcessingError = error,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var content = await GetDocumentContentAsync(tenantId, documentId, cancellationToken) ?? Array.Empty<byte>();
        await UpsertDocumentAsync(updated, content, cancellationToken);
    }

    public Task DeleteDocumentAsync(string tenantId, string documentId, CancellationToken cancellationToken)
    {
        if (_documentsByTenant.TryGetValue(tenantId, out var tenantDocs))
        {
            tenantDocs.TryRemove(documentId, out _);
        }

        if (_pagesByTenant.TryGetValue(tenantId, out var tenantPages))
        {
            tenantPages.TryRemove(documentId, out _);
        }

        if (_chunksByTenant.TryGetValue(tenantId, out var tenantChunks))
        {
            tenantChunks.TryRemove(documentId, out _);
        }

        return Task.CompletedTask;
    }
}
