using System.ComponentModel;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Models;

public sealed class CustomerPatchToolRequest
{
    [Description("The DebtorID of the existing customer to update when it is already known.")]
    public string? DebtorID { get; set; }

    [Description("A user-supplied customer reference such as AccountNo, AltAccountNo, debtor number, customer code, or customer name. Use this when DebtorID is not known. The tool will resolve the matching DebtorID first.")]
    public string? Identifier { get; set; }

    [Description("Only the customer fields to change. Omit unchanged fields. Example: {\"Phone\":\"03 9000 0000\",\"Email\":\"accounts@example.com\"}.")]
    public JsonObject Changes { get; set; } = [];
}

public sealed class CustomerDeleteToolRequest
{
    [Description("The DebtorID of the existing customer to delete when it is already known.")]
    public string? DebtorID { get; set; }

    [Description("A user-supplied customer reference such as AccountNo, AltAccountNo, debtor number, customer code, or customer name. Use this when DebtorID is not known. The tool will resolve the matching DebtorID first.")]
    public string? Identifier { get; set; }
}

public sealed class ProductPatchToolRequest
{
    [Description("The InventoryID of the existing product to update.")]
    public string InventoryID { get; set; } = string.Empty;

    [Description("Only the product fields to change. Omit unchanged fields. Example: {\"Description\":\"New description\",\"SellPrice1\":19.95}.")]
    public JsonObject Changes { get; set; } = [];
}

public sealed class ProductDeleteToolRequest
{
    [Description("The InventoryID of the existing product to delete when it is already known.")]
    public string? InventoryID { get; set; }

    [Description("The PartNo of the existing product to delete when InventoryID is not known. The tool will resolve the matching InventoryID first.")]
    public string? PartNo { get; set; }
}

public sealed class CreditorPurchasePatchToolRequest
{
    [Description("The BatchID of the supplier purchase transaction to update.")]
    public string BatchID { get; set; } = string.Empty;

    [Description("Only the purchase fields to change. Omit unchanged fields. Example: {\"Reference\":\"INV-10025\",\"Notes\":\"Urgent\"}.")]
    public JsonObject Changes { get; set; } = [];
}

public sealed class CreditorPurchaseLinePatchToolRequest
{
    [Description("The BatchID of the supplier purchase that owns the line.")]
    public string BatchID { get; set; } = string.Empty;

    [Description("The LineID of the supplier purchase line to update.")]
    public string LineID { get; set; } = string.Empty;

    [Description("Only the line fields to change. Omit unchanged fields. Example: {\"Quantity\":5,\"UnitCost\":12.50}.")]
    public JsonObject Changes { get; set; } = [];
}
