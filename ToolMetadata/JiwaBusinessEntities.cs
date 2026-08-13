namespace JiwaMcpServer.ToolMetadata;

public static class JiwaBusinessEntities
{
    public static readonly BusinessEntityDefinition Supplier = new()
    {
        Name = "Supplier",
        Aliases = ["supplier", "creditor", "vendor", "payee"],
        Tags = ["accounts payable", "ap", "purchasing", "procurement"]
    };

    public static readonly BusinessEntityDefinition Customer = new()
    {
        Name = "Customer",
        Aliases = ["customer", "debtor", "client", "account holder"],
        Tags = ["accounts receivable", "ar", "sales", "receivables"]
    };

    public static readonly BusinessEntityDefinition Inventory = new()
    {
        Name = "Inventory",
        Aliases = ["inventory", "stock", "product", "item", "sku", "part"],
        Tags = ["warehouse", "stock control", "fulfillment", "catalog"]
    };

    public static readonly BusinessEntityDefinition PurchaseOrder = new()
    {
        Name = "PurchaseOrder",
        Aliases = ["purchase order", "po", "supplier order", "procurement order"],
        Tags = ["purchasing", "procurement", "buying", "inbound"]
    };

    public static readonly BusinessEntityDefinition SalesOrder = new()
    {
        Name = "SalesOrder",
        Aliases = ["sales order", "sales invoice", "customer invoice", "invoice", "customer order"],
        Tags = ["sales", "order to cash", "outbound", "invoicing"]
    };

    public static readonly BusinessEntityDefinition CreditorPurchase = new()
    {
        Name = "CreditorPurchase",
        Aliases = ["supplier invoice", "supplier bill", "vendor bill", "creditor invoice", "ap invoice", "accounts payable invoice"],
        Tags = ["accounts payable", "ap", "supplier transaction", "supplier purchase"]
    };

    public static readonly BusinessEntityDefinition Warehouse = new()
    {
        Name = "Warehouse",
        Aliases = ["warehouse", "location", "storage location"],
        Tags = ["inventory", "stock", "distribution", "operations"]
    };

    public static readonly BusinessEntityDefinition Document = new()
    {
        Name = "Document",
        Aliases = ["document", "attachment", "invoice file", "pdf", "scan"],
        Tags = ["ocr", "extraction", "semantic search", "document intelligence"]
    };

    public static readonly BusinessEntityDefinition File = new()
    {
        Name = "File",
        Aliases = ["file", "csv", "excel", "xml", "json", "local file"],
        Tags = ["upload", "read", "query", "filesystem"]
    };

    public static readonly BusinessEntityDefinition Form = new()
    {
        Name = "Form",
        Aliases = ["form", "screen", "plugin", "module"],
        Tags = ["navigation", "open", "ui", "workspace"]
    };

    public static readonly BusinessEntityDefinition Schema = new()
    {
        Name = "Schema",
        Aliases = ["schema", "dto", "request model", "response model"],
        Tags = ["contract", "shape", "fields", "api model"]
    };

    public static IReadOnlyList<BusinessEntityDefinition> All { get; } =
    [
        Supplier,
        Customer,
        Inventory,
        PurchaseOrder,
        SalesOrder,
        CreditorPurchase,
        Warehouse,
        Document,
        File,
        Form,
        Schema
    ];
}
