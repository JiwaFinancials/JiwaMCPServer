using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class CustomerTools : JiwaToolBase
{
    [BusinessTool(EntityType = "Customer", ActionType = "Search")]
    [McpServerTool(Name = "ListCustomers", ReadOnly = true), Description("List or search customers. Customers are also known as debtors, accounts, account holders, or clients. Use this tool when the user asks to show customers, find customers, or return a customer list. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [BusinessTool(EntityType = "Customer", ActionType = "Get")]
    [McpServerTool(Name = "GetCustomerDetails"), Description("Get a specific customer with full details. Customers are also known as debtors, accounts, account holders, or clients. Use this after identifying the customer you want. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCustomer(DebtorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [BusinessTool(EntityType = "Customer", ActionType = "Create")]
    [McpServerTool(Name = "CreateCustomer"), Description("Create a new customer record. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateCustomer(DebtorPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [BusinessTool(EntityType = "Customer", ActionType = "Update")]
    [McpServerTool(Name = "UpdateCustomer"), Description("Update an existing customer record. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifyCustomer(DebtorPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [BusinessTool(EntityType = "Customer", ActionType = "Delete")]
    [McpServerTool(Name = "DeleteCustomer"), Description("Delete a customer record. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteCustomer(DebtorDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, DebtorID = requestDTO.DebtorID }.ToJson();
        });

    [BusinessTool(EntityType = "Customer", ActionType = "List")]
    [McpServerTool(Name = "ListCustomerOutstandingTransactions", ReadOnly = true), Description("List outstanding customer transactions, including invoices and payments. Customers are also known as debtors, accounts, account holders, or clients. Use this when the user asks for a customer statement, open items, outstanding invoices, or payments. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [BusinessTool(EntityType = "Customer", ActionType = "List")]
    [McpServerTool(Name = "ListCustomerClassifications", ReadOnly = true), Description("List customer classifications. Debtors are also known as customers, accounts, account holders, or clients. Use this when the user asks for customer classifications or debtor classifications. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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

    [BusinessTool(EntityType = "Customer", ActionType = "List")]
    [McpServerTool(Name = "ListCustomerCategories", ReadOnly = true), Description("List customer categories. Debtors are also known as customers, accounts, account holders, or clients. Use this when the user asks for customer categories or debtor categories. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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
