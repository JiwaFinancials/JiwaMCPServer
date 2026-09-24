using JiwaMcpServer.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace JiwaMcpServer.Resources;

[McpServerResourceType]
public sealed class DocumentFileResources(IFileStorageService storageService, ILogger<DocumentFileResources> logger)
{
    private readonly IFileStorageService _storageService = storageService;
    private readonly ILogger<DocumentFileResources> _logger = logger;

    [McpServerResource(UriTemplate = "jiwa-file://files/{fileId}", Name = "DocumentFile", Title = "Uploaded or generated document")]
    public async Task<ResourceContents> GetDocumentFile(string fileId, CancellationToken ct)
    {
        var metadata = await _storageService.GetMetadataAsync(fileId, ct);
        await using var stream = await _storageService.OpenReadAsync(fileId, ct);

        if (DocumentFormatMappings.IsTextFormat(metadata.FileType))
        {
            using var reader = new StreamReader(stream, leaveOpen: false);
            var text = await reader.ReadToEndAsync(ct);
            _logger.LogDebug("Serving text resource {FileId}", fileId);
            return new TextResourceContents
            {
                Uri = metadata.ResourceUri,
                MimeType = metadata.ContentType,
                Text = text
            };
        }

        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        _logger.LogDebug("Serving binary resource {FileId}", fileId);
        return BlobResourceContents.FromBytes(buffer.ToArray(), metadata.ResourceUri, metadata.ContentType);
    }
}
