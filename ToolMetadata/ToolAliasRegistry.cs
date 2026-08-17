namespace JiwaMcpServer.ToolMetadata;

public static class ToolAliasRegistry
{
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["GetPurchaseOrder"] = "GetPurchaseOrderDetails",
        ["GetPO"] = "GetPurchaseOrderDetails",
        ["CreatePO"] = "CreatePurchaseOrder",
        ["AddItemToPO"] = "AddItemToPurchaseOrder",
        ["CreateSO"] = "CreateSalesOrder",
        ["AddItemToSO"] = "AddItemToSalesOrder"
    };

    public static bool IsAlias(string toolName)
        => TryGetCanonicalName(toolName, out _);

    public static bool TryGetCanonicalName(string toolName, out string canonicalToolName)
    {
        if (Aliases.TryGetValue(toolName, out var canonical))
        {
            canonicalToolName = canonical;
            return true;
        }

        canonicalToolName = string.Empty;
        return false;
    }

    public static string GetCanonicalNameOrSelf(string toolName)
        => TryGetCanonicalName(toolName, out var canonicalToolName)
            ? canonicalToolName
            : toolName;
}
