using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ServiceStack;
using ServiceStack.Metadata;
using ServiceStack.Text;
using System.ComponentModel;
using System.Data;
using System.Numerics;
using static ServiceStack.Logging.TestLogger;
using static ServiceStack.Script.Lisp;

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

    [McpServerTool(Name = "SearchSalesInformation", ReadOnly = true), Description("Search and return sales information by field. Sales orders are also known as sales invoices. Lots of current and historical sales data. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages. " +
        "Results are flattened by line — collect all pages first, then de-duplicate or group if the user is asking for order-level results. " +
        "Sales orders have multiple history records; return only the latest history record per sales order. " +
        "You can use the GetSalesOrder tool to retrieve full details for a specific sales order if required.")]
    public Task<string> SearchSalesInformation(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_SalesInformation>>();
        });

    //[McpServerTool, Description("Returns ALL matching records without limits. Datasets in the thousands of rows are normal in Jiwa and should usually be returned in full when requested. De-duplicating is OK - sales orders have a number of histories, you should just return the latest history record for a sales order. This is sales information so it is a flattened list of products sold - you can group lines together to form lists of sales orders. Sales orders are also known as sales invoices. Lot's of current and historical sales data. You can use the GetSalesOrder tool to retrieve full details for a specific sales order if required. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    //public Task<string> GetAllSalesInformation(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var response = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return response.Results.ToJson<List<v_Jiwa_SalesInformation>>();
    //    });
}
