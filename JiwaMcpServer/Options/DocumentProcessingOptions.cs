namespace JiwaMcpServer.Options;

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";

    public int MaxUploadSizeMB { get; init; } = 50;
    public string TempFolder { get; init; } = "TempFiles";
    public int RetentionHours { get; init; } = 24;

    public long MaxUploadSizeBytes => Math.Max(1, MaxUploadSizeMB) * 1024L * 1024L;
    public TimeSpan RetentionPeriod => TimeSpan.FromHours(Math.Max(1, RetentionHours));
}
