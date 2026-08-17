using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class PurchaseOrderTools(
    IToolRoutingContextAccessor? routingContextAccessor = null,
    IHttpContextAccessor? httpContextAccessor = null) : JiwaToolBase
{
    private static readonly Regex PurchaseOrderPromptIdentifierRegex = new(
        "\\b(?:po|purchase\\s+order)(?:\\s+number)?\\s*(?<id>[\\p{L}\\p{Nd}][\\p{L}\\p{Nd}\\-/]*)\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private readonly IToolRoutingContextAccessor? _routingContextAccessor = routingContextAccessor;
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Get", Aliases = ["get po details", "get purchase order details", "purchase order id", "po details by id", "show po", "view po"])]
    [McpServerTool(Name = "GetPurchaseOrderDetails"), Description("Get purchase order details by PurchaseOrderID or PO number.")]
    public Task<string> GetPurchaseOrder(PurchaseOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedPurchaseOrderIdAsync(
                requestDTO.PurchaseOrderID,
                ct,
                async (purchaseOrderId, innerCt) => await JiwaApiClient.GetAsync(
                    new PurchaseOrderGETRequest { PurchaseOrderID = purchaseOrderId },
                    innerCt));

            return result.ToJson<PurchaseOrder>();
        });

    private static Task<T> ExecuteWithResolvedPurchaseOrderIdAsync<T>(
        string? purchaseOrderId,
        CancellationToken ct,
        Func<string, CancellationToken, Task<T>> executeAsync)
        => ExecuteWithResolvedIdentifierAsync(
            purchaseOrderId,
            ct,
            executeAsync,
            TryResolvePurchaseOrderIdFromDocumentNumberAsync);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Get", Aliases = ["get purchase order", "get po by id", "retrieve purchase order"])]
    [McpServerTool(Name = "GetPurchaseOrder"), Description("Alias for GetPurchaseOrderDetails.")]

    public Task<string> GetPurchaseOrderAlias(PurchaseOrderGETRequest requestDTO, CancellationToken ct = default)
        => GetPurchaseOrder(requestDTO, ct);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Get", Aliases = ["get po", "retrieve po"])]
    [McpServerTool(Name = "GetPO"), Description("Alias for GetPurchaseOrderDetails.")]

    public Task<string> GetPO(PurchaseOrderGETRequest requestDTO, CancellationToken ct = default)
        => GetPurchaseOrder(requestDTO, ct);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Create", Aliases = ["create po", "new po", "raise po", "create purchase order", "purchase order header", "po header", "create po for creditor", "create po for supplier"])]
    [McpServerTool(Name = "CreatePurchaseOrder"), Description("Create a purchase order header.")]
    public Task<string> CreatePurchaseOrder(PurchaseOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<PurchaseOrder>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Create", Aliases = ["create po", "new po", "raise po", "create purchase order"])]
    [McpServerTool(Name = "CreatePO"), Description("Alias for CreatePurchaseOrder.")]
    public Task<string> CreatePO(PurchaseOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => CreatePurchaseOrder(requestDTO, ct);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Create", Aliases = ["create po", "new po", "raise po", "create purchase order"])]
    [McpServerTool(Name = "CreatePOWithLines"), Description("Alias for CreatePurchaseOrder.")]
    public Task<string> CreatePOWithLines(CreatePurchaseOrderWithLinesRequest requestDTO, CancellationToken ct = default)
        => CreatePurchaseOrderWithLines(requestDTO, ct);

    public sealed class CreatePurchaseOrderWithLinesRequest
    {
        public string? Creditor { get; set; }
        public DateTime? OrderDate { get; set; }
        public List<PurchaseOrderLineInput>? Lines { get; set; }
        public decimal? DefaultQuantity { get; set; } = 0m;
        public bool SkipMissingParts { get; set; } = true;
    }

    public sealed class PurchaseOrderLineInput
    {
        public string? PartNo { get; set; }
        public decimal? Quantity { get; set; }
    }

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Create", Aliases = ["create po with parts", "create purchase order with lines", "create po for creditor with parts", "add parts to new po", "create po and skip missing parts"]) ]
    [McpServerTool(Name = "CreatePurchaseOrderWithLines"), Description("Create a purchase order with lines.")]
    public Task<string> CreatePurchaseOrderWithLines(CreatePurchaseOrderWithLinesRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (requestDTO is null)
                throw new ArgumentNullException(nameof(requestDTO));

            var creditorValue = requestDTO.Creditor?.Trim();
            if (string.IsNullOrWhiteSpace(creditorValue))
                throw new ArgumentException("Creditor is required.", nameof(requestDTO));

            var creditorRecId = await ResolveCreditorRecIdAsync(creditorValue, ct);

            var createdPurchaseOrder = await JiwaApiClient.PostAsync(new PurchaseOrderPOSTRequest
            {
                CreditorRecID = creditorRecId,
                OrderDate = requestDTO.OrderDate
            }, ct);

            var purchaseOrderId = createdPurchaseOrder.PurchaseOrderID?.Trim();
            if (string.IsNullOrWhiteSpace(purchaseOrderId))
                throw new InvalidOperationException("Purchase order was created but no PurchaseOrderID was returned.");

            var defaultQuantity = requestDTO.DefaultQuantity ?? 0m;
            var requestedLines = requestDTO.Lines?
                .Where(x => !string.IsNullOrWhiteSpace(x?.PartNo))
                .Select(x => new
                {
                    PartNo = x!.PartNo!.Trim(),
                    Quantity = x.Quantity ?? defaultQuantity
                })
                .ToList() ?? [];

            var addedLines = new List<object>();
            var skippedLines = new List<object>();

            foreach (var line in requestedLines)
            {
                try
                {
                    var added = await JiwaApiClient.PostAsync(new PurchaseOrderLinePOSTRequest
                    {
                        PurchaseOrderID = purchaseOrderId,
                        PartNo = line.PartNo,
                        Quantity = line.Quantity
                    }, ct);

                    addedLines.Add(new
                    {
                        line.PartNo,
                        line.Quantity,
                        added.PurchaseOrderLineID
                    });
                }
                catch (WebServiceException ex) when (requestDTO.SkipMissingParts && IsMissingPartError(ex))
                {
                    skippedLines.Add(new
                    {
                        line.PartNo,
                        line.Quantity,
                        Reason = "Part does not exist in inventory."
                    });
                }
            }

            return new
            {
                createdPurchaseOrder.PurchaseOrderID,
                createdPurchaseOrder.OrderNo,
                createdPurchaseOrder.CreditorRecID,
                createdPurchaseOrder.OrderDate,
                RequestedLineCount = requestedLines.Count,
                AddedLineCount = addedLines.Count,
                SkippedLineCount = skippedLines.Count,
                AddedLines = addedLines,
                SkippedLines = skippedLines
            }.ToJson();
        });

    [BusinessTool(
        EntityType = "PurchaseOrder",
        ActionType = "Update",
        Aliases = ["update po", "update purchase order", "update po quantity", "change po line quantity", "set quantity on po lines", "update all lines on po", "po number to update", "patch purchase order"],
        Tags = ["po", "purchase order", "update", "line", "lines", "quantity", "qty", "order number"],
        RelatedEntities = "PurchaseOrderLine",
        RequiredCompanionTools = "ResolvePurchaseOrderId")]
    [McpServerTool(Name = "UpdatePurchaseOrder"), Description("Update a purchase order and its line quantities by PO number or PurchaseOrderID.")]
    public Task<string> ModifyPurchaseOrder(PurchaseOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var purchaseOrderIdentifier = ResolvePurchaseOrderIdentifier(requestDTO);
            if (string.IsNullOrWhiteSpace(purchaseOrderIdentifier))
            {
                throw new InvalidOperationException("A PurchaseOrderID or OrderNo is required to update a purchase order.");
            }

            var result = await ExecuteWithResolvedPurchaseOrderIdAsync(
                purchaseOrderIdentifier,
                ct,
                async (purchaseOrderId, innerCt) =>
                {
                    requestDTO.PurchaseOrderID = purchaseOrderId;
                    return await JiwaApiClient.PatchAsync(requestDTO, innerCt);
                });

            return result.ToJson<PurchaseOrder>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Delete")]
    [McpServerTool(Name = "DeletePurchaseOrder"), Description("Delete a purchase order.")]
    public Task<string> DeletePurchaseOrder(PurchaseOrderDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var deletedPurchaseOrderId = await ExecuteWithResolvedPurchaseOrderIdAsync(
                requestDTO.PurchaseOrderID,
                ct,
                async (purchaseOrderId, innerCt) =>
                {
                    requestDTO.PurchaseOrderID = purchaseOrderId;
                    await JiwaApiClient.DeleteAsync(requestDTO, innerCt);
                    return purchaseOrderId;
                });

            return new { Deleted = true, PurchaseOrderID = deletedPurchaseOrderId }.ToJson();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Add")]
    [McpServerTool(Name = "AddItemToPurchaseOrder"), Description("Add a line to an existing purchase order.")]
    public Task<string> AddAProductToAPurchaseOrder(PurchaseOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedPurchaseOrderIdAsync(
                requestDTO.PurchaseOrderID,
                ct,
                async (purchaseOrderId, innerCt) =>
                {
                    requestDTO.PurchaseOrderID = purchaseOrderId;
                    return await JiwaApiClient.PostAsync(requestDTO, innerCt);
                });

            return result.ToJson<PurchaseOrderLine>();
        });

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Add", Aliases = ["add po line", "add item to po"])]
    [McpServerTool(Name = "AddItemToPO"), Description("Alias for AddItemToPurchaseOrder.")]
    public Task<string> AddItemToPO(PurchaseOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => AddAProductToAPurchaseOrder(requestDTO, ct);

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Search")]
    [McpServerTool(Name = "ListPurchaseHistory", ReadOnly = true), Description("List purchase history.")]
    public Task<string> SearchPurchaseInformation(
        v_Jiwa_PurchaseInformationQuery requestDTO,
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

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Search", Aliases = ["show po", "view po", "po number", "purchase order number", "find po", "lookup po"])]
    [McpServerTool(Name = "ListPurchaseOrders", ReadOnly = true), Description("List or search purchase orders.")]
    public Task<string> SearchPurchaseOrders(
        v_Jiwa_PurchaseOrdersQuery requestDTO,
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

    [BusinessTool(EntityType = "PurchaseOrder", ActionType = "Resolve", Aliases = ["resolve po number", "resolve purchase order number", "purchase order no to id", "po number to id", "document number to order id"])]
    [McpServerTool(Name = "ResolvePurchaseOrderId", ReadOnly = true), Description("Resolve a purchase order document number to PurchaseOrderID.")]
    public Task<string> ResolvePurchaseOrderId(string documentNumber, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);

            var trimmedDocumentNumber = documentNumber.Trim();
            var resolvedOrderId = await TryResolvePurchaseOrderIdFromDocumentNumberAsync(trimmedDocumentNumber, ct);
            if (string.IsNullOrWhiteSpace(resolvedOrderId))
            {
                return new
                {
                    success = false,
                    error = $"Unable to resolve purchase order document number '{trimmedDocumentNumber}' to a unique PurchaseOrderID.",
                    hint = "Use ListPurchaseOrders with OrderNo to locate the correct OrderID, then retry."
                }.ToJson();
            }

            return new
            {
                success = true,
                documentNo = trimmedDocumentNumber,
                purchaseOrderID = resolvedOrderId
            }.ToJson();
        });

    private static async Task<string?> TryResolvePurchaseOrderIdFromDocumentNumberAsync(string documentNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return null;
        }

        var candidates = BuildDocumentNumberCandidates(documentNumber);
        foreach (var candidate in candidates)
        {
            var query = new v_Jiwa_PurchaseOrdersQuery
            {
                OrderNo = candidate,
                Take = 2,
                Skip = 0
            };

            var response = await JiwaApiClient.GetAsync(query, ct);
            var matches = response.Results?
                .Select(x => x.OrderID?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matches is { Count: 1 })
            {
                return matches[0];
            }
        }

        return null;
    }

    private static async Task<string> ResolveCreditorRecIdAsync(string creditorValue, CancellationToken ct)
    {
        var candidates = BuildCreditorCandidates(creditorValue);
        var matches = new Dictionary<string, v_Jiwa_CreditorSummary>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery
            {
                CreditorID = candidate,
                Take = 5,
                Skip = 0
            }, matches, ct);

            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery
            {
                AccountNo = candidate,
                Take = 5,
                Skip = 0
            }, matches, ct);
        }

        if (matches.Count == 1)
            return matches.Keys.Single();

        if (matches.Count > 1)
        {
            var options = matches.Values
                .Take(5)
                .Select(x => $"{x.CreditorID} ({x.AccountNo}) {x.Name}")
                .ToArray();

            throw new InvalidOperationException($"Multiple creditors matched '{creditorValue}'. Please specify a unique creditor. Matches: {string.Join("; ", options)}.");
        }

        throw new InvalidOperationException($"No creditor matched '{creditorValue}'. Use ListSuppliers to find the correct creditor, then retry.");
    }

    private static async Task AddCreditorMatchesAsync(
        v_Jiwa_CreditorSummaryQuery query,
        IDictionary<string, v_Jiwa_CreditorSummary> matches,
        CancellationToken ct)
    {
        var response = await JiwaApiClient.GetAsync(query, ct);
        foreach (var row in response.Results ?? [])
        {
            var creditorId = row.CreditorID?.Trim();
            if (string.IsNullOrWhiteSpace(creditorId))
                continue;

            if (!matches.ContainsKey(creditorId))
                matches[creditorId] = row;
        }
    }

    private static bool IsMissingPartError(WebServiceException ex)
    {
        if (ex.StatusCode == 404)
            return true;

        var combined = string.Join(" ",
            ex.Message ?? string.Empty,
            ex.StatusDescription ?? string.Empty,
            ex.ResponseBody ?? string.Empty);

        return combined.Contains("part", StringComparison.OrdinalIgnoreCase)
            && (combined.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || combined.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                || combined.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }

    private string? ResolvePurchaseOrderIdentifier(PurchaseOrderPATCHRequest requestDTO)
    {
        if (!string.IsNullOrWhiteSpace(requestDTO.PurchaseOrderID))
        {
            return requestDTO.PurchaseOrderID.Trim();
        }

        if (!string.IsNullOrWhiteSpace(requestDTO.OrderNo))
        {
            return requestDTO.OrderNo.Trim();
        }

        var prompt = GetCurrentUserPrompt();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var match = PurchaseOrderPromptIdentifierRegex.Match(prompt);
        if (!match.Success)
        {
            return null;
        }

        var extractedIdentifier = match.Groups["id"].Value.Trim();
        return string.IsNullOrWhiteSpace(extractedIdentifier)
            ? null
            : extractedIdentifier;
    }

    private string? GetCurrentUserPrompt()
    {
        var routingPrompt = _routingContextAccessor?.Current?.Prompt;
        if (!string.IsNullOrWhiteSpace(routingPrompt))
        {
            return routingPrompt;
        }

        var headers = _httpContextAccessor?.HttpContext?.Request?.Headers;
        var headerPrompt = headers?["X-User-Prompt"].FirstOrDefault()
            ?? headers?["X-Tool-Query"].FirstOrDefault();

        return string.IsNullOrWhiteSpace(headerPrompt)
            ? null
            : headerPrompt;
    }

    private static IReadOnlyList<string> BuildDocumentNumberCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        var withoutPrefix = Regex.Replace(trimmed, "^PO[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
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

    private static IReadOnlyList<string> BuildCreditorCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        var withoutPrefix = Regex.Replace(trimmed, "^(CR|CREDITOR)[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
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
