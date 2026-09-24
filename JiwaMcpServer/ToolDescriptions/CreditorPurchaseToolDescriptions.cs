namespace JiwaMcpServer.ToolDescriptions;

public static class CreditorToolDescriptions
{
    public const string QuerySupplierPurchases = """
Search and retrieve supplier purchases, supplier invoices, creditor purchases, and purchase batches from the live purchasing database.

IMPORTANT:
• This is the authoritative source for supplier purchase information.
• Suppliers and creditors are equivalent terms.
• Use whenever the user wants to find, search, list, filter, analyse, compare, or count purchases.

Use this tool to:
• Find purchases or supplier invoices
• Find recent, latest, or most recent purchases
• Search purchases by supplier, invoice, batch, or date range
• Locate purchases before retrieving full details
• Count matching purchases

Workflow:
• Use this tool first when a specific BatchID is not already known.
• Use the returned BatchID with GetCreditorPurchase when detailed information is required.
• Do not ask for a BatchID if it can be discovered through this tool.
""";

    public const string QuerySupplierPurchaseHistory = """
Search supplier purchasing history and historical purchasing activity.

IMPORTANT:
• This is the authoritative source for purchase history information.
• Suppliers and creditors are equivalent terms.

Use this tool to:
• Review supplier purchase history
• Determine what products were purchased
• Find who supplied a product
• Analyse purchasing trends
• Review historical invoices and purchasing activity

Use this tool for historical analysis rather than a specific purchase batch.
""";

    public const string GetCreditorPurchase = """
Retrieve detailed information for a specific supplier purchase transaction.

IMPORTANT:
• Requires an identified purchase or BatchID.
• Suppliers and creditors are equivalent terms.
• Use QuerySupplierPurchases or QuerySupplierPurchaseHistory first when the purchase is not already known.
• Do not ask for a BatchID if it can be discovered using a query tool.

Use for purchase details, supplier invoices, and purchase batch information.
""";

    public const string CreateCreditorPurchase = """
Create a new supplier purchase transaction.

Use when the user wants to create a purchase, supplier invoice, creditor purchase, or purchase batch.

IMPORTANT:
• Creates live business data.
• Ensure required information has been supplied.
• Use GetDtoSchema if field requirements are unclear.
""";

    public const string ModifyCreditorPurchase = """
Modify an existing supplier purchase transaction.

Use when the user wants to update purchasing information, invoice details, or purchase records.

IMPORTANT:
• Updates live business data.
• Ensure the correct purchase has been identified before making changes.
""";

    public const string DeleteCreditorPurchase = """
Delete an existing supplier purchase transaction.

IMPORTANT:
• Deletes live business data.
• Use only when the user's intent to permanently delete the purchase is explicit.
• Do not use for review, correction, amendment, or cancellation requests.
""";

    public const string ActivateCreditorPurchase = """
Activate or finalise a supplier purchase transaction.

Use when the user wants to activate, complete, finalise, or make a purchase active.

IMPORTANT:
• Changes the status of live business data.
• Ensure the correct purchase has been identified before activation.
""";

    public const string GetCreditorPurchaseLines = """
Retrieve line items for a supplier purchase transaction.

Use when the user wants to:
• View purchased products
• Review purchase lines
• Examine quantities and pricing
• See items included in a supplier invoice
""";

    public const string AddCreditorPurchaseLine = """
Add a line item to a supplier purchase transaction.

Use when the user wants to add products or additional items to an existing purchase.

IMPORTANT:
• Modifies live business data.
• Ensure the target purchase exists before adding lines.
""";

    public const string GetCreditorPurchaseLine = """
Retrieve details for a specific supplier purchase line item.

Use when information about an individual purchased product or line item is required.
""";

    public const string ModifyCreditorPurchaseLine = """
Modify a supplier purchase line item.

Use to update quantities, pricing, products, costs, or other line-level purchase information.

IMPORTANT:
• Modifies live business data.
""";

    public const string DeleteCreditorPurchaseLine = """
Delete a line item from a supplier purchase transaction.

IMPORTANT:
• Modifies live business data.
• Use only when the user's intent to remove the line item is explicit.
""";
}
