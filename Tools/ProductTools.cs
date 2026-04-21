using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using System.Collections;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class ProductTools(JiwaApiClient apiClient) : JiwaToolBase(apiClient)
{
    [McpServerTool, Description("Search for products by field. Products are also known as inventory items.")]
    public async Task<string> SearchProducts(
        [Description("This is a comma separated list of fields to include in the result set.")] string fields,
        [Description("This is a comma separated list of fields to order the result set by, ascending.")] string OrderBy,
        [Description("This is a comma separated list of fields to order the result set by, descending.")] string OrderByDesc,
        [Description("Product fields to search for. Products are also known as inventory items.")] string query,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAutoQueryAsync($"/Queries/IN_Main", fields, query, OrderBy, OrderByDesc, ct);
        return result.ToString();
    }

    [McpServerTool, Description("Get full details for a product by its Jiwa Inventory ID. Products are also known as inventory items.")]
    public async Task<string> GetProduct([Description("The Jiwa Inventory GUID (e.g. 00000000040000000002).")] string inventoryId,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAsync($"/Inventory/{inventoryId}", ct);
        return result.ToString();
    }

    [McpServerTool, Description("Get stock on hand quantities for a product by field. Products are also know as inventory items.")]
    public async Task<string> GetStockOnHand(
        [Description("This is a comma separated list of fields to include in the result set.")] string fields,
        [Description("This is a comma separated list of fields to order the result set by, ascending.")] string OrderBy,
        [Description("This is a comma separated list of fields to order the result set by, descending.")] string OrderByDesc,
        [Description("Product fields to search for. Products are also known as just inventory items.")] string query,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAutoQueryAsync($"/Queries/INSOHWithBinLocations", fields, query, OrderBy, OrderByDesc, ct);
        return result.ToString();
    }
}
