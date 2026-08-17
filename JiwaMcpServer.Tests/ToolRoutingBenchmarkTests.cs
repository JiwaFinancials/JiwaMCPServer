using System.Diagnostics;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingBenchmarkTests
{
    [Fact]
    public async Task LargeCatalog_Benchmark_CompletesWithinBudget()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 5,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 768
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("benchmark")
        };

        var tools = Enumerable.Range(1, 5000)
            .Select(i => new ToolCatalogEntry(
                $"Tool{i}",
                i % 20 == 0 ? "Search invoices by customer and amount" : "Generic tool",
                i % 20 == 0 ? "Finance" : "ERP",
                "Category",
                "Capability",
                i % 20 == 0 ? ["invoice", "customer", "payment"] : ["generic"],
                ["tag"]))
            .ToArray();

        var catalog = new InMemoryToolCatalog(new StaticMetadataProvider(tools));
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var classifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, classifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var sw = Stopwatch.StartNew();
        var result = await router.RouteAsync("search invoices from last month");
        sw.Stop();

        Assert.NotEmpty(result.RetrievedTools);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"Routing exceeded time budget: {sw.Elapsed}.");
    }

    private sealed class StaticMetadataProvider(IReadOnlyList<ToolCatalogEntry> tools) : IToolMetadataProvider
    {
        public Task<IReadOnlyList<ToolCatalogEntry>> GetToolMetadataAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(tools);
    }
}
