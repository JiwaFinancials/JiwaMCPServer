using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Services;
using JiwaMcpServer.Tools;
using ServiceStack;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tests.Tools;

[CollectionDefinition("Inventory API", DisableParallelization = true)]
public class InventoryApiCollection;

[Collection("Inventory API")]
public class ProductToolsTests
{
    [Theory]
    [InlineData(101, null)]
    [InlineData(101, "PartNo, Description")]
    [InlineData(297, null)]
    [InlineData(297, "PartNo, Description")]
    [InlineData(501, null)]
    [InlineData(501, "PartNo, Description")]
    public async Task QueryInventory_Confirmed_ReturnsCompleteSelectedDataset(int count, string? fields)
    {
        await using var api = new InventoryApi(count);
        var tool = new ProductTools();
        var request = NewRequest(fields);
        request.Take = 10;
        request.Skip = 20;
        request.Include = "Custom";
        var warning = await tool.QueryInventory(request);
        var token = ExtractToken(warning);
        Assert.Empty(api.FetchOffsets);

        var json = await tool.QueryInventory(request, true, token);
        var root = JsonNode.Parse(json)!.AsObject();
        Assert.Equal(count, root["total"]!.GetValue<int>());
        Assert.Equal(count, root["returned"]!.GetValue<int>());
        Assert.False(root["truncated"]!.GetValue<bool>());
        Assert.True(root["fullResultSetConfirmed"]!.GetValue<bool>());
        Assert.Equal(count, root["metadata"]!["totalCount"]!.GetValue<int>());
        Assert.Equal("chat", root["metadata"]!["recommendedOutput"]!.GetValue<string>());
        Assert.Equal(count <= 500 ? "medium" : "large", root["metadata"]!["resultSetSize"]!.GetValue<string>());
        Assert.Equal(new[] { "csv", "xlsx" }, root["metadata"]!["availableExports"]!.AsArray().Select(value => value!.GetValue<string>()));
        var rows = root["results"]!.AsArray();
        Assert.Equal(count, rows.Count);
        Assert.Equal(Enumerable.Range(1, count).Select(index => $"P{index:00000}"), rows.Select(row => row!["PartNo"]!.GetValue<string>()));
        Assert.All(rows, row =>
        {
            Assert.NotNull(row!["Description"]);
            Assert.Equal(fields is null, row.AsObject().ContainsKey("InventoryID"));
            if (fields is not null)
                Assert.Equal(2, row.AsObject().Count);
        });
        Assert.Equal(Enumerable.Range(0, (count + 99) / 100).Select(index => index * 100), api.FetchOffsets);
        Assert.Equal(10, request.Take);
        Assert.Equal(20, request.Skip);
        Assert.Equal("Custom", request.Include);
        Assert.Equal(fields, request.Fields);

        var instructions = root["assistantInstructions"]!.ToJsonString();
        Assert.Contains("Answer directly with all matching records in chat", instructions);
        Assert.Contains("no pagination", instructions);
        Assert.DoesNotContain("Do not paste", instructions);
        Assert.DoesNotContain("Show only", instructions);

        AssertFullStoredResult(root, count);
        // A successful call must not authorize later calls through the shared confirmation cache.
        Assert.StartsWith("ERROR: Missing or invalid confirmation token", await tool.QueryInventory(request, true, token));
        Assert.StartsWith("ERROR: Missing or invalid confirmation token", await tool.QueryInventory(request, true));
        Assert.StartsWith("WARNING:", await tool.QueryInventory(request));
    }

