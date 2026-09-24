using JiwaMcpServer.Options;
using JiwaMcpServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JiwaMcpServer.BackgroundServices;

public sealed class DocumentCleanupService(IOptions<DocumentProcessingOptions> options, IFileStorageService storageService, ILogger<DocumentCleanupService> logger) : BackgroundService
{
    private sealed class CleanupMetadata
    {
        public string? FileId { get; init; }
        public DateTimeOffset? CreatedUtc { get; init; }
    }

    private readonly DocumentProcessingOptions _options = options.Value;
    private readonly IFileStorageService _storageService = storageService;
    private readonly ILogger<DocumentCleanupService> _logger = logger;
    private readonly string _storageRoot = FileStorageService.ResolveStorageRoot(options.Value);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredFilesAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    public async Task<int> CleanupExpiredFilesAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_storageRoot))
        {
            return 0;
        }

        var deletedCount = 0;
        var cutoff = DateTimeOffset.UtcNow.Subtract(_options.RetentionPeriod);

        foreach (var metadataPath in Directory.EnumerateFiles(_storageRoot, "*" + FileStorageService.MetadataSuffix))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var fileId = TryExtractFileIdFromMetadataPath(metadataPath);
                if (fileId is null)
                {
                    _logger.LogWarning("Skipping cleanup candidate with invalid metadata filename {MetadataPath}", metadataPath);
                    continue;
                }

                var createdUtc = DateTimeOffset.FromFileTime(File.GetLastWriteTimeUtc(metadataPath).ToFileTimeUtc());

                await using (var metadataStream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    var metadata = await JsonSerializer.DeserializeAsync<CleanupMetadata>(metadataStream, cancellationToken: ct);
                    if (metadata?.CreatedUtc is { } metadataCreatedUtc)
                    {
                        createdUtc = metadataCreatedUtc;
                    }

                    if (!string.IsNullOrWhiteSpace(metadata?.FileId) && IsValidFileId(metadata.FileId))
                    {
                        fileId = metadata.FileId;
                    }
                }

                if (createdUtc > cutoff)
                {
                    continue;
                }

                await _storageService.DeleteAsync(fileId, ct);
                deletedCount++;
            }
            catch (JsonException ex)
            {
                var fallbackCreatedUtc = DateTimeOffset.FromFileTime(File.GetLastWriteTimeUtc(metadataPath).ToFileTimeUtc());
                if (fallbackCreatedUtc > cutoff)
                {
                    _logger.LogWarning(ex, "Failed to read cleanup candidate {MetadataPath}", metadataPath);
                    continue;
                }

                var fallbackFileId = TryExtractFileIdFromMetadataPath(metadataPath);
                if (fallbackFileId is not null)
                {
                    await _storageService.DeleteAsync(fallbackFileId, ct);
                }
                else
                {
                    File.Delete(metadataPath);
                }

                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate cleanup candidate {MetadataPath}", metadataPath);
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation("Document cleanup removed {DeletedCount} expired files", deletedCount);
        }

        return deletedCount;
    }

    private static string? TryExtractFileIdFromMetadataPath(string metadataPath)
    {
        var metadataFileName = Path.GetFileName(metadataPath);
        if (!metadataFileName.EndsWith(FileStorageService.MetadataSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fileId = metadataFileName[..^FileStorageService.MetadataSuffix.Length];
        return IsValidFileId(fileId) ? fileId : null;
    }

    private static bool IsValidFileId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetterOrDigit);
}
