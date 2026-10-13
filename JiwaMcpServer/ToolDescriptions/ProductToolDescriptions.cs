namespace JiwaMcpServer.ToolDescriptions;

public static class ProductToolDescriptions
{
    public const string QueryInventory = """
    Search and retrieve products, parts, pricing, categories, classifications, and inventory records from the live inventory database.

    IMPORTANT:
    • This is the authoritative source for all product and inventory information.
    • The AI model does not have access to inventory data without this tool.
    • Use whenever the answer depends on products, SKUs, part numbers, pricing, stock items, categories, classifications, or inventory records.
    • Do not use this tool for sales-history questions such as best selling products, top sellers, sold items, or product sales analysis. Use ProductSales instead.
    • When the user asks for counts, summaries, groupings, or totals based on the returned inventory rows (for example: 'count parts by classification', 'group by first letter of part no', 'totals by category'), do not count the preview rows in chat. Use the returned resultHandle with AggregateInventoryCounts.

    Use this tool to:
    • Find products or parts
    • Search inventory
    • Look up prices or RRP
    • Find products by SKU or part number
    • Filter products by text, categories, classifications, prices, or dates
    • Count matching products
    • Compare products
    • Find recently modified products

    Workflow:
    • Use QueryInventory first to locate matching items.
    • If the user then asks for grouped counts or summaries, call AggregateInventoryCounts using the resultHandle from this response instead of calling QueryInventory again.
    • Use the returned InventoryID with:
      - GetProduct
      - GetStockOnHand
      - GetProductPicture

    For large exports, retrieve matching data and then use CreateDataExport.
    """;

    public const string GetProduct = """
Retrieve detailed information for a product using InventoryID.

Use this tool when the user wants product details, settings, pricing, classifications, categories, or inventory attributes.

IMPORTANT:
• Requires InventoryID.
• Do not use for product images. Use GetProductPicture instead.
• Use GetDtoSchema if field information is needed.
""";

    public const string CreateProduct = """
Create a new product in the live inventory system.

Use when the user wants to add a product, inventory item, stock item, service item, SKU, catalog item, or part.

IMPORTANT:
• Creates live business data.
• Ensure required fields are provided.
• Use GetDtoSchema if field information is needed.
• Obtain missing mandatory information before creation.
""";

    public const string ModifyProduct = """
Modify an existing product.

Use when the user wants to change product information such as descriptions, pricing, classifications, categories, or settings.

IMPORTANT:
• Updates live business data.
• The product must already exist.
• If only a PartNo or SKU is provided, use QueryInventory first to obtain InventoryID.
• Supply only the fields being changed inside Changes. Do not send the full product payload.
• Use GetDtoSchema if field information is needed.
""";

    public const string DeleteProduct = """
Delete an existing product.

Use only when the user explicitly requests permanent removal of a product.

IMPORTANT:
• Deletes live business data.
• Accepts InventoryID directly, or PartNo and resolves the matching InventoryID before deleting.
• If both are available, prefer InventoryID.
• Verify the correct product before deleting.
• Do not use for deactivate, discontinue, hide, or modify requests.
""";

    public const string GetStockOnHand = """
Retrieve stock-on-hand quantities and inventory balances.

This is the authoritative source for stock quantity information.

Use when the user wants to:
• Check stock levels
• Find products in stock or out of stock
• View warehouse quantities
• Review inventory balances

IMPORTANT:
• Supports filtering and pagination.
• Large result sets may require confirmation.
• Use CreateDataExport for full downloadable inventory exports.
""";

    public const string SearchProductClassifications = """
Search and retrieve product classifications.

Use when the user wants to:
• List classifications
• Search classifications
• Explore inventory grouping structures
• Find available classifications

IMPORTANT:
• Returns classification records, not products.
• For simple list requests, request only the fields needed to answer, usually Description unless the user asked for identifiers or flags as well.
• If the returned result set is small and not truncated, list the classifications directly in chat instead of offering export.
• Supports pagination.
""";

    public const string SearchProductCategories = """
    Search and retrieve product categories.

    Use when the user wants to:
    • List categories
    • Search categories
    • Find category values
    • Explore category structures

    IMPORTANT:
    • Returns category records, not products.
    • For simple list requests, request only the fields needed to answer, usually Description unless the user asked for identifiers or flags as well.
    • If the returned result set is small and not truncated, list the categories directly in chat instead of offering export.
    • Supports pagination.
    """;

    public const string AggregateInventoryCounts = """
    Aggregate a full inventory result set by a field or transform and count rows server-side.

    Use when the user asks for counts or totals grouped by a field such as:
    • InventoryClassificationID
    • CategoryID
    • WarehouseID
    • PartNo prefix or first letter of PartNo
    • 'how many parts per classification?'
    • 'count products by first letter of part number'
    • 'group products by category and show totals'
    • any other product field

    IMPORTANT:
    • Always compute the aggregation from the full stored result, never from a truncated chat preview.
    • Prefer a resultHandle returned by QueryInventory or a related inventory query.
    • If no resultHandle is supplied, first fetch the full matching inventory data, then aggregate it.
    • This tool is for exact counts and grouping summaries, not for displaying raw row data.
    • Do not ask the user to confirm again or ask whether to show a sample when the user has already requested a grouped count or summary.
    """;

    public const string GetProductPicture = """
Retrieve a product image using InventoryID or PartNo.

Use when the user requests:
• A product image
• A product picture
• A product photo

IMPORTANT:
• Requires InventoryID or PartNo.
• Returns image content.
• Use GetProduct instead when only product details are required.
""";
}