using System.Text.RegularExpressions;

namespace JiwaMcpServer.Agent.Routing;

public static class RoutingVocabulary
{
    private static readonly IReadOnlyDictionary<string, string> EntityAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["customer"] = "Customer",
        ["customers"] = "Customer",
        ["debtor"] = "Customer",
        ["debtors"] = "Customer",
        ["account"] = "Customer",
        ["accounts"] = "Customer",
        ["invoice"] = "Invoice",
        ["invoices"] = "Invoice",
        ["purchase order"] = "PurchaseOrder",
        ["purchase orders"] = "PurchaseOrder",
        ["po"] = "PurchaseOrder",
        ["purchase"] = "CreditorPurchase",
        ["purchases"] = "CreditorPurchase",
        ["purchase batch"] = "CreditorPurchase",
        ["purchase batches"] = "CreditorPurchase",
        ["supplier purchase"] = "CreditorPurchase",
        ["supplier purchases"] = "CreditorPurchase",
        ["creditor purchase"] = "CreditorPurchase",
        ["creditor purchases"] = "CreditorPurchase",
        ["supplier"] = "Supplier",
        ["suppliers"] = "Supplier",
        ["creditor"] = "Supplier",
        ["creditors"] = "Supplier",
        ["product"] = "Product",
        ["products"] = "Product",
        ["part"] = "Product",
        ["parts"] = "Product",
        ["part no"] = "Product",
        ["part number"] = "Product",
        ["sku"] = "Product",
        ["item code"] = "Product",
        ["product code"] = "Product",
        ["inventory"] = "Product",
        ["inventory item"] = "Product",
        ["inventory items"] = "Product",
        ["rrp"] = "Product",
        ["retail price"] = "Product",
        ["sell price"] = "Product",
        ["price"] = "Product",
        ["warehouse"] = "Warehouse",
        ["warehouses"] = "Warehouse",
        ["transaction"] = "CustomerTransaction",
        ["transactions"] = "CustomerTransaction",
        ["payment"] = "CustomerTransaction",
        ["payments"] = "CustomerTransaction",
        ["document"] = "Document",
        ["documents"] = "Document",
        ["file"] = "Document",
        ["files"] = "Document"
    };

    private static readonly IReadOnlyDictionary<string, string> DomainAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Inventory"] = "InventoryTools",
        ["InventoryTools"] = "InventoryTools",
        ["Product"] = "InventoryTools",
        ["Warehouse"] = "InventoryTools",
        ["Customer"] = "CustomerTools",
        ["CustomerTools"] = "CustomerTools",
        ["Debtor"] = "CustomerTools",
        ["CustomerTransaction"] = "CustomerTools",
        ["Purchasing"] = "CreditorPurchaseTools",
        ["CreditorPurchaseTools"] = "CreditorPurchaseTools",
        ["CreditorPurchase"] = "CreditorPurchaseTools",
        ["Supplier"] = "CreditorPurchaseTools",
        ["Invoice"] = "CreditorPurchaseTools",
        ["PurchaseOrder"] = "CreditorPurchaseTools",
        ["Documents"] = "DocumentTools",
        ["Document"] = "DocumentTools",
        ["DocumentTools"] = "DocumentTools"
    };

    private static readonly IReadOnlyDictionary<string, string[]> ActionAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Read"] = ["show", "get", "view", "display", "open", "retrieve"],
        ["Query"] = ["query", "search", "find", "list", "browse", "history", "transactions"],
        ["Update"] = ["change", "update", "modify", "edit", "set", "correct", "amend"],
        ["Delete"] = ["delete", "remove"],
        ["Create"] = ["create", "add", "register", "new", "upload", "import"],
        ["Approve"] = ["approve", "authorise", "authorize", "activate", "finalise", "finalize", "complete"]
    };

    public static string ResolveCanonicalDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return DomainAliases.TryGetValue(value.Trim(), out var canonical)
            ? canonical
            : value.Trim();
    }

    public static string ResolveDomainForEntityType(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return string.Empty;

        var normalized = ResolveEntityType(entityType);
        return string.IsNullOrWhiteSpace(normalized)
            ? ResolveCanonicalDomain(entityType)
            : ResolveCanonicalDomain(normalized);
    }

    public static string ResolveCanonicalAction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        foreach (var pair in ActionAliases)
        {
            if (pair.Key.Equals(value, StringComparison.OrdinalIgnoreCase))
                return pair.Key;

            if (pair.Value.Any(keyword => ContainsKeyword(value, keyword)))
                return pair.Key;
        }

        return value.Trim();
    }

    public static bool AreActionsCompatible(string? requestedAction, string? supportedAction)
    {
        var left = ResolveCanonicalAction(requestedAction);
        var right = ResolveCanonicalAction(supportedAction);
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        if (left.Equals(right, StringComparison.OrdinalIgnoreCase))
            return true;

        return (left, right) switch
        {
            ("Read", "Query") => true,
            ("Query", "Read") => true,
            _ => false
        };
    }

    public static string ResolveEntityType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (EntityAliases.TryGetValue(value.Trim(), out var canonical))
            return canonical;

        var normalized = NormalizeEntityKey(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        foreach (var pair in EntityAliases)
        {
            if (NormalizeEntityKey(pair.Key).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return HumanizePascalCase(value.Trim()).Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    public static string? TryResolveEntityTypeFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        foreach (var alias in EntityAliases.Keys.OrderByDescending(static key => key.Length))
        {
            if (!ContainsKeyword(text, alias))
                continue;

            return EntityAliases[alias];
        }

        return null;
    }

    public static bool AreEntityTypesCompatible(string? left, string? right)
    {
        var normalizedLeft = NormalizeEntityKey(ResolveEntityType(left));
        var normalizedRight = NormalizeEntityKey(ResolveEntityType(right));
        if (string.IsNullOrWhiteSpace(normalizedLeft) || string.IsNullOrWhiteSpace(normalizedRight))
            return false;

        return normalizedLeft.Equals(normalizedRight, StringComparison.OrdinalIgnoreCase)
            || normalizedLeft.Contains(normalizedRight, StringComparison.OrdinalIgnoreCase)
            || normalizedRight.Contains(normalizedLeft, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsReferencePronoun(string? text)
        => !string.IsNullOrWhiteSpace(text)
            && Regex.IsMatch(text, @"\b(it|that|this|them|those|these)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool ContainsKeyword(string input, string keyword)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(keyword))
            return false;

        if (keyword.Contains(' '))
            return input.Contains(keyword, StringComparison.OrdinalIgnoreCase);

        return Regex.IsMatch(input, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static string NormalizeEntityKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = Regex.Replace(value, @"[^A-Za-z0-9]+", string.Empty).ToLowerInvariant();
        return normalized.EndsWith('s') && normalized.Length > 4
            ? normalized[..^1]
            : normalized;
    }

    public static string HumanizePascalCase(string value)
        => Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2").Replace('_', ' ').Trim();
}
