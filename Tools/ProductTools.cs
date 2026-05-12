using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class ProductTools : JiwaToolBase
{
    [McpServerTool, Description("Search for products by field. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> SearchProducts(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_ListQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_Inventory_Item_List>>();
        });

    [McpServerTool, Description("Get full details for a product. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetProduct(InventoryGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<InventoryItem>();
        });

    [McpServerTool, Description("Get stock on hand quantities for a product by field. Products are also know as inventory items.")]
    public Task<string> GetStockOnHand(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_IN_SOHWithBinLocationsQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_IN_SOHWithBinLocations>>();
        });

    [McpServerTool, Description("Retrieves a list of inventory item classifications. Inventory items are also known as products. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> SearchProductClassifications(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.IN_ClassificationQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<IN_Classification>>();
        });

    [McpServerTool, Description("Retrieves a list of inventory item categories. Inventory items are also known as products. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> SearchProductCategories(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.IN_CategoriesQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<IN_Categories>>();
        });

    [McpServerTool, Description("Get the picture for a product (inventory item) by InventoryID. Returns the picture as image content.")]
    public async Task<IEnumerable<ModelContextProtocol.Protocol.ContentBlock>> GetProductPicture([Description("The InventoryID of the product to retrieve the picture for.")] string inventoryID, CancellationToken ct = default)
    {
        try
        {
            var jsonBody = $"{{\"InventoryID\":\"{inventoryID}\"}}";
            var (bytes, contentType) = await JiwaApiClient.GetRawBytesAsync("Inventory/Picture", jsonBody, ct);
            if (bytes == null || bytes.Length == 0)
                return [new ModelContextProtocol.Protocol.TextContentBlock { Text = $"No picture found for product '{inventoryID}'." }];

            var mimeType = contentType ?? "image/jpeg";
            return [ModelContextProtocol.Protocol.ImageContentBlock.FromBytes(bytes, mimeType)];
        }
        catch (WebServiceException ex)
        {
            return [new ModelContextProtocol.Protocol.TextContentBlock { Text = $"Error retrieving picture for product '{inventoryID}': {ex.StatusCode} {ex.StatusDescription} - {ex.ErrorMessage}" }];
        }
        catch (Exception ex)
        {
            return [new ModelContextProtocol.Protocol.TextContentBlock { Text = $"Error retrieving picture for product '{inventoryID}': {ex.Message}" }];
        }
    }
}
