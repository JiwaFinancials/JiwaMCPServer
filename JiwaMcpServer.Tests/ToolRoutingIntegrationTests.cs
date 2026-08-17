using System.Text;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using JiwaMcpServer.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingIntegrationTests
{
    [Fact]
    public async Task EndToEnd_RoutesAndBuildsCloudToolContext()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = true,
            RetrievalTopK = 20,
            MaxSelectedTools = 2,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256,
            Domains = ["Finance", "Procurement"]
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-route")
        };

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("SearchInvoices", "Search invoices by date", "Finance", "Invoices", "Search invoices", ["invoice", "payment"], ["finance"]),
            new ToolCatalogEntry("ListPurchaseOrders", "List purchase orders", "Procurement", "PurchaseOrders", "List POs", ["purchase", "po"], ["procurement"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var routing = await router.RouteAsync("search invoices from last month");

        Assert.Single(routing.ClassifiedDomains);
        Assert.Single(routing.SelectedTools);
        Assert.Equal("SearchInvoices", routing.SelectedTools[0].Name);
    }

    [Fact]
    public async Task EndToEnd_PoCreatePrompt_SelectsRelevantTool()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 5,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-po-guardrail")
        };

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["supplier invoice", "vendor bill", "ap invoice", "purchase"], ["accounts payable"]),
            new ToolCatalogEntry("CreatePurchaseOrder", "Create a purchase order.", "Procurement", "PurchaseOrder", "Create purchase order", ["create po", "purchase order", "po", "raise po"], ["procurement"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var routing = await router.RouteAsync("Create a PO");

        Assert.Single(routing.SelectedTools);
        Assert.Equal("CreatePurchaseOrder", routing.SelectedTools[0].Name);
    }

    [Fact]
    public async Task EndToEnd_NewCreditorPurchasePrompt_DoesNotSelectSupplierCreation()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 1,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-new-creditor-purchase")
        };

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["creditor purchase", "supplier invoice", "vendor bill"], ["accounts payable"]),
            new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor"], ["procurement"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = CreateMetadataCatalog("CreateCreditorPurchase", "CreateSupplier");
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var routing = await router.RouteAsync(@"Import the file in C:\Users\scott\Documents\AIChat as a new creditor purchase");

        Assert.Single(routing.SelectedTools);
        Assert.Equal("CreateCreditorPurchase", routing.SelectedTools[0].Name);
    }

    [Fact]
    public async Task EndToEnd_MultiActionPrompt_SelectsToolsForEachAction()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 2,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-multi-action")
        };

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"]),
            new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice from an uploaded file.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["create supplier invoice", "creditor purchase", "vendor bill", "attached file"], ["accounts payable"]),
            new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor", "new supplier"], ["procurement"]),
            new ToolCatalogEntry("GetSupplierDetails", "Get supplier details.", "Procurement", "Supplier", "Get supplier", ["get supplier", "supplier details"], ["procurement"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var routing = await router.RouteAsync("create a creditor purchase from the attached file, create a new creditor if necessary");

        Assert.Equal(2, routing.SelectedTools.Count);
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateSupplier");
        Assert.DoesNotContain(routing.SelectedTools, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
    }

    [Fact]
    public async Task EndToEnd_PromptRequestedCreditorCreation_IsAddedBeyondPrimarySelectionLimit()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 1,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-creditor-companion")
        };

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"]),
            new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice from an uploaded file.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["create supplier invoice", "creditor purchase", "vendor bill", "attached file"], ["accounts payable"]),
            new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor", "new supplier"], ["procurement"]),
            new ToolCatalogEntry("GetSupplierDetails", "Get supplier details.", "Procurement", "Supplier", "Get supplier", ["get supplier", "supplier details"], ["procurement"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = CreateMetadataCatalog("ImportCreditorPurchaseFromLocalCsv", "CreateCreditorPurchase", "CreateSupplier", "GetSupplierDetails");
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var routing = await router.RouteAsync("create a creditor purchase from the attached file, create a new creditor if necessary");

        Assert.Equal(2, routing.SelectedTools.Count);
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateSupplier");
        Assert.DoesNotContain(routing.SelectedTools, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
    }

    [Fact]
    public async Task EndToEnd_PdfAttachment_AddsDocumentParsingToolsBeyondSelectionLimit()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 1,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-pdf-attachment-tools")
        };
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.pdf", "application/pdf");

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename; do not use this tool for PDF or image invoice attachments.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"]),
            new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice from an uploaded file.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["create supplier invoice", "creditor purchase", "vendor bill", "attached file"], ["accounts payable"]),
            new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor", "new supplier"], ["procurement"]),
            new ToolCatalogEntry("DocumentIngest", "Ingest an uploaded document for search and extraction.", "Documents", "Document", "Ingest document", ["document", "pdf", "uploaded document", "source file"], ["document analysis", "ocr"]),
            new ToolCatalogEntry("DocumentExtractInvoice", "Extract invoice fields from an ingested document.", "Documents", "Document", "Extract invoice", ["invoice extraction", "pdf", "document"], ["document analysis"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance, fileStorage);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = CreateMetadataCatalog("ImportCreditorPurchaseFromLocalCsv", "CreateCreditorPurchase", "CreateSupplier", "DocumentIngest", "DocumentExtractInvoice");
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance, fileStorage);

        var routing = await router.RouteAsync("create a creditor purchase from the attached file, create a new creditor if necessary");

        Assert.Equal(4, routing.SelectedTools.Count);
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateSupplier");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "DocumentIngest");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "DocumentExtractInvoice");
        Assert.DoesNotContain(routing.SelectedTools, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
    }

    [Fact]
    public async Task EndToEnd_PdfAttachment_UsesUploadedContextWithoutAttachmentPhrase()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 2,
            SimilarityThreshold = 0.2,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-pdf-uploaded-context")
        };
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.pdf", "application/pdf");

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename; do not use this tool for PDF or image invoice attachments.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"]),
            new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice from an uploaded file.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["create supplier invoice", "creditor purchase", "vendor bill", "attached file"], ["accounts payable"]),
            new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor", "new supplier"], ["procurement"]),
            new ToolCatalogEntry("DocumentIngest", "Ingest an uploaded document for search and extraction.", "Documents", "Document", "Ingest document", ["document", "pdf", "uploaded document", "source file"], ["document analysis", "ocr"]),
            new ToolCatalogEntry("DocumentExtractInvoice", "Extract invoice fields from an ingested document.", "Documents", "Document", "Extract invoice", ["invoice extraction", "pdf", "document"], ["document analysis"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance, fileStorage);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = CreateMetadataCatalog("ImportCreditorPurchaseFromLocalCsv", "CreateCreditorPurchase", "CreateSupplier", "DocumentIngest", "DocumentExtractInvoice");
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance, fileStorage);

        var routing = await router.RouteAsync("create a creditor purchase from this, create a new creditor if necessary");

        Assert.Equal(4, routing.SelectedTools.Count);
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "CreateSupplier");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "DocumentIngest");
        Assert.Contains(routing.SelectedTools, tool => tool.Name == "DocumentExtractInvoice");
        Assert.DoesNotContain(routing.SelectedTools, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
    }

    [Fact]
    public async Task EndToEnd_TopsUpRerankerCandidatesToRetrievalTopK()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 4,
            MaxSelectedTools = 2,
            SimilarityThreshold = 0.9,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-reranker-floor")
        };

        IReadOnlyList<ToolCatalogEntry> toolEntries =
        [
            new ToolCatalogEntry("SearchInvoices", "Search invoices", "Finance", "Invoices", "Search invoices", ["invoice"], ["finance"]),
            new ToolCatalogEntry("ListPurchaseOrders", "List purchase orders", "Procurement", "PurchaseOrders", "List purchase orders", ["purchase", "order"], ["procurement"]),
            new ToolCatalogEntry("GetCustomer", "Get customer details", "CRM", "Customer", "Get customer", ["customer"], ["crm"]),
            new ToolCatalogEntry("GetSupplier", "Get supplier details", "Procurement", "Supplier", "Get supplier", ["supplier"], ["procurement"]),
            new ToolCatalogEntry("GetCalendarEvent", "Read calendar events", "Calendar", "Event", "Get events", ["calendar", "event"], ["calendar"])
        ];

        var catalog = new InMemoryToolCatalog(new StaticMetadataProvider(toolEntries));
        IToolRetriever retriever = new FixedRetriever([
            new ToolCandidate("SearchInvoices", 0.95, toolEntries[0])
        ]);
        var reranker = new RecordingReranker();
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(
            options,
            new EmptyDomainClassifier(),
            retriever,
            reranker,
            catalog,
            routingPolicy,
            contextAccessor,
            new InMemoryToolRoutingDiagnosticsStore(),
            diagnostics,
            NullLogger<RetrievalAugmentedToolRouter>.Instance);

        await router.RouteAsync("search invoices");

        Assert.Single(reranker.CandidateCounts);
        Assert.Equal(options.Value.RetrievalTopK, reranker.CandidateCounts[0]);
    }

    [Fact]
    public async Task EndToEnd_HandlesNoMatches()
    {
        var options = Options.Create(new ToolRoutingOptions
        {
            EnableDomainClassification = false,
            RetrievalTopK = 20,
            MaxSelectedTools = 5,
            SimilarityThreshold = 0.99,
            EmbeddingDimensions = 256
        });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("integration-empty")
        };

        var domainClassifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, NullLogger<HeuristicDomainClassifier>.Instance);
        var metadataProvider = new StaticMetadataProvider([
            new ToolCatalogEntry("GetCalendarEvent", "Read calendar events", "Calendar", "Event", "Get events", ["calendar", "event"], ["calendar"])
        ]);

        var catalog = new InMemoryToolCatalog(metadataProvider);
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, NullLogger<DeterministicToolReranker>.Instance);
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, domainClassifier, retriever, reranker, catalog, routingPolicy, contextAccessor, new InMemoryToolRoutingDiagnosticsStore(), diagnostics, NullLogger<RetrievalAugmentedToolRouter>.Instance);

        var routing = await router.RouteAsync("raise purchase orders");

        Assert.Empty(routing.RetrievedTools);
        Assert.Empty(routing.SelectedTools);
    }

    private static FileStorageService CreateFileStorageWithUploadedFile(string fileName, string mimeType)
    {
        var fileStorage = new FileStorageService();
        FileStorageService.SetSessionId(Guid.NewGuid().ToString("n"));
        var uploadResult = fileStorage.UploadFile(fileName, mimeType, Convert.ToBase64String(Encoding.UTF8.GetBytes("test-content")));
        Assert.True(uploadResult.IsSuccess);
        return fileStorage;
    }

    private static BusinessToolMetadataCatalog CreateMetadataCatalog(params string[] toolNames)
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var selectedToolNames = toolNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var descriptors = extractor
            .ExtractFromAssemblies([typeof(SupplierTools).Assembly])
            .Where(descriptor => selectedToolNames.Contains(descriptor.ToolName))
            .ToArray();

        return new BusinessToolMetadataCatalog(descriptors);
    }

    private sealed class EmptyDomainClassifier : IDomainClassifier
    {
        public Task<DomainClassificationResult> ClassifyAsync(string userPrompt, CancellationToken cancellationToken = default)
            => Task.FromResult(DomainClassificationResult.Empty);
    }

    private sealed class FixedRetriever(IReadOnlyList<ToolCandidate> candidates) : IToolRetriever
    {
        public Task<IReadOnlyList<ToolCandidate>> RetrieveAsync(string userPrompt, IReadOnlyList<DomainScore> domains, CancellationToken cancellationToken = default)
            => Task.FromResult(candidates);
    }

    private sealed class RecordingReranker : IToolReranker
    {
        public List<int> CandidateCounts { get; } = [];

        public Task<IReadOnlyList<RerankedTool>> RerankAsync(
            string userPrompt,
            IReadOnlyList<ToolCandidate> candidates,
            int maxSelectedTools,
            CancellationToken cancellationToken = default)
        {
            CandidateCounts.Add(candidates.Count);
            var selected = candidates
                .Take(Math.Min(maxSelectedTools, candidates.Count))
                .Select(candidate => new RerankedTool(candidate.Name, candidate.Similarity, "test", candidate.Tool))
                .ToArray();
            return Task.FromResult<IReadOnlyList<RerankedTool>>(selected);
        }
    }

    private sealed class StaticMetadataProvider(IReadOnlyList<ToolCatalogEntry> tools) : IToolMetadataProvider
    {
        public Task<IReadOnlyList<ToolCatalogEntry>> GetToolMetadataAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(tools);
    }
}
