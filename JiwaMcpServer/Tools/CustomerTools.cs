using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using JiwaMcpServer.ToolDescriptions;

namespace JiwaMcpServer.Tools;

[PlannerDomain("CustomerTools")]
[McpServerToolType]
public class CustomerTools : JiwaToolBase
{
    [McpServerTool(ReadOnly = true), Description(CustomerToolDescriptions.QueryCustomers)]
    public Task<string> QueryCustomers(
        [Description("Search and filter criteria for customer or debtor records. Leave fields empty to return all matching records, or set exact-match and *Contains filters to narrow the search.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching customer result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same customer query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [McpServerTool, Description(CustomerToolDescriptions.GetCustomer)]
    public Task<string> GetCustomer(DebtorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [McpServerTool, Description(CustomerToolDescriptions.CreateCustomer)]
    public Task<string> CreateCustomer(DebtorPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [McpServerTool, Description(CustomerToolDescriptions.ModifyCustomer)]
    public Task<string> ModifyCustomer(DebtorPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [McpServerTool, Description(CustomerToolDescriptions.DeleteCustomer)]
    public Task<string> DeleteCustomer(DebtorDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, DebtorID = requestDTO.DebtorID }.ToJson();
        });

    [McpServerTool(ReadOnly = true), Description(CustomerToolDescriptions.QueryCustomerTransactions)]
    public Task<string> QueryCustomerTransactions(
        [Description("Search and filter criteria for customer transaction history, including invoices, credits, receipts, balances, and date ranges.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_Transactions_ListQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching customer transaction result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same customer transaction query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [McpServerTool(ReadOnly = true), Description(CustomerToolDescriptions.SearchCustomerClassifications)]
    public Task<string> SearchCustomerClassifications(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.DB_ClassificationQuery requestDTO,
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

    [McpServerTool(ReadOnly = true), Description(CustomerToolDescriptions.SearchCustomerCategories)]
    public Task<string> SearchCustomerCategories(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.DB_CategoriesQuery requestDTO,
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