    [Fact]
    public async Task QueryInventory_ResponseLargerThanClientLimit_RetainsFullResultHandle()
    {
        await using var api = new InventoryApi(501, descriptionLength: 1000);
        var tool = new ProductTools();
        var request = NewRequest("PartNo, Description");
        var token = ExtractToken(await tool.QueryInventory(request));
        var json = await tool.QueryInventory(request, true, token);
        Assert.True(json.Length > 256_000);
        AssertFullStoredResult(JsonNode.Parse(json)!.AsObject(), 501);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("expired")]
    [InlineData("mismatched")]
    public async Task QueryInventory_RejectsMissingInvalidExpiredOrMismatchedTokens(string? tokenKind)
    {
        await using var api = new InventoryApi(297);
        var request = NewRequest("PartNo");
        var tool = new ProductTools();
        string? token = tokenKind;
        if (tokenKind == "expired")
            token = ExtractToken((await ValidationHarness.Validate(request, false, null, TimeSpan.FromTicks(-1)))!);
        else if (tokenKind == "mismatched")
        {
            token = ExtractToken(await tool.QueryInventory(request));
            request.PartNoContains = "different-query";
        }

        Assert.StartsWith("ERROR: Missing or invalid confirmation token", await tool.QueryInventory(request, true, token));
        Assert.Empty(api.FetchOffsets);
        var validToken = ExtractToken(await tool.QueryInventory(request));
        var root = JsonNode.Parse(await tool.QueryInventory(request, true, validToken))!;
        Assert.Equal(297, root["returned"]!.GetValue<int>());
    }

    [Fact]
    public async Task QueryInventory_DoesNotTreatTokenOrCachedApprovalAsExplicitConfirmation()
    {
        await using var api = new InventoryApi(297);
        var request = NewRequest();
        var tool = new ProductTools();
        var token = ExtractToken(await tool.QueryInventory(request));
        Assert.StartsWith("WARNING:", await tool.QueryInventory(request, false, token));
        Assert.Null(await ValidationHarness.Validate(request, true, token));
        Assert.StartsWith("WARNING:", await tool.QueryInventory(request));
        Assert.StartsWith("ERROR: Missing or invalid confirmation token", await tool.QueryInventory(request, true));
        Assert.Empty(api.FetchOffsets);
    }

    [Fact]
    public async Task QueryInventory_FailedCountProbe_DoesNotGrantConfirmation()
    {
        await using var api = new InventoryApi(297) { CountStatusCode = 404 };
        var result = await new ProductTools().QueryInventory(NewRequest(), true, "invalid");
        Assert.StartsWith("ERROR: Unable to validate large result set confirmation", result);
        Assert.Empty(api.FetchOffsets);
    }

    [Fact]
    public async Task GetStockOnHand_Confirmed_KeepsDefaultPreview()
    {
        await using var api = new InventoryApi(297);
        var tool = new ProductTools();
        var request = new v_IN_SOHWithBinLocationsQuery { InventoryID = Guid.NewGuid().ToString("N") };
        var token = ExtractToken(await tool.GetStockOnHand(request));
        var root = JsonNode.Parse(await tool.GetStockOnHand(request, true, token))!;
        Assert.Equal(297, root["total"]!.GetValue<int>());
        Assert.Equal(10, root["returned"]!.GetValue<int>());
        Assert.True(root["truncated"]!.GetValue<bool>());
        Assert.Null(root["fullResultSetConfirmed"]);
        Assert.Equal("preview", root["metadata"]!["recommendedOutput"]!.GetValue<string>());
    }

    [Fact]
    public async Task QueryInventory_ExplicitConfirmationWithLowProbeCount_StillRequiresValidToken()
    {
        await using var api = new InventoryApi(297) { ProbeTotal = 100 };
        var result = await new ProductTools().QueryInventory(NewRequest(), true);
        Assert.StartsWith("ERROR: Missing or invalid confirmation token", result);
        Assert.Empty(api.FetchOffsets);
    }

    [Fact]
    public async Task QueryInventory_WithoutExplicitConfirmation_KeepsDefaultPreviewIfCountChanges()
    {
        await using var api = new InventoryApi(297) { ProbeTotal = 100 };
        var root = JsonNode.Parse(await new ProductTools().QueryInventory(NewRequest("PartNo")))!;
        Assert.Equal(297, root["total"]!.GetValue<int>());
        Assert.Equal(10, root["returned"]!.GetValue<int>());
        Assert.True(root["truncated"]!.GetValue<bool>());
        Assert.Null(root["fullResultSetConfirmed"]);
        Assert.Equal("preview", root["metadata"]!["recommendedOutput"]!.GetValue<string>());
    }

