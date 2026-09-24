using System.Text.Json.Nodes;

namespace JiwaMcpServer.Models;

public sealed class ExtractedDocument
{
    public required string FileName { get; init; }
    public required string FileType { get; init; }
    public required DocumentMetadata Metadata { get; init; }
    public required string ExtractedText { get; init; }
    public JsonNode? StructuredData { get; init; }
}
