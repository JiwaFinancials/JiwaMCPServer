using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JiwaMcpServer.BackgroundServices;
using JiwaMcpServer.Models;
using JiwaMcpServer.Options;
using JiwaMcpServer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using System.Text.Json;

namespace JiwaMcpServer.Tests;

public class DocumentProcessingTests
{
    [Fact]
    public async Task ExtractCsvAsync_ReturnsStructuredRowsAndMetadata()
    {
        var service = CreateDocumentProcessingService();
        await using var stream = CreateUtf8Stream("PartNo,Description,SellPrice\r\nA1,Widget,12.50\r\nA2,Gadget,9.99\r\n");

        var result = await service.ExtractCsvAsync(stream, "inventory.csv", CancellationToken.None);

        Assert.Equal("csv", result.FileType);
        Assert.Equal(2, result.Metadata.RowCount);
        Assert.Equal(3, result.Metadata.ColumnCount);
        Assert.Contains("Widget", result.ExtractedText);
        Assert.Equal(2, result.StructuredData!.AsArray().Count);
    }

    [Fact]
    public async Task ExtractXlsxAsync_ReturnsWorksheetNamesAndRows()
    {
        var service = CreateDocumentProcessingService();
        await using var stream = CreateWorkbookStream();

        var result = await service.ExtractXlsxAsync(stream, "inventory.xlsx", CancellationToken.None);

        Assert.Equal("xlsx", result.FileType);
        Assert.Equal(1, result.Metadata.WorksheetCount);
        Assert.Equal(2, result.Metadata.RowCount);
        Assert.Contains("Products", result.ExtractedText);
        Assert.Contains("Products", result.StructuredData!.ToJsonString());
    }

    [Fact]
    public async Task ExtractJsonAsync_PreservesHierarchy()
    {
        var service = CreateDocumentProcessingService();
        await using var stream = CreateUtf8Stream("{" + "\"customer\":{\"name\":\"Acme\",\"addresses\":[{\"city\":\"Sydney\"}]}}");

        var result = await service.ExtractJsonAsync(stream, "customer.json", CancellationToken.None);

        Assert.Equal("json", result.FileType);
        Assert.Contains("Acme", result.ExtractedText);
        Assert.Contains("addresses", result.StructuredData!.ToJsonString());
    }

    [Fact]
    public async Task ExtractXmlAsync_PreservesHierarchyAndAttributes()
    {
        var service = CreateDocumentProcessingService();
        await using var stream = CreateUtf8Stream("<Customers><Customer id=\"A1\"><Name>Acme</Name></Customer></Customers>");

        var result = await service.ExtractXmlAsync(stream, "customers.xml", CancellationToken.None);

        Assert.Equal("xml", result.FileType);
        Assert.Contains("Customers", result.ExtractedText);
        Assert.Contains("A1", result.StructuredData!.ToJsonString());
    }

    [Fact]
    public async Task ExtractPdfAsync_ExtractsPageText()
    {
        var exportService = CreateFileExportService();
        var service = CreateDocumentProcessingService(exportService);
        var pdf = await exportService.ExportPdfAsync(new[] { new { PartNo = "A1", Description = "Widget" } }, "report", CancellationToken.None);
        await using var stream = new MemoryStream(pdf.Content, writable: false);

        var result = await service.ExtractPdfAsync(stream, "report.pdf", CancellationToken.None);

        Assert.Equal("pdf", result.FileType);
        Assert.True(result.Metadata.PageCount >= 1);
        Assert.Contains("Widget", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractDocxAsync_ExtractsParagraphsAndTables()
    {
        var service = CreateDocumentProcessingService();
        await using var stream = CreateDocxStream();

        var result = await service.ExtractDocxAsync(stream, "report.docx", CancellationToken.None);

        Assert.Equal("docx", result.FileType);
        Assert.Equal(1, result.Metadata.TableCount);
        Assert.True(result.Metadata.ParagraphCount >= 1);
        Assert.Contains("Quarterly Inventory Report", result.ExtractedText);
        Assert.Contains("Widget", result.ExtractedText);
    }

    [Fact]
    public async Task ConvertDocumentAsync_CsvToJson_ReturnsJsonFile()
    {
        var service = CreateDocumentProcessingService();
        await using var stream = CreateUtf8Stream("PartNo,Description\r\nA1,Widget\r\n");

        var export = await service.ConvertDocumentAsync(stream, "inventory.csv", "text/csv", "json", CancellationToken.None);
        var json = Encoding.UTF8.GetString(export.Content);

        Assert.EndsWith(".json", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/json", export.ContentType);
        Assert.Contains("Widget", json);
    }

    [Fact]
    public async Task SaveAsync_RejectsUnsupportedFileType()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var storage = CreateStorageService(tempDir);
            await using var stream = CreateUtf8Stream("hello");

            await Assert.ThrowsAsync<NotSupportedException>(() => storage.SaveAsync(stream, "notes.txt", "text/plain", CancellationToken.None));
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsMismatchedMimeType()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var storage = CreateStorageService(tempDir);
            await using var stream = CreateUtf8Stream("PartNo,Description\r\nA1,Widget\r\n");

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(stream, "inventory.csv", "application/pdf", CancellationToken.None));
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsOversizedFile()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var storage = CreateStorageService(tempDir, maxUploadSizeMb: 1);
            await using var stream = new MemoryStream(new byte[2 * 1024 * 1024]);

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(stream, "large.json", "application/json", CancellationToken.None));
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_RemovesExpiredFiles()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var storage = CreateStorageService(tempDir, retentionHours: 1);
            await using var stream = CreateUtf8Stream("{\"value\":1}");
            var stored = await storage.SaveAsync(stream, "payload.json", "application/json", CancellationToken.None);
            var metadataPath = Path.Combine(tempDir, stored.FileId + FileStorageService.MetadataSuffix);
            var expiredMetadata = new FileMetadata
            {
                FileId = stored.FileId,
                FileName = stored.FileName,
                ContentType = stored.ContentType,
                FileType = stored.FileType,
                ResourceUri = stored.ResourceUri,
                Length = stored.Length,
                CreatedUtc = DateTimeOffset.UtcNow.AddHours(-2)
            };

            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(expiredMetadata), CancellationToken.None);
            var cleanup = CreateCleanupService(tempDir, storage, retentionHours: 1);

