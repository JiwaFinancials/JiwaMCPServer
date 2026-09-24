using JiwaMcpServer.Models;
using JiwaMcpServer.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JiwaMcpServer.Services;

public sealed class FileStorageService(IOptions<DocumentProcessingOptions> options, ILogger<FileStorageService> logger) : IFileStorageService
{
    public const string MetadataSuffix = ".metadata.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly DocumentProcessingOptions _options = options.Value;
    private readonly ILogger<FileStorageService> _logger = logger;
    private readonly string _storageRoot = ResolveStorageRoot(options.Value);

    public async Task<StoredFileReference> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("A file name is required.");
        }

        Directory.CreateDirectory(_storageRoot);

        var format = DocumentFormatMappings.DetectFormat(fileName, contentType);
        var normalizedContentType = DocumentFormatMappings.NormalizeMimeType(fileName, contentType);
        var extension = DocumentFormatMappings.GetExtension(format);
        var fileId = Guid.NewGuid().ToString("N");
        var filePath = Path.Combine(_storageRoot, fileId + extension);
        var metadataPath = GetMetadataPath(fileId);
        var createdUtc = DateTimeOffset.UtcNow;
        long bytesWritten = 0;

        try
        {
            await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 64, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = GC.AllocateUninitializedArray<byte>(1024 * 64);
                while (true)
                {
                    var bytesRead = await content.ReadAsync(buffer, ct);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    bytesWritten += bytesRead;
                    if (bytesWritten > _options.MaxUploadSizeBytes)
                    {
                        throw new InvalidOperationException($"File '{fileName}' exceeds the maximum upload size of {_options.MaxUploadSizeMB} MB.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                }
            }

            var storedFile = new StoredFileReference
            {
                FileId = fileId,
                FileName = Path.GetFileName(fileName),
                ContentType = normalizedContentType,
                FileType = format,
                ResourceUri = CreateResourceUri(fileId),
                PhysicalPath = filePath,
                Length = bytesWritten,
                CreatedUtc = createdUtc
            };

            var metadata = new FileMetadata
            {
                FileId = storedFile.FileId,
                FileName = storedFile.FileName,
                ContentType = storedFile.ContentType,
                FileType = storedFile.FileType,
                ResourceUri = storedFile.ResourceUri,
                Length = storedFile.Length,
                CreatedUtc = storedFile.CreatedUtc
            };

            await WriteMetadataAsync(metadataPath, metadata, ct);
            _logger.LogInformation("Stored file {FileId} ({FileType}, {Length} bytes)", fileId, format, bytesWritten);
            return storedFile;
        }
        catch
        {
            TryDelete(filePath);
            TryDelete(metadataPath);
            throw;
        }
    }

    public async Task<Stream> OpenReadAsync(string fileId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var filePath = await ResolveDataFilePathAsync(fileId, ct);
        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public async Task DeleteAsync(string fileId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var metadataPath = GetMetadataPath(fileId);
        var filePath = await TryResolveDataFilePathAsync(fileId, ct);

        if (filePath is not null)
        {
            TryDelete(filePath);
        }

        TryDelete(metadataPath);
        _logger.LogInformation("Deleted temporary file {FileId}", fileId);
    }

    public async Task<bool> ExistsAsync(string fileId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await TryResolveDataFilePathAsync(fileId, ct) is not null && File.Exists(GetMetadataPath(fileId));
    }

    public async Task<FileMetadata> GetMetadataAsync(string fileId, CancellationToken ct)
    {
        var metadataPath = GetMetadataPath(fileId);
        if (!File.Exists(metadataPath))
        {
            throw new FileNotFoundException($"No metadata exists for file '{fileId}'.", fileId);
        }

        await using var stream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var metadata = await JsonSerializer.DeserializeAsync<FileMetadata>(stream, JsonOptions, ct);
        return metadata ?? throw new InvalidOperationException($"Metadata for file '{fileId}' is invalid.");
    }

    internal string StorageRoot => _storageRoot;

    internal static string CreateResourceUri(string fileId) => $"jiwa-file://files/{SanitizeFileId(fileId)}";

    internal static string ResolveStorageRoot(DocumentProcessingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.TempFolder))
        {
            throw new InvalidOperationException("DocumentProcessing:TempFolder must be configured.");
        }

        return Path.IsPathRooted(options.TempFolder)
            ? options.TempFolder
            : Path.Combine(AppContext.BaseDirectory, options.TempFolder);
    }

    private async Task<string> ResolveDataFilePathAsync(string fileId, CancellationToken ct)
        => await TryResolveDataFilePathAsync(fileId, ct) ?? throw new FileNotFoundException($"No file exists for '{fileId}'.", fileId);

    private Task<string?> TryResolveDataFilePathAsync(string fileId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sanitizedFileId = SanitizeFileId(fileId);
        if (!Directory.Exists(_storageRoot))
        {
            return Task.FromResult<string?>(null);
        }

        var filePath = Directory.EnumerateFiles(_storageRoot, sanitizedFileId + ".*")
            .FirstOrDefault(path => !path.EndsWith(MetadataSuffix, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(filePath);
    }

    private string GetMetadataPath(string fileId)
        => Path.Combine(_storageRoot, SanitizeFileId(fileId) + MetadataSuffix);

    private async Task WriteMetadataAsync(string metadataPath, FileMetadata metadata, CancellationToken ct)
    {
        await using var metadataStream = new FileStream(metadataPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await JsonSerializer.SerializeAsync(metadataStream, metadata, JsonOptions, ct);
    }

    private static string SanitizeFileId(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId) || fileId.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new InvalidOperationException("FileId is invalid.");
        }

        return fileId;
    }

    private static void TryDelete(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
