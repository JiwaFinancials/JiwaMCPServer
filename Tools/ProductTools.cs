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
    [McpServerTool(Name = "ListProducts", ReadOnly = true), Description("List or search products.")]
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
    [McpServerTool(Name = "GetProductDetails"), Description("Get product details. Accepts InventoryID, and if not found automatically retries by treating the provided value as a part number.")]
    public Task<string> GetProduct(InventoryGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedInventoryIdAsync(
                requestDTO.InventoryID,
                ct,
                async (inventoryId, innerCt) =>
                {
                    requestDTO.InventoryID = inventoryId;
                    return await JiwaApiClient.GetAsync(requestDTO, innerCt);
                });

            return result.ToJson<InventoryItem>();
        });

    private Task<T> ExecuteWithResolvedInventoryIdAsync<T>(
        string? inventoryId,
        CancellationToken ct,
        Func<string, CancellationToken, Task<T>> executeAsync)
        => ExecuteWithResolvedIdentifierAsync(
            inventoryId,
            ct,
            executeAsync,
            ResolveInventoryIdFromIdentifierAsync);

    private async Task<string?> ResolveInventoryIdFromIdentifierAsync(string identifier, CancellationToken ct)
    {
        var trimmedIdentifier = identifier.Trim();
        if (trimmedIdentifier.Length == 0)
            return null;

        var pageSize = Config.PageSize > 0 ? Math.Min(Config.PageSize, 100) : 50;

        // Try targeted fields first, then broader text-style fields.
        var searchFields = new[]
        {
            "PartNo",
            "PartNumber",
            "Code",
            "InventoryID",
            "Search",
            "SearchText",
            "Keywords"
        };

        foreach (var field in searchFields)
        {
            var query = new v_Jiwa_Inventory_Item_ListQuery();
            if (!TrySetStringProperty(query, field, trimmedIdentifier))
                continue;

            var results = await GetAllQueryResultsAsync(query, pageSize, ct);
            if (results.Count == 0)
                continue;

            var exactMatch = results.FirstOrDefault(r =>
                string.Equals(GetStringProperty(r, "InventoryID"), trimmedIdentifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetStringProperty(r, "PartNo"), trimmedIdentifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetStringProperty(r, "PartNumber"), trimmedIdentifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetStringProperty(r, "Code"), trimmedIdentifier, StringComparison.OrdinalIgnoreCase));

            var selected = exactMatch ?? results[0];
            var resolvedInventoryId = GetStringProperty(selected, "InventoryID");
            if (!string.IsNullOrWhiteSpace(resolvedInventoryId))
                return resolvedInventoryId;
        }

        return null;
    }

    private static bool TrySetStringProperty(object target, string propertyName, string value)
    {
        var property = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
        if (property is null || !property.CanWrite || property.PropertyType != typeof(string))
            return false;

        property.SetValue(target, value);
        return true;
    }

    private static string? GetStringProperty(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
        if (property is null || property.PropertyType != typeof(string))
            return null;

        return property.GetValue(target) as string;
    }

    [BusinessTool(EntityType = "Inventory", ActionType = "Create")]
    [McpServerTool(Name = "CreateProduct"), Description("Create a product.")]
    public Task<string> CreateProduct(InventoryPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "Update")]
    [McpServerTool(Name = "UpdateProduct"), Description("Update a product.")]
    public Task<string> ModifyProduct(InventoryPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedInventoryIdAsync(
                requestDTO.InventoryID,
                ct,
                async (inventoryId, innerCt) =>
                {
                    requestDTO.InventoryID = inventoryId;
                    return await JiwaApiClient.PatchAsync(requestDTO, innerCt);
                });

            return result.ToJson<InventoryItem>();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "Delete")]
    [McpServerTool(Name = "DeleteProduct"), Description("Delete a product.")]
    public Task<string> DeleteProduct(InventoryDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var deletedInventoryId = await ExecuteWithResolvedInventoryIdAsync(
                requestDTO.InventoryID,
                ct,
                async (inventoryId, innerCt) =>
                {
                    requestDTO.InventoryID = inventoryId;
                    await JiwaApiClient.DeleteAsync(requestDTO, innerCt);
                    return inventoryId;
                });

            return new { Deleted = true, InventoryItemID = deletedInventoryId }.ToJson();
        });

    [BusinessTool(EntityType = "Inventory", ActionType = "List")]
    [McpServerTool(Name = "ListProductStockOnHand", ReadOnly = true), Description("List product stock on hand.")]
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
    [McpServerTool(Name = "ListProductClassifications", ReadOnly = true), Description("List product classifications.")]
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
    [McpServerTool(Name = "ListProductCategories", ReadOnly = true), Description("List product categories.")]
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
    [McpServerTool(Name = "GetProductPicture"), Description("Get a product picture by InventoryID or PartNo.")]
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
