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
    [McpServerTool, Description("Search for customers by field. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
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

    [McpServerTool, Description("Retrieve outstanding transactions (invoices/credits) for a customer. Customers are also known as debtors, accounts, account holders, or clients. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCustomerTransactions(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_Transactions_ListQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_Debtor_Transactions_List>>();
        });
}