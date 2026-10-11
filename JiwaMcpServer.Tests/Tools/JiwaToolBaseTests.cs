using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Services;
using JiwaMcpServer.Tools;
using ServiceStack;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tests.Tools;

public class JiwaToolBaseTests
{
    public static TheoryData<int, int, bool, bool, bool, string, string, bool, string[]> SearchResponseMetadataCases => new()
    {
        { 0, 0, false, false, false, "small", "chat", false, ["Result set contains 0 records.", "Displaying all records in chat is acceptable.", "Answer directly with the matching records instead of offering export or asking whether to show a sample."] },
        { 1, 1, false, false, false, "small", "chat", false, ["Result set contains 1 records.", "Displaying all records in chat is acceptable.", "Answer directly with the matching records instead of offering export or asking whether to show a sample."] },
        { 100, 100, false, false, false, "small", "chat", false, ["Result set contains 100 records.", "Displaying all records in chat is acceptable.", "Answer directly with the matching records instead of offering export or asking whether to show a sample."] },
        { 101, 10, true, true, true, "medium", "preview", true, ["Result set contains 101 records.", "Show only a short preview (for example, the first 10 rows).", "Do not paste the full dataset into chat.", "For grouped counts, totals, or summaries, use the stored result handle with AggregateInventoryCounts rather than counting rows from the preview.", "Offer CSV and XLSX downloads.", "Provide pagination if supported."] },
        { 500, 10, true, true, true, "medium", "preview", true, ["Result set contains 500 records.", "Show only a short preview (for example, the first 10 rows).", "Do not paste the full dataset into chat.", "For grouped counts, totals, or summaries, use the stored result handle with AggregateInventoryCounts rather than counting rows from the preview.", "Offer CSV and XLSX downloads.", "Provide pagination if supported."] },
        { 501, 10, true, true, true, "large", "file", true, ["Result set contains 501 records.", "Do not render the entire dataset in chat.", "Show only a very small preview (for example, the first 10 rows).", "Do not paste the full dataset into chat.", "For grouped counts, totals, or summaries, use the stored result handle with AggregateInventoryCounts rather than counting rows from the preview.", "Recommend CSV or XLSX download.", "Avoid generating thousands of rows in the response."] },
        { 5000, 10, true, true, true, "large", "file", true, ["Result set contains 5000 records.", "Do not render the entire dataset in chat.", "Show only a very small preview (for example, the first 10 rows).", "Do not paste the full dataset into chat.", "For grouped counts, totals, or summaries, use the stored result handle with AggregateInventoryCounts rather than counting rows from the preview.", "Recommend CSV or XLSX download.", "Avoid generating thousands of rows in the response."] }
    };

    [Theory]
    [MemberData(nameof(SearchResponseMetadataCases))]
    public async Task InvokeToolAsync_AddsMetadataAndAssistantInstructionsToSearchResponses(
        int totalCount,
        int expectedReturnedCount,
        bool expectedTruncated,
        bool expectedHasMessage,
        bool expectedHasResultHandle,
        string expectedResultSetSize,
        string expectedRecommendedOutput,
        bool expectedShouldOfferDownload,
        string[] expectedAssistantInstructions)
    {
        var items = Enumerable.Range(1, totalCount)
            .Select(index => new { PartNo = $"P{index:00000}", Description = new string('x', 10) })
            .ToList();

        var json = await ToolBaseHarness.InvokeAsync(() => Task.FromResult(ToolBaseHarness.Render(items, 100)));
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(totalCount, doc.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(expectedReturnedCount, doc.RootElement.GetProperty("returned").GetInt32());
        Assert.Equal(expectedTruncated, doc.RootElement.GetProperty("truncated").GetBoolean());
        Assert.Equal(expectedReturnedCount, doc.RootElement.GetProperty("results").GetArrayLength());

        if (expectedHasMessage)
        {
            Assert.True(doc.RootElement.TryGetProperty("message", out var messageElement));
            Assert.False(string.IsNullOrWhiteSpace(messageElement.GetString()));
        }
        else
        {
            Assert.False(doc.RootElement.TryGetProperty("message", out _));
        }

        if (expectedHasResultHandle)
        {
            Assert.True(doc.RootElement.TryGetProperty("resultHandle", out var resultHandleElement));
            Assert.False(string.IsNullOrWhiteSpace(resultHandleElement.GetString()));
        }
        else
        {
            Assert.False(doc.RootElement.TryGetProperty("resultHandle", out _));
        }

        AssertMetadata(doc.RootElement, totalCount, expectedResultSetSize, expectedRecommendedOutput, expectedShouldOfferDownload);
        AssertAssistantInstructions(doc.RootElement, expectedAssistantInstructions);
    }

    [Fact]
    public void CreateSearchResponseJson_ForPreviewedResults_AllowsConversationStoreToRecoverFullDataset()
    {
        var items = Enumerable.Range(1, 101)
            .Select(index => new { PartNo = $"P{index:00000}", Description = new string('x', 10) })
            .ToList();

        var json = ToolBaseHarness.Render(items, 100);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("truncated").GetBoolean());
        Assert.Equal(10, doc.RootElement.GetProperty("results").GetArrayLength());
        Assert.True(doc.RootElement.TryGetProperty("resultHandle", out var resultHandleElement));
        Assert.False(string.IsNullOrWhiteSpace(resultHandleElement.GetString()));

        var toolResultStore = new ToolResultStore();
        toolResultStore.Store(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "chat-123",
            UserMessage = "Give me a list of parts.",
            ToolName = "QueryInventory",
            Successful = true,
            ToolResultText = json
        });

