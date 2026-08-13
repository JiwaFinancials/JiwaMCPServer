using System.Text;

namespace JiwaMcpServer.ToolMetadata;

public sealed class ToolDescriptionComposer
{
    private static readonly HashSet<string> AliasOnlyTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "GetPurchaseOrder",
        "GetPO",
        "CreatePO",
        "AddItemToPO",
        "CreateSO",
        "AddItemToSO"
    };

    private readonly BusinessTerminologyRegistry _terminologyRegistry;

    public ToolDescriptionComposer(BusinessTerminologyRegistry terminologyRegistry)
    {
        _terminologyRegistry = terminologyRegistry;
    }

    public string Compose(BusinessToolDescriptor descriptor)
    {
        var metadata = descriptor.Metadata;
        var action = string.IsNullOrWhiteSpace(metadata.ActionType) ? "Use" : metadata.ActionType;
        var entity = string.IsNullOrWhiteSpace(metadata.EntityType) ? "BusinessEntity" : metadata.EntityType;
        var synonyms = MergeDistinct(_terminologyRegistry.GetSynonyms(entity), metadata.Aliases);
        var tags = metadata.Tags;
        var commonRequests = BuildCommonRequests(action, synonyms, descriptor.ToolName);
        var doNotUse = BuildDoNotUse(action, descriptor.ToolName);
        var intentExamples = BuildIntentExamples(commonRequests, descriptor.ToolName);

        var sb = new StringBuilder();
        var baseDescription = string.IsNullOrWhiteSpace(descriptor.Description)
            ? $"{descriptor.ToolName} business operation."
            : descriptor.Description.Trim();
        sb.AppendLine(baseDescription);

        if (AliasOnlyTools.Contains(descriptor.ToolName))
        {
            var canonical = ResolveCanonicalToolName(descriptor.ToolName);
            sb.AppendLine();
            sb.AppendLine($"Alias for {canonical}.");
        }

        if (synonyms.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Business synonyms: {string.Join(", ", synonyms)}.");
        }

        if (tags.Count > 0)
        {
            sb.AppendLine($"Alternative terminology: {string.Join(", ", tags)}.");
        }

        sb.AppendLine();
        sb.AppendLine("Use this tool when users ask:");
        foreach (var request in commonRequests)
        {
            sb.AppendLine($"- {request}");
        }

        sb.AppendLine();
        sb.AppendLine("Do not use this tool for:");
        foreach (var notUse in doNotUse)
        {
            sb.AppendLine($"- {notUse}");
        }

        sb.AppendLine();
        sb.AppendLine("User intent examples:");
        foreach (var example in intentExamples)
        {
            sb.AppendLine($"- User: {example.UserPhrase}");
            sb.AppendLine($"  Tool: {example.ToolName}");
        }

        return sb.ToString().Trim();
    }

    private static string ResolveCanonicalToolName(string toolName)
        => toolName switch
        {
            "GetPurchaseOrder" => "GetPurchaseOrderDetails",
            "GetPO" => "GetPurchaseOrderDetails",
            "CreatePO" => "CreatePurchaseOrder",
            "AddItemToPO" => "AddItemToPurchaseOrder",
            "CreateSO" => "CreateSalesOrder",
            "AddItemToSO" => "AddItemToSalesOrder",
            _ => toolName
        };

    private static IReadOnlyList<(string UserPhrase, string ToolName)> BuildIntentExamples(IReadOnlyList<string> requests, string toolName)
        => requests.Take(5).Select(x => (x, toolName)).ToArray();

    private static IReadOnlyList<string> BuildCommonRequests(string actionType, IReadOnlyList<string> synonyms, string toolName)
    {
        var action = string.IsNullOrWhiteSpace(actionType)
            ? "use"
            : actionType.Trim().ToLowerInvariant();
        var verbs = action switch
        {
            "create" => new[] { "create", "add", "new" },
            "get" => new[] { "get", "show", "view" },
            "list" => new[] { "list", "show", "find" },
            "search" => new[] { "search", "find", "show" },
            "update" => new[] { "update", "edit", "change" },
            "delete" => new[] { "delete", "remove" },
            "set" => new[] { "set", "change" },
            "add" => new[] { "add", "append" },
            _ => new[] { action }
        };

        var requests = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var synonym in synonyms.DefaultIfEmpty(toolName))
        {
            foreach (var verb in verbs)
            {
                var phrase = $"{verb} {synonym}".Trim();
                if (seen.Add(phrase))
                {
                    requests.Add(phrase);
                }
            }
        }

        return requests.Take(8).ToArray();
    }

    private static IReadOnlyList<string> BuildDoNotUse(string actionType, string toolName)
    {
        var action = string.IsNullOrWhiteSpace(actionType)
            ? "use"
            : actionType.Trim().ToLowerInvariant();
        return action switch
        {
            "create" => ["updating existing records", "deleting records", "searching records"],
            "get" => ["listing many records", "creating records", "deleting records"],
            "list" or "search" => ["creating new records", "updating a specific record", "deleting records"],
            "update" => ["creating new records", "deleting records", "searching records"],
            "delete" => ["creating records", "updating records", "searching records"],
            _ => ["unrelated business domains", "administrative tasks outside this tool", $"calling alias tools when {toolName} is not relevant"]
        };
    }

    private static IReadOnlyList<string> MergeDistinct(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in first.Concat(second))
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            if (seen.Add(normalized))
                merged.Add(normalized);
        }

        return merged;
    }
}
