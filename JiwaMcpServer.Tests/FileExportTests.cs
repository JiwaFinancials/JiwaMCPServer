using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using JiwaMcpServer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig;
using System.Text;
using System.Xml.Linq;

namespace JiwaMcpServer.Tests;

public class FileExportTests
{
    [Fact]
    public async Task ExportCsvAsync_WritesDelimitedContent()
    {
        var service = CreateService();

        var export = await service.ExportCsvAsync(new[] { new { PartNo = "A1", Description = "Widget" } }, "inventory", CancellationToken.None);
        var content = Encoding.UTF8.GetString(export.Content);

        Assert.EndsWith(".csv", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PartNo", content);
        Assert.Contains("Widget", content);
    }

    [Fact]
    public async Task ExportXlsxAsync_WritesWorkbook()
    {
        var service = CreateService();

        var export = await service.ExportXlsxAsync(new[] { new { PartNo = "A1", Description = "Widget" } }, "inventory", CancellationToken.None);

        await using var stream = new MemoryStream(export.Content, writable: false);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Export");

        Assert.EndsWith(".xlsx", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("PartNo", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Widget", worksheet.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task ExportJsonAsync_WritesJsonPayload()
    {
        var service = CreateService();

        var export = await service.ExportJsonAsync(new { Customer = "Acme", Count = 2 }, "customers", CancellationToken.None);
        var content = Encoding.UTF8.GetString(export.Content);

        Assert.EndsWith(".json", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Acme", content);
        Assert.Contains("Count", content);
    }

    [Fact]
    public async Task ExportXmlAsync_WritesXmlPayload()
    {
        var service = CreateService();

        var export = await service.ExportXmlAsync(new[] { new { Customer = "Acme" } }, "customers", CancellationToken.None);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(export.Content));

        Assert.EndsWith(".xml", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Export", xml.Root!.Name.LocalName);
        Assert.Contains("Acme", xml.ToString());
    }

    [Fact]
    public async Task ExportPdfAsync_WritesPdfContainingText()
    {
        var service = CreateService();

        var export = await service.ExportPdfAsync(new[] { new { PartNo = "A1", Description = "Widget" } }, "report", CancellationToken.None);
        await using var stream = new MemoryStream(export.Content, writable: false);
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join(Environment.NewLine, pdf.GetPages().Select(page => page.Text));

        Assert.EndsWith(".pdf", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Widget", text);
    }

    [Fact]
    public async Task ExportDocxAsync_WritesDocxContainingText()
    {
        var service = CreateService();

        var export = await service.ExportDocxAsync(new[] { new { PartNo = "A1", Description = "Widget" } }, "report", CancellationToken.None);
        await using var stream = new MemoryStream(export.Content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body ?? throw new InvalidOperationException("DOCX export is missing a document body.");
        var text = body.InnerText;

        Assert.EndsWith(".docx", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Widget", text);
    }

    [Fact]
    public async Task CreateExportAsync_SelectsRequestedFormat()
    {
        var service = CreateService();

        var export = await service.CreateExportAsync(new { Value = 42 }, "json", "value-export", CancellationToken.None);

        Assert.EndsWith(".json", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/json", export.ContentType);
    }

    private static FileExportService CreateService()
        => new(NullLogger<FileExportService>.Instance);
}
