using JiwaMcpServer.Models;

namespace JiwaMcpServer.Services;

public interface IDocumentProcessingService
{
    Task<ExtractedDocument> ExtractCsvAsync(Stream content, string fileName, CancellationToken ct);
    Task<ExtractedDocument> ExtractXlsxAsync(Stream content, string fileName, CancellationToken ct);
    Task<ExtractedDocument> ExtractJsonAsync(Stream content, string fileName, CancellationToken ct);
    Task<ExtractedDocument> ExtractXmlAsync(Stream content, string fileName, CancellationToken ct);
    Task<ExtractedDocument> ExtractPdfAsync(Stream content, string fileName, CancellationToken ct);
    Task<ExtractedDocument> ExtractDocxAsync(Stream content, string fileName, CancellationToken ct);
    Task<ExtractedDocument> ExtractDocumentAsync(Stream content, string fileName, string? contentType, CancellationToken ct);
    Task<ExportFileResult> ConvertDocumentAsync(Stream content, string fileName, string? contentType, string targetFormat, CancellationToken ct);
    string DetectFileType(string fileName, string? contentType = null);
}
