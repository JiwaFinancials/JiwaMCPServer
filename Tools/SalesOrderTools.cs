using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SalesOrderTools : JiwaToolBase
{
    [BusinessTool(EntityType = "SalesOrder", ActionType = "Get")]
    [McpServerTool(Name = "GetSalesOrderDetails"), Description("Get sales order details by InvoiceID or order number.")]
    public Task<string> GetSalesOrder(SalesOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedSalesOrderIdAsync(
                requestDTO.InvoiceID,
                ct,
                async (invoiceId, innerCt) => await JiwaApiClient.GetAsync(
                    new SalesOrderGETRequest { InvoiceID = invoiceId },
                    innerCt));

            return result.ToJson<SalesOrder>();
        });

    private static Task<T> ExecuteWithResolvedSalesOrderIdAsync<T>(
        string? invoiceId,
        CancellationToken ct,
        Func<string, CancellationToken, Task<T>> executeAsync)
        => ExecuteWithResolvedIdentifierAsync(
            invoiceId,
            ct,
            executeAsync,
            TryResolveSalesOrderIdFromDocumentNumberAsync);

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Create")]
    [McpServerTool(Name = "CreateSalesOrder"), Description("Create a sales order.")]
    public Task<string> CreateSalesOrder(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Create", Aliases = ["create so", "new so"])]
    [McpServerTool(Name = "CreateSO"), Description("Alias for CreateSalesOrder.")]
    public Task<string> CreateSO(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => CreateSalesOrder(requestDTO, ct);

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Update")]
    [McpServerTool(Name = "UpdateSalesOrder"), Description("Update a sales order.")]
    public Task<string> ModifySalesOrder(SalesOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedSalesOrderIdAsync(
                requestDTO.InvoiceID,
                ct,
                async (invoiceId, innerCt) =>
                {
                    requestDTO.InvoiceID = invoiceId;
                    return await JiwaApiClient.PatchAsync(requestDTO, innerCt);
                });

            return result.ToJson<SalesOrder>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Add")]
    [McpServerTool(Name = "AddItemToSalesOrder"), Description("Add a line to an existing sales order.")]
    public Task<string> AddAProductToASalesOrder(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(requestDTO.InvoiceHistoryID))
            {
                if (string.IsNullOrWhiteSpace(requestDTO.InvoiceID))
                {
                    return new
                    {
                        success = false,
                        error = "InvoiceID is required when InvoiceHistoryID is not provided."
                    }.ToJson();
                }

                var currentHistory = await ResolveCurrentHistoryAsync(requestDTO.InvoiceID, ct);
                if (string.IsNullOrWhiteSpace(currentHistory?.InvoiceHistoryID))
                {
                    return new
                    {
                        success = false,
                        error = $"I couldn't determine the current history for sales order '{requestDTO.InvoiceID}'."
                    }.ToJson();
                }

                requestDTO.InvoiceHistoryID = currentHistory.InvoiceHistoryID;
            }

            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrderLine>();
        });

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Add", Aliases = ["add so line", "add item to so"])]
    [McpServerTool(Name = "AddItemToSO"), Description("Alias for AddItemToSalesOrder.")]
    public Task<string> AddItemToSO(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => AddAProductToASalesOrder(requestDTO, ct);

    private async Task<SalesOrderHistory?> ResolveCurrentHistoryAsync(string invoiceId, CancellationToken ct)
    {
        var effectiveInvoiceId = invoiceId?.Trim();
        if (string.IsNullOrWhiteSpace(effectiveInvoiceId))
            return null;

        SalesOrder? salesOrder;
        try
        {
            salesOrder = await JiwaApiClient.GetAsync(new SalesOrderGETRequest { InvoiceID = effectiveInvoiceId }, ct);
        }
        catch (WebServiceException ex) when (ShouldRetryIdentifierResolution(ex))
        {
            var resolvedInvoiceId = await TryResolveSalesOrderIdFromDocumentNumberAsync(effectiveInvoiceId, ct);
            if (string.IsNullOrWhiteSpace(resolvedInvoiceId) ||
                string.Equals(resolvedInvoiceId, effectiveInvoiceId, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            salesOrder = await JiwaApiClient.GetAsync(new SalesOrderGETRequest { InvoiceID = resolvedInvoiceId }, ct);
        }

        return salesOrder?.Histories?
            .Where(history => history != null && !string.IsNullOrWhiteSpace(history.InvoiceHistoryID))
            .OrderByDescending(history => history.HistoryNo ?? int.MinValue)
            .FirstOrDefault();
    }

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListSalesHistory", ReadOnly = true), Description("List sales history.")]
    public Task<string> SearchSalesInformation(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery requestDTO,
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

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListSalesOrders", ReadOnly = true), Description("List or search sales orders.")]
    public Task<string> SearchSalesOrders(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesOrdersQuery requestDTO,
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

    [BusinessTool(EntityType = "SalesOrder", ActionType = "Resolve", Aliases = ["resolve sales order number", "resolve invoice number", "sales order no to id", "invoice no to id", "document number to invoice id"])]
    [McpServerTool(Name = "ResolveSalesOrderId", ReadOnly = true), Description("Resolve a sales order or invoice document number to InvoiceID.")]
    public Task<string> ResolveSalesOrderId(string documentNumber, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);

            var trimmedDocumentNumber = documentNumber.Trim();
            var resolvedInvoiceId = await TryResolveSalesOrderIdFromDocumentNumberAsync(trimmedDocumentNumber, ct);
            if (string.IsNullOrWhiteSpace(resolvedInvoiceId))
            {
                return new
                {
                    success = false,
                    error = $"Unable to resolve sales order/invoice document number '{trimmedDocumentNumber}' to a unique InvoiceID.",
                    hint = "Use ListSalesOrders with InvoiceNo or OrderNo to locate the correct InvoiceID, then retry."
                }.ToJson();
            }

            return new
            {
                success = true,
                documentNo = trimmedDocumentNumber,
                invoiceID = resolvedInvoiceId
            }.ToJson();
        });

    private static async Task<string?> TryResolveSalesOrderIdFromDocumentNumberAsync(string documentNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return null;

        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in BuildSalesDocumentNumberCandidates(documentNumber))
        {
            await AddSalesOrderMatchesAsync(new v_Jiwa_SalesOrdersQuery
            {
                InvoiceNo = candidate,
                Take = 2,
                Skip = 0
            }, matches, ct);

            await AddSalesOrderMatchesAsync(new v_Jiwa_SalesOrdersQuery
            {
                OrderNo = candidate,
                Take = 2,
                Skip = 0
            }, matches, ct);

            if (matches.Count > 1)
                return null;
        }

        return matches.Count == 1 ? matches.First() : null;
    }

    private static async Task AddSalesOrderMatchesAsync(v_Jiwa_SalesOrdersQuery query, ISet<string> matches, CancellationToken ct)
    {
        var response = await JiwaApiClient.GetAsync(query, ct);
        foreach (var row in response.Results ?? [])
        {
            var invoiceId = row.InvoiceID?.Trim();
            if (!string.IsNullOrWhiteSpace(invoiceId))
                matches.Add(invoiceId);
        }
    }

    private static IReadOnlyList<string> BuildSalesDocumentNumberCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        var withoutPrefix = Regex.Replace(trimmed, "^(SO|INV|INVOICE)[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
        AddCandidate(withoutPrefix);

        return candidates;

        void AddCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (seen.Add(value))
                candidates.Add(value);
        }
    }
}

