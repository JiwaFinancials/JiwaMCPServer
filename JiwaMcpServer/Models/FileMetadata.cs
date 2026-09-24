namespace JiwaMcpServer.Models;

public sealed class FileMetadata
{
    public required string FileId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required string FileType { get; init; }
    public required string ResourceUri { get; init; }
    public required long Length { get; init; }
    public required DateTimeOffset CreatedUtc { get; init; }
}
