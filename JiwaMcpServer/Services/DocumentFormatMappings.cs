using System.Collections.Frozen;

namespace JiwaMcpServer.Services;

internal static class DocumentFormatMappings
{
    private static readonly FrozenDictionary<string, string> ExtensionToFormat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".csv"] = "csv",
        [".xlsx"] = "xlsx",
        [".json"] = "json",
        [".xml"] = "xml",
        [".pdf"] = "pdf",
        [".docx"] = "docx"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> FormatToExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["csv"] = ".csv",
        ["xlsx"] = ".xlsx",
        ["json"] = ".json",
        ["xml"] = ".xml",
        ["pdf"] = ".pdf",
        ["docx"] = ".docx"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> FormatToMimeType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["csv"] = "text/csv",
        ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ["json"] = "application/json",
        ["xml"] = "application/xml",
        ["pdf"] = "application/pdf",
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string[]> AllowedMimeTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel", "text/plain", "application/octet-stream"],
        ["xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream"],
        ["json"] = ["application/json", "text/json", "text/plain", "application/octet-stream"],
        ["xml"] = ["application/xml", "text/xml", "text/plain", "application/octet-stream"],
        ["pdf"] = ["application/pdf", "application/octet-stream"],
        ["docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/octet-stream"]
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> TextFormats = new[] { "csv", "json", "xml" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> SupportedFormats => FormatToExtension.Keys;

    public static string NormalizeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new InvalidOperationException("A file format is required.");
        }

        var normalized = format.Trim().TrimStart('.');
        if (!FormatToExtension.ContainsKey(normalized))
        {
            throw new NotSupportedException($"Unsupported file format '{format}'. Supported formats: {string.Join(", ", SupportedFormats)}.");
        }

        return normalized;
    }

    public static string DetectFormat(string fileName, string? contentType = null)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            if (ExtensionToFormat.TryGetValue(extension, out var extensionFormat))
            {
                return extensionFormat;
            }

            throw new NotSupportedException($"Unsupported file type for '{fileName}'. Supported formats: {string.Join(", ", SupportedFormats)}.");
        }

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            foreach (var pair in AllowedMimeTypes)
            {
                if (pair.Value.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    return pair.Key;
                }
            }
        }

        throw new NotSupportedException($"Unsupported file type for '{fileName}'. Supported formats: {string.Join(", ", SupportedFormats)}.");
    }

    public static string GetExtension(string format) => FormatToExtension[NormalizeFormat(format)];

    public static string GetMimeType(string format) => FormatToMimeType[NormalizeFormat(format)];

    public static string NormalizeMimeType(string fileName, string? contentType)
    {
        var format = DetectFormat(fileName, contentType);
        var normalizedMimeType = string.IsNullOrWhiteSpace(contentType)
            ? GetMimeType(format)
            : contentType.Trim();

        if (!AllowedMimeTypes[format].Contains(normalizedMimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Content type '{contentType}' does not match the supported '{format}' format.");
        }

        return normalizedMimeType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            ? GetMimeType(format)
            : normalizedMimeType;
    }

    public static bool IsTextFormat(string format) => TextFormats.Contains(NormalizeFormat(format));
}
