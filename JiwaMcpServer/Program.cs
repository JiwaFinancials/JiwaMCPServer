using JiwaMcpServer;
using JiwaMcpServer.Agent;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.BackgroundServices;
using JiwaMcpServer.Options;
using JiwaMcpServer.Resources;
using JiwaMcpServer.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Host.UseWindowsService();

// Read and validate configuration settings
ConfigurationManager configuration = builder.Configuration;
Config.JiwaAPIURL = configuration.GetSection("JiwaAPIURL").Value;
Config.JiwaAPIKey = configuration.GetSection("JiwaAPIKey").Value;
var pageSizeConfig = configuration.GetSection("PageSize").Value;
Config.PageSize = int.TryParse(pageSizeConfig, out var pageSize) ? pageSize : 100;

//Filesystem access configuration
var configuredRoots = configuration.GetSection("LocalFileSystem:AllowedRoots").Get<string[]>() ?? Array.Empty<string>();
Config.LocalFileSystemAllowedRoots = configuredRoots
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Select(path => path.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var maxReadBytesConfig = configuration.GetSection("LocalFileSystem:MaxReadBytes").Value;
Config.LocalFileSystemMaxReadBytes = int.TryParse(maxReadBytesConfig, out var maxReadBytes) && maxReadBytes > 0
    ? maxReadBytes
    : 256 * 1024;

var maxWriteBytesConfig = configuration.GetSection("LocalFileSystem:MaxWriteBytes").Value;
Config.LocalFileSystemMaxWriteBytes = int.TryParse(maxWriteBytesConfig, out var maxWriteBytes) && maxWriteBytes > 0
    ? maxWriteBytes
    : 256 * 1024;

if (string.IsNullOrWhiteSpace(Config.JiwaAPIURL))
{
    throw new InvalidOperationException("JiwaAPIURL is blank - check appsettings.json");
}

// Console logging configuration
var startupLogger = LoggerFactory
    .Create(logging => logging.AddSimpleConsole(options => options.SingleLine = true))
    .CreateLogger("Startup");

var pluginAssemblies = PluginAssemblyLoader.LoadPluginAssemblies(configuration, builder.Environment.ContentRootPath, startupLogger);

builder.Services.AddOptions<DocumentProcessingOptions>()
    .Bind(builder.Configuration.GetSection(DocumentProcessingOptions.SectionName))
    .Validate(options => options.MaxUploadSizeMB > 0, "DocumentProcessing:MaxUploadSizeMB must be greater than zero.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.TempFolder), "DocumentProcessing:TempFolder is required.")
    .Validate(options => options.RetentionHours > 0, "DocumentProcessing:RetentionHours must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSingleton<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddSingleton<IFileExportService, FileExportService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IToolResultStore, ToolResultStore>();
builder.Services.AddHostedService<DocumentCleanupService>();

// Register MCP server with HTTP streaming transport and auto-discover tools/resources
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly, null)
    .WithResourcesFromAssembly(typeof(DocumentFileResources).Assembly);

foreach (var pluginAssembly in pluginAssemblies)
{
    mcpBuilder.WithToolsFromAssembly(pluginAssembly, null);
}

// Register the deterministic domain router and its tool catalog support.
builder.Services.AddJiwaAgentPipeline(builder.Configuration, pluginAssemblies);

// Configure Cross-Origin Resource Sharing (fully open policy)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

var diagnosticsLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AgentStartupDiagnostics");
var catalog = app.Services.GetRequiredService<IToolCatalog>();
var registeredDomains = catalog.GetDomains().Select(domain => domain.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
var registeredTools = catalog.GetAllTools().Select(tool => tool.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
var toolsPerDomain = registeredDomains
    .Select(domain => $"{domain}: {catalog.GetToolsForDomains([domain]).Count}")
    .ToList();

diagnosticsLogger.LogInformation(
    "Registered Domains:\n{Domains}\n\nRegistered Tools:\n{Tools}\n\nTools Per Domain:\n{Counts}",
    registeredDomains.Count == 0 ? "(none)" : string.Join(Environment.NewLine, registeredDomains),
    registeredTools.Count == 0 ? "(none)" : string.Join(Environment.NewLine, registeredTools),
    toolsPerDomain.Count == 0 ? "(none)" : string.Join(Environment.NewLine, toolsPerDomain));

app.UseCors();

// Capture the Jiwa API key supplied by the client for this request
app.Use(async (context, next) =>
{
    var clientApiKey = context.Request.Headers["X-Jiwa-API-Key"].FirstOrDefault();
    JiwaMcpServer.Services.JiwaApiClient.CurrentApiKey.Value = clientApiKey;
    await next(context);
});

app.MapMcp("/mcp");

app.MapPost("/agent/tools/route", async (ToolRouteRequest request, IToolRouter router, CancellationToken ct) =>
{
    var route = await router.ResolveAsync(request ?? new ToolRouteRequest(), ct);
    return TypedResults.Ok(route);
});

app.MapPost("/mcp/route", async (ToolRouteRequest request, IToolRouter router, CancellationToken ct) =>
{
    var route = await router.ResolveAsync(request ?? new ToolRouteRequest(), ct);
    return TypedResults.Ok(route);
});

app.MapPost("/agent/tools/context", async (ToolExecutionContextUpdateRequest request, IToolRouter router, IToolResultStore toolResultStore, CancellationToken ct) =>
{
    var safeRequest = request ?? new ToolExecutionContextUpdateRequest();
    toolResultStore.Store(safeRequest);
    var context = await router.UpdateContextAsync(safeRequest, ct);
    return TypedResults.Ok(context);
});

app.MapPost("/mcp/context", async (ToolExecutionContextUpdateRequest request, IToolRouter router, IToolResultStore toolResultStore, CancellationToken ct) =>
{
    var safeRequest = request ?? new ToolExecutionContextUpdateRequest();
    toolResultStore.Store(safeRequest);
    var context = await router.UpdateContextAsync(safeRequest, ct);
    return TypedResults.Ok(context);
});

app.MapDocumentEndpoints();

await app.RunAsync();