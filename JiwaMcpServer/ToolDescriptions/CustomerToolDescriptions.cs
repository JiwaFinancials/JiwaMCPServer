namespace JiwaMcpServer.ToolDescriptions;

public static class CustomerToolDescriptions
{
    public const string QueryCustomers = """
Search and retrieve customers from the live customer database.

IMPORTANT:
• This is the authoritative source for customer information.
• Customers and debtors are equivalent terms.
• Use whenever a customer must be located or identified.
• Most user-supplied customer numbers, debtor numbers, account numbers, customer codes, names, phone numbers, and email addresses are not DebtorIDs.

Use for:
• Searching customers
• Locating a customer
• Resolving identifiers to a DebtorID
• Filtering customers
• Counting customers

Workflow:
• Use this tool first when a DebtorID is not already known.
• Use the returned DebtorID with GetCustomer, ModifyCustomer, DeleteCustomer, or QueryCustomerTransactions.
• Do not ask the user for a DebtorID if it can be determined from this tool.
""";

    public const string GetCustomer = """
Retrieve detailed information for a specific customer.

IMPORTANT:
• Requires a DebtorID.
• Customers and debtors are equivalent terms.
• If the user supplies a customer number, debtor number, account number, code, name, phone number, or email address, use QueryCustomers first.
• Do not ask the user for a DebtorID if QueryCustomers can determine it.

Use for customer details, account information, and customer settings.
""";

    public const string CreateCustomer = """
Create a new customer record.

IMPORTANT:
• Creates live business data.
• Customers and debtors are equivalent terms.
• Ensure required information has been provided before creation.

Use when the user wants to create a customer, debtor, or customer account.
""";

    public const string ModifyCustomer = """
Modify an existing customer record.

IMPORTANT:
• Updates live business data.
• Requires a DebtorID.
• If the customer has not been identified, use QueryCustomers first.
• Do not ask the user for a DebtorID if QueryCustomers can determine it.

Use for customer details, contact information, account settings, and customer maintenance.
""";

    public const string DeleteCustomer = """
Delete an existing customer record.

IMPORTANT:
• Deletes live business data.
• Requires a DebtorID.
• If the customer has not been identified, use QueryCustomers first.
• Use only when the user's intent to delete the customer is explicit.
""";

    public const string QueryCustomerTransactions = """
Search customer transactions from the live database.

IMPORTANT:
• This is the authoritative source for customer transaction information.
• Requires a customer to be identified.
• If the customer has not been identified, use QueryCustomers first.
• Do not ask the user for a DebtorID if QueryCustomers can determine it.

Use for:
• Transactions
• Invoices
• Credits
• Payments
• Account activity
• Transaction history
• Balance analysis
""";

    public const string SearchCustomerClassifications = """
Search and retrieve customer classifications.

Use when classification information or available customer classifications are required.
""";

    public const string SearchCustomerCategories = """
Search and retrieve customer categories.

Use when category information or available customer categories are required.
""";
}