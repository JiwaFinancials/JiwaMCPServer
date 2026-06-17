using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class PurchaseOrderTools : JiwaToolBase
{
    [McpServerTool, Description("Get full details for a purchase order. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetPurchaseOrder(PurchaseOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [McpServerTool, Description("Create a new purchase order for a customer. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreatePurchaseOrder(PurchaseOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [McpServerTool, Description("Modify a purchase order. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifyPurchaseOrder(PurchaseOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [McpServerTool, Description("Add a product to an existing purchase order. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> AddAProductToAPurchaseOrder(PurchaseOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrderLine>();
        });

    [McpServerTool(Name = "SearchPurchaseInformation", ReadOnly = true), Description("Search and return purchase information by field. Includes part nos that were purchased. Lots of current and historical purchase data. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token. " +
        "You can use the GetPurchaseOrder tool to retrieve full details for a specific purchase order if required.")]
    public Task<string> SearchPurchaseInformation(
        v_Jiwa_PurchaseInformationQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return allResults.ToJson<List<v_Jiwa_PurchaseInformation>>();
        });

    [McpServerTool(Name = "SearchPurchaseOrders", ReadOnly = true), Description("Search and return purchase orders by field. Lots of current and historical header level purchase data. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchPurchaseOrders(
        v_Jiwa_PurchaseOrdersQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return allResults.ToJson<List<v_Jiwa_PurchaseOrders>>();
        });

    }
