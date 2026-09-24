using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
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
    public Task<string> ModifyProduct(InventoryPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [McpServerTool, Description(ProductToolDescriptions.DeleteProduct)]
    public Task<string> DeleteProduct(InventoryDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, InventoryItemID = requestDTO.InventoryID }.ToJson();
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
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
}