using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Creditors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
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
public class SupplierTools : JiwaToolBase
{
    [BusinessTool(EntityType = "Supplier", ActionType = "Search")]
    [McpServerTool(Name = "ListSuppliers", ReadOnly = true), Description("List or search suppliers. Suppliers are also known as creditors. Use this tool when the user asks to show suppliers, find suppliers, or return a supplier list. Treat visible supplier/creditor codes or names as the default user input and resolve them here before calling GetSupplierDetails with the internal CreditorID. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Get")]
    [McpServerTool(Name = "GetSupplierDetails"), Description("Get a specific supplier with full details. Accepts internal CreditorID or visible supplier/creditor code/name and auto-resolves it. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetSupplier(CreditorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var creditorId = requestDTO.CreditorID?.Trim();

            try
            {
                var result = await JiwaApiClient.GetAsync(new CreditorGETRequest { CreditorID = creditorId }, ct);
                return result.ToJson<Creditor>();
            }
            catch (WebServiceException ex) when (ex.StatusCode == 404 && !string.IsNullOrWhiteSpace(creditorId))
            {
                var resolvedCreditorId = await TryResolveCreditorIdAsync(creditorId, ct);
                if (string.IsNullOrWhiteSpace(resolvedCreditorId))
                    throw;

                var resolved = await JiwaApiClient.GetAsync(new CreditorGETRequest { CreditorID = resolvedCreditorId }, ct);
                return resolved.ToJson<Creditor>();
            }
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Create")]
    [McpServerTool(Name = "CreateSupplier"), Description("Create a new supplier record. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateSupplier(CreditorPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Update")]
    [McpServerTool(Name = "UpdateSupplier"), Description("Update an existing supplier record. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ModifySupplier(CreditorPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "Delete")]
    [McpServerTool(Name = "DeleteSupplier"), Description("Delete a supplier record. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteSupplier(CreditorDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, CreditorID = requestDTO.CreditorID }.ToJson();
        });

    [BusinessTool(EntityType = "Supplier", ActionType = "List")]
    [McpServerTool(Name = "ListSupplierClassifications", ReadOnly = true), Description("List supplier classifications. Creditors are also known as suppliers. Use this when the user asks for supplier classifications or creditor classifications. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token, then call again with confirmLargeResultSet=true and that token.")]
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
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    private static async Task<string?> TryResolveCreditorIdAsync(string userValue, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userValue))
            return null;

        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in BuildSupplierCandidates(userValue))
        {
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery { CreditorID = candidate, Take = 2, Skip = 0 }, matches, ct);
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery { AccountNo = candidate, Take = 2, Skip = 0 }, matches, ct);
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery { Name = candidate, Take = 2, Skip = 0 }, matches, ct);

            if (matches.Count > 1)
                return null;
        }

        return matches.Count == 1 ? matches.First() : null;
    }

    private static async Task AddCreditorMatchesAsync(v_Jiwa_CreditorSummaryQuery query, ISet<string> matches, CancellationToken ct)
    {
        var response = await JiwaApiClient.GetAsync(query, ct);
        foreach (var row in response.Results ?? [])
        {
            var creditorId = row.CreditorID?.Trim();
            if (!string.IsNullOrWhiteSpace(creditorId))
                matches.Add(creditorId);
        }
    }

    private static IReadOnlyList<string> BuildSupplierCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        var withoutPrefix = Regex.Replace(trimmed, "^(SUP|SUPPLIER|CR|CREDITOR)[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
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

