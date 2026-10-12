using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Models;
using JiwaMcpServer.Options;
using JiwaMcpServer.Services;
using JiwaMcpServer.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using System.Text;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tests;

public class DocumentToolExportTests
{
    [Fact]
    public async Task CreateDataExport_UsesLatestStoredStructuredToolResultForFullCsvExport()
    {
        var toolResultStore = new ToolResultStore();
        toolResultStore.Store(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "chat-123",
            UserMessage = "Give me a downloadable csv of parts.",
            ToolName = "QueryInventory",
            Successful = true,
            ToolResultText = """
            {"total":2,"returned":2,"results":[{"PartNo":"A1","Description":"Widget","AvailableStock":5,"SellPrice":10.5,"Extra":"x"},{"PartNo":"A2","Description":"Gadget","AvailableStock":0,"SellPrice":22.0,"Extra":"y"}]}
            """
        });

        var storage = new FakeFileStorageService();
        var tool = new DocumentTools(
            new FakeDocumentProcessingService(),
            new FileExportService(NullLogger<FileExportService>.Instance),
            storage,
            toolResultStore,
            Microsoft.Extensions.Options.Options.Create(new DocumentProcessingOptions
            {
                MaxUploadSizeMB = 50,
                TempFolder = Path.GetTempPath(),
                RetentionHours = 24
            }),
            NullLogger<DocumentTools>.Instance);

        var blocks = (await tool.CreateDataExport(new FileExportRequest
        {
            ClientSessionId = "chat-123",
            UseLatestStructuredToolResult = true,
            SourceToolName = "QueryInventory",
            Fields = ["PartNo", "Description", "AvailableStock", "SellPrice"],
            Format = "csv",
            FileName = "parts"
        }, CancellationToken.None)).ToList();

        var csv = Encoding.UTF8.GetString(storage.LastSavedContent);
        var resourceLink = Assert.Single(blocks.OfType<ResourceLinkBlock>());
        Assert.Equal("jiwa-file://files/file-123", resourceLink.Uri?.ToString());
        Assert.DoesNotContain(blocks, block => block is EmbeddedResourceBlock);
        Assert.Equal("parts.csv", storage.LastSavedFileName);
        Assert.Contains("PartNo,Description,AvailableStock,SellPrice", csv);
        Assert.Contains("A1,Widget,5,10.5", csv);
        Assert.Contains("A2,Gadget,0,22", csv);
        Assert.DoesNotContain("Extra", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDataExport_UsesLatestStoredStructuredToolResultWhenToolNameIsNamespaced()
    {
        var toolResultStore = new ToolResultStore();
        toolResultStore.Store(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "chat-456",
            UserMessage = "Give me the parts list.",
            ToolName = "QueryInventory",
            Successful = true,
            ToolResultText = """
            {"total":2,"returned":2,"results":[{"PartNo":"B1","Description":"Bolt","AvailableStock":4,"SellPrice":12.5},{"PartNo":"B2","Description":"Nut","AvailableStock":8,"SellPrice":1.25}]}
            """
        });

        var storage = new FakeFileStorageService();
        var tool = new DocumentTools(
            new FakeDocumentProcessingService(),
            new FileExportService(NullLogger<FileExportService>.Instance),
            storage,
            toolResultStore,
            Microsoft.Extensions.Options.Options.Create(new DocumentProcessingOptions
            {
                MaxUploadSizeMB = 50,
                TempFolder = Path.GetTempPath(),
                RetentionHours = 24
            }),
            NullLogger<DocumentTools>.Instance);

        await tool.CreateDataExport(new FileExportRequest
        {
            ClientSessionId = "chat-456",
            UseLatestStructuredToolResult = true,
            SourceToolName = "functions.query_inventory",
            Fields = ["PartNo", "Description", "AvailableStock", "SellPrice"],
            Format = "csv",
            FileName = "parts"
        }, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(storage.LastSavedContent);
        Assert.Contains("B1,Bolt,4,12.5", csv);
        Assert.Contains("B2,Nut,8,1.25", csv);
    }

    [Fact]
    public async Task CreateDataExport_UsesStoredResultHandleForFullCsvExport()
    {
        var toolResultStore = new ToolResultStore();
        var resultHandle = toolResultStore.StoreStructuredResult(JsonNode.Parse("""
        [{"PartNo":"A1","Description":"Widget","AvailableStock":5,"SellPrice":10.5,"Extra":"x"},{"PartNo":"A2","Description":"Gadget","AvailableStock":0,"SellPrice":22.0,"Extra":"y"}]
        """)!);

        var storage = new FakeFileStorageService();
        var tool = new DocumentTools(
            new FakeDocumentProcessingService(),
            new FileExportService(NullLogger<FileExportService>.Instance),
            storage,
            toolResultStore,
            Microsoft.Extensions.Options.Options.Create(new DocumentProcessingOptions
            {
                MaxUploadSizeMB = 50,
                TempFolder = Path.GetTempPath(),
                RetentionHours = 24
            }),
            NullLogger<DocumentTools>.Instance);

        await tool.CreateDataExport(new FileExportRequest
        {
            ResultHandle = resultHandle,
            Fields = ["PartNo", "Description", "AvailableStock", "SellPrice"],
            Format = "csv",
            FileName = "parts"
        }, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(storage.LastSavedContent);
        Assert.Equal("parts.csv", storage.LastSavedFileName);
        Assert.Contains("PartNo,Description,AvailableStock,SellPrice", csv);
        Assert.Contains("A1,Widget,5,10.5", csv);
        Assert.Contains("A2,Gadget,0,22", csv);
        Assert.DoesNotContain("Extra", csv, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public byte[] LastSavedContent { get; private set; } = [];
        public string LastSavedFileName { get; private set; } = string.Empty;
        public string LastSavedContentType { get; private set; } = string.Empty;

        public async Task<StoredFileReference> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct)
        {
            await using var copy = new MemoryStream();
            await content.CopyToAsync(copy, ct);
            LastSavedContent = copy.ToArray();
            LastSavedFileName = fileName;
            LastSavedContentType = contentType;
            return new StoredFileReference
            {
                FileId = "file-123",
                FileName = fileName,
                ContentType = contentType,
                FileType = "csv",
                ResourceUri = "jiwa-file://files/file-123",
                PhysicalPath = "C:\\temp\\file-123",
                Length = LastSavedContent.LongLength,
                CreatedUtc = DateTimeOffset.UtcNow
            };
        }

        public Task<Stream> OpenReadAsync(string fileId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task DeleteAsync(string fileId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<bool> ExistsAsync(string fileId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<FileMetadata> GetMetadataAsync(string fileId, CancellationToken ct)
            => throw new NotSupportedException();
    }

    private sealed class FakeDocumentProcessingService : IDocumentProcessingService
    {
        public Task<ExtractedDocument> ExtractCsvAsync(Stream content, string fileName, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExtractedDocument> ExtractXlsxAsync(Stream content, string fileName, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExtractedDocument> ExtractJsonAsync(Stream content, string fileName, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExtractedDocument> ExtractXmlAsync(Stream content, string fileName, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExtractedDocument> ExtractPdfAsync(Stream content, string fileName, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExtractedDocument> ExtractDocxAsync(Stream content, string fileName, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExtractedDocument> ExtractDocumentAsync(Stream content, string fileName, string? contentType, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ExportFileResult> ConvertDocumentAsync(Stream content, string fileName, string? contentType, string targetFormat, CancellationToken ct)
            => throw new NotSupportedException();

        public string DetectFileType(string fileName, string? contentType = null)
            => throw new NotSupportedException();
    }
}
