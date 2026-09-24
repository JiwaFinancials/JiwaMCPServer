using JiwaMcpServer.Models;

namespace JiwaMcpServer.Services;

public interface IFileStorageService
{
    Task<StoredFileReference> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct);
    Task<Stream> OpenReadAsync(string fileId, CancellationToken ct);
    Task DeleteAsync(string fileId, CancellationToken ct);
    Task<bool> ExistsAsync(string fileId, CancellationToken ct);
    Task<FileMetadata> GetMetadataAsync(string fileId, CancellationToken ct);
}
