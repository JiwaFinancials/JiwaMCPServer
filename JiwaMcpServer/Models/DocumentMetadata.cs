using System.Text.Json.Nodes;

namespace JiwaMcpServer.Models;

public sealed class DocumentMetadata
{
    public int? PageCount { get; init; }
    public int? WorksheetCount { get; init; }
    public int? RowCount { get; init; }
    public int? ColumnCount { get; init; }
    public int? ParagraphCount { get; init; }
    public int? TableCount { get; init; }
    public JsonObject AdditionalProperties { get; init; } = [];
}
