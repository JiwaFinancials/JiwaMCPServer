using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class CustomerTools : JiwaToolBase
{
    [McpServerTool(ReadOnly = true), Description("Search for customers by field. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchCustomers(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return allResults.ToJson<List<v_Jiwa_Debtor_List>>();
        });

    [McpServerTool, Description("Get full details for a customer. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCustomer(DebtorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieve outstanding transactions (invoices and payments) for a customer. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> GetCustomerTransactions(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_Transactions_ListQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return allResults.ToJson<List<v_Jiwa_Debtor_Transactions_List>>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieves a list of debtor classifications. Debtors are also known as customers, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
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
            return allResults.ToJson<List<DB_Classification>>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieves a list of debtor categories. Debtors are also known as customers, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
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
            return allResults.ToJson<List<DB_Categories>>();
        });
}
