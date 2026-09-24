using ClosedXML.Excel;
using CsvHelper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JiwaMcpServer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace JiwaMcpServer.Services;

public sealed class FileExportService(ILogger<FileExportService> logger) : IFileExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<FileExportService> _logger = logger;

    static FileExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<ExportFileResult> ExportCsvAsync(object? data, string fileName, CancellationToken ct)
        => CreateDelimitedExportAsync(data, fileName, "csv", ct);

    public async Task<ExportFileResult> ExportXlsxAsync(object? data, string fileName, CancellationToken ct)
    {
        var rows = CreateRows(data);
        var fileNameWithExtension = EnsureFileName(fileName, "xlsx");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Export");
        var headers = GetHeaders(rows);

        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                row.TryGetValue(headers[columnIndex], out var value);
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = value ?? string.Empty;
            }
        }

        worksheet.ColumnsUsed().AdjustToContents();

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _logger.LogInformation("Generated XLSX export {FileName}", fileNameWithExtension);
        return CreateResult(fileNameWithExtension, "xlsx", stream.ToArray());
    }

    public async Task<ExportFileResult> ExportJsonAsync(object? data, string fileName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var node = NormalizeNode(data);
        var fileNameWithExtension = EnsureFileName(fileName, "json");
        var bytes = Encoding.UTF8.GetBytes(node.ToJsonString(JsonOptions));
        _logger.LogInformation("Generated JSON export {FileName}", fileNameWithExtension);
        return await Task.FromResult(CreateResult(fileNameWithExtension, "json", bytes));
    }

    public async Task<ExportFileResult> ExportXmlAsync(object? data, string fileName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var document = new XDocument(ToXml("Export", NormalizeNode(data)));
        var fileNameWithExtension = EnsureFileName(fileName, "xml");
        await using var stream = new MemoryStream();
        var settings = new System.Xml.XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(false),
            Indent = true
        };

        await using (var writer = System.Xml.XmlWriter.Create(stream, settings))
        {
            await document.SaveAsync(writer, ct);
            await writer.FlushAsync();
        }

        _logger.LogInformation("Generated XML export {FileName}", fileNameWithExtension);
        return CreateResult(fileNameWithExtension, "xml", stream.ToArray());
    }

    public async Task<ExportFileResult> ExportPdfAsync(object? data, string fileName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rows = CreateRows(data);
        var fileNameWithExtension = EnsureFileName(fileName, "pdf");
        await using var stream = new MemoryStream();

        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Header().Text(Path.GetFileNameWithoutExtension(fileNameWithExtension)).SemiBold().FontSize(18);
                page.Content().Element(content => BuildPdfContent(content, rows, NormalizeNode(data)));
            });
        }).GeneratePdf(stream);

        _logger.LogInformation("Generated PDF export {FileName}", fileNameWithExtension);
        return CreateResult(fileNameWithExtension, "pdf", stream.ToArray());
    }

    public async Task<ExportFileResult> ExportDocxAsync(object? data, string fileName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rows = CreateRows(data);
        var node = NormalizeNode(data);
        var fileNameWithExtension = EnsureFileName(fileName, "docx");
        await using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
            var body = mainPart.Document.Body ?? throw new InvalidOperationException("DOCX document body could not be created.");

            body.Append(CreateParagraph(Path.GetFileNameWithoutExtension(fileNameWithExtension), true));

            if (rows.Count > 0)
            {
                body.Append(CreateWordTable(rows));
            }
            else
            {
                foreach (var line in node.ToJsonString(JsonOptions).Split(Environment.NewLine, StringSplitOptions.None))
                {
                    body.Append(CreateParagraph(line, false));
                }
            }

            mainPart.Document.Save();
        }

        _logger.LogInformation("Generated DOCX export {FileName}", fileNameWithExtension);
        return CreateResult(fileNameWithExtension, "docx", stream.ToArray());
    }

    public Task<ExportFileResult> CreateExportAsync(object? data, string format, string fileName, CancellationToken ct)
    {
        var normalizedFormat = DocumentFormatMappings.NormalizeFormat(format);
        return normalizedFormat switch
        {
            "csv" => ExportCsvAsync(data, fileName, ct),
            "xlsx" => ExportXlsxAsync(data, fileName, ct),
            "json" => ExportJsonAsync(data, fileName, ct),
            "xml" => ExportXmlAsync(data, fileName, ct),
            "pdf" => ExportPdfAsync(data, fileName, ct),
            "docx" => ExportDocxAsync(data, fileName, ct),
            _ => throw new NotSupportedException($"Unsupported export format '{format}'.")
        };
    }

    private async Task<ExportFileResult> CreateDelimitedExportAsync(object? data, string fileName, string format, CancellationToken ct)
    {
        var rows = CreateRows(data);
        var fileNameWithExtension = EnsureFileName(fileName, format);
        var headers = GetHeaders(rows);
        await using var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var header in headers)
        {
            csv.WriteField(header);
        }

        await csv.NextRecordAsync();

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var header in headers)
            {
                row.TryGetValue(header, out var value);
                csv.WriteField(value);
            }

            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(ct);
        _logger.LogInformation("Generated CSV export {FileName}", fileNameWithExtension);
        return CreateResult(fileNameWithExtension, format, stream.ToArray());
    }

    private static ExportFileResult CreateResult(string fileName, string format, byte[] content)
        => new()
        {
            FileName = fileName,
            ContentType = DocumentFormatMappings.GetMimeType(format),
            Content = content
        };

    private static string EnsureFileName(string fileName, string format)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("A file name is required.");
        }

        var sanitized = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "export";
        }

        return sanitized + DocumentFormatMappings.GetExtension(format);
    }

    private static JsonNode NormalizeNode(object? data)
        => data switch
        {
            null => new JsonArray(),
            JsonNode node => node.DeepClone(),
            JsonElement element => JsonNode.Parse(element.GetRawText()) ?? new JsonArray(),
            _ => JsonSerializer.SerializeToNode(data, JsonOptions) ?? new JsonArray()
        };

    private static List<Dictionary<string, string?>> CreateRows(object? data)
    {
        var node = NormalizeNode(data);

        return node switch
        {
            JsonArray array => CreateRowsFromArray(array),
            JsonObject jsonObject => [FlattenObject(jsonObject)],
            _ => [new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["Value"] = AsText(node) }]
        };
    }

    private static List<Dictionary<string, string?>> CreateRowsFromArray(JsonArray array)
    {
        var rows = new List<Dictionary<string, string?>>();
        foreach (var item in array)
        {
            rows.Add(item switch
            {
                JsonObject obj => FlattenObject(obj),
                _ => new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["Value"] = AsText(item) }
            });
        }

        return rows.Count > 0 ? rows : [new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)];
    }

    private static Dictionary<string, string?> FlattenObject(JsonObject obj)
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in obj)
        {
            FlattenInto(row, property.Key, property.Value);
        }

        return row;
    }

    private static void FlattenInto(IDictionary<string, string?> target, string path, JsonNode? node)
    {
        switch (node)
        {
            case JsonObject childObject:
                foreach (var property in childObject)
                {
                    FlattenInto(target, string.IsNullOrWhiteSpace(path) ? property.Key : $"{path}.{property.Key}", property.Value);
                }
                break;
            case JsonArray childArray:
                target[path] = childArray.ToJsonString(JsonOptions);
                break;
            default:
                target[path] = AsText(node);
                break;
        }
    }

    private static string? AsText(JsonNode? node)
        => node is null ? null : node is JsonValue value ? value.ToString() : node.ToJsonString(JsonOptions);

    private static List<string> GetHeaders(IReadOnlyList<Dictionary<string, string?>> rows)
    {
        var headers = new List<string>();
        foreach (var row in rows)
        {
            foreach (var key in row.Keys)
            {
                if (!headers.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    headers.Add(key);
                }
            }
        }

        return headers.Count > 0 ? headers : ["Value"];
    }

    private static XElement ToXml(string name, JsonNode? node)
    {
        return node switch
        {
            JsonObject obj => new XElement(name, obj.Select(property => ToXml(property.Key, property.Value))),
            JsonArray array => new XElement(name, array.Select(item => ToXml("Item", item))),
            JsonValue value => new XElement(name, value.ToString()),
            _ => new XElement(name)
        };
    }

    private static void BuildPdfContent(IContainer container, IReadOnlyList<Dictionary<string, string?>> rows, JsonNode node)
    {
        if (rows.Count == 0 || (rows.Count == 1 && rows[0].Count == 0))
        {
            container.Text(node.ToJsonString(JsonOptions));
            return;
        }

        var headers = GetHeaders(rows);
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                {
                    columns.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                foreach (var column in headers)
                {
                    header.Cell().BorderBottom(1).Padding(4).Text(column).SemiBold();
                }
            });

            foreach (var row in rows)
            {
                foreach (var headerName in headers)
                {
                    row.TryGetValue(headerName, out var value);
                    table.Cell().BorderBottom(0.5f).Padding(4).Text(value ?? string.Empty);
                }
            }
        });
    }

    private static Paragraph CreateParagraph(string textValue, bool bold)
    {
        var text = new Text(textValue ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve };
        var run = bold
            ? new Run(new RunProperties(new Bold()), text)
            : new Run(text);

        return new Paragraph(run);
    }

    private static Table CreateWordTable(IReadOnlyList<Dictionary<string, string?>> rows)
    {
        var headers = GetHeaders(rows);
        var table = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 6 },
                    new BottomBorder { Val = BorderValues.Single, Size = 6 },
                    new LeftBorder { Val = BorderValues.Single, Size = 6 },
                    new RightBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 })));

        var headerRow = new TableRow(headers.Select(header => CreateTableCell(header, true)));
        table.Append(headerRow);

        foreach (var row in rows)
        {
            table.Append(new TableRow(headers.Select(header =>
            {
                row.TryGetValue(header, out var value);
                return CreateTableCell(value ?? string.Empty, false);
            })));
        }

        return table;
    }

    private static TableCell CreateTableCell(string textValue, bool bold)
    {
        var text = new Text(textValue) { Space = SpaceProcessingModeValues.Preserve };
        var run = bold
            ? new Run(new RunProperties(new Bold()), text)
            : new Run(text);

        return new TableCell(new Paragraph(run));
    }
}
