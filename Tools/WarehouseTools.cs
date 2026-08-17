using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class WarehouseTools : JiwaToolBase
{
    private static readonly IReadOnlyDictionary<string, string[]> AustralianStateAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["nsw"] = new[] { "new south wales", "nsw" },
        ["vic"] = new[] { "victoria", "vic" },
        ["qld"] = new[] { "queensland", "qld" },
        ["sa"] = new[] { "south australia", "sa" },
        ["wa"] = new[] { "western australia", "wa" },
        ["tas"] = new[] { "tasmania", "tas" },
        ["nt"] = new[] { "northern territory", "nt" },
        ["act"] = new[] { "australian capital territory", "act" }
    };

    [BusinessTool(EntityType = "Warehouse", ActionType = "Search")]
    [McpServerTool(Name = "ListWarehouses", ReadOnly = true), Description("List or search warehouses.")]
    public Task<string> SearchWarehouses(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_WarehouseSelectionQuery requestDTO,
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

    [BusinessTool(EntityType = "Warehouse", ActionType = "Get")]
    [McpServerTool(Name = "GetCurrentWarehouse"), Description("Get the current warehouse.")]
    public Task<string> GetCurrentLogicalWarehouse(CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var requestDTO = new JiwaFinancials.Jiwa.JiwaServiceModel.LogicalWarehousesCurrentGETRequest();
            IN_Logical response = await JiwaApiClient.GetAsync(requestDTO, ct);

            //Resolve physical
            var v_WarehouseSelectionResponse = await JiwaApiClient.GetAsync(new v_WarehouseSelectionQuery { IN_LogicalID = response.IN_LogicalID }, ct);

            return v_WarehouseSelectionResponse?.ToJson() ?? "No logical warehouse found.";
        });

    [BusinessTool(EntityType = "Warehouse", ActionType = "Set")]
    [McpServerTool(Name = "SetCurrentWarehouse"), Description("Set the current warehouse.")]
    public Task<string> ChangeCurrentLogicalWarehouse(SetCurrentWarehouseRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var logicalWarehouseId = requestDTO.IN_LogicalID;

            // If warehouseName is provided, resolve it to IN_LogicalID
            if (string.IsNullOrWhiteSpace(logicalWarehouseId) && !string.IsNullOrWhiteSpace(requestDTO.WarehouseName))
            {
                logicalWarehouseId = await ResolveWarehouseNameToIdAsync(requestDTO.WarehouseName, ct);
                if (string.IsNullOrWhiteSpace(logicalWarehouseId))
                {
                    return new
                    {
                        success = false,
                        error = $"I couldn't find a warehouse matching '{requestDTO.WarehouseName}'. Please check the warehouse name and try again."
                    }.ToJson();
                }
            }

            if (string.IsNullOrWhiteSpace(logicalWarehouseId))
            {
                return new
                {
                    success = false,
                    error = "No warehouse ID or name provided."
                }.ToJson();
            }

            var patchRequest = new JiwaFinancials.Jiwa.JiwaServiceModel.LogicalWarehousesCurrentPATCHRequest
            {
                IN_LogicalID = logicalWarehouseId
            };

            var result = await JiwaApiClient.PatchAsync(patchRequest, ct);

            // Construct response with action directive for the Jiwa client's Manager.CurrentLogicalWarehouse
            var response = new
            {
                success = true,
                newWarehouseId = result.IN_LogicalID,
                newWarehouseDetails = result?.ToJson() ?? "Warehouse updated",
            };

            return response.ToJson();
        });

    private async Task<string?> ResolveWarehouseNameToIdAsync(string warehouseName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(warehouseName))
            return null;

        var trimmedInput = warehouseName.Trim();

        // Get all warehouses
        var allWarehouses = await JiwaApiClient.GetAsync(new v_WarehouseSelectionQuery { Skip = 0, Take = 1000 }, ct);
        if (allWarehouses?.Results == null || allWarehouses.Results.Count == 0)
            return null;

        // Try exact matching first (case-insensitive)
        var exactMatches = new List<v_WarehouseSelection>();

        foreach (var warehouse in allWarehouses.Results)
        {
            if (warehouse.LogicalDescription != null &&
                string.Equals(warehouse.LogicalDescription, trimmedInput, StringComparison.OrdinalIgnoreCase))
                exactMatches.Add(warehouse);
        }

        if (exactMatches.Count > 0)
            return exactMatches.First().IN_LogicalID;

        // Parse the input to extract physical and logical warehouse names
        var candidates = ParseWarehouseNameCandidates(trimmedInput);
        foreach (var candidate in candidates)
        {
            foreach (var warehouse in allWarehouses.Results)
            {
                bool logicalMatch = true;
                bool physicalMatch = true;

                if (candidate.LogicalName != null &&
                    (warehouse.LogicalDescription == null ||
                     !ContainsWithStateAliases(warehouse.LogicalDescription, candidate.LogicalName)))
                    logicalMatch = false;

                if (candidate.PhysicalName != null &&
                    (warehouse.Description == null ||
                     !ContainsWithStateAliases(warehouse.Description, candidate.PhysicalName)))
                    physicalMatch = false;

                if (logicalMatch && physicalMatch)
                    return warehouse.IN_LogicalID;
            }
        }

        // Fallback: try substring matching on logical warehouse name
        foreach (var warehouse in allWarehouses.Results)
        {
            if (warehouse.LogicalDescription != null &&
                ContainsWithStateAliases(warehouse.LogicalDescription, trimmedInput))
                return warehouse.IN_LogicalID;
        }

        // Final fallback: try substring matching on physical warehouse name
        foreach (var warehouse in allWarehouses.Results)
        {
            if (warehouse.Description != null &&
                ContainsWithStateAliases(warehouse.Description, trimmedInput))
                return warehouse.IN_LogicalID;
        }

        return null;
    }

    private static IReadOnlyList<(string? PhysicalName, string? LogicalName)> ParseWarehouseNameCandidates(string input)
    {
        var candidates = new List<(string? PhysicalName, string? LogicalName)>();
        if (string.IsNullOrWhiteSpace(input))
            return candidates;

        // Check for slash separator first
        if (input.Contains("/"))
        {
            var parts = input.Split('/');
            if (parts.Length == 2)
            {
                var part1 = parts[0].Trim();
                var part2 = parts[1].Trim();

                candidates.Add((part1, part2));
                candidates.Add((part2, part1));
                return candidates;
            }
        }

        // Check for space-separated words
        var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 2)
        {
            candidates.Add((words[0], words[1]));
            candidates.Add((words[1], words[0]));
            return candidates;
        }
        else if (words.Length > 2)
        {
            var first = words[0];
            var rest = string.Join(" ", words.Skip(1));
            candidates.Add((rest, first));

            var last = words[words.Length - 1];
            var leading = string.Join(" ", words.Take(words.Length - 1));
            candidates.Add((leading, last));
            candidates.Add((last, leading));
            return candidates;
        }
        else if (words.Length == 1)
        {
            candidates.Add((null, words[0]));
            candidates.Add((words[0], null));
            return candidates;
        }

        return candidates;
    }

    private static bool ContainsWithStateAliases(string source, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(searchTerm))
            return false;

        if (source.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            return true;

        var canonicalSource = CanonicaliseAustralianStateNames(source);
        var canonicalSearchTerm = CanonicaliseAustralianStateNames(searchTerm);
        return canonicalSource.Contains(canonicalSearchTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static string CanonicaliseAustralianStateNames(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var canonical = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^\p{L}\p{Nd}\s]+", " ");
        canonical = Regex.Replace(canonical, @"\s+", " ");

        foreach (var aliasEntry in AustralianStateAliases)
        {
            foreach (var alias in aliasEntry.Value.OrderByDescending(item => item.Length))
            {
                canonical = Regex.Replace(canonical, $@"\b{Regex.Escape(alias)}\b", aliasEntry.Key, RegexOptions.IgnoreCase);
            }
        }

        return canonical;
    }
}
