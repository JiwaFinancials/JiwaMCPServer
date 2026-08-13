namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessTerminologyRegistry
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _terms;

    public BusinessTerminologyRegistry()
    {
        _terms = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Supplier"] = ["supplier", "creditor", "vendor", "payee"],
            ["Customer"] = ["customer", "debtor", "client", "account holder"],
            ["PurchaseOrder"] = ["purchase order", "po", "supplier order", "procurement order"],
            ["SalesOrder"] = ["sales order", "sales invoice", "customer invoice", "invoice", "customer order"],
            ["CreditorPurchase"] = ["supplier invoice", "supplier bill", "vendor bill", "creditor invoice", "ap invoice", "accounts payable invoice"],
            ["Inventory"] = ["inventory", "stock", "product", "item", "sku", "part"],
            ["Warehouse"] = ["warehouse", "location", "storage location"],
            ["Document"] = ["document", "invoice file", "pdf", "scan", "attachment"],
            ["File"] = ["file", "csv", "excel", "xml", "json", "local file"],
            ["Form"] = ["form", "screen", "plugin", "module"],
            ["Schema"] = ["schema", "dto", "request model", "response model"]
        };
    }

    public IReadOnlyList<string> GetSynonyms(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return [];

        return _terms.TryGetValue(entityType.Trim(), out var synonyms)
            ? synonyms
            : [];
    }
}
