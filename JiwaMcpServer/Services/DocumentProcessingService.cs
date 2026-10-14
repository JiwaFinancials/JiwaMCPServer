using ClosedXML.Excel;
using CsvHelper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JiwaMcpServer.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace JiwaMcpServer.Services;

public sealed class DocumentProcessingService(IFileExportService fileExportService, ILogger<DocumentProcessingService> logger) : IDocumentProcessingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileExportService _fileExportService = fileExportService;
    private readonly ILogger<DocumentProcessingService> _logger = logger;

    public async Task<ExtractedDocument> ExtractCsvAsync(Stream content, string fileName, CancellationToken ct)
    {
        ResetStream(content);
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync())
        {
            return CreateDocument(fileName, "csv", string.Empty, new JsonArray(), new DocumentMetadata());
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        var rows = new JsonArray();
        var textBuilder = new StringBuilder();
        var rowCount = 0;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            var row = new JsonObject();
            var values = new List<string?>(headers.Length);
            foreach (var header in headers)
            {
                var value = csv.GetField(header);
                row[header] = value;
                values.Add(value);
            }

            rows.Add(row);
            textBuilder.AppendLine(string.Join(", ", headers.Zip(values, static (header, value) => $"{header}: {value}")));
            rowCount++;
        }

        return CreateDocument(
            fileName,
            "csv",
            textBuilder.ToString().Trim(),
            rows,
            new DocumentMetadata
            {
                RowCount = rowCount,
                ColumnCount = headers.Length,
                AdditionalProperties = new JsonObject
                {
                    ["Headers"] = new JsonArray(headers.Select(static header => (JsonNode?)JsonValue.Create(header)).ToArray())
                }
            });
    }

    public async Task<ExtractedDocument> ExtractXlsxAsync(Stream content, string fileName, CancellationToken ct)
    {
        ResetStream(content);
        using var workbook = new XLWorkbook(content);
        var sheets = new JsonArray();
        var textBuilder = new StringBuilder();
        var totalRows = 0;
        var maxColumns = 0;

        foreach (var worksheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();
            var range = worksheet.RangeUsed();
            var sheetRows = new JsonArray();
            var headers = new List<string>();

            if (range is not null)
            {
                var usedRows = range.RowsUsed().ToList();
                if (usedRows.Count > 0)
                {
                    headers = usedRows[0].Cells().Select((cell, index) => string.IsNullOrWhiteSpace(cell.GetString()) ? $"Column{index + 1}" : cell.GetString()).ToList();
                    maxColumns = Math.Max(maxColumns, headers.Count);

                    foreach (var row in usedRows.Skip(1))
                    {
                        var rowObject = new JsonObject();
                        var values = new List<string?>(headers.Count);
                        for (var index = 0; index < headers.Count; index++)
                        {
                            var value = row.Cell(index + 1).GetFormattedString();
                            rowObject[headers[index]] = value;
                            values.Add(value);
                        }

                        sheetRows.Add(rowObject);
                        textBuilder.AppendLine($"[{worksheet.Name}] {string.Join(", ", headers.Zip(values, static (header, value) => $"{header}: {value}"))}");
                        totalRows++;
                    }
                }
            }

            sheets.Add(new JsonObject
            {
                ["Name"] = worksheet.Name,
                ["Headers"] = new JsonArray(headers.Select(static header => (JsonNode?)JsonValue.Create(header)).ToArray()),
                ["Rows"] = sheetRows
            });
        }

        return CreateDocument(
            fileName,
            "xlsx",
            textBuilder.ToString().Trim(),
            new JsonObject { ["Worksheets"] = sheets },
            new DocumentMetadata
            {
                WorksheetCount = workbook.Worksheets.Count,
                RowCount = totalRows,
                ColumnCount = maxColumns
            });
    }

    public async Task<ExtractedDocument> ExtractJsonAsync(Stream content, string fileName, CancellationToken ct)
    {
        ResetStream(content);
        var node = await JsonNode.ParseAsync(content, cancellationToken: ct) ?? new JsonObject();
        return CreateDocument(
            fileName,
            "json",
            node.ToJsonString(JsonOptions),
            node,
            new DocumentMetadata
            {
                AdditionalProperties = new JsonObject
                {
                    ["RootKind"] = node.GetType().Name
                }
            });
    }

    public async Task<ExtractedDocument> ExtractXmlAsync(Stream content, string fileName, CancellationToken ct)
    {
        ResetStream(content);
        var document = await XDocument.LoadAsync(content, System.Xml.Linq.LoadOptions.None, ct);
        var root = document.Root ?? throw new InvalidOperationException("XML document does not contain a root element.");
        return CreateDocument(
            fileName,
            "xml",
            document.ToString(),
            ToJson(root),
            new DocumentMetadata
            {
                AdditionalProperties = new JsonObject
                {
                    ["RootName"] = root.Name.LocalName
                }
            });
    }

    public async Task<ExtractedDocument> ExtractPdfAsync(Stream content, string fileName, CancellationToken ct)
    {
        ResetStream(content);
        using var document = PdfDocument.Open(content);
        var pages = new JsonArray();
        var textBuilder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            var pageText = page.Text.Trim();
            pages.Add(new JsonObject
            {
                ["PageNumber"] = page.Number,
                ["Text"] = pageText
            });

            textBuilder.AppendLine($"[Page {page.Number}]");
            textBuilder.AppendLine(pageText);
        }

        var extractedText = textBuilder.ToString().Trim();
        var pdfMetadata = ExtractPdfMetadata(extractedText);

        return CreateDocument(
            fileName,
            "pdf",
            extractedText,
            new JsonObject
            {
                ["Pages"] = pages,
                ["Metadata"] = pdfMetadata
            },
            new DocumentMetadata
            {
                PageCount = document.NumberOfPages,
                AdditionalProperties = pdfMetadata
            });
    }

    public async Task<ExtractedDocument> ExtractDocxAsync(Stream content, string fileName, CancellationToken ct)
    {
        ResetStream(content);
        using var document = WordprocessingDocument.Open(content, false);
        var body = document.MainDocumentPart?.Document?.Body ?? throw new InvalidOperationException("DOCX document is missing a body.");
        var elements = new JsonArray();
        var paragraphs = new JsonArray();
        var tables = new JsonArray();
        var textBuilder = new StringBuilder();
        var paragraphCount = 0;
        var tableCount = 0;

        foreach (var child in body.ChildElements)
        {
            ct.ThrowIfCancellationRequested();
            if (child is Paragraph paragraph)
            {
                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    paragraphs.Add(text);
                    elements.Add(new JsonObject { ["Type"] = "Paragraph", ["Text"] = text });
                    textBuilder.AppendLine(text);
                    paragraphCount++;
                }

                continue;
            }

            if (child is Table table)
            {
                var rows = new JsonArray();
                foreach (var row in table.Elements<TableRow>())
                {
                    var cells = new JsonArray(row.Elements<TableCell>().Select(cell => JsonValue.Create(cell.InnerText)).ToArray());
                    rows.Add(cells);
                    textBuilder.AppendLine(string.Join(" | ", row.Elements<TableCell>().Select(cell => cell.InnerText)));
                }

                tables.Add(rows.DeepClone());
                elements.Add(new JsonObject { ["Type"] = "Table", ["Rows"] = rows });
                tableCount++;
            }
        }

        return CreateDocument(
            fileName,
            "docx",
            textBuilder.ToString().Trim(),
            new JsonObject
            {
                ["Elements"] = elements,
                ["Paragraphs"] = paragraphs,
                ["Tables"] = tables
            },
            new DocumentMetadata
            {
                ParagraphCount = paragraphCount,
                TableCount = tableCount
            });
    }

    public Task<ExtractedDocument> ExtractDocumentAsync(Stream content, string fileName, string? contentType, CancellationToken ct)
    {
        var format = DetectFileType(fileName, contentType);
        _logger.LogInformation("Starting document extraction for {FileName} as {Format}", fileName, format);
        return format switch
        {
            "csv" => ExtractCsvAsync(content, fileName, ct),
            "xlsx" => ExtractXlsxAsync(content, fileName, ct),
            "json" => ExtractJsonAsync(content, fileName, ct),
            "xml" => ExtractXmlAsync(content, fileName, ct),
            "pdf" => ExtractPdfAsync(content, fileName, ct),
            "docx" => ExtractDocxAsync(content, fileName, ct),
            _ => throw new NotSupportedException($"Unsupported document type '{format}'.")
        };
    }

    public async Task<ExportFileResult> ConvertDocumentAsync(Stream content, string fileName, string? contentType, string targetFormat, CancellationToken ct)
    {
        var extracted = await ExtractDocumentAsync(content, fileName, contentType, ct);
        var exportPayload = extracted.StructuredData ?? new JsonObject
        {
            ["ExtractedText"] = extracted.ExtractedText,
            ["Metadata"] = JsonSerializer.SerializeToNode(extracted.Metadata, JsonOptions)
        };

        var export = await _fileExportService.CreateExportAsync(exportPayload, targetFormat, Path.GetFileNameWithoutExtension(fileName), ct);
        _logger.LogInformation("Completed document conversion from {SourceType} to {TargetType} for {FileName}", extracted.FileType, targetFormat, fileName);
        return export;
    }

    public string DetectFileType(string fileName, string? contentType = null)
        => DocumentFormatMappings.DetectFormat(fileName, contentType);

    private static ExtractedDocument CreateDocument(string fileName, string fileType, string extractedText, JsonNode structuredData, DocumentMetadata metadata)
        => new()
        {
            FileName = Path.GetFileName(fileName),
            FileType = fileType,
            Metadata = metadata,
            ExtractedText = extractedText,
            StructuredData = structuredData
        };

    private static JsonObject ExtractPdfMetadata(string extractedText)
    {
        var metadata = new JsonObject();
        var normalized = extractedText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return metadata;

        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (TryExtractMetadataPair(line, out var key, out var value))
            {
                metadata[key] = value;
                continue;
            }

            var nextLine = index + 1 < lines.Length ? lines[index + 1] : null;
            if (string.IsNullOrWhiteSpace(nextLine) || !LooksLikeMetadataValue(nextLine))
                continue;

            var candidateKey = NormalizeMetadataKey(line);
            if (string.IsNullOrWhiteSpace(candidateKey) || !LooksLikeMetadataLabel(line))
                continue;

            var candidateValue = NormalizeMetadataValue(nextLine);
            if (string.IsNullOrWhiteSpace(candidateValue))
                continue;

            if (string.Equals(candidateKey, "Page", StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidateKey, "Need help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidateKey, "Visit", StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidateKey, "Includes", StringComparison.OrdinalIgnoreCase)
                || candidateKey.StartsWith("[", StringComparison.Ordinal)
                || candidateKey.StartsWith("Page ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            metadata[candidateKey] = candidateValue;
            index++;
        }

        AddCompactTextMetadata(metadata, normalized);

        var title = ExtractDocumentTitle(lines);
        if (!string.IsNullOrWhiteSpace(title))
            metadata["DocumentTitle"] = title;

        if (metadata.Count == 0)
            metadata["DocumentType"] = "document";

        return metadata;
    }

    private static bool TryExtractMetadataPair(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        if (trimmed.Contains(':'))
        {
            var index = trimmed.IndexOf(':');
            key = trimmed[..index].Trim();
            value = trimmed[(index + 1)..].Trim();
        }
        else if (Regex.IsMatch(trimmed, @"^(?<label>[A-Za-z][A-Za-z0-9 /&().-]{1,60})\s+(?<value>\$?\d[\d,]*\.\d{2}|[A-Z0-9][A-Za-z0-9 .,&()/:-]{2,})$", RegexOptions.CultureInvariant))
        {
            var match = Regex.Match(trimmed, @"^(?<label>[A-Za-z][A-Za-z0-9 /&().-]{1,60})\s+(?<value>\$?\d[\d,]*\.\d{2}|[A-Z0-9][A-Za-z0-9 .,&()/:-]{2,})$", RegexOptions.CultureInvariant);
            key = match.Groups["label"].Value.Trim();
            value = match.Groups["value"].Value.Trim();
        }

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return false;

        var normalizedKey = NormalizeMetadataKey(key);
        if (string.IsNullOrWhiteSpace(normalizedKey))
            return false;

        if (string.Equals(normalizedKey, "Page", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedKey, "Need help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedKey, "Visit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedKey, "Includes", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        key = normalizedKey;
        value = NormalizeMetadataValue(value);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string ExtractDocumentTitle(IReadOnlyList<string> lines)
    {
        var candidates = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.Contains(':')
                || trimmed.Contains("Page ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("ABN", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Need help", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Visit ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trimmed.Length <= 120
                && char.IsLetter(trimmed[0])
                && !Regex.IsMatch(trimmed, @"\d"))
            {
                candidates.Add(trimmed);
            }
        }

        return candidates.FirstOrDefault(candidate => !string.Equals(candidate, "Tax Invoice", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static void AddCompactTextMetadata(JsonObject metadata, string normalizedText)
    {
        var compactText = Regex.Replace(normalizedText, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(compactText))
            return;

        var titleMatch = Regex.Match(compactText, @"(?is)(?<title>[A-Z][A-Za-z0-9&.'()/-]+(?:\s+[A-Z][A-Za-z0-9&.'()/-]+)*)\s*ABN");
        if (titleMatch.Success)
            metadata["DocumentTitle"] = titleMatch.Groups["title"].Value.Trim();

        AddCompactMetadataEntry(metadata, "ABN", compactText, @"ABN\s+(?<value>\d{2}\s+\d{3}\s+\d{3}\s+\d{3})");
        AddCompactMetadataEntry(metadata, "Invoice number", compactText, @"Invoice\s+number\s*(?:[:\-]\s*)?(?<value>[A-Z0-9/\-]+)(?=\s*(?:Issued\s+on|Mobile\s+service|Page|Payment|Need\s+help|Visit|$))");
        AddCompactMetadataEntry(metadata, "Total", compactText, @"Total\s*(?:[:\-]\s*)?(?<value>\$?\d[\d,]*\.\d{2})(?=\s*(?:Incl|Includes|Page|Need\s+help|Visit|$))");
        AddCompactMetadataEntry(metadata, "Issued on", compactText, @"Issued\s+on\s*(?:[:\-]\s*)?(?<value>[A-Z][a-z]{2}\s+\d{1,2}\s+\d{4})");
    }

    private static void AddCompactMetadataEntry(JsonObject metadata, string key, string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
            return;

        var value = match.Groups["value"].Value.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return;

        metadata[key] = value;
    }

    private static bool LooksLikeMetadataLabel(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Length > 80
            || trimmed.Contains(':')
            || trimmed.StartsWith("[", StringComparison.Ordinal)
            || trimmed.StartsWith("Page", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Regex.IsMatch(trimmed, @"^[A-Za-z][A-Za-z0-9 /&().-]{1,79}$", RegexOptions.CultureInvariant);
    }

    private static bool LooksLikeMetadataValue(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        if (trimmed.StartsWith("Page ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Need help", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Visit ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Includes", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return trimmed.Contains('$')
            || Regex.IsMatch(trimmed, @"\d")
            || Regex.IsMatch(trimmed, @"^[A-Z0-9][A-Za-z0-9 .,&()/:-]{2,}$");
    }

    private static string NormalizeMetadataKey(string key)
    {
        var normalized = Regex.Replace(key, @"\s+", " ").Trim();
        normalized = normalized.TrimEnd(':', '-', ';');
        if (normalized.Length == 0 || (normalized.Any(char.IsDigit) && normalized.Length < 3))
            return string.Empty;

        return normalized;
    }

    private static string NormalizeMetadataValue(string value)
    {
        var normalized = Regex.Replace(value, @"\s+", " ").Trim();
        return normalized.TrimEnd('.', ',', ';');
    }

    private static JsonObject ToJson(XElement element)
    {
        var json = new JsonObject
        {
            ["Name"] = element.Name.LocalName
        };

        if (element.HasAttributes)
        {
            json["Attributes"] = new JsonObject(element.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => (JsonNode?)attribute.Value));
        }

        var childElements = element.Elements().ToList();
        if (childElements.Count > 0)
        {
            json["Children"] = new JsonArray(childElements.Select(child => (JsonNode)ToJson(child)).ToArray());
        }

        if (!string.IsNullOrWhiteSpace(element.Value))
        {
            json["Value"] = element.Value;
        }

        return json;
    }

    private static void ResetStream(Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }
    }
}
