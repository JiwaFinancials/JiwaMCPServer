namespace JiwaMcpServer.Services.DocumentIntelligence;

public interface IDocumentRepository
{
    Task UpsertDocumentAsync(DocumentRecord document, byte[] content, CancellationToken cancellationToken);

    Task<DocumentRecord?> GetDocumentAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task<byte[]?> GetDocumentContentAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task<DocumentRecord?> FindByHashAsync(string tenantId, string fileHash, CancellationToken cancellationToken);

    Task UpsertPagesAsync(string tenantId, string documentId, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentPage>> GetPagesAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task UpsertChunksAsync(string tenantId, string documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task UpdateStatusAsync(string tenantId, string documentId, DocumentProcessingStatus status, string? error, CancellationToken cancellationToken);

    Task DeleteDocumentAsync(string tenantId, string documentId, CancellationToken cancellationToken);
}

public interface IVectorStore
{
    Task UpsertEmbeddingsAsync(string tenantId, string documentId, IReadOnlyList<EmbeddedChunk> chunks, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultChunk>> SearchAsync(string tenantId, IReadOnlyList<float> queryEmbedding, IReadOnlyList<string>? documentIds, int topK, CancellationToken cancellationToken);

    Task DeleteDocumentAsync(string tenantId, string documentId, CancellationToken cancellationToken);
}

public interface IEmbeddingService
{
    Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken);
}

public interface IDocumentExtractor
{
    bool CanExtract(string fileName, string mimeType);

    Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken);
}

public interface IOcrService
{
    Task<OcrResult> ReadAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken);
}

public interface IDocumentPipeline
{
    Task<DocumentIngestResponse> IngestAsync(DocumentIngestRequest request, CancellationToken cancellationToken);

    Task ProcessDocumentAsync(string tenantId, string documentId, bool forceReindex, CancellationToken cancellationToken);

    Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request, CancellationToken cancellationToken);

    Task<DocumentSummaryResponse?> GetSummaryAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task<DocumentInvoiceResponse?> ExtractInvoiceAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task<DocumentTablesResponse?> ExtractTablesAsync(string tenantId, string documentId, int? pageNumber, CancellationToken cancellationToken);

    Task<DeleteDocumentResponse> DeleteAsync(string tenantId, string documentId, CancellationToken cancellationToken);

    Task<DocumentIngestResponse> ReindexAsync(string tenantId, string documentId, bool forceOcr, CancellationToken cancellationToken);
}

public interface IDocumentProcessingQueue
{
    void Enqueue(DocumentProcessingJob job);

    IAsyncEnumerable<DocumentProcessingJob> DequeueAsync(CancellationToken cancellationToken);
}

public interface IMalwareScanner
{
    Task<MalwareScanResult> ScanAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken);
}

public interface IDocumentAuditLogger
{
    Task WriteAsync(string tenantId, string action, string documentId, string message, CancellationToken cancellationToken);
}

public sealed record ExtractionResult(
    IReadOnlyList<DocumentPage> Pages,
    int ExtractedCharacterCount,
    IReadOnlyList<DocumentTable> Tables,
    Dictionary<string, string> Metadata,
    bool NeedsOcr);

public sealed record OcrPage(int PageNumber, string Text);

public sealed record OcrResult(bool Success, string? Error, IReadOnlyList<OcrPage> Pages);

public sealed record DocumentProcessingJob(string TenantId, string DocumentId, bool ForceReindex);

public sealed record MalwareScanResult(bool IsClean, string? Reason);
