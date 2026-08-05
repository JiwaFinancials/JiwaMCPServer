namespace JiwaMcpServer.ToolMetadata;

public static class JiwaBusinessEntities
{
    public static readonly BusinessEntityDefinition Supplier = new()
    {
        Name = "Supplier",
        Aliases = ["supplier", "creditor", "vendor"],
        Tags = ["accounts payable", "ap", "purchasing", "procurement"]
    };

    public static readonly BusinessEntityDefinition Customer = new()
    {
        Name = "Customer",
        Aliases = ["customer", "debtor", "client"],
        Tags = ["accounts receivable", "ar", "sales", "receivables"]
    };

    public static readonly BusinessEntityDefinition Inventory = new()
    {
        Name = "Inventory",
        Aliases = ["inventory", "stock", "item", "part", "product", "sku"],
        Tags = ["warehouse", "stock control", "fulfillment", "catalog"]
    };

    public static readonly BusinessEntityDefinition PurchaseOrder = new()
    {
        Name = "PurchaseOrder",
        Aliases = ["purchase order", "po", "supplier order", "creditor order"],
        Tags = ["purchasing", "procurement", "buying", "inbound"]
    };

    public static readonly BusinessEntityDefinition SalesOrder = new()
    {
        Name = "SalesOrder",
        Aliases = ["sales order", "sales invoice", "invoice", "so", "customer order"],
        Tags = ["sales", "order to cash", "outbound", "invoicing"]
    };

    public static readonly BusinessEntityDefinition Warehouse = new()
    {
        Name = "Warehouse",
        Aliases = ["warehouse", "store", "location", "logical warehouse"],
        Tags = ["inventory", "stock", "distribution", "operations"]
    };

    public static IReadOnlyList<BusinessEntityDefinition> All { get; } =
    [
        Supplier,
        Customer,
        Inventory,
        PurchaseOrder,
        SalesOrder,
        Warehouse
    ];
}
