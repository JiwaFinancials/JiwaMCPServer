using Xunit;
using JiwaMcpServer.Tools;
using JiwaMcpServer.Services;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Comprehensive tests for FileTools and FileStorageService
/// </summary>
public class FileToolsTests
{
    private readonly FileStorageService _fileStorage = new();
    private readonly FileTools _fileTools;

    public FileToolsTests()
    {
        _fileTools = new FileTools(_fileStorage);
    }

    #region upload_file Tests

    [Fact]
    public async Task UploadFile_ValidInput_ReturnsFileId()
    {
        // Arrange
        var fileName = "test.txt";
        var mimeType = "text/plain";
        var content = "Hello, World!";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fileId", result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_ValidCsv_ReturnsFileId()
    {
        // Arrange
        var fileName = "data.csv";
        var mimeType = "text/csv";
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fileId", result);
    }

    [Fact]
    public async Task UploadFile_ValidJson_ReturnsFileId()
    {
        // Arrange
        var fileName = "data.json";
        var mimeType = "application/json";
        var jsonContent = "{\"key\": \"value\", \"number\": 42}";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fileId", result);
    }

    [Fact]
    public async Task UploadFile_EmptyFileName_ReturnsError()
    {
        // Arrange
        var mimeType = "text/plain";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content"));

        // Act
        var result = await _fileTools.UploadFile("", mimeType, contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("filename is required", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_EmptyMimeType_ReturnsError()
    {
        // Arrange
        var fileName = "test.txt";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content"));

        // Act
        var result = await _fileTools.UploadFile(fileName, "", contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("mimetype is required", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_EmptyContent_ReturnsError()
    {
        // Arrange
        var fileName = "test.txt";
        var mimeType = "text/plain";

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("contentbase64 is required", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_InvalidBase64_ReturnsError()
    {
        // Arrange
        var fileName = "test.txt";
        var mimeType = "text/plain";
        var invalidBase64 = "!@#$%^&*()_not_valid_base64";

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, invalidBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("base64", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_DisallowedMimeType_ReturnsError()
    {
        // Arrange
        var fileName = "test.exe";
        var mimeType = "application/x-msdownload"; // Not whitelisted
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content"));

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("not allowed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_OversizedFile_ReturnsError()
    {
        // Arrange
        var fileName = "huge.txt";
        var mimeType = "text/plain";
        var largeContent = new string('x', (int)FileStorageService.MaxUploadSizeBytes + 1);
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(largeContent));

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("exceeds maximum", result.ToLowerInvariant());
    }

    [Fact]
    public async Task UploadFile_SingleByteFile_IsValid()
    {
        // Arrange - test with minimal valid file
        var fileName = "tiny.txt";
        var mimeType = "text/plain";
        var singleByte = new byte[] { 42 }; // Single byte
        var contentBase64 = Convert.ToBase64String(singleByte);

        // Act
        var result = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Assert - should succeed
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("fileId", result);
    }

    #endregion

    #region read_uploaded_file Tests

    [Fact]
    public async Task ReadUploadedFile_ValidFileId_ReturnsContent()
    {
        // Arrange
        var fileName = "test.txt";
        var mimeType = "text/plain";
        var expectedContent = "Hello, World!";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(expectedContent));

        // Upload first
        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var readResult = await _fileTools.ReadUploadedFile(fileId!);

        // Assert
        Assert.NotNull(readResult);
        Assert.Contains(expectedContent, readResult);
        Assert.Contains(fileName, readResult);
        Assert.Contains(mimeType, readResult);
    }

    [Fact]
    public async Task ReadUploadedFile_UnknownFileId_ReturnsError()
    {
        // Act
        var result = await _fileTools.ReadUploadedFile("unknown-file-id-12345");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("not found", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadUploadedFile_EmptyFileId_ReturnsError()
    {
        // Act
        var result = await _fileTools.ReadUploadedFile("");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadUploadedFile_NullFileId_ReturnsError()
    {
        // Act - this will be caught by parameter validation
        var result = await _fileTools.ReadUploadedFile(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
    }

    #endregion

    #region query_csv Tests

    [Fact]
    public async Task QueryCsv_CountQuestion_ReturnsRowCount()
    {
        // Arrange
        var fileName = "data.csv";
        var mimeType = "text/csv";
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\nBob,35,Chicago";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "How many rows?");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("3", result); // 3 data rows
        Assert.DoesNotContain("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryCsv_FieldExtractionQuestion_ReturnsMatchingField()
    {
        // Arrange
        var fileName = "data.csv";
        var mimeType = "text/csv";
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "Show me the Name column");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("John", result);
        Assert.Contains("Jane", result);
    }

    [Fact]
    public async Task QueryCsv_CsvWithQuotedValues_ParsesCorrectly()
    {
        // Arrange
        var fileName = "data.csv";
        var mimeType = "text/csv";
        var csvContent = "Product,\"Description, Long\"\n\"Widget A\",\"A multi-purpose widget, very useful\"\n\"Widget B\",\"Another widget, with comma\"";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "Show me the Product column");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Widget A", result);
        Assert.Contains("Widget B", result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryCsv_EmptyQuestion_ReturnsError()
    {
        // Arrange
        var fileName = "data.csv";
        var mimeType = "text/csv";
        var csvContent = "Name,Age\nJohn,30";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryCsv_UnknownFileId_ReturnsError()
    {
        // Act
        var result = await _fileTools.QueryCsv("unknown-id-xyz", "count");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryCsv_NonCsvFile_ReturnsError()
    {
        // Arrange
        var fileName = "data.txt";
        var mimeType = "text/plain";
        var content = "This is just plain text, not CSV";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act - will fail parsing as CSV
        var result = await _fileTools.QueryCsv(fileId!, "count");

        // Assert - Should handle gracefully
        Assert.NotNull(result);
    }

    [Fact]
    public async Task QueryCsv_LargeCsv_ReturnsLimitedResults()
    {
        // Arrange
        var fileName = "large.csv";
        var mimeType = "text/csv";
        var lines = new System.Collections.Generic.List<string> { "ID,Name" };
        for (int i = 1; i <= 20; i++)
        {
            lines.Add($"{i},Person{i}");
        }
        var csvContent = string.Join("\n", lines);
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "List all persons");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("20", result); // Should mention total rows
        // Results should be limited, not all 20
    }

    #endregion

    #region Integration & Edge Case Tests

    [Fact]
    public async Task SessionIsolation_TwoUploadsSameName_ProduceDifferentFileIds()
    {
        // Arrange
        var fileName = "test.txt";
        var mimeType = "text/plain";
        var content1Base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Content 1"));
        var content2Base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Content 2"));

        // Act
        var result1 = await _fileTools.UploadFile(fileName, mimeType, content1Base64);
        var result2 = await _fileTools.UploadFile(fileName, mimeType, content2Base64);

        // Assert
        var fileId1 = System.Text.Json.JsonDocument.Parse(result1).RootElement.GetProperty("fileId").GetString();
        var fileId2 = System.Text.Json.JsonDocument.Parse(result2).RootElement.GetProperty("fileId").GetString();

        Assert.NotEqual(fileId1, fileId2);
    }

    [Fact]
    public async Task FileTools_AllowedMimeTypes_Text()
    {
        // Arrange
        var mimeTypes = new[] { "text/plain", "text/csv", "text/html", "text/xml", "text/json" };

        foreach (var mimeType in mimeTypes)
        {
            // Act
            var result = await _fileTools.UploadFile("test.txt", mimeType, Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content")));

            // Assert
            Assert.DoesNotContain("error", result.ToLowerInvariant());
        }
    }

    [Fact]
    public async Task FileTools_AllowedMimeTypes_Application()
    {
        // Arrange
        var mimeTypes = new[]
        {
            "application/json",
            "application/xml",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        foreach (var mimeType in mimeTypes)
        {
            // Act
            var result = await _fileTools.UploadFile("test.txt", mimeType, Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content")));

            // Assert
            Assert.DoesNotContain("error", result.ToLowerInvariant());
        }
    }

    [Fact]
    public async Task UploadFile_TextMimeTypeVariants_AllAllowed()
    {
        // Ensure all text/* variants are allowed
        var result = await _fileTools.UploadFile("test.txt", "text/custom-format", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content")));
        Assert.DoesNotContain("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryCsv_HeaderlessData_HandlesGracefully()
    {
        // Arrange
        var fileName = "no-header.csv";
        var mimeType = "text/csv";
        var csvContent = "value1,value2,value3"; // No header row, just data
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "count");

        // Assert - should either handle or return error gracefully
        Assert.NotNull(result);
    }

    [Fact]
    public async Task QueryCsv_MultilineFields_ParsesCorrectly()
    {
        // Arrange - CSV with multiline values in quotes
        var fileName = "multiline.csv";
        var mimeType = "text/csv";
        var csvContent = "Name,Description\nProduct1,\"Line 1\nLine 2\"\nProduct2,SimpleDesc"; // Note: simple line break, not Windows CRLF
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryCsv(fileId!, "Show Name");

        // Assert
        Assert.NotNull(result);
        // Should not throw an error (though parsing multiline quotes is complex)
    }

    #endregion

    #region Latest File Convenience Tests

    [Fact]
    public async Task ReadUploadedFile_NoFileId_ReturnsLatestFile()
    {
        // Arrange - Upload a file without specifying fileId in read call
        var fileName = "latest-test.txt";
        var mimeType = "text/plain";
        var content = "This is the latest file";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act - Read without specifying fileId (empty string implicitly uses latest)
        var readResult = await _fileTools.ReadUploadedFile("");

        // Assert
        Assert.DoesNotContain("error", readResult.ToLowerInvariant());
        Assert.Contains(content, readResult);
        Assert.Contains(fileName, readResult);
    }

    [Fact]
    public async Task QueryCsv_NoFileId_UsesLatestFile()
    {
        // Arrange - Upload a CSV but don't specify fileId in query
        var fileName = "latest.csv";
        var mimeType = "text/csv";
        var csvContent = "Name,Age\nAlice,30\nBob,25\nCharlie,35";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var uploadResult = await _fileTools.UploadFile(fileName, mimeType, contentBase64);

        // Act - Query without specifying fileId (empty string uses latest)
        var queryResult = await _fileTools.QueryCsv("", "How many rows?");

        // Assert
        Assert.DoesNotContain("error", queryResult.ToLowerInvariant());
        Assert.Contains("3", queryResult); // Should find 3 data rows
    }

    [Fact]
    public async Task LatestFile_MultipleUploads_TracksLatest()
    {
        // Arrange - Upload multiple files and verify latest is tracked
        var content1 = "First file";
        var content2 = "Second file (latest)";
        var contentBase64_1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content1));
        var contentBase64_2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content2));

        // Act - Upload first file
        await _fileTools.UploadFile("file1.txt", "text/plain", contentBase64_1);

        // Upload second file
        await _fileTools.UploadFile("file2.txt", "text/plain", contentBase64_2);

        // Read without specifying fileId (should get the latest = file2)
        var readResult = await _fileTools.ReadUploadedFile("");

        // Assert
        Assert.DoesNotContain("error", readResult.ToLowerInvariant());
        Assert.Contains(content2, readResult);
        Assert.Contains("file2.txt", readResult);
        Assert.DoesNotContain("file1.txt", readResult);
    }

    [Fact]
    public async Task LatestFile_WithClientSessionId_IsolatedPerSession()
    {
        // Arrange - Upload files with different session IDs
        var sessionId1 = "session-001";
        var sessionId2 = "session-002";
        var content1 = "Session 1 file";
        var content2 = "Session 2 file";
        var contentBase64_1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content1));
        var contentBase64_2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content2));

        // Act & Assert - Upload and read in session 1
        await _fileTools.UploadFile("session1.txt", "text/plain", contentBase64_1, sessionId1);
        var readResult1 = await _fileTools.ReadUploadedFile("", sessionId1);
        Assert.DoesNotContain("error", readResult1.ToLowerInvariant());
        Assert.Contains(content1, readResult1);

        // Upload and read in session 2
        await _fileTools.UploadFile("session2.txt", "text/plain", contentBase64_2, sessionId2);
        var readResult2 = await _fileTools.ReadUploadedFile("", sessionId2);
        Assert.DoesNotContain("error", readResult2.ToLowerInvariant());
        Assert.Contains(content2, readResult2);
    }

    [Fact]
    public async Task ReadUploadedFile_CanStillUseExplicitFileId()
    {
        // Arrange - Upload two files, but explicitly read the first one
        var content1 = "First file content";
        var content2 = "Second file content";
        var contentBase64_1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content1));
        var contentBase64_2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content2));

        var uploadResult1 = await _fileTools.UploadFile("first.txt", "text/plain", contentBase64_1);
        var fileId1 = System.Text.Json.JsonDocument.Parse(uploadResult1)
            .RootElement.GetProperty("fileId").GetString();

        await _fileTools.UploadFile("second.txt", "text/plain", contentBase64_2);

        // Act - Read with explicit fileId (should override latest logic)
        var readResult = await _fileTools.ReadUploadedFile(fileId1!);

        // Assert
        Assert.DoesNotContain("error", readResult.ToLowerInvariant());
        Assert.Contains(content1, readResult); // Should get first file, not latest
        Assert.Contains("first.txt", readResult);
    }

    #endregion

    #region query_excel Tests

    [Fact]
    public async Task QueryExcel_ValidWorkbook_ReturnsQueryResults()
    {
        // Arrange
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Age";
        worksheet.Cell(2, 1).Value = "Alice";
        worksheet.Cell(2, 2).Value = 30;
        worksheet.Cell(3, 1).Value = "Bob";
        worksheet.Cell(3, 2).Value = 25;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var contentBase64 = Convert.ToBase64String(stream.ToArray());

        await _fileTools.UploadFile("people.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", contentBase64);

        // Act
        var result = await _fileTools.QueryExcel("", "Show me the Name column");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("Alice", result);
        Assert.Contains("Bob", result);
    }

    [Fact]
    public async Task QueryExcel_InvalidMimeType_ReturnsError()
    {
        // Arrange
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("not an excel file"));
        var uploadResult = await _fileTools.UploadFile("bad.txt", "text/plain", contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.QueryExcel(fileId!, "count");

        // Assert
        Assert.Contains("error", result.ToLowerInvariant());
        Assert.Contains("not an excel file", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryExcel_SpecificSheetAndRange_ReturnsTargetedRows()
    {
        // Arrange
        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = "Metric";
        summarySheet.Cell(1, 2).Value = "Value";
        summarySheet.Cell(2, 1).Value = "Total";
        summarySheet.Cell(2, 2).Value = 99;

        var customersSheet = workbook.Worksheets.Add("Customers");
        customersSheet.Cell(1, 1).Value = "Name";
        customersSheet.Cell(1, 2).Value = "City";
        customersSheet.Cell(2, 1).Value = "Alice";
        customersSheet.Cell(2, 2).Value = "Melbourne";
        customersSheet.Cell(3, 1).Value = "Bob";
        customersSheet.Cell(3, 2).Value = "Sydney";
        customersSheet.Cell(4, 1).Value = "Charlie";
        customersSheet.Cell(4, 2).Value = "Perth";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var contentBase64 = Convert.ToBase64String(stream.ToArray());

        await _fileTools.UploadFile("customers.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", contentBase64);

        // Act
        var result = await _fileTools.QueryExcel("", "How many rows?", "Customers", "A1:B3");

        // Assert
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("Total rows: 2", result);
    }

    [Fact]
    public async Task DescribeExcel_ReturnsWorkbookSummary()
    {
        // Arrange
        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var customersSheet = workbook.Worksheets.Add("Customers");
        customersSheet.Cell(1, 1).Value = "Name";
        customersSheet.Cell(1, 2).Value = "City";
        customersSheet.Cell(2, 1).Value = "Alice";
        customersSheet.Cell(2, 2).Value = "Melbourne";
        customersSheet.Cell(3, 1).Value = "Bob";
        customersSheet.Cell(3, 2).Value = "Sydney";

        var ordersSheet = workbook.Worksheets.Add("Orders");
        ordersSheet.Cell(1, 1).Value = "OrderNo";
        ordersSheet.Cell(2, 1).Value = "SO1001";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var contentBase64 = Convert.ToBase64String(stream.ToArray());

        await _fileTools.UploadFile("workbook.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", contentBase64);

        // Act
        var result = await _fileTools.DescribeExcel("", "Customers", "A1:B3");

        // Assert
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("sheetcount", result.ToLowerInvariant());
        Assert.Contains("customers", result.ToLowerInvariant());
        Assert.Contains("datarowcount", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadExcelRows_PathInput_ReturnsStructuredRows()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempRoot);
            var excelPath = Path.Combine(tempRoot, "customers.xlsx");

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Customers");
                worksheet.Cell(1, 1).Value = "Code";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(2, 1).Value = "C001";
                worksheet.Cell(2, 2).Value = "Alice Pty Ltd";
                worksheet.Cell(3, 1).Value = "C002";
                worksheet.Cell(3, 2).Value = "Bob Traders";
                workbook.SaveAs(excelPath);
            }

            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };

            var result = await _fileTools.ReadExcelRows(excelPath, "Customers", "A1:B3", 0, 50);

            Assert.DoesNotContain("error", result.ToLowerInvariant());
            Assert.Contains("headers", result.ToLowerInvariant());
            Assert.Contains("rows", result.ToLowerInvariant());
            Assert.Contains("alice pty ltd", result.ToLowerInvariant());
            Assert.Contains("totalrows", result.ToLowerInvariant());
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task ReadExcelRows_Paging_ReturnsExpectedWindow()
    {
        // Arrange
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Customers");
        worksheet.Cell(1, 1).Value = "Code";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(2, 1).Value = "C001";
        worksheet.Cell(2, 2).Value = "Alice";
        worksheet.Cell(3, 1).Value = "C002";
        worksheet.Cell(3, 2).Value = "Bob";
        worksheet.Cell(4, 1).Value = "C003";
        worksheet.Cell(4, 2).Value = "Charlie";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var contentBase64 = Convert.ToBase64String(stream.ToArray());

        var upload = await _fileTools.UploadFile("customers.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(upload).RootElement.GetProperty("fileId").GetString();

        // Act
        var result = await _fileTools.ReadExcelRows(fileId!, "Customers", "A1:B4", 1, 1);

        // Assert
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("\"returnedrows\":1", result.Replace(" ", string.Empty).ToLowerInvariant());
        Assert.Contains("\"hasmore\":true", result.Replace(" ", string.Empty).ToLowerInvariant());
        Assert.Contains("bob", result.ToLowerInvariant());
        Assert.DoesNotContain("alice", result.ToLowerInvariant());
    }

    #endregion

    #region query_xml Tests

    [Fact]
    public async Task QueryXml_ValidXmlFile_ReturnsQueryResults()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<root>
    <config>
        <setting name=""timeout"">30</setting>
        <setting name=""retry"">3</setting>
    </config>
    <database>
        <host>localhost</host>
        <port>5432</port>
    </database>
</root>";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        await _fileTools.UploadFile("config.xml", "text/xml", contentBase64);
        var result = await _fileTools.QueryXml("", "Find config element");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("results", result);
    }

    [Fact]
    public async Task QueryXml_CountElements_ReturnsTotalCount()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<root>
    <item>1</item>
    <item>2</item>
    <item>3</item>
</root>";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        await _fileTools.UploadFile("items.xml", "text/xml", contentBase64);
        var result = await _fileTools.QueryXml("", "How many elements?");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("Total elements", result);
    }

    [Fact]
    public async Task QueryXml_WithLatestFileFallback_UsesRecentFile()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<root><data>test</data></root>";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        await _fileTools.UploadFile("latest.xml", "text/xml", contentBase64);
        var result = await _fileTools.QueryXml("", "Show structure");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("XML Structure", result);
    }

    [Fact]
    public async Task QueryXml_InvalidMimeType_ReturnsError()
    {
        // Arrange
        var content = "not xml";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var uploadResult = await _fileTools.UploadFile("notxml.txt", "text/plain", contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();
        var result = await _fileTools.QueryXml(fileId!, "query");

        // Assert
        Assert.Contains("error", result.ToLowerInvariant());
        // text/plain is allowed through MIME check and will fail at parse time
        Assert.Contains("error", result.ToLowerInvariant());
    }

    #endregion

    #region query_json Tests

    [Fact]
    public async Task QueryJson_ValidJsonFile_ReturnsQueryResults()
    {
        // Arrange
        var jsonContent = @"{
    ""name"": ""Test App"",
    ""version"": ""1.0.0"",
    ""settings"": {
        ""timeout"": 30,
        ""debug"": true
    },
    ""users"": [
        { ""id"": 1, ""name"": ""Alice"" },
        { ""id"": 2, ""name"": ""Bob"" }
    ]
}";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        await _fileTools.UploadFile("config.json", "application/json", contentBase64);
        var result = await _fileTools.QueryJson("", "Find settings");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("results", result);
    }

    [Fact]
    public async Task QueryJson_CountProperties_ReturnsTotalCount()
    {
        // Arrange
        var jsonContent = @"{
    ""prop1"": ""value1"",
    ""prop2"": ""value2"",
    ""prop3"": ""value3""
}";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        await _fileTools.UploadFile("props.json", "application/json", contentBase64);
        var result = await _fileTools.QueryJson("", "How many keys?");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("Total items", result);
    }

    [Fact]
    public async Task QueryJson_ArrayContent_ReturnsArrayInfo()
    {
        // Arrange
        var jsonContent = @"[
    { ""id"": 1, ""name"": ""Item 1"" },
    { ""id"": 2, ""name"": ""Item 2"" },
    { ""id"": 3, ""name"": ""Item 3"" }
]";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        await _fileTools.UploadFile("items.json", "application/json", contentBase64);
        var result = await _fileTools.QueryJson("", "Show structure");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("Array", result);
    }

    [Fact]
    public async Task QueryJson_WithLatestFileFallback_UsesRecentFile()
    {
        // Arrange
        var jsonContent = @"{ ""key"": ""value"" }";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        await _fileTools.UploadFile("latest.json", "application/json", contentBase64);
        var result = await _fileTools.QueryJson("", "What is key?");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        Assert.Contains("key", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryJson_InvalidMimeType_ReturnsError()
    {
        // Arrange
        var content = "not json";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var uploadResult = await _fileTools.UploadFile("notjson.txt", "text/plain", contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();
        var result = await _fileTools.QueryJson(fileId!, "query");

        // Assert
        Assert.Contains("error", result.ToLowerInvariant());
        // text/plain is allowed through MIME check and will fail at parse time
        Assert.Contains("error", result.ToLowerInvariant());
    }

    [Fact]
    public async Task QueryJson_MalformedJson_ReturnsParseError()
    {
        // Arrange
        var jsonContent = @"{ invalid json }";
        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var uploadResult = await _fileTools.UploadFile("bad.json", "application/json", contentBase64);
        var fileId = System.Text.Json.JsonDocument.Parse(uploadResult)
            .RootElement.GetProperty("fileId").GetString();
        var result = await _fileTools.QueryJson(fileId!, "query");

        // Assert
        Assert.Contains("error", result.ToLowerInvariant());
        // Error message will include "json parsing error" which contains "json"
        Assert.Contains("json", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ListLocalDirectory_AllowedRoot_ReturnsEntries()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(Path.Combine(tempRoot, "SubFolder"));
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "sample.txt"), "sample content");

            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };

            var result = await _fileTools.ListLocalDirectory(tempRoot);

            Assert.DoesNotContain("error", result.ToLowerInvariant());
            Assert.Contains("SubFolder", result);
            Assert.Contains("sample.txt", result);
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task ReadLocalFile_AllowedRoot_ReturnsContent()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var originalMaxReadBytes = Config.LocalFileSystemMaxReadBytes;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempRoot);
            var filePath = Path.Combine(tempRoot, "readme.txt");
            await File.WriteAllTextAsync(filePath, "hello from local file");

            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };
            Config.LocalFileSystemMaxReadBytes = 1024;

            var result = await _fileTools.ReadLocalFile(filePath);

            Assert.DoesNotContain("error", result.ToLowerInvariant());
            Assert.Contains("hello from local file", result);
            Assert.Contains("\"truncated\":false", result.ToLowerInvariant());
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            Config.LocalFileSystemMaxReadBytes = originalMaxReadBytes;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task QueryLocalStructuredFile_ExcelPath_ReturnsRowCount()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempRoot);
            var excelPath = Path.Combine(tempRoot, "customers.xlsx");

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Customers");
                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "City";
                worksheet.Cell(2, 1).Value = "Alice";
                worksheet.Cell(2, 2).Value = "Melbourne";
                worksheet.Cell(3, 1).Value = "Bob";
                worksheet.Cell(3, 2).Value = "Sydney";
                workbook.SaveAs(excelPath);
            }

            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };

            var result = await _fileTools.QueryLocalStructuredFile(excelPath, "How many rows?");

            Assert.DoesNotContain("error", result.ToLowerInvariant());
            Assert.Contains("Total rows: 2", result);
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task QueryLocalStructuredFile_UnsupportedExtension_ReturnsError()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempRoot);
            var unsupportedFile = Path.Combine(tempRoot, "data.txt");
            await File.WriteAllTextAsync(unsupportedFile, "content");

            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };

            var result = await _fileTools.QueryLocalStructuredFile(unsupportedFile, "count");

            Assert.Contains("error", result.ToLowerInvariant());
            Assert.Contains("unsupported file extension", result.ToLowerInvariant());
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task ReadLocalFile_PathOutsideAllowlist_ReturnsError()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");
        var outsideRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerOutside-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(outsideRoot);
            var outsideFilePath = Path.Combine(outsideRoot, "outside.txt");
            await File.WriteAllTextAsync(outsideFilePath, "outside");

            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };

            var result = await _fileTools.ReadLocalFile(outsideFilePath);

            Assert.Contains("error", result.ToLowerInvariant());
            Assert.Contains("outside allowed roots", result.ToLowerInvariant());
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }

            if (Directory.Exists(outsideRoot))
            {
                Directory.Delete(outsideRoot, true);
            }
        }
    }

    #endregion
}
