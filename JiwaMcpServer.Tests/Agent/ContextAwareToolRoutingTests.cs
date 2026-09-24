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
        Assert.Contains("downloadable csv export", route.WorkingContext.ConversationSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DocumentTools", route.Domains);
        Assert.Contains("InventoryTools", route.Domains);
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
