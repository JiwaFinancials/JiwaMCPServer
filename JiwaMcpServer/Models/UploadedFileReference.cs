namespace JiwaMcpServer.Models;

public sealed class UploadedFileReference
{
    public required string FileId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
}
