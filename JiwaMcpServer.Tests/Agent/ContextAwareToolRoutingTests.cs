using JiwaMcpServer.Agent;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.LlmClient;
using JiwaMcpServer.Agent.Models;
using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Agent.SchemaResolver;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tests.Agent;

public class ContextAwareToolRoutingTests
{
    [Fact]
    public async Task CustomerLookupFollowedByCustomerUpdate_SelectsModifyCustomer()
    {
        var router = CreateActualRouter();

        var context = await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "customer-update",
            UserMessage = "Show customer 1149",
            ToolName = "QueryCustomers",
            ToolResultText = """
            {"total":1,"returned":1,"pageSize":100,"truncated":false,"results":[{"DebtorID":901,"CustomerNumber":"1149","DebtorName":"Armadale Shire Council","PurchaseHistory":"extensive supplier purchases","Budget":"annual budget","Transactions":"many transactions","Pricing":"special pricing","Contacts":"multiple contacts"}],"message":null}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "customer-update",
            Prompt = "Change the phone to 03 9509 1113",
            MaxTools = 5
        });

        Assert.Equal("Customer", context.FocusedEntity?.EntityType);
        Assert.Equal("901", context.FocusedEntity?.EntityId);
        Assert.Equal("ModifyCustomer", route.Tools[0].Name);
        Assert.Equal("Customer", route.Intent.EntityType);
        Assert.Equal("Update", route.Intent.Action);
        Assert.DoesNotContain("purchase history", route.WorkingContext.ConversationSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvoiceLookupFollowedByInvoiceUpdate_SelectsInvoiceTool()
    {
        var router = CreateSyntheticRouter(CreateSyntheticCatalog());

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "invoice-update",
            UserMessage = "Show invoice 50021",
            ToolName = "GetInvoice",
            ToolResultText = """
            {"InvoiceID":"50021","InvoiceNumber":"50021","DisplayName":"Invoice 50021","CustomerName":"Acme Pty Ltd"}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "invoice-update",
            Prompt = "Change the due date to 2026-10-01",
            MaxTools = 5
        });

        Assert.Equal("ModifyInvoice", route.Tools[0].Name);
        Assert.Equal("Invoice", route.Intent.EntityType);
        Assert.Equal("Update", route.Intent.Action);
    }

    [Fact]
    public async Task PurchaseOrderLookupFollowedByApprove_SelectsApprovePurchaseOrder()
    {
        var router = CreateSyntheticRouter(CreateSyntheticCatalog());

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "po-approve",
            UserMessage = "Show purchase order PO-1004",
            ToolName = "GetPurchaseOrder",
            ToolResultText = """
            {"PurchaseOrderID":"PO-1004","DisplayName":"PO-1004","Status":"Pending"}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "po-approve",
            Prompt = "Approve it",
            MaxTools = 5
        });

        Assert.Equal("ApprovePurchaseOrder", route.Tools[0].Name);
        Assert.Equal("PurchaseOrder", route.Intent.EntityType);
        Assert.Equal("Approve", route.Intent.Action);
    }

    [Fact]
    public async Task AmbiguousFollowUpOnCustomerContext_PrefersCustomerTransactions()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "customer-transactions",
            UserMessage = "Show customer 1149",
            ToolName = "GetCustomer",
            ToolResultText = """
            {"DebtorID":901,"CustomerNumber":"1149","DebtorName":"Armadale Shire Council"}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "customer-transactions",
            Prompt = "Show transactions",
            MaxTools = 5
        });

        Assert.Equal("QueryCustomerTransactions", route.Tools[0].Name);
        Assert.Equal("Customer", route.WorkingContext.FocusedEntity?.EntityType);
        Assert.Equal("Query", route.Intent.Action);
    }

    [Fact]
    public async Task ConfirmAfterLargeInventoryExportRequest_PreservesExportIntentAndIncludesDocumentTools()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-confirm",
            UserMessage = "Give me a downloadable csv of parts. Only include PartNo, Description, AvailableStock, SellPrice.",
            ToolName = "QueryInventory",
            ToolResultText = "WARNING: This query will return 1485 records which exceeds the threshold of 100. Please confirm with the user before proceeding."
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "inventory-export-confirm",
            Prompt = "Confirm",
            MaxTools = 5
        });

        Assert.Equal("QueryInventory", route.Tools[0].Name);
        Assert.Contains(route.Tools, tool => tool.Name == "CreateDataExport");
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "GetProduct");
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "GetStockOnHand");
        Assert.Contains("downloadable csv export", route.WorkingContext.ConversationSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DocumentTools", route.Domains);
        Assert.Contains("InventoryTools", route.Domains);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public async Task AttachedPdfInCreditorPurchaseRequest_IncludesDocumentExtractionTools(int maxTools)
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Create a creditor purchase from the attached pdf. Create a new creditor if required.",
            MaxTools = maxTools
        });

        Assert.Contains("CreditorPurchaseTools", route.Domains);
        Assert.Contains("SupplierTools", route.Domains);
        Assert.Contains("DocumentTools", route.Domains);
        Assert.Contains(route.Tools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(route.Tools, tool => tool.Name == "CreateSupplier");
        Assert.Contains(route.Tools, tool => tool.Name == "QuerySuppliers");
        Assert.Contains(route.Tools, tool => tool.Name == "ExtractLocalDocumentData");
        Assert.Contains(route.Tools, tool => tool.Name == "UploadLocalDocument");
        Assert.Contains(route.Tools, tool => tool.Name == "QueryUploadedDocument");
        Assert.True(route.Tools.Count <= maxTools);
    }

    [Theory]
    [InlineData("Create a creditor purchase. Create a new creditor if required.")]
    [InlineData("Create a creditor purchase and create a new creditor if required.")]
    [InlineData("Create a creditor purchase; then create a new creditor if required.")]
    [InlineData("Create a creditor purchase and a new creditor if required.")]
    public async Task ChainedBusinessActions_IncludeBothCreationToolsAndSupplierLookup(string prompt)
    {
        var route = await CreateActualRouter().ResolveAsync(new ToolRouteRequest { Prompt = prompt, MaxTools = 5 });

        Assert.Contains(route.Tools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(route.Tools, tool => tool.Name == "CreateSupplier");
        Assert.Contains(route.Tools, tool => tool.Name == "QuerySuppliers");
        Assert.True(route.Tools.Count <= 5);
    }

    [Fact]
    public async Task ChainedActionsWithDifferentVerbs_PreserveBothIntents()
    {
        var route = await CreateActualRouter().ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Create a creditor purchase and update supplier Telstra's phone.",
            MaxTools = 5
        });

        Assert.Contains(route.Tools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(route.Tools, tool => tool.Name == "ModifySupplier");
        Assert.Contains(route.Tools, tool => tool.Name == "QuerySuppliers");
    }

    [Fact]
    public async Task ChainedCustomerAndProductCreation_PreservesOtherBusinessDomains()
    {
        var route = await CreateActualRouter().ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Create a customer and create a product.",
            MaxTools = 5
        });

        Assert.Contains(route.Tools, tool => tool.Name == "CreateCustomer");
        Assert.Contains(route.Tools, tool => tool.Name == "CreateProduct");
        Assert.Contains(route.Tools, tool => tool.Name == "QueryCustomers");
        Assert.Contains(route.Tools, tool => tool.Name == "QueryInventory");
    }

    [Fact]
    public async Task ChainedActionsWithInsufficientBudget_ReportIncompleteCoverage()
    {
        var route = await CreateActualRouter().ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Create a creditor purchase. Create a new creditor if required.",
            MaxTools = 1
        });

        Assert.Single(route.Tools);
        Assert.Contains(route.SelectionAdjustments, adjustment => adjustment.Contains("too small", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ConfirmWithoutContext_DoesNotFallBackToFullCatalog()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Confirm",
            MaxTools = 20
        });

        Assert.Empty(route.Tools);
        Assert.Contains(route.SelectionAdjustments, adjustment => adjustment.Contains("no recoverable context", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ConfirmWithSummaryOnly_PrefersInventoryToolsOverFullCatalog()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Confirm",
            ConversationSummary = "User asked for a downloadable csv export of parts with stock and sell price.",
            MaxTools = 5
        });

        Assert.NotEmpty(route.Tools);
        Assert.Equal("QueryInventory", route.Tools[0].Name);
        Assert.Contains(route.Tools, tool => tool.Name == "CreateDataExport");
        Assert.DoesNotContain(route.Tools, tool => tool.Domain == "CustomerTools");
    }

    [Fact]
    public async Task CreateDataExportAfterInventoryQuery_PreservesInventoryFocus()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-follow-up",
            UserMessage = "Give me a list of parts",
            ToolName = "QueryInventory",
            ToolResultText = """
            {"total":1,"returned":1,"pageSize":100,"truncated":false,"results":[{"InventoryID":"955a7c7884104808ae97","PartNo":"T1170","Description":"Widget"}],"message":null}
            """
        });

        var context = await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-follow-up",
            UserMessage = "make it a downloadable csv file",
            ToolName = "CreateDataExport",
            ToolArgumentsJson = """
            {"useLatestStructuredToolResult":true,"format":"csv","fileName":"InventoryPartsList.csv","sourceToolName":"query_inventory"}
            """,
            ToolResultText = "Created 'InventoryPartsList.csv' (26808 bytes). FileId: a1e2ccd93d544df0bb9c65c7379a65b4. Resource URI: jiwa-file://files/a1e2ccd93d544df0bb9c65c7379a65b4."
        });

        Assert.Equal("Product", context.FocusedEntity?.EntityType);
        Assert.Equal("955a7c7884104808ae97", context.FocusedEntity?.EntityId);
        Assert.Equal("Widget", context.FocusedEntity?.DisplayName);
        Assert.Equal("InventoryTools", context.CurrentDomain);
    }

    [Fact]
    public async Task ExportFollowUpAfterConfirmedInventoryList_PrefersCreateDataExport()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-after-confirm",
            UserMessage = "Give me a list of parts",
            ToolName = "QueryInventory",
            ToolResultText = "WARNING: This query will return 297 records which exceeds the threshold of 100. Please confirm with the user before proceeding."
        });

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-after-confirm",
            UserMessage = "show all parts",
            ToolName = "QueryInventory",
            ToolResultText = """
            {"text":"Showing the first 10 of 297 records. The full result set is stored on the server for follow-up actions and exports.","total":297,"returned":10,"pageSize":100,"truncated":true,"results":[{"InventoryID":"955a7c7884104808ae97","PartNo":"111345","Description":"Bottled Fiji Water"}],"resultHandle":"bd86f7beef934b54b8f7b3c69cf7c3be"}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "inventory-export-after-confirm",
            Prompt = "make it a downloadable csv file",
            MaxTools = 5
        });

        Assert.Equal("CreateDataExport", route.Tools[0].Name);
        Assert.Contains("DocumentTools", route.Domains);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "ModifyProduct");
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "DeleteProduct");
    }

    [Fact]
    public async Task CustomerFollowUpAfterDebtorExportWithSnakeCaseToolNames_PrefersQueryCustomers()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "customer-export-follow-up",
            UserMessage = "give me a list of debtors",
            ToolName = "query_customers",
            ToolResultText = """
            {"total":35,"returned":35,"pageSize":100,"truncated":false,"results":[{"DebtorID":"000000000C000000000T","AccountNo":"1149","Name":"Armadale Shire Council"}],"message":null}
            """
        });

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "customer-export-follow-up",
            UserMessage = "make it a downloadable excel file",
            ToolName = "create_data_export",
            ToolArgumentsJson = """
            {"request":{"useLatestStructuredToolResult":true,"format":"xlsx","fileName":"debtors.xlsx"}}
            """,
            ToolResultText = "Created 'debtors.xlsx' (7838 bytes). FileId: c3673e1d7931478ea96f32268b9bdd95. Resource URI: jiwa-file://files/c3673e1d7931478ea96f32268b9bdd95."
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "customer-export-follow-up",
            Prompt = "tell me more about 1149",
            MaxTools = 5
        });

        Assert.Equal("QueryCustomers", route.Tools[0].Name);
        Assert.Equal("Customer", route.Intent.EntityType);
        Assert.Equal("1149", route.Intent.Parameters["identifier"]);
        Assert.DoesNotContain(route.Tools, tool => tool.Domain == "InventoryTools");
    }

    [Fact]
    public async Task SummaryOnlyCustomerExportFollowUp_PrefersQueryCustomers()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "tell me more about 1149",
            ConversationSummary = "User is working with customer Armadale Shire Council (000000000C000000000T). Current domain: customer. Current task: query customer (identifier). Requested output: downloadable xlsx export. Recent request: make it a downloadable excel file. Last tool: CreateDataExport.",
            MaxTools = 5
        });

        Assert.Equal("QueryCustomers", route.Tools[0].Name);
        Assert.Equal("Customer", route.Intent.EntityType);
        Assert.Equal("1149", route.Intent.Parameters["identifier"]);
        Assert.DoesNotContain(route.Tools, tool => tool.Domain == "InventoryTools");
    }

    [Fact]
    public async Task ProductFollowUpAfterInventoryExport_PrefersQueryInventory()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-product-follow-up",
            UserMessage = "Give me a list of parts",
            ToolName = "QueryInventory",
            ToolResultText = """
            {"total":1,"returned":1,"pageSize":100,"truncated":false,"results":[{"InventoryID":"955a7c7884104808ae97","PartNo":"T1170","Description":"Widget"}],"message":null}
            """
        });

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-export-product-follow-up",
            UserMessage = "make it a downloadable csv file",
            ToolName = "CreateDataExport",
            ToolArgumentsJson = """
            {"useLatestStructuredToolResult":true,"format":"csv","fileName":"InventoryPartsList.csv","sourceToolName":"query_inventory"}
            """,
            ToolResultText = "Created 'InventoryPartsList.csv' (26808 bytes). FileId: a1e2ccd93d544df0bb9c65c7379a65b4. Resource URI: jiwa-file://files/a1e2ccd93d544df0bb9c65c7379a65b4."
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "inventory-export-product-follow-up",
            Prompt = "tell me more about T1170",
            MaxTools = 5
        });

        Assert.Equal("QueryInventory", route.Tools[0].Name);
        Assert.Equal("Read", route.Intent.Action);
        Assert.Equal("Product", route.Intent.EntityType);
        Assert.Equal("T1170", route.Intent.Parameters["identifier"]);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "QueryUploadedDocument");
    }

    [Fact]
    public async Task PartNumberRrpUpdate_PrefersQueryInventoryBeforeModifyProduct()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Change the RRP of T1170 to 210",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Contains(route.Tools, tool => tool.Name == "QueryInventory");
        Assert.Equal("QueryInventory", route.Tools[0].Name);
    }

    [Fact]
    public async Task CountPartsByClassification_PrefersAggregateInventoryCounts()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "show me the list again but with a count of parts that belong to each classification",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Contains(route.Tools, tool => tool.Name == "AggregateInventoryCounts");
        Assert.Equal("AggregateInventoryCounts", route.Tools[0].Name);
    }

    [Fact]
    public async Task ConfirmAfterGroupedInventoryCountRequest_PrefersAggregateInventoryCounts()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "inventory-count-confirm",
            UserMessage = "show me the list again but with a count of parts that belong to each classification",
            ToolName = "QueryInventory",
            ToolResultText = "{\"total\":297,\"returned\":10,\"pageSize\":100,\"truncated\":true,\"results\":[{\"InventoryClassificationID\":\"A\"},{\"InventoryClassificationID\":\"B\"}],\"resultHandle\":\"abc123\"}"
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "inventory-count-confirm",
            Prompt = "confirm",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Contains(route.Tools, tool => tool.Name == "AggregateInventoryCounts");
        Assert.Equal("AggregateInventoryCounts", route.Tools[0].Name);
    }

    [Fact]
    public async Task DeleteProductByPartNumber_PrefersQueryInventoryBeforeDeleteProduct()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Delete the part with an part no of 000Test",
            MaxTools = 1
        });

        Assert.NotNull(route.Tools);
        Assert.Single(route.Tools);
        Assert.Equal("QueryInventory", route.Tools[0].Name);
        Assert.Equal("000Test", route.Intent.Parameters["identifier"]);
    }

    [Fact]
    public async Task StockQueryByPartNumber_PrefersStockToolAndNotModifyDelete()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "How much stock do I have of part no T1170",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Contains(route.Tools, tool => tool.Name == "GetStockOnHand");
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "ModifyProduct");
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "DeleteProduct");
        Assert.Equal("Product", route.Intent.EntityType);
        Assert.Equal("Read", route.Intent.Action);
    }

    [Fact]
    public async Task StockQueryForFocusedProductPronoun_PrefersGetStockOnHand()
    {
        var router = CreateActualRouter();

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "product-stock-pronoun",
            UserMessage = "tell me more about T1170",
            ToolName = "GetProduct",
            ToolResultText = "{\"InventoryID\":\"000000000K00000000BQ\",\"PartNo\":\"T1170\",\"Description\":\"Keyboard - Wireless Illuminated Compact\"}"
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "product-stock-pronoun",
            Prompt = "how much stock of it do i have?",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("GetStockOnHand", route.Tools[0].Name);
        Assert.Equal("Product", route.Intent.EntityType);
        Assert.Equal("Read", route.Intent.Action);
        Assert.Equal("000000000K00000000BQ", route.Intent.Parameters["identifier"]);
    }

    [Fact]
    public async Task DeleteCustomerByCustomerNumber_PrefersQueryCustomersBeforeDeleteCustomer()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Delete customer 1001",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Contains(route.Tools, tool => tool.Name == "QueryCustomers");
        Assert.Equal("QueryCustomers", route.Tools[0].Name);
        Assert.Equal("1001", route.Intent.Parameters["identifier"]);
    }

    [Fact]
    public async Task SaleOrdersForCustomer_PrefersListSalesOrdersOverCustomerTools()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Give me a list of sale orders for customer 1001",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Contains(route.Tools, tool => tool.Name == "ListSalesOrders");
        Assert.Equal("ListSalesOrders", route.Tools[0].Name);
        Assert.Equal("SalesOrder", route.Intent.EntityType);
        Assert.Equal("Query", route.Intent.Action);
        Assert.Equal("1001", route.Intent.Parameters["identifier"]);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "QueryCustomers");
    }

    [Fact]
    public async Task PartsQueryAfterCustomerContext_PrefersQueryInventory()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Give me a list of parts",
            ConversationSummary = "User is maintaining customer account 1149 and updating contact information.",
            WorkingContext = new WorkingContext
            {
                FocusedEntity = new ActiveEntity
                {
                    EntityType = "Customer",
                    EntityId = "1149",
                    DisplayName = "Armadale Shire Council"
                },
                CurrentDomain = "CustomerTools"
            },
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("QueryInventory", route.Tools[0].Name);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "QueryCustomers");
    }

    [Fact]
    public async Task BestSellingProductPrompt_PrefersSummarizeProductSales()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "What is my best selling product?",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("SummarizeProductSales", route.Tools[0].Name);
        Assert.Contains("SalesOrderTools", route.Domains);
        Assert.Equal("SalesOrder", route.Intent.EntityType);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "QueryInventory");
    }

    [Fact]
    public async Task TopProductsByQuantitySoldPrompt_PrefersSummarizeProductSales()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "top 10 products list by quantity sold",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("SummarizeProductSales", route.Tools[0].Name);
        Assert.Contains("SalesOrderTools", route.Domains);
        Assert.Equal("SalesOrder", route.Intent.EntityType);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "QueryInventory");
    }

    [Fact]
    public async Task TopProductsBySalesValuePrompt_PrefersSummarizeProductSales()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "what is my top ten best selling products by sales value?",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("SummarizeProductSales", route.Tools[0].Name);
        Assert.Contains("SalesOrderTools", route.Domains);
        Assert.Equal("SalesOrder", route.Intent.EntityType);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "QueryInventory");
    }

    [Fact]
    public async Task AnnualSalesValueChartPrompt_PrefersSummarizeProductSalesTimelineOverRawSalesLines()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "give me a bar chart of sales value per year",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("SummarizeProductSalesTimeline", route.Tools[0].Name);
        Assert.Contains("SalesOrderTools", route.Domains);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "ProductSales");
    }

    [Fact]
    public async Task MonthlySalesValuePrompt_PrefersSummarizeProductSalesTimeline()
    {
        var router = CreateActualRouter();

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "show me sales value per month for 2024",
            MaxTools = 5
        });

        Assert.NotNull(route.Tools);
        Assert.Equal("SummarizeProductSalesTimeline", route.Tools[0].Name);
        Assert.Contains("SalesOrderTools", route.Domains);
        Assert.DoesNotContain(route.Tools, tool => tool.Name == "ProductSales");
    }

    [Fact]
    public async Task LargeLegacyConversationContext_DoesNotContaminateRanking()
    {
        var router = CreateActualRouter();
        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            Prompt = "Change the phone to 03 9509 1113",
            ConversationContext = string.Join(' ', Enumerable.Repeat("purchase history budget transactions pricing contacts supplier invoice payments", 40)),
            ConversationSummary = "User is maintaining customer account 1149 and updating contact information.",
            WorkingContext = new WorkingContext
            {
                FocusedEntity = new ActiveEntity
                {
                    EntityType = "Customer",
                    EntityId = "1149",
                    DisplayName = "Armadale Shire Council"
                },
                CurrentDomain = "CustomerTools"
            },
            MaxTools = 5
        });

        Assert.Equal("ModifyCustomer", route.Tools[0].Name);
        Assert.Equal("User is maintaining customer account 1149 and updating contact information.", route.RankingInput.ConversationSummary);
    }

    [Fact]
    public async Task PronounResolution_UsesFocusedEntityForFollowUpRequest()
    {
        var router = CreateSyntheticRouter(CreateSyntheticCatalog());

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "invoice-pronoun",
            UserMessage = "Show invoice 50021",
            ToolName = "GetInvoice",
            ToolResultText = """
            {"InvoiceID":"50021","InvoiceNumber":"50021","DisplayName":"Invoice 50021"}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "invoice-pronoun",
            Prompt = "Change it",
            MaxTools = 5
        });

        Assert.Equal("ModifyInvoice", route.Tools[0].Name);
        Assert.Equal("Invoice", route.Intent.EntityType);
    }

    [Fact]
    public async Task ToolSelectionOverride_CanReplaceShortlistForEmergencyPrompt()
    {
        var router = CreateSyntheticRouter(CreateSyntheticCatalog(), [new EmergencyCustomerFallbackOverride()]);

        await router.UpdateContextAsync(new ToolExecutionContextUpdateRequest
        {
            ConversationId = "override-recovery",
            UserMessage = "Show purchase order PO-1004",
            ToolName = "GetPurchaseOrder",
            ToolResultText = """
            {"PurchaseOrderID":"PO-1004","DisplayName":"PO-1004","Status":"Pending"}
            """
        });

        var route = await router.ResolveAsync(new ToolRouteRequest
        {
            ConversationId = "override-recovery",
            Prompt = "Approve it. emergency customer fallback",
            MaxTools = 5
        });

        Assert.Single(route.Tools);
        Assert.Equal("GetCustomer", route.Tools[0].Name);
        Assert.Equal(["CustomerTools"], route.Domains);
        Assert.Contains(route.SelectionAdjustments, adjustment => adjustment.Contains("EmergencyCustomerFallbackOverride", StringComparison.Ordinal));
    }

    private static IToolRouter CreateActualRouter()
    {
        var catalog = JiwaToolCatalogFactory.Create(new ToolRegistry([typeof(AgentServiceExtensions).Assembly]));
        return CreateSyntheticRouter(catalog);
    }

    private static IToolRouter CreateSyntheticRouter(IToolCatalog catalog, IEnumerable<IToolSelectionOverride>? selectionOverrides = null)
    {
        var intentExtractor = new IntentExtractor();
        var contextService = new ToolRoutingContextService(catalog, intentExtractor);
        var shortlistService = new ToolShortlistService(
            catalog,
            new DomainRouter(),
            new FakeToolSchemaResolver(),
            intentExtractor,
            contextService,
            selectionOverrides ?? [],
            NullLogger<ToolShortlistService>.Instance);

        return new ToolRouter(shortlistService, contextService, NullLogger<ToolRouter>.Instance);
    }

    private static IToolCatalog CreateSyntheticCatalog()
    {
        var domains = new[]
        {
            new DomainDefinition
            {
                Name = "CustomerTools",
                Description = "Customer tools.",
                Capabilities = ["Read and update customers"]
            },
            new DomainDefinition
            {
                Name = "CreditorPurchaseTools",
                Description = "Invoice and purchasing tools.",
                Capabilities = ["Read and update invoices and purchase orders"]
            }
        };

        var tools = new[]
        {
            CreateTool("GetInvoice", "CreditorPurchaseTools", "Invoice", ["Read"], "Retrieve invoice details."),
            CreateTool("ModifyInvoice", "CreditorPurchaseTools", "Invoice", ["Update"], "Modify an invoice."),
            CreateTool("GetPurchaseOrder", "CreditorPurchaseTools", "PurchaseOrder", ["Read"], "Retrieve purchase order details."),
            CreateTool("ApprovePurchaseOrder", "CreditorPurchaseTools", "PurchaseOrder", ["Approve"], "Approve a purchase order."),
            CreateTool("GetCustomer", "CustomerTools", "Customer", ["Read"], "Retrieve customer details."),
            CreateTool("ModifyCustomer", "CustomerTools", "Customer", ["Update"], "Modify a customer record.")
        };

        return new ToolCatalog(domains, tools);
    }

    private static ToolDefinition CreateTool(string name, string domain, string entityType, string[] supportedActions, string description)
        => new()
        {
            Name = name,
            Domain = domain,
            Description = description,
            Action = supportedActions[0],
            Resource = entityType,
            Metadata = new ToolMetadata
            {
                ToolName = name,
                Domain = domain,
                EntityType = entityType,
                SupportedActions = supportedActions
            },
            Keywords = [name, entityType, .. supportedActions]
        };

    private sealed class EmergencyCustomerFallbackOverride : IToolSelectionOverride
    {
        public void Apply(ToolSelectionContext context)
        {
            if (!context.Prompt.Contains("emergency customer fallback", StringComparison.OrdinalIgnoreCase))
                return;

            context.ReplaceSelection(["GetCustomer"]);
            context.AddAdjustment("EmergencyCustomerFallbackOverride replaced the shortlist with GetCustomer.");
        }
    }

    private sealed class FakeToolSchemaResolver : IToolSchemaResolver
    {
        public int TotalToolCount => 0;

        public IReadOnlyList<string> RegisteredToolNames => [];

        public Task<IReadOnlyDictionary<string, LlmToolDefinition>> ResolveAsync(IEnumerable<string> toolNames, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, LlmToolDefinition>>(toolNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    name => name,
                    name => new LlmToolDefinition
                    {
                        Name = name,
                        Description = name,
                        ParametersSchema = JsonNode.Parse("{}")!
                    },
                    StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyDictionary<string, LlmToolDefinition>> ResolveAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, LlmToolDefinition>>(new Dictionary<string, LlmToolDefinition>(StringComparer.OrdinalIgnoreCase));
    }
}
