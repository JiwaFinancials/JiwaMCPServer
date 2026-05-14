using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using ServiceStack.Metadata;
using ServiceStack.Text;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SalesOrderTools : JiwaToolBase
{
    [McpServerTool, Description("Get full details for a sales order. Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetSalesOrder(SalesOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [McpServerTool, Description("Create a new sales order for a customer. Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateSalesOrder(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [McpServerTool, Description("Modify a sales order. Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifySalesOrder(SalesOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [McpServerTool, Description("Add a product to an existing sales order. Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> AddAProductToASalesOrder(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrderLine>();
        });

    [McpServerTool, Description("Search for sales orders by field. Sales orders are also known as sales invoices. Lot's of current and historical sales data. You can use the GetSalesOrder tool to retrieve full details for a specific sales order if required. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> SearchSalesOrders(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_SalesInformation>>();
        });
}
