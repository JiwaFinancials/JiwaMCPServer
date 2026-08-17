using JiwaMcpServer.Services;
using JiwaMcpServer.Services.DocumentIntelligence;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Host.UseWindowsService();

ConfigurationManager configuration = builder.Configuration;
Config.JiwaAPIURL = configuration.GetSection("JiwaAPIURL").Value;
Config.JiwaAPIKey = configuration.GetSection("JiwaAPIKey").Value;
var pageSizeConfig = configuration.GetSection("PageSize").Value;
Config.PageSize = int.TryParse(pageSizeConfig, out var pageSize) ? pageSize : 100;

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

var startupLogger = LoggerFactory
    .Create(logging => logging.AddSimpleConsole(options => options.SingleLine = true))
    .CreateLogger("Startup");

var pluginAssemblies = PluginAssemblyLoader.LoadPluginAssemblies(configuration, builder.Environment.ContentRootPath, startupLogger);

var toolAssemblies = new List<Assembly> { typeof(Program).Assembly };

var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly, null);

foreach (var pluginAssembly in pluginAssemblies)
{
    toolAssemblies.Add(pluginAssembly);
    mcpBuilder.WithToolsFromAssembly(pluginAssembly, null);
}

mcpBuilder.WithBusinessToolMetadata(toolAssemblies.ToArray());
mcpBuilder.WithRetrievalAugmentedToolRouting();

builder.Services.AddRetrievalAugmentedToolRouting(configuration, toolAssemblies.ToArray());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddDocumentIntelligence(configuration);

// Register FormNameRegistry with mappings from configuration
var formNameMappings = configuration.GetSection("FormNameMappings").Get<Dictionary<string, string>>() 
    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
builder.Services.AddSingleton<FormNameRegistry>(new FormNameRegistry(formNameMappings));

var app = builder.Build();

app.UseCors();

app.Use(async (context, next) =>
{
    var sessionId = context.Request.Headers["X-Session-ID"].FirstOrDefault()
                    ?? context.Connection.Id
                    ?? Guid.NewGuid().ToString();

    FileStorageService.SetSessionId(sessionId);

    var clientApiKey = context.Request.Headers["X-Jiwa-API-Key"].FirstOrDefault();
    JiwaMcpServer.Services.JiwaApiClient.CurrentApiKey.Value = clientApiKey;

    var routingContextAccessor = context.RequestServices.GetRequiredService<IToolRoutingContextAccessor>();
    var priorContext = routingContextAccessor.Current;
    var routingContext = priorContext ?? new ToolRoutingExecutionContext(ToolRoutingCorrelation.CreateRoutingId());
    routingContextAccessor.Current = routingContext;
    context.Response.Headers[ToolRoutingCorrelation.ResponseHeaderName] = routingContext.RoutingId;

    try
    {
        await next(context);
    }
    finally
    {
        routingContextAccessor.Current = priorContext;
    }
});

app.MapMcp("/mcp");
app.MapToolRoutingDiagnostics();

await app.RunAsync();