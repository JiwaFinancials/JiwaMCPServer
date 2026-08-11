using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class PurchaseOrderTools : JiwaToolBase
{
    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Get")]
    [McpServerTool(Name = "GetPurchaseOrderDetails"), Description("Get a specific purchase order (PO) with full details. Use this after identifying the purchase order you want. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetPurchaseOrder(PurchaseOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Create")]
    [McpServerTool(Name = "CreatePurchaseOrder"), Description("Create a new purchase order (PO) for a supplier or creditor. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreatePurchaseOrder(PurchaseOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Create", Aliases = ["create po", "new po"])]
    [McpServerTool(Name = "CreatePO"), Description("Alias for CreatePurchaseOrder. Create a new purchase order (PO) for a supplier or creditor.")]
    public Task<string> CreatePO(PurchaseOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => CreatePurchaseOrder(requestDTO, ct);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Update")]
    [McpServerTool(Name = "UpdatePurchaseOrder"), Description("Update an existing purchase order (PO). Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifyPurchaseOrder(PurchaseOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Delete")]
    [McpServerTool(Name = "DeletePurchaseOrder"), Description("Delete a purchase order (PO). Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeletePurchaseOrder(PurchaseOrderDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, PurchaseOrderID = requestDTO.PurchaseOrderID }.ToJson();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Add")]
    [McpServerTool(Name = "AddItemToPurchaseOrder"), Description("Add a product or line item to an existing purchase order (PO). Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> AddAProductToAPurchaseOrder(PurchaseOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrderLine>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Add", Aliases = ["add po line", "add item to po"])]
    [McpServerTool(Name = "AddItemToPO"), Description("Alias for AddItemToPurchaseOrder. Add a product or line item to an existing purchase order (PO).")]
    public Task<string> AddItemToPO(PurchaseOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => AddAProductToAPurchaseOrder(requestDTO, ct);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListPurchaseHistory", ReadOnly = true), Description("List or search purchase order (PO) history by supplier, creditor, product, invoice, or other fields. Includes part numbers that were purchased. Use this when the user asks for purchase history or what was bought. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token. " +
        "You can use the GetPurchaseOrderDetails tool to retrieve full details for a specific purchase order if required.")]
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListPurchaseOrders", ReadOnly = true), Description("List or search purchase orders (POs) by supplier, creditor, order number, invoice, or other fields. Use this when the user asks to show purchase orders. " +
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    }
