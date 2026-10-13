using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Creditors;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Models;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolDescriptions;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[PlannerDomain("SupplierTools")]
[McpServerToolType]
public class SupplierTools : JiwaToolBase
{
    private static readonly string[] DefaultSupplierListFields = ["CreditorID", "AccountNo", "Name"];
    private static readonly string[] DefaultSupplierClassificationFields = ["CreditorClassificationID", "Description"];

    [McpServerTool(ReadOnly = true), Description(SupplierToolDescriptions.QuerySuppliers)]
    public Task<string> QuerySuppliers(
        [Description("Search and filter criteria for supplier or creditor records. Leave fields empty to return all matching records, or set exact-match and *Contains filters to narrow the search.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching supplier result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same supplier query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultSupplierListFields);
        });

    [McpServerTool, Description(SupplierToolDescriptions.GetSupplier)]
    public Task<string> GetSupplier(SupplierGetToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var creditorId = await ResolveCreditorIdAsync(requestDTO.CreditorID, requestDTO.Identifier, ct);
            var result = await JiwaApiClient.GetAsync(new CreditorGETRequest { CreditorID = creditorId }, ct);
            return result.ToJson<Creditor>();
        });

    [McpServerTool, Description(SupplierToolDescriptions.CreateSupplier)]
    public Task<string> CreateSupplier(CreditorPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [McpServerTool, Description(SupplierToolDescriptions.ModifySupplier)]
    public Task<string> ModifySupplier(SupplierPatchToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var creditorId = await ResolveCreditorIdAsync(requestDTO.CreditorID, requestDTO.Identifier, ct);
            var changes = PreparePatchChanges(requestDTO.Changes, nameof(CreditorPATCHRequest.CreditorID));
            var result = await JiwaApiClient.PatchPartialAsync<CreditorPATCHRequest, Creditor>(
                changes,
                [(nameof(CreditorPATCHRequest.CreditorID), creditorId)],
                ct);
            return CreatePatchResponseJson("supplier", changes, result, [(nameof(CreditorPATCHRequest.CreditorID), creditorId)]);
        });

    [McpServerTool, Description(SupplierToolDescriptions.DeleteSupplier)]
    public Task<string> DeleteSupplier(SupplierDeleteToolRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var creditorId = await ResolveCreditorIdAsync(requestDTO.CreditorID, requestDTO.Identifier, ct);
            await JiwaApiClient.DeleteAsync(new CreditorDELETERequest { CreditorID = creditorId }, ct);
            return new { Deleted = true, CreditorID = creditorId, Identifier = requestDTO.Identifier }.ToJson();
        });

    //Need to add CR_Classification autoquery to the REST API Plugin
    //[McpServerTool(ReadOnly = true), Description(SupplierToolDescriptions.SearchSupplierClassifications)]
    //public Task<string> SearchSupplierClassifications(
    //    JiwaFinancials.Jiwa.JiwaServiceModel.Tables.CR_ClassificationQuery requestDTO,
    //    bool confirmLargeResultSet = false,
    //    string? confirmationToken = null,
    //    CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
    //        if (!string.IsNullOrEmpty(confirmationMessage))
    //            return confirmationMessage;

    //        var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
    //        return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultSupplierClassificationFields);
    //    });

    private static async Task<string> ResolveCreditorIdAsync(string? creditorId, string? identifier, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(creditorId))
            return creditorId;

        if (string.IsNullOrWhiteSpace(identifier))
            throw new InvalidOperationException("Either CreditorID or Identifier must be provided.");

        var accountMatches = DeduplicateSupplierMatches(
            (await QuerySuppliersForResolutionAsync(query => query.AccountNo = identifier, ct))
                .Concat(await QuerySuppliersForResolutionAsync(query => query.AlternativeAccountNo = identifier, ct)))
            .Where(supplier => string.Equals(supplier.AccountNo, identifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(supplier.AlternativeAccountNo, identifier, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var resolvedCreditorId = ResolveSingleSupplierMatch(accountMatches, identifier, "account number or supplier code");
        if (!string.IsNullOrWhiteSpace(resolvedCreditorId))
            return resolvedCreditorId;

        var nameMatches = DeduplicateSupplierMatches(await QuerySuppliersForResolutionAsync(query => query.Name = identifier, ct))
            .Where(supplier => string.Equals(supplier.Name, identifier, StringComparison.OrdinalIgnoreCase))
            .ToList();

        resolvedCreditorId = ResolveSingleSupplierMatch(nameMatches, identifier, "supplier name");
        if (!string.IsNullOrWhiteSpace(resolvedCreditorId))
            return resolvedCreditorId;

        throw new InvalidOperationException($"No supplier found for identifier '{identifier}'. Provide a CreditorID, exact account number, supplier code, or exact supplier name.");
    }

    private static async Task<List<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummary>> QuerySuppliersForResolutionAsync(
        Action<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery> configureQuery,
        CancellationToken ct)
    {
        var query = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery
        {
            Fields = "CreditorID, AccountNo, AlternativeAccountNo, Name"
        };

        configureQuery(query);
        return await GetAllQueryResultsAsync(query, Config.PageSize, ct);
    }

    private static List<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummary> DeduplicateSupplierMatches(
        IEnumerable<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummary> matches)
        => matches
            .Where(match => !string.IsNullOrWhiteSpace(match.CreditorID))
            .GroupBy(match => match.CreditorID, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

    private static string? ResolveSingleSupplierMatch(
        IReadOnlyCollection<JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummary> matches,
        string identifier,
        string matchType)
    {
        if (matches.Count == 0)
            return null;

        if (matches.Count > 1)
            throw new InvalidOperationException($"Multiple suppliers matched {matchType} '{identifier}'. Provide the CreditorID instead.");

        var resolvedCreditorId = matches.First().CreditorID;
        if (string.IsNullOrWhiteSpace(resolvedCreditorId))
            throw new InvalidOperationException($"The supplier matched {matchType} '{identifier}' but did not return a CreditorID.");

        return resolvedCreditorId;
    }
}
