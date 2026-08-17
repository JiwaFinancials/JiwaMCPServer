using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingRetrievalTests
{
    [Fact]
    public async Task Retrieval_ReturnsExactMatchToolFirst()
    {
        var retriever = BuildRetriever([
            new ToolCatalogEntry("SearchInvoices", "Search invoices by date and amount", "Finance", "Invoices", "Search invoices", ["invoice", "payment", "customer"], ["finance"]),
            new ToolCatalogEntry("GetCustomer", "Get customer details", "CRM", "Customer", "Get customer", ["customer", "account"], ["crm"])
        ]);

        var candidates = await retriever.RetrieveAsync("search invoices from last month", []);

        Assert.NotEmpty(candidates);
        Assert.Equal("SearchInvoices", candidates[0].Name);
    }

    [Fact]
    public async Task Retrieval_RespectsDomainFiltering()
    {
        var retriever = BuildRetriever([
            new ToolCatalogEntry("SearchInvoices", "Search invoices", "Finance", "Invoices", "Search invoices", ["invoice"], ["finance"]),
            new ToolCatalogEntry("ListPurchaseOrders", "List purchase orders", "Procurement", "PurchaseOrders", "List POs", ["purchase", "order", "po"], ["procurement"])
        ]);

        var candidates = await retriever.RetrieveAsync("show purchase orders", [new DomainScore("Procurement", 0.9)]);

        Assert.NotEmpty(candidates);
        Assert.All(candidates, c => Assert.Equal("Procurement", c.Tool.Domain));
    }

    [Fact]
    public async Task Retrieval_ReturnsEmpty_WhenNoToolMeetsThreshold()
    {
        var options = Options.Create(new ToolRoutingOptions { RetrievalTopK = 20, SimilarityThreshold = 0.99, EmbeddingDimensions = 256 });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var catalog = new InMemoryToolCatalog(new StaticMetadataProvider([
            new ToolCatalogEntry("GetCalendarEvent", "Read calendar events", "Calendar", "Event", "Get event", ["calendar", "event"], ["calendar"])
        ]));
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, new ToolRoutingContextAccessor(), diagnostics);

        var candidates = await retriever.RetrieveAsync("purchase orders for vendor", []);

        Assert.Empty(candidates);
    }

    private static IToolRetriever BuildRetriever(IReadOnlyList<ToolCatalogEntry> tools)
    {
        var options = Options.Create(new ToolRoutingOptions { RetrievalTopK = 20, SimilarityThreshold = 0.2, EmbeddingDimensions = 256 });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var catalog = new InMemoryToolCatalog(new StaticMetadataProvider(tools));
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        return new SemanticToolRetriever(indexer, embedding, vectorStore, options, new ToolRoutingContextAccessor(), diagnostics);
    }

    private sealed class StaticMetadataProvider(IReadOnlyList<ToolCatalogEntry> tools) : IToolMetadataProvider
    {
        public Task<IReadOnlyList<ToolCatalogEntry>> GetToolMetadataAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(tools);
    }
}