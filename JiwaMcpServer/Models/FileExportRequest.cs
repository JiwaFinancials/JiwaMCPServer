using System.Text.Json;

namespace JiwaMcpServer.Models;

public sealed class FileExportRequest
{
    public JsonElement? Data { get; init; }
    public string? ClientSessionId { get; init; }
    public string? SourceToolName { get; init; }
    public bool UseLatestStructuredToolResult { get; init; }
    public string[]? Fields { get; init; }
    public required string Format { get; init; }
    public required string FileName { get; init; }
}
