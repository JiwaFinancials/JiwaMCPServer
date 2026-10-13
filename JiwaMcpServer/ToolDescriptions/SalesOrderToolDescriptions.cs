namespace JiwaMcpServer.ToolDescriptions;

public static class SalesOrderToolDescriptions
{
    public const string GetSalesOrder = """
    Retrieve detailed information for a specific sales order.

    IMPORTANT:
    • Sales orders and sales invoices are equivalent terms.
    • Use this tool after a sales order has been identified.
    • Accepts the order's request DTO with the fields required to locate the specific sales order.

    Use when the user needs the full details of a particular sales order or invoice.
    """;

    public const string CreateSalesOrder = """
    Create a new sales order for a customer.

    IMPORTANT:
    • Sales orders and sales invoices are equivalent terms.
    • Ensure the required customer and line data are provided before creation.
    • This creates live business data.

    Use when the user wants to create a new sales order or invoice.
    """;

    public const string CreateSalesOrderAlias = """
    Alias for CreateSalesOrder. Create a new sales order for a customer.
    """;

    public const string ModifySalesOrder = """
    Update an existing sales order.

    IMPORTANT:
    • Sales orders and sales invoices are equivalent terms.
    • Use this tool to patch or update a current sales order.
    • Supply only the fields being changed in the request DTO.

    Use when the user wants to modify a sales order or invoice.
    """;

    public const string AddItemToSalesOrder = """
    Add a product or line item to an existing sales order.

    IMPORTANT:
    • Sales orders and sales invoices are equivalent terms.
    • If InvoiceHistoryID is omitted, the current history for the invoice is resolved automatically.
    • InvoiceID is required when InvoiceHistoryID is not supplied.

    Use when adding a product, stock item, or line item to a sales order.
    """;

    public const string AddItemToSalesOrderAlias = """
    Alias for AddItemToSalesOrder. Add a product or line item to an existing sales order.
    """;

    public const string ProductSales = """
    Search raw sales history and sold-item lines across customers, products, invoices, and other criteria.

    IMPORTANT:
    • Sales orders and sales invoices are equivalent terms.
    • Use this tool when the user asks for raw sales history, sold-item details, invoice lines, or line-level product sales records.
    • For best selling products, top sellers, top 10 products, quantity sold rankings, or sales-value rankings, prefer SummarizeProductSales.
    • For yearly, quarterly, or monthly sales totals, trends, or charts, prefer SummarizeProductSalesTimeline.
    • Supports confirmation for large result sets and pagination.
    • Do not create charts, trends, or grouped totals from the preview rows of a truncated response.

    Use for raw sales sold-product analysis.
    """;

    public const string SummarizeProductSales = """
    Aggregate product sales server-side and return the top products by quantity sold or sales value.

    IMPORTANT:
    • Use this tool for questions like 'What is my best selling product?', 'top 10 products by quantity sold', or 'top sellers by revenue'.
    • This tool summarizes the full matching sales dataset by product before ranking it, so it can answer best-seller questions without returning hundreds of raw sales lines to chat.
    • Prefer the resultHandle from a previous ProductSales call when available. If neither resultHandle nor requestDTO is supplied, this tool defaults to summarizing all matching sales history.
    • Supported ranking metrics: QuantityDelivered / quantity sold, and LocalLineTotalInc / sales value / revenue.
    • Use top=1 for a single best seller, or a higher number such as 10 for a ranking list.
    • When the output omits a value, do not infer it; answer only from the returned summary rows.

    Use for best-seller, top-product, and ranked product-sales answers.
    """;

    public const string SummarizeProductSalesTimeline = """
    Aggregate product sales server-side into exact time buckets such as year, quarter, or month and return totals from the full matching dataset.

    IMPORTANT:
    • Use this tool for questions like 'sales value per year', 'revenue by month', 'monthly sales trend for 2024', or 'bar chart of sales value per quarter'.
    • Always compute the timeline from the full stored result or a full sales query, never from preview rows shown in chat.
    • Prefer the resultHandle from a previous ProductSales call when available.
    • If no resultHandle is supplied, this tool fetches the full matching sales dataset server-side before grouping it.
    • Supported metrics: QuantityDelivered / quantity sold, and LocalLineTotalInc / sales value / revenue.
    • Supported periods: year, quarter, and month.
    • Supports confirmation for large result sets when requestDTO is used.

    Use for exact sales charts, time-series totals, and trends.
    """;

    public const string QuerySalesOrders = """
    Search and retrieve sales orders from the live database.

    IMPORTANT:
    • Sales orders and sales invoices are equivalent terms.
    • Use this tool when the user wants to show or find sales orders or invoices.
    • Supports confirmation for large result sets and pagination.

    Use for locating, filtering, and reviewing sales orders and invoices.
    """;
}