            var deleted = await cleanup.CleanupExpiredFilesAsync(CancellationToken.None);

            Assert.Equal(1, deleted);
            Assert.False(await storage.ExistsAsync(stored.FileId, CancellationToken.None));
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task SaveAsync_SupportsConcurrentUploads()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var storage = CreateStorageService(tempDir);
            var uploadTasks = Enumerable.Range(1, 10)
                .Select(async index =>
                {
                    await using var stream = CreateUtf8Stream($"{{\"index\":{index}}}");
                    return await storage.SaveAsync(stream, $"payload-{index}.json", "application/json", CancellationToken.None);
                });

            var storedFiles = await Task.WhenAll(uploadTasks);

            Assert.Equal(10, storedFiles.Select(file => file.FileId).Distinct(StringComparer.Ordinal).Count());
            foreach (var storedFile in storedFiles)
            {
                Assert.True(await storage.ExistsAsync(storedFile.FileId, CancellationToken.None));
            }
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    private static IDocumentProcessingService CreateDocumentProcessingService(IFileExportService? exportService = null)
        => new DocumentProcessingService(exportService ?? CreateFileExportService(), NullLogger<DocumentProcessingService>.Instance);

    private static IFileExportService CreateFileExportService()
        => new FileExportService(NullLogger<FileExportService>.Instance);

    private static FileStorageService CreateStorageService(string tempDir, int maxUploadSizeMb = 50, int retentionHours = 24)
        => new(Microsoft.Extensions.Options.Options.Create(new DocumentProcessingOptions
        {
            MaxUploadSizeMB = maxUploadSizeMb,
            TempFolder = tempDir,
            RetentionHours = retentionHours
        }), NullLogger<FileStorageService>.Instance);

    private static DocumentCleanupService CreateCleanupService(string tempDir, IFileStorageService storageService, int retentionHours)
        => new(Microsoft.Extensions.Options.Options.Create(new DocumentProcessingOptions
        {
            MaxUploadSizeMB = 50,
            TempFolder = tempDir,
            RetentionHours = retentionHours
        }), storageService, NullLogger<DocumentCleanupService>.Instance);

    private static MemoryStream CreateUtf8Stream(string content)
        => new(Encoding.UTF8.GetBytes(content));

    private static MemoryStream CreateWorkbookStream()
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.AddWorksheet("Products");
            worksheet.Cell(1, 1).Value = "PartNo";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(2, 1).Value = "A1";
            worksheet.Cell(2, 2).Value = "Widget";
            worksheet.Cell(3, 1).Value = "A2";
            worksheet.Cell(3, 2).Value = "Gadget";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxStream()
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            body.Append(new Paragraph(new Run(new Text("Quarterly Inventory Report"))));
            var table = new Table();
            table.Append(new TableRow(
                new TableCell(new Paragraph(new Run(new Text("PartNo")))),
                new TableCell(new Paragraph(new Run(new Text("Description"))))));
            table.Append(new TableRow(
                new TableCell(new Paragraph(new Run(new Text("A1")))),
                new TableCell(new Paragraph(new Run(new Text("Widget"))))));
            body.Append(table);
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JiwaMcpServerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
