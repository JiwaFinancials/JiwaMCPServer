using JiwaMcpServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class CustomerTools : JiwaToolBase
{
    private readonly ILogger<CustomerTools> _logger;

    public CustomerTools(JiwaApiClient apiClient, ILogger<CustomerTools> logger)
        : base(apiClient)
    {
        _logger = logger;
    }

    [McpServerTool, Description("Search for customers by field. Customers are also known as debtors, accounts, account holders, or clients.")]
    public async Task<string> SearchCustomers(
        [Description("This is a comma separated list of fields to include in the result set.")] string fields,
        [Description("This is a comma separated list of fields to order the result set by, ascending.")] string OrderBy,
        [Description("This is a comma separated list of fields to order the result set by, descending.")] string OrderByDesc,
        [Description("Customer fields to search for. Customers are also known as debtors, accounts, account holders, or clients.")] string query,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAutoQueryAsync($"/Queries/DebtorList", fields, query, OrderBy, OrderByDesc, ct);
        return result.ToString();
    }

    [McpServerTool, Description("Get full details for a customer by its Jiwa Debtor ID. Customers are also known as debtors, accounts, account holders, or clients.")]
    public async Task<string> GetCustomer(
        [Description("The Jiwa Debtor GUID (e.g. 00000000080000000002).")] string debtorId,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAsync($"/Debtors/{debtorId}", ct);
        return result.ToString();
    }

    [McpServerTool, Description("Retrieve outstanding transactions (invoices/credits) for a customer. Customers are also known as debtors, accounts, account holders, or clients.")]
    public async Task<string> GetCustomerTransactions(
        [Description("This is a comma separated list of fields to include in the result set.")] string fields,
        [Description("This is a comma separated list of fields to order the result set by, ascending.")] string OrderBy,
        [Description("This is a comma separated list of fields to order the result set by, descending.")] string OrderByDesc,
        [Description("Customer fields to search for. Customers are also known as debtors, accounts, account holders, or clients.")] string query,
        CancellationToken ct = default)
    {
        var result = await ApiClient.GetAutoQueryAsync($"/Queries/DebtorTransactionList", fields, query, OrderBy, OrderByDesc, ct);
        return result.ToString();
    }
}
