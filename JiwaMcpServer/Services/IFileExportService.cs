using JiwaMcpServer.Models;

namespace JiwaMcpServer.Services;

public interface IFileExportService
{
    Task<ExportFileResult> ExportCsvAsync(object? data, string fileName, CancellationToken ct);
    Task<ExportFileResult> ExportXlsxAsync(object? data, string fileName, CancellationToken ct);
    Task<ExportFileResult> ExportJsonAsync(object? data, string fileName, CancellationToken ct);
    Task<ExportFileResult> ExportXmlAsync(object? data, string fileName, CancellationToken ct);
    Task<ExportFileResult> ExportPdfAsync(object? data, string fileName, CancellationToken ct);
    Task<ExportFileResult> ExportDocxAsync(object? data, string fileName, CancellationToken ct);
    Task<ExportFileResult> CreateExportAsync(object? data, string format, string fileName, CancellationToken ct);
}
