using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Creditors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SupplierTools : JiwaToolBase
{
    [McpServerTool(ReadOnly = true), Description("Search for suppliers by field. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
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
            return allResults.ToJson<List<v_Jiwa_CreditorSummary>>();
        });

    [McpServerTool, Description("Get full details for a supplier. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetSupplier(CreditorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [McpServerTool, Description("Create a new supplier. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateSupplier(CreditorPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [McpServerTool, Description("Modify a supplier. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifySupplier(CreditorPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [McpServerTool, Description("Delete a supplier. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteSupplier(CreditorDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, CreditorID = requestDTO.CreditorID }.ToJson();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieves a list of creditor classifications. Creditors are also known as suppliers. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
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
            return allResults.ToJson<List<CR_Classification>>();
        });
}
