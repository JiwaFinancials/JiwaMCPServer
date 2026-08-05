using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class ProductTools : JiwaToolBase
{
    [BusinessTool(EntityType = "Inventory", ActionType = "Search")]
    [McpServerTool(Name = "ListProducts", ReadOnly = true), Description("List or search products. Products are also known as inventory items. Use this tool when the user asks to show products, find products, or return a product list. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchProducts(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_ListQuery requestDTO,
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

    [BusinessTool(EntityType = "Inventory", ActionType = "Get")]
    [McpServerTool(Name = "GetProductDetails"), Description("Get a specific product with full details. Products are also known as inventory items. Use this after identifying the product you want. Do not use this for images; use GetProductPicture if the user wants the product picture. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetProduct(InventoryGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "Create")]
    [McpServerTool(Name = "CreateProduct"), Description("Create a new product record. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateProduct(InventoryPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "Update")]
    [McpServerTool(Name = "UpdateProduct"), Description("Update an existing product record. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifyProduct(InventoryPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "Delete")]
    [McpServerTool(Name = "DeleteProduct"), Description("Delete a product record. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteProduct(InventoryDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, InventoryItemID = requestDTO.InventoryID }.ToJson();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "List")]
    [McpServerTool(Name = "ListProductStockOnHand", ReadOnly = true), Description("List product stock on hand quantities. Products are also known as inventory items. Use this when the user asks for stock on hand, inventory quantities, or bin location stock. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> GetStockOnHand(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_IN_SOHWithBinLocationsQuery requestDTO,
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

    [BusinessTool(EntityType = "Inventory", ActionType = "List")]
    [McpServerTool(Name = "ListProductClassifications", ReadOnly = true), Description("List product classifications. Inventory items are also known as products. Use this when the user asks for product classifications or inventory item classifications. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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

    [BusinessTool(EntityType = "Inventory", ActionType = "List")]
    [McpServerTool(Name = "ListProductCategories", ReadOnly = true), Description("List product categories. Inventory items are also known as products. Use this when the user asks for product categories or inventory item categories. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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

    [BusinessTool(EntityType = "Inventory", ActionType = "Get")]
    [McpServerTool(Name = "GetProductPicture"), Description("Get the picture for a product by InventoryID or PartNo. Products are also known as inventory items. Returns image content.")]
    public async Task<IEnumerable<ModelContextProtocol.Protocol.ContentBlock>> GetProductPicture(
        [Description("The InventoryID of the product to retrieve the picture for.")] string? inventoryID = null,
        [Description("The PartNo of the product to retrieve the picture for.")] string? partNo = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inventoryID) && string.IsNullOrWhiteSpace(partNo))
            return [new ModelContextProtocol.Protocol.TextContentBlock { Text = "Either InventoryID or PartNo must be provided." }];

        var identifier = string.IsNullOrWhiteSpace(inventoryID) ? $"PartNo '{partNo}'" : $"InventoryID '{inventoryID}'";
        try
        {
            var jsonBody = string.IsNullOrWhiteSpace(inventoryID)
                ? $"{{\"PartNo\":\"{partNo}\"}}"
                : $"{{\"InventoryID\":\"{inventoryID}\"}}";
            var (bytes, contentType) = await JiwaApiClient.GetRawBytesAsync("Inventory/Picture", jsonBody, ct);
            if (bytes == null || bytes.Length == 0)
                return [new ModelContextProtocol.Protocol.TextContentBlock { Text = $"No picture found for product {identifier}." }];

            var mimeType = contentType ?? "image/jpeg";
            return [ModelContextProtocol.Protocol.ImageContentBlock.FromBytes(bytes, mimeType)];
        }
        catch (WebServiceException ex)
        {
            return [new ModelContextProtocol.Protocol.TextContentBlock { Text = $"Error retrieving picture for product {identifier}: {ex.StatusCode} {ex.StatusDescription} - {ex.ErrorMessage}" }];
        }
        catch (Exception ex)
        {
            return [new ModelContextProtocol.Protocol.TextContentBlock { Text = $"Error retrieving picture for product {identifier}: {ex.Message}" }];
        }
    }
}
