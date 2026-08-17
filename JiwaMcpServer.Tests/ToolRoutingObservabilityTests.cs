using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingObservabilityTests
{
    [Fact]
    public async Task Routing_LogsInvocationCountsMetricsAndCorrelation()
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
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions
        {
            EnableMetrics = true,
            EnablePromptLogging = true
        });
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("route-123")
            {
                Prompt = "search invoices from last month"
            }
        };
        var logger = new ListLogger<DeterministicToolReranker>();
        var routerLogger = new ListLogger<RetrievalAugmentedToolRouter>();
        var catalog = new InMemoryToolCatalog(new StaticMetadataProvider([
            new ToolCatalogEntry("SearchInvoices", "Search invoices", "Finance", "Invoices", "Search invoices", ["invoice"], ["finance"]),
            new ToolCatalogEntry("ListPurchaseOrders", "List purchase orders", "Procurement", "PurchaseOrders", "List purchase orders", ["purchase"], ["procurement"])
        ]));
        var embedding = new LocalHashEmbeddingService(options);
        var vectorStore = new InMemoryToolVectorStore();
        var indexer = new ToolCatalogIndexer(catalog, embedding, vectorStore, Microsoft.Extensions.Logging.Abstractions.NullLogger<ToolCatalogIndexer>.Instance);
        var retriever = new SemanticToolRetriever(indexer, embedding, vectorStore, options, contextAccessor, diagnostics);
        var classifier = new HeuristicDomainClassifier(options, new StaticDomainCatalog(options), contextAccessor, diagnostics, Microsoft.Extensions.Logging.Abstractions.NullLogger<HeuristicDomainClassifier>.Instance);
        var reranker = new DeterministicToolReranker(contextAccessor, diagnostics, logger);
        var diagnosticsStore = new InMemoryToolRoutingDiagnosticsStore();
        var terminology = new BusinessTerminologyRegistry();
        var metadataCatalog = new BusinessToolMetadataCatalog([]);
        var routingPolicy = new BusinessToolRoutingPolicy(terminology, metadataCatalog);
        var router = new RetrievalAugmentedToolRouter(options, classifier, retriever, reranker, catalog, routingPolicy, contextAccessor, diagnosticsStore, diagnostics, routerLogger);

        using var activityCollector = new ActivityCollector();
        using var meterCollector = new MeterCollector(ToolRoutingTelemetry.MeterName, "tool_routing_requests_total", "tool_retrieval_duration_ms", "tool_reranking_duration_ms");

        var result = await router.RouteAsync("search invoices from last month");

        var rerankerEntries = logger.Entries.ToArray();
        var routerEntries = routerLogger.Entries.ToArray();
        var measurements = meterCollector.Measurements.ToArray();
        var activities = activityCollector.Activities.ToArray();

        Assert.Single(result.SelectedTools);
        Assert.Contains(rerankerEntries, entry => entry.Message.Contains("TOOL SELECTION RESULT", StringComparison.Ordinal) && entry.Message.Contains("route-123", StringComparison.Ordinal));
        Assert.Contains(routerEntries, entry => entry.Message.Contains("TOOL REDUCTION METRICS", StringComparison.Ordinal) && entry.Message.Contains("route-123", StringComparison.Ordinal));
        Assert.Contains(measurements, measurement => measurement.InstrumentName == "tool_routing_requests_total");
        Assert.Contains(measurements, measurement => measurement.InstrumentName == "tool_retrieval_duration_ms");
        Assert.Contains(measurements, measurement => measurement.InstrumentName == "tool_reranking_duration_ms");
        Assert.Contains(activities, activity => activity.OperationName == "DomainClassification");
        Assert.Contains(activities, activity => activity.OperationName == "ToolRetrieval");
        Assert.Contains(activities, activity => activity.OperationName == "ToolReranking");

        var latest = diagnosticsStore.GetLatest();
        Assert.NotNull(latest);
        Assert.Equal("route-123", latest!.RoutingId);
        Assert.Single(latest.SelectedTools);
    }

    [Fact]
    public async Task DiagnosticEndpoint_ReturnsExpectedSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IToolRoutingDiagnosticsStore>(new InMemoryToolRoutingDiagnosticsStore());
        builder.Services.Configure<ToolRoutingDiagnosticsOptions>(options =>
        {
            options.EnableDiagnosticEndpoint = true;
            options.RestrictDiagnosticEndpointToLocalhost = false;
            options.DiagnosticApiKey = "secret";
            options.DiagnosticApiKeyHeaderName = "X-Diag-Key";
        });
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        var store = app.Services.GetRequiredService<IToolRoutingDiagnosticsStore>();
        store.Capture(new ToolRoutingExecutionContext("abc123")
        {
            Prompt = "Show me invoices from Acme",
            CatalogSize = 742,
            RetrievedTools = [new ToolCandidate("SearchInvoices", 0.91, new ToolCatalogEntry("SearchInvoices", "Search invoices", "Finance", "Invoices", "Search invoices", ["invoice"], ["finance"]))],
            SelectedTools = [new RerankedTool("SearchInvoices", 0.96, "Invoice search requested", new ToolCatalogEntry("SearchInvoices", "Search invoices", "Finance", "Invoices", "Search invoices", ["invoice"], ["finance"]))],
            ToolsSentToCloud = ["SearchInvoices"],
            SchemaCount = 1,
            RerankingLatency = TimeSpan.FromMilliseconds(182)
        }, includePrompt: true);

        app.MapToolRoutingDiagnostics();
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Diag-Key", "secret");

        var response = await client.GetAsync("/diagnostics/tool-routing/latest");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"routingId\":\"abc123\"", body, StringComparison.Ordinal);
        Assert.Contains("\"catalogSize\":742", body, StringComparison.Ordinal);
        Assert.Contains("\"schemaCount\":1", body, StringComparison.Ordinal);
        Assert.Contains("\"toolsSentToCloud\":[\"SearchInvoices\"]", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthCheck_ReturnsExpectedFlags()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ToolRoutingOptions>(options => options.EnableRouting = true);
        services.AddSingleton<IEmbeddingService>(new LocalHashEmbeddingService(Options.Create(new ToolRoutingOptions())));
        services.AddSingleton<IVectorStore, InMemoryToolVectorStore>();
        services.AddHealthChecks().AddCheck<ToolRoutingHealthCheck>("tool-routing");

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();

        var report = await service.CheckHealthAsync();
        var entry = report.Entries["tool-routing"];

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, report.Status);
        Assert.Equal(true, entry.Data["routingEnabled"]);
        Assert.Equal(true, entry.Data["embeddingService"]);
        Assert.Equal(true, entry.Data["vectorStore"]);
    }

    private sealed class StaticMetadataProvider(IReadOnlyList<ToolCatalogEntry> tools) : IToolMetadataProvider
    {
        public Task<IReadOnlyList<ToolCatalogEntry>> GetToolMetadataAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(tools);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message);

    private sealed class ActivityCollector : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityCollector()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ToolRoutingTelemetry.ActivitySourceName,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => Activities.Add(activity)
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Activities { get; } = [];

        public void Dispose() => _listener.Dispose();
    }

    private sealed class MeterCollector : IDisposable
    {
        private readonly MeterListener _listener = new();

        public MeterCollector(string meterName, params string[] instrumentNames)
        {
            var names = instrumentNames.ToHashSet(StringComparer.Ordinal);
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == meterName && names.Contains(instrument.Name))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) => Measurements.Add(new MeasurementRecord(instrument.Name, measurement)));
            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) => Measurements.Add(new MeasurementRecord(instrument.Name, measurement)));
            _listener.Start();
        }

        public List<MeasurementRecord> Measurements { get; } = [];

        public void Dispose() => _listener.Dispose();
    }

    private sealed record MeasurementRecord(string InstrumentName, double Value);
}
