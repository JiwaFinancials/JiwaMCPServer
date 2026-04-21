using System.ComponentModel;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SalesOrderTools(JiwaApiClient apiClient) : JiwaToolBase(apiClient)
{
    [McpServerTool, Description("Search for sales order by field. Sales orders are also known as sales invoices.")]
    public async Task<string> SearchSalesOrders(
        [Description("This is a comma separated list of fields to include in the result set.")] string fields,
        [Description("This is a comma separated list of fields to order the result set by, ascending.")] string OrderBy,
        [Description("This is a comma separated list of fields to order the result set by, descending.")] string OrderByDesc,
        [Description("Sales order fields to search for. Sales orders are also known as sales invoices.")] string query,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAutoQueryAsync($"/Queries/SalesOrderList", fields, query, OrderBy, OrderByDesc, ct);
        return result.ToString();
    }

    [McpServerTool, Description("Get full details for a sales order by its Jiwa Sales Order ID (including the previous and latest invoice history details). Sales orders are also known as sales invoices.")]
    public async Task<string> GetSalesOrder([Description("The Jiwa Sales Order GUID (e.g. 0025e8ead5db41279c22).")] string salesOrderId,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAsync($"/SalesOrders/{salesOrderId}", ct);
        return result.ToString();
    }

    [McpServerTool, Description("Create a new sales order for a customer.")]
    public async Task<string> CreateSalesOrder(
        [Description("Jiwa Debtor GUID for the customer.")] string debtorId,
        [Description("Order reference/description.")] string orderReference,
        CancellationToken ct = default)
    {
        var body = new
        {
            DebtorID = debtorId,
            OrderReference = orderReference
        };

        var result = await ApiClient.PostAsync("/SalesOrders", body, ct);
        return result.ToString();
    }

    [McpServerTool, Description("Add a product to an existing sales order.")]
    public async Task<string> AddAProductToASalesOrder(
        [Description("Jiwa Sales Order GUID.")] string salesOrderId,
        [Description("Jiwa Invoice History GUID for the latest invoice history.")] string currentInvoiceHistoryID,
        [Description("Jiwa Inventory GUID for the product.")] string inventoryId,
        [Description("Quantity to order.")] decimal quantity,
        CancellationToken ct = default)
    {
        var body = new  
        {
            InventoryID = inventoryId,
            OrderQty = quantity
        };

        var result = await ApiClient.PostAsync($"/SalesOrders/{salesOrderId}/Historys/{currentInvoiceHistoryID}/Lines", body, ct);
        return result.ToString();
    }
}
