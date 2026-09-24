namespace JiwaMcpServer.ToolDescriptions;

public static class ProductToolDescriptions
{
    public const string QueryInventory = """
Search and retrieve products, parts, pricing, categories, classifications, and inventory records from the live inventory database.

IMPORTANT:
• This is the authoritative source for all product and inventory information.
• The AI model does not have access to inventory data without this tool.
• Use whenever the answer depends on products, SKUs, part numbers, pricing, stock items, categories, classifications, or inventory records.

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
• Use GetDtoSchema if field information is needed.
""";

    public const string DeleteProduct = """
Delete an existing product.

Use only when the user explicitly requests permanent removal of a product.

IMPORTANT:
• Deletes live business data.
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
• Supports pagination.
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