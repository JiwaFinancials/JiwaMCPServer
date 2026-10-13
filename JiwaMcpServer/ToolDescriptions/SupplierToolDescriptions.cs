namespace JiwaMcpServer.ToolDescriptions;

public static class SupplierToolDescriptions
{
    public const string QuerySuppliers = """
Search and retrieve suppliers from the live supplier database.

IMPORTANT:
• This is the authoritative source for supplier information.
• Suppliers and creditors are equivalent terms.
• Use whenever a supplier must be located or identified.
• Most user-supplied supplier numbers, creditor numbers, account numbers, supplier codes, names, phone numbers, and email addresses are not CreditorIDs.

Use for:
• Searching suppliers
• Locating a supplier
• Resolving identifiers to a CreditorID
• Filtering suppliers
• Counting suppliers

Workflow:
• Use this tool first when a CreditorID is not already known.
• Use the returned CreditorID with GetSupplier, ModifySupplier, DeleteSupplier, or SearchSupplierClassifications.
• Do not ask the user for a CreditorID if it can be determined from this tool.
""";

    public const string GetSupplier = """
Retrieve detailed information for a specific supplier.

IMPORTANT:
• Suppliers and creditors are equivalent terms.
• Accepts either a CreditorID or a user-supplied Identifier such as AccountNo, AltAccountNo, creditor number, supplier code, or exact supplier name.
• If Identifier is used, the tool resolves the matching CreditorID first.
• If the user supplies a supplier number, creditor number, account number, code, name, phone number, or email address, prefer QuerySuppliers first when the supplier still needs to be located or disambiguated.
• Do not ask the user for a CreditorID if QuerySuppliers can determine it.

Use for supplier details, account information, and supplier settings.
""";

    public const string CreateSupplier = """
Create a new supplier record.

IMPORTANT:
• Creates live business data.
• Suppliers and creditors are equivalent terms.
• Ensure required information has been provided before creation.

Use when the user wants to create a supplier, creditor, or supplier account.
""";

    public const string ModifySupplier = """
Modify an existing supplier record.

IMPORTANT:
• Updates live business data.
• Accepts either a CreditorID or a user-supplied Identifier such as AccountNo, AltAccountNo, creditor number, supplier code, or exact supplier name.
• If Identifier is used, the tool resolves the matching CreditorID first.
• If the supplier has not been identified, use QuerySuppliers first.
• Supply only the fields being changed inside Changes. Do not send the full supplier payload.
• Do not ask the user for a CreditorID if QuerySuppliers can determine it.

Use for supplier details, contact information, account settings, and supplier maintenance.
""";

    public const string DeleteSupplier = """
Delete an existing supplier record.

IMPORTANT:
• Deletes live business data.
• Accepts either a CreditorID or a user-supplied Identifier such as AccountNo, AltAccountNo, creditor number, supplier code, or exact supplier name.
• If Identifier is used, the tool resolves the matching CreditorID first.
• If the supplier has not been identified, use QuerySuppliers first.
• Use only when the user's intent to delete the supplier is explicit.
""";

    public const string SearchSupplierClassifications = """
Search and retrieve supplier classifications.

Use when classification information or available supplier classifications are required.
""";
}
