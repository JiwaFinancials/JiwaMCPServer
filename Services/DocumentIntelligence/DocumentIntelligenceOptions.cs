using JiwaMcpServer.Services.DocumentIntelligence;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    public long MaxDocumentSizeBytes { get; set; } = 80L * 1024 * 1024;

    public int MaxPages { get; set; } = 1200;

    public bool EnableBackgroundProcessing { get; set; } = true;

    public int ChunkTargetTokens { get; set; } = 1200;

    public int ChunkMinTokens { get; set; } = 1000;

    public int ChunkMaxTokens { get; set; } = 1500;

    public int ChunkOverlapTokens { get; set; } = 180;

    public int MaxSearchResults { get; set; } = 10;

    public int EmbeddingDimensions { get; set; } = 1536;

    public int MinimumExtractedCharactersBeforeOcr { get; set; } = 40;

    public string[] AllowedMimeTypes { get; set; } =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "text/csv",
        "application/json",
        "application/xml",
        "image/png",
        "image/jpeg",
        "image/webp"
    ];

    public string[] AllowedFileExtensions { get; set; } =
    [
        ".pdf",
        ".docx",
        ".xlsx",
        ".txt",
        ".csv",
        ".json",
        ".xml",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    ];

    public OpenAiEmbeddingOptions Embeddings { get; set; } = new();

    public AzureDocumentIntelligenceOptions AzureDocumentIntelligence { get; set; } = new();

    public TesseractOptions Tesseract { get; set; } = new();
}

public sealed class OpenAiEmbeddingOptions
{
    public string Provider { get; set; } = "disabled";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "text-embedding-3-small";
}

public sealed class AzureDocumentIntelligenceOptions
{
    public bool Enabled { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = "prebuilt-read";
}

public sealed class TesseractOptions
{
    public bool Enabled { get; set; }

    public string ExecutablePath { get; set; } = "tesseract";

    public string Language { get; set; } = "eng";
}