    private static v_Jiwa_Inventory_Item_List_No_SOHQuery NewRequest(string? fields = null)
        => new() { PartNoContains = Guid.NewGuid().ToString("N"), Fields = fields };

    private static string ExtractToken(string warning)
    {
        Assert.StartsWith("WARNING:", warning);
        var match = Regex.Match(warning, "confirmationToken='([a-f0-9]+)'");
        Assert.True(match.Success, warning);
        return match.Groups[1].Value;
    }

    private static void AssertFullStoredResult(JsonObject root, int count)
    {
        var store = new ToolResultStore();
        var handle = root["resultHandle"]!.GetValue<string>();
        var stored = Assert.IsType<JsonArray>(store.GetStructuredResultByHandle(handle));
        Assert.Equal(count, stored.Count);
        Assert.True(JsonNode.DeepEquals(root["results"], stored));
        var conversationId = Guid.NewGuid().ToString("N");
        store.Store(new ToolExecutionContextUpdateRequest
        {
            ConversationId = conversationId,
            ToolName = "QueryInventory",
            Successful = true,
            ToolResultText = new JsonObject
            {
                ["resultHandle"] = handle,
                ["results"] = new JsonArray(root["results"]![0]!.DeepClone())
            }.ToJsonString()
        });
        var recovered = Assert.IsType<JsonArray>(store.GetLatestStructuredResult(conversationId, "QueryInventory"));
        Assert.True(JsonNode.DeepEquals(stored, recovered));
    }

    private sealed class ValidationHarness : JiwaToolBase
    {
        public static Task<string?> Validate(v_Jiwa_Inventory_Item_List_No_SOHQuery request, bool confirmed, string? token, TimeSpan? lifetime = null)
            => ValidateLargeResultSetConfirmationAsync(request, confirmed, token, CancellationToken.None, confirmationTokenLifetime: lifetime);
    }

    private sealed class InventoryApi : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly Task serverTask;
        private readonly string? originalUrl = Config.JiwaAPIURL;
        private readonly int originalPageSize = Config.PageSize;
        private readonly int total;
        private readonly int descriptionLength;
        private volatile bool stopping;
        public ConcurrentQueue<int> FetchOffsets { get; } = new();
        public int CountStatusCode { get; init; } = 200;
        public int? ProbeTotal { get; init; }

        public InventoryApi(int total, int descriptionLength = 10)
        {
            this.total = total;
            this.descriptionLength = descriptionLength;
            using var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();
            var url = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(url);
            listener.Start();
            Config.JiwaAPIURL = url;
            Config.PageSize = 100;
            serverTask = ServeAsync();
        }

        private async Task ServeAsync()
        {
            try
            {
                while (!stopping)
                {
                    var context = await listener.GetContextAsync();
                    var query = context.Request.QueryString;
                    var take = int.TryParse(query["Take"], out var parsedTake) ? parsedTake : 100;
                    var skip = int.TryParse(query["Skip"], out var parsedSkip) ? parsedSkip : 0;
                    var rows = new JsonArray();
                    if (take != 0)
                    {
                        FetchOffsets.Enqueue(skip);
                        for (var index = skip + 1; index <= Math.Min(total, skip + take); index++)
                            rows.Add(new JsonObject
                            {
                                ["InventoryID"] = $"ID{index}",
                                ["PartNo"] = $"P{index:00000}",
                                ["Description"] = new string('x', descriptionLength),
                                ["SellPrice"] = index
                            });
                    }
                    context.Response.StatusCode = take == 0 ? CountStatusCode : 200;
                    context.Response.ContentType = "application/json";
                    var payload = new JsonObject { ["Total"] = take == 0 ? ProbeTotal ?? total : total, ["Results"] = rows };
                    var bytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) when (stopping) { }
            catch (ObjectDisposedException) when (stopping) { }
        }

        public async ValueTask DisposeAsync()
        {
            stopping = true;
            listener.Close();
            try
            {
                await serverTask;
            }
            finally
            {
                Config.JiwaAPIURL = originalUrl;
                Config.PageSize = originalPageSize;
            }
        }
    }
}