        var storedResult = Assert.IsType<JsonArray>(toolResultStore.GetLatestStructuredResult("chat-123", "QueryInventory"));
        Assert.Equal(101, storedResult.Count);
    }

    [Fact]
    public void CreateSearchResponseJson_WithRequestedFields_FiltersSearchRows()
    {
        var items = new[]
        {
            new
            {
                InventoryClassificationID = "0000000033000000001M",
                Description = "Labour - Technology",
                DefaultClassification = false,
                WebEnabled = false,
                LastSavedDateTime = "/Date(-62135596800000)/"
            }
        };

        var json = ToolBaseHarness.RenderWithRequestedFields(items, 100, "InventoryClassificationID, Description");
        using var doc = JsonDocument.Parse(json);

        var result = doc.RootElement.GetProperty("results")[0];
        Assert.Equal("0000000033000000001M", result.GetProperty("InventoryClassificationID").GetString());
        Assert.Equal("Labour - Technology", result.GetProperty("Description").GetString());
        Assert.False(result.TryGetProperty("DefaultClassification", out _));
        Assert.False(result.TryGetProperty("WebEnabled", out _));
        Assert.False(result.TryGetProperty("LastSavedDateTime", out _));
    }

    [Fact]
    public void AggregateStructuredResultsByField_CountsRowsFromFullStoredResult()
    {
        var rows = new JsonArray
        {
            new JsonObject { ["InventoryClassificationID"] = "A", ["PartNo"] = "ABC-001" },
            new JsonObject { ["InventoryClassificationID"] = "A", ["PartNo"] = "ABC-002" },
            new JsonObject { ["InventoryClassificationID"] = "B", ["PartNo"] = "XYZ-001" },
            new JsonObject { ["InventoryClassificationID"] = "B", ["PartNo"] = "XYZ-002" },
            new JsonObject { ["InventoryClassificationID"] = "B", ["PartNo"] = "XYZ-003" }
        };

        var summary = ToolBaseHarness.AggregateByField(rows, "InventoryClassificationID");
        using var doc = JsonDocument.Parse(summary.ToJsonString());

        Assert.Equal("InventoryClassificationID", doc.RootElement.GetProperty("groupByField").GetString());
        Assert.Equal(5, doc.RootElement.GetProperty("totalRows").GetInt32());

        var groupCounts = doc.RootElement.GetProperty("groups")
            .EnumerateArray()
            .ToDictionary(item => item.GetProperty("key").GetString()!, item => item.GetProperty("count").GetInt32());

        Assert.Equal(2, groupCounts["A"]);
        Assert.Equal(3, groupCounts["B"]);
    }

    [Fact]
    public async Task InvokeToolAsync_AddsMetadataToExistingCollectionResponsesUsingDeclaredTotal()
    {
        var payload = new
        {
            Total = 101,
            Returned = 2,
            Results = new[]
            {
                new { LineID = 1, Description = "A" },
                new { LineID = 2, Description = "B" }
            }
        };

        var json = await ToolBaseHarness.InvokeAsync(() => Task.FromResult(payload.ToJson()));
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(101, doc.RootElement.GetProperty("Total").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("Returned").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("Results").GetArrayLength());
        AssertMetadata(doc.RootElement, 101, "medium", "preview", true);
        AssertAssistantInstructions(doc.RootElement, ["Result set contains 101 records.", "Show only a short preview (for example, the first 10 rows).", "Do not paste the full dataset into chat.", "For grouped counts, totals, or summaries, use the stored result handle with AggregateInventoryCounts rather than counting rows from the preview.", "Offer CSV and XLSX downloads.", "Provide pagination if supported."]);
    }

    private static void AssertMetadata(JsonElement root, int expectedTotalCount, string expectedResultSetSize, string expectedRecommendedOutput, bool expectedShouldOfferDownload)
    {
        var metadata = root.GetProperty("metadata");
        Assert.Equal(expectedTotalCount, metadata.GetProperty("totalCount").GetInt32());
        Assert.Equal(expectedResultSetSize, metadata.GetProperty("resultSetSize").GetString());
        Assert.Equal(expectedRecommendedOutput, metadata.GetProperty("recommendedOutput").GetString());
        Assert.Equal(expectedShouldOfferDownload, metadata.GetProperty("shouldOfferDownload").GetBoolean());

        var availableExports = metadata.GetProperty("availableExports").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Assert.Equal(["csv", "xlsx"], availableExports);
    }

    private static void AssertAssistantInstructions(JsonElement root, string[] expectedAssistantInstructions)
    {
        var instructions = root.GetProperty("assistantInstructions").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Assert.Equal(expectedAssistantInstructions, instructions);
    }

    private sealed class ToolBaseHarness : JiwaToolBase
    {
        public static string Render<T>(IReadOnlyCollection<T> results, int pageSize)
            => CreateSearchResponseJson(results, pageSize);

        public static string RenderWithRequestedFields<T>(IReadOnlyCollection<T> results, int pageSize, string? requestedFields)
            => CreateSearchResponseJson(results, pageSize, requestedFields);

        public static JsonNode AggregateByField(JsonNode structuredResult, string groupByField, string? groupByTransform = null)
            => AggregateStructuredResultsByField(structuredResult, groupByField, groupByTransform);

        public static Task<string> InvokeAsync(Func<Task<string>> action)
            => InvokeToolAsync(action);
    }
}
