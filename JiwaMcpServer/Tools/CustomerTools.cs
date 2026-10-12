using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Models;
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
    private static readonly string[] DefaultCustomerListFields = ["DebtorID", "AccountNo", "Name"];
    private static readonly string[] DefaultCustomerTransactionFields = ["TransID", "DebtorID", "AccountNo", "Name", "TranDate", "InvRemitNo", "Amount", "OutstandingAmount", "Description"];
    private static readonly string[] DefaultCustomerClassificationFields = ["DebtorClassificationID", "Description"];
    private static readonly string[] DefaultCustomerCategoryFields = ["CategoryID", "Description"];

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
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultCustomerListFields);
        });

    [McpServerTool, Description(CustomerToolDescriptions.GetCustomer)]
    public Task<string> GetCustomer(CustomerGetToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var debtorId = await ResolveDebtorIdAsync(requestDTO.DebtorID, requestDTO.Identifier, ct);
            var result = await JiwaApiClient.GetAsync(new DebtorGETRequest { DebtorID = debtorId }, ct);
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
    public Task<string> ModifyCustomer(CustomerPatchToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var debtorId = await ResolveDebtorIdAsync(requestDTO.DebtorID, requestDTO.Identifier, ct);
            var changes = PreparePatchChanges(requestDTO.Changes, nameof(DebtorPATCHRequest.DebtorID));
            var result = await JiwaApiClient.PatchPartialAsync<DebtorPATCHRequest, Debtor>(
                changes,
                [(nameof(DebtorPATCHRequest.DebtorID), debtorId)],
                ct);
            return CreatePatchResponseJson("customer", changes, result, [(nameof(DebtorPATCHRequest.DebtorID), debtorId)]);
        });

    [McpServerTool, Description(CustomerToolDescriptions.DeleteCustomer)]
    public Task<string> DeleteCustomer(CustomerDeleteToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var debtorId = await ResolveDebtorIdAsync(requestDTO.DebtorID, requestDTO.Identifier, ct);
            await JiwaApiClient.DeleteAsync(new DebtorDELETERequest { DebtorID = debtorId }, ct);
            return new { Deleted = true, DebtorID = debtorId, Identifier = requestDTO.Identifier }.ToJson();
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
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultCustomerTransactionFields);
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
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultCustomerClassificationFields);
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
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultCustomerCategoryFields);
        });

    private static async Task<string> ResolveDebtorIdAsync(string? debtorId, string? identifier, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(debtorId))
            return debtorId;

        if (string.IsNullOrWhiteSpace(identifier))
            throw new InvalidOperationException("Either DebtorID or Identifier must be provided.");

        var accountMatches = DeduplicateCustomerMatches(
            (await QueryCustomersForResolutionAsync(query => query.AccountNo = identifier, ct))
                .Concat(await QueryCustomersForResolutionAsync(query => query.AltAccountNo = identifier, ct)))
            .Where(customer => string.Equals(customer.AccountNo, identifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(customer.AltAccountNo, identifier, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var resolvedDebtorId = ResolveSingleCustomerMatch(accountMatches, identifier, "account number or customer code");
        if (!string.IsNullOrWhiteSpace(resolvedDebtorId))
            return resolvedDebtorId;

        var nameMatches = DeduplicateCustomerMatches(await QueryCustomersForResolutionAsync(query => query.Name = identifier, ct))
            .Where(customer => string.Equals(customer.Name, identifier, StringComparison.OrdinalIgnoreCase))
            .ToList();

        resolvedDebtorId = ResolveSingleCustomerMatch(nameMatches, identifier, "customer name");
        if (!string.IsNullOrWhiteSpace(resolvedDebtorId))
            return resolvedDebtorId;

        throw new InvalidOperationException($"No customer found for identifier '{identifier}'. Provide a DebtorID, exact account number, customer code, or exact customer name.");
    }

    private static async Task<List<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_List>> QueryCustomersForResolutionAsync(
        Action<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery> configureQuery,
        CancellationToken ct)
    {
        var query = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery
        {
            Fields = "DebtorID, AccountNo, AltAccountNo, Name"
        };

        configureQuery(query);
        return await GetAllQueryResultsAsync(query, Config.PageSize, ct);
    }

    private static List<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_List> DeduplicateCustomerMatches(
        IEnumerable<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_List> matches)
        => matches
            .Where(match => !string.IsNullOrWhiteSpace(match.DebtorID))
            .GroupBy(match => match.DebtorID, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

    private static string? ResolveSingleCustomerMatch(
        IReadOnlyCollection<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_List> matches,
        string identifier,
        string matchType)
    {
        if (matches.Count == 0)
            return null;

        if (matches.Count > 1)
            throw new InvalidOperationException($"Multiple customers matched {matchType} '{identifier}'. Provide the DebtorID instead.");

        var resolvedDebtorId = matches.First().DebtorID;
        if (string.IsNullOrWhiteSpace(resolvedDebtorId))
            throw new InvalidOperationException($"The customer matched {matchType} '{identifier}' but did not return a DebtorID.");

        return resolvedDebtorId;
    }
}
