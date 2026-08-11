using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Linq;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SalesOrderTools : JiwaToolBase
{
    [BusinessTool(EntityType = "SalesOrder", ActionType = "Get")]
    [McpServerTool(Name = "GetSalesOrderDetails"), Description("Get a specific sales order (SO) with full details. Sales orders are also known as sales invoices. Use this after identifying the sales order you want. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetSalesOrder(SalesOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Create")]
    [McpServerTool(Name = "CreateSalesOrder"), Description("Create a new sales order (SO) for a customer. Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateSalesOrder(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Create", Aliases = ["create so", "new so"])]
    [McpServerTool(Name = "CreateSO"), Description("Alias for CreateSalesOrder. Create a new sales order (SO) for a customer.")]
    public Task<string> CreateSO(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => CreateSalesOrder(requestDTO, ct);

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Update")]
    [McpServerTool(Name = "UpdateSalesOrder"), Description("Update an existing sales order (SO). Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifySalesOrder(SalesOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Add")]
    [McpServerTool(Name = "AddItemToSalesOrder"), Description("Add a product or line item to an existing sales order (SO). Sales orders are also known as sales invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. If InvoiceHistoryID is omitted, the current history (highest HistoryNo for that InvoiceID) is used.")]
    public Task<string> AddAProductToASalesOrder(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(requestDTO.InvoiceHistoryID))
            {
                if (string.IsNullOrWhiteSpace(requestDTO.InvoiceID))
                {
                    return new
                    {
                        success = false,
                        error = "InvoiceID is required when InvoiceHistoryID is not provided."
                    }.ToJson();
                }

                var currentHistory = await ResolveCurrentHistoryAsync(requestDTO.InvoiceID, ct);
                if (string.IsNullOrWhiteSpace(currentHistory?.InvoiceHistoryID))
                {
                    return new
                    {
                        success = false,
                        error = $"I couldn't determine the current history for sales order '{requestDTO.InvoiceID}'."
                    }.ToJson();
                }

                requestDTO.InvoiceHistoryID = currentHistory.InvoiceHistoryID;
            }

            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrderLine>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Add", Aliases = ["add so line", "add item to so"])]
    [McpServerTool(Name = "AddItemToSO"), Description("Alias for AddItemToSalesOrder. Add a product or line item to an existing sales order (SO).")]
    public Task<string> AddItemToSO(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => AddAProductToASalesOrder(requestDTO, ct);

    private async Task<SalesOrderHistory?> ResolveCurrentHistoryAsync(string invoiceId, CancellationToken ct)
    {
        var salesOrder = await JiwaApiClient.GetAsync(new SalesOrderGETRequest { InvoiceID = invoiceId }, ct);
        return salesOrder?.Histories?
            .Where(history => history != null && !string.IsNullOrWhiteSpace(history.InvoiceHistoryID))
            .OrderByDescending(history => history.HistoryNo ?? int.MinValue)
            .FirstOrDefault();
    }

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListSalesHistory", ReadOnly = true), Description("List or search sales history by customer, product, invoice, or other fields. Includes part numbers that were sold. Sales orders are also known as sales invoices. Use this when the user asks for sales history or what was sold. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token. " +
        "You can use the GetSalesOrderDetails tool to retrieve full details for a specific sales order if required.")]
    public Task<string> SearchSalesInformation(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery requestDTO,
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

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListSalesOrders", ReadOnly = true), Description("List or search sales orders (SOs) by customer, order number, invoice, or other fields. Sales orders are also known as sales invoices. Use this when the user asks to show sales orders or sales invoices. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token. " +
        "You can use the GetSalesOrderDetails tool to retrieve full details for a specific sales order if required.")]
    public Task<string> SearchSalesOrders(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesOrdersQuery requestDTO,
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

}
