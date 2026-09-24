namespace JiwaMcpServer.Models;

public sealed class ExportFileResult
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Content { get; init; }
    public long ContentLength => Content.LongLength;
}
