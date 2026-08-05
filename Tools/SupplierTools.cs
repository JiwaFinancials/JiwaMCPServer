using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Creditors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SupplierTools : JiwaToolBase
{
    [BusinessTool(EntityType = "Supplier", ActionType = "Search")]
    [McpServerTool(Name = "ListSuppliers", ReadOnly = true), Description("List or search suppliers. Suppliers are also known as creditors. Use this tool when the user asks to show suppliers, find suppliers, or return a supplier list. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchSuppliers(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery requestDTO,
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

    [BusinessTool(EntityType = "Supplier", ActionType = "Get")]
    [McpServerTool(Name = "GetSupplierDetails"), Description("Get a specific supplier with full details. Suppliers are also known as creditors. Use this after identifying the supplier you want. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetSupplier(CreditorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Create")]
    [McpServerTool(Name = "CreateSupplier"), Description("Create a new supplier record. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateSupplier(CreditorPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Update")]
    [McpServerTool(Name = "UpdateSupplier"), Description("Update an existing supplier record. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifySupplier(CreditorPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Delete")]
    [McpServerTool(Name = "DeleteSupplier"), Description("Delete a supplier record. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteSupplier(CreditorDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, CreditorID = requestDTO.CreditorID }.ToJson();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "List")]
    [McpServerTool(Name = "ListSupplierClassifications", ReadOnly = true), Description("List supplier classifications. Creditors are also known as suppliers. Use this when the user asks for supplier classifications or creditor classifications. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchCreditorClassifications(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.CR_ClassificationQuery requestDTO,
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
