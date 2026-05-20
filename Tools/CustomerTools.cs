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
    [McpServerTool(ReadOnly = true), Description("Search for customers by field. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages.")]
    public Task<string> SearchCustomers(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery requestDTO,  CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_Debtor_List>>();
        });

    [McpServerTool, Description("Get full details for a customer. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCustomer(DebtorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Debtor>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieve outstanding transactions (invoices/credits) for a customer. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages.")]
    public Task<string> GetCustomerTransactions(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_Transactions_ListQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_Debtor_Transactions_List>>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieves a list of debtor classifications. Debtors are also known as customers, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages.")]
    public Task<string> SearchCustomerClassifications(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.DB_ClassificationQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<DB_Classification>>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieves a list of debtor categories. Debtors are also known as customers, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages.")]
    public Task<string> SearchCustomerCategories(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.DB_CategoriesQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<DB_Categories>>();
        });
}