using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Text.Json;
using Xunit;

namespace JiwaMcpServer.Tests;

public class BusinessToolMetadataConcurrencyTests
{
    [Fact]
    public async Task ApplyBusinessToolMetadata_HandlesConcurrentRequests()
    {
        // Arrange
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var catalog = new BusinessToolMetadataCatalog(extractor.ExtractFromAssemblies([typeof(BusinessToolMetadataConcurrencyTests).Assembly]));
        var descriptionComposer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var sharedTools = new List<Tool>
        {
            new Tool
            {
                Name = "CreateSupplierTest",
                Description = "Creates a supplier account.",
                InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
            },
            new Tool
            {
                Name = "PlainTool",
                Description = "A plain tool.",
                InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
            }
        };

        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        // Act - Simulate concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    // Each request gets its own result but with reference to shared tools list
                    var result = new ListToolsResult { Tools = sharedTools };
                    result.ApplyBusinessToolMetadata(catalog, descriptionComposer);

                    // Verify the result has tools
                    Assert.NotEmpty(result.Tools);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - No concurrent modification exceptions should occur
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ApplyBusinessToolMetadata_FiltersAliasTools()
    {
        // Arrange
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var catalog = new BusinessToolMetadataCatalog(extractor.ExtractFromAssemblies([typeof(BusinessToolMetadataConcurrencyTests).Assembly]));
        var descriptionComposer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var result = new ListToolsResult
        {
            Tools =
            [
                new Tool
                {
                    Name = "CreateSupplierTest",
                    Description = "Creates a supplier account.",
                    InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
                },
                new Tool
                {
                    Name = "GetPO", // This is an alias and should be filtered out
                    Description = "Gets a PO.",
                    InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
                },
                new Tool
                {
                    Name = "PlainTool",
                    Description = "A plain tool.",
                    InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
                }
            ]
        };

        // Act
        result.ApplyBusinessToolMetadata(catalog, descriptionComposer);

        // Assert
        Assert.Equal(2, result.Tools.Count);
        Assert.DoesNotContain(result.Tools, t => t.Name == "GetPO");
        Assert.Contains(result.Tools, t => t.Name == "CreateSupplierTest");
        Assert.Contains(result.Tools, t => t.Name == "PlainTool");
    }

    [Fact]
    public void ApplyBusinessToolMetadata_DoesNotMutateOriginalToolsList()
    {
        // Arrange
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var catalog = new BusinessToolMetadataCatalog(extractor.ExtractFromAssemblies([typeof(BusinessToolMetadataConcurrencyTests).Assembly]));
        var descriptionComposer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var originalTools = new List<Tool>
        {
            new Tool
            {
                Name = "CreateSupplierTest",
                Description = "Creates a supplier account.",
                InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
            },
            new Tool
            {
                Name = "GetPO", // This is an alias
                Description = "Gets a PO.",
                InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
            }
        };

        var originalCount = originalTools.Count;
        var result = new ListToolsResult { Tools = originalTools };

        // Act
        result.ApplyBusinessToolMetadata(catalog, descriptionComposer);

        // Assert - Original list should be unchanged
        Assert.Equal(originalCount, originalTools.Count);
        Assert.Contains(originalTools, t => t.Name == "GetPO");

        // Result should have filtered list
        Assert.Equal(1, result.Tools.Count);
        Assert.DoesNotContain(result.Tools, t => t.Name == "GetPO");

        // Result.Tools should be a different instance
        Assert.NotSame(originalTools, result.Tools);
    }
}
