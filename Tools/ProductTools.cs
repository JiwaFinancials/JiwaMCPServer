using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class ProductTools : JiwaToolBase
{
    [McpServerTool, Description("Search for products by field. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public async Task<string> SearchProducts(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_ListQuery requestDTO, CancellationToken ct = default)
    {
        var response = await JiwaApiClient.GetAsync(requestDTO, ct);
        return response.Results.ToJson<List<v_Jiwa_Inventory_Item_List>>();
    }

    [McpServerTool, Description("Get full details for a product. Products are also known as inventory items. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public async Task<string> GetProduct(InventoryGETRequest requestDTO, CancellationToken ct = default)
    {
        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
        return result.ToJson<InventoryItem>();
    }

    [McpServerTool, Description("Get stock on hand quantities for a product by field. Products are also know as inventory items.")]
    public async Task<string> GetStockOnHand(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_IN_SOHWithBinLocationsQuery requestDTO, CancellationToken ct = default)
    {
        var response = await JiwaApiClient.GetAsync(requestDTO, ct);
        return response.Results.ToJson<List<v_IN_SOHWithBinLocations>>();
    }
}
