using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Models;
using JiwaMcpServer.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using JiwaMcpServer.ToolDescriptions;

namespace JiwaMcpServer.Tools;

[PlannerDomain("InventoryTools")]
[McpServerToolType]
public class ProductTools : JiwaToolBase
{
    [McpServerTool(ReadOnly = true), Description(ProductToolDescriptions.QueryInventory)]
    public Task<string> QueryInventory(
        [Description("Search and filter criteria for inventory items, such as part numbers, descriptions, pricing, categories, and classifications.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_List_No_SOHQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching inventory result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same inventory query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields);
        });

    [McpServerTool, Description(ProductToolDescriptions.GetProduct)]
    public Task<string> GetProduct(InventoryGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [McpServerTool, Description(ProductToolDescriptions.CreateProduct)]
    public Task<string> CreateProduct(InventoryPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [McpServerTool, Description(ProductToolDescriptions.ModifyProduct)]
    public Task<string> ModifyProduct(ProductPatchToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var changes = PreparePatchChanges(requestDTO.Changes, nameof(InventoryPATCHRequest.InventoryID));
            var result = await JiwaApiClient.PatchPartialAsync<InventoryPATCHRequest, InventoryItem>(
                changes,
                [(nameof(InventoryPATCHRequest.InventoryID), requestDTO.InventoryID)],
                ct);
            return CreatePatchResponseJson("product", changes, result, [(nameof(InventoryPATCHRequest.InventoryID), requestDTO.InventoryID)]);
        });

    [McpServerTool, Description(ProductToolDescriptions.DeleteProduct)]
    public Task<string> DeleteProduct(ProductDeleteToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var inventoryId = await ResolveInventoryIdForDeleteAsync(requestDTO, ct);
            await JiwaApiClient.DeleteAsync(new InventoryDELETERequest { InventoryID = inventoryId }, ct);
            return new { Deleted = true, InventoryID = inventoryId, PartNo = requestDTO.PartNo }.ToJson();
        });

    [McpServerTool(ReadOnly = true), Description(ProductToolDescriptions.GetStockOnHand)]
    public Task<string> GetStockOnHand(
        [Description("Search and filter criteria for stock-on-hand records, including inventory IDs, part numbers, warehouse or bin fields, and quantity thresholds.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_IN_SOHWithBinLocationsQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching stock-on-hand result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same stock-on-hand query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields);
        });

    [McpServerTool(ReadOnly = true), Description(ProductToolDescriptions.SearchProductClassifications)]
    public Task<string> SearchProductClassifications(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.IN_ClassificationQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields);
        });

    [McpServerTool(ReadOnly = true), Description(ProductToolDescriptions.SearchProductCategories)]
    public Task<string> SearchProductCategories(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.IN_CategoriesQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields);
        });

    [McpServerTool(ReadOnly = true), Description(ProductToolDescriptions.AggregateInventoryCounts)]
    public Task<string> AggregateInventoryCounts(
        [Description("The resultHandle returned by a previous QueryInventory or related inventory search. Use this whenever possible so the full result set is aggregated, not a preview.")] string? resultHandle = null,
        [Description("The property to count by, such as InventoryClassificationID, CategoryID, PartNo, or WarehouseID.")] string groupByField = "InventoryClassificationID",
        [Description("Optional transform before grouping. Supported values: none, trim, lower, upper, firstLetter, firstLetterUpper.")] string? groupByTransform = null,
        [Description("Optional full inventory query to execute when resultHandle is not supplied.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_List_No_SOHQuery? requestDTO = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            JsonNode? structuredResult;
            if (!string.IsNullOrWhiteSpace(resultHandle))
            {
                structuredResult = StructuredResultHandleStore.Get(resultHandle);
                if (structuredResult is null)
                    throw new InvalidOperationException("The supplied resultHandle is not available.");
            }
            else
            {
                if (requestDTO is null)
                    throw new InvalidOperationException("Either a resultHandle or requestDTO is required to aggregate inventory counts.");

                var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
                structuredResult = JsonSerializer.SerializeToNode(allResults) ?? new JsonArray();
            }

            var summary = AggregateStructuredResultsByField(structuredResult, groupByField, groupByTransform);
            return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        });

    [McpServerTool, Description(ProductToolDescriptions.GetProductPicture)]
    public async Task<IEnumerable<ContentBlock>> GetProductPicture(
        [Description("The InventoryID of the product to retrieve the picture for.")] string? inventoryID = null,
        [Description("The PartNo of the product to retrieve the picture for.")] string? partNo = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inventoryID) && string.IsNullOrWhiteSpace(partNo))
            return [new TextContentBlock { Text = "Either InventoryID or PartNo must be provided." }];

        var identifier = string.IsNullOrWhiteSpace(inventoryID) ? $"PartNo '{partNo}'" : $"InventoryID '{inventoryID}'";
        try
        {
            var jsonBody = string.IsNullOrWhiteSpace(inventoryID)
                ? $"{{\"PartNo\":\"{partNo}\"}}"
                : $"{{\"InventoryID\":\"{inventoryID}\"}}";
            var (bytes, contentType) = await JiwaApiClient.GetRawBytesAsync("Inventory/Picture", jsonBody, ct);
            if (bytes == null || bytes.Length == 0)
                return [new TextContentBlock { Text = $"No picture found for product {identifier}." }];

            var mimeType = contentType ?? "image/jpeg";
            return [ImageContentBlock.FromBytes(bytes, mimeType)];
        }
        catch (WebServiceException ex)
        {
            return [new TextContentBlock { Text = $"Error retrieving picture for product {identifier}: {ex.StatusCode} {ex.StatusDescription} - {ex.ErrorMessage}" }];
        }
        catch (Exception ex)
        {
            return [new TextContentBlock { Text = $"Error retrieving picture for product {identifier}: {ex.Message}" }];
        }
    }

    private static async Task<string> ResolveInventoryIdForDeleteAsync(ProductDeleteToolRequest requestDTO, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestDTO.InventoryID))
            return requestDTO.InventoryID;

        if (string.IsNullOrWhiteSpace(requestDTO.PartNo))
            throw new InvalidOperationException("Either InventoryID or PartNo must be provided.");

        var matches = await GetAllQueryResultsAsync(
            new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_List_No_SOHQuery
            {
                PartNo = requestDTO.PartNo
            },
            Config.PageSize,
            ct);

        var exactMatches = matches
            .Where(item => string.Equals(item.PartNo, requestDTO.PartNo, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var candidates = exactMatches.Count > 0 ? exactMatches : matches;

        if (candidates.Count == 0)
            throw new InvalidOperationException($"No product found for PartNo '{requestDTO.PartNo}'.");

        if (candidates.Count > 1)
            throw new InvalidOperationException($"Multiple products matched PartNo '{requestDTO.PartNo}'. Use InventoryID instead.");

        var inventoryId = candidates[0].InventoryID;
        if (string.IsNullOrWhiteSpace(inventoryId))
            throw new InvalidOperationException($"The product matched PartNo '{requestDTO.PartNo}' but did not return an InventoryID.");

        return inventoryId;
    }
}
