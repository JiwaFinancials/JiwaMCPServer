namespace JiwaMcpServer.Models;

public sealed class DocumentConversionRequest
{
    public required string FileId { get; init; }
    public required string TargetFormat { get; init; }
}
