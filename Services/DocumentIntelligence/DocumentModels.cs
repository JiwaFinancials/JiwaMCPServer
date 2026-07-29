using System.Text.Json.Serialization;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public enum DocumentProcessingStatus
{
    Queued,
    Extracting,
    RunningOcr,
    Chunking,
    Embedding,
    Indexed,
    Failed,
    Deleted
}

public enum DocumentType
{
    Unknown,
    Invoice,
    Statement,
    Contract,
    Manual,
    Report
}

public sealed record InvoiceData(
    string InvoiceNumber,
    string Vendor,
    string InvoiceDate,
    decimal TotalAmount,
    decimal TaxAmount,
    string Currency);

public sealed record DocumentRecord(
    string DocumentId,
    string TenantId,
    string Title,
    string FileName,
    string MimeType,
    long FileSizeBytes,
    string FileHash,
    int PageCount,
    DocumentType DocumentType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DocumentProcessingStatus ProcessingStatus,
    string? ProcessingError,
    InvoiceData? Invoice,
    Dictionary<string, string> Metadata);

public sealed record DocumentPage(
    string DocumentId,
    int PageNumber,
    string Text,
    bool UsedOcr,
    IReadOnlyList<string> Headings,
    IReadOnlyList<DocumentTable> Tables);

public sealed record DocumentTable(
    int PageNumber,
    string Caption,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record DocumentChunk(
    string ChunkId,
    string DocumentId,
    int StartPage,
    int EndPage,
    string Text,
    IReadOnlyList<DocumentTable> Tables,
    IReadOnlyList<string> Headings,
    Dictionary<string, string> Metadata);

public sealed record EmbeddedChunk(
    DocumentChunk Chunk,
    IReadOnlyList<float> Embedding);

public sealed record SearchResultChunk(
    string DocumentId,
    string ChunkId,
    double Score,
    int StartPage,
    int EndPage,
    string Text,
    Dictionary<string, string> Metadata);

public sealed record DocumentIngestRequest(
    string TenantId,
    string FileName,
    string MimeType,
    string ContentBase64,
    string? Title,
    bool ProcessAsync,
    string? ExternalDocumentId,
    bool ForceReindex,
    string? SourceFileId);

public sealed record DocumentIngestResponse(
    string DocumentId,
    string TenantId,
    DocumentProcessingStatus Status,
    string Message,
    int? PageCount,
    DocumentType? DocumentType,
    InvoiceData? Invoice,
    DateTimeOffset UpdatedAt);

public sealed record DocumentSearchRequest(
    string TenantId,
    string Query,
    IReadOnlyList<string>? DocumentIds,
    int TopK,
    bool IncludeChunkText);

public sealed record DocumentSearchResponse(
    string Query,
    IReadOnlyList<SearchResultChunk> Results,
    string SuggestedAnswer);

public sealed record DocumentSummaryResponse(
    string DocumentId,
    string TenantId,
    string Title,
    DocumentType DocumentType,
    int PageCount,
    DocumentProcessingStatus Status,
    InvoiceData? Invoice,
    IReadOnlyList<string> KeyDates,
    IReadOnlyList<string> KeyEntities,
    string SummaryText,
    Dictionary<string, string> Metadata);

public sealed record DocumentInvoiceResponse(
    string DocumentId,
    string TenantId,
    bool IsInvoice,
    InvoiceData? Invoice,
    IReadOnlyList<string> Evidence);

public sealed record DocumentTablesResponse(
    string DocumentId,
    string TenantId,
    int TableCount,
    IReadOnlyList<DocumentTable> Tables);

public sealed record DeleteDocumentResponse(
    string DocumentId,
    string TenantId,
    bool Deleted,
    string Message);

public sealed record ToolSchemaProperty(string Type, string Description, bool Required = false);

public sealed record ToolSchema(string ToolName, IReadOnlyDictionary<string, ToolSchemaProperty> Request, IReadOnlyDictionary<string, ToolSchemaProperty> Response);

public sealed record DocumentToolSchemaResponse(IReadOnlyList<ToolSchema> Schemas);
