using JiwaMcpServer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Host.UseWindowsService();

// Read and validate configuration settings for the Jiwa API URL and API key
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

if (string.IsNullOrWhiteSpace(Config.JiwaAPIURL))
{
    throw new InvalidOperationException("JiwaAPIURL is blank - check appsettings.json");
}

var startupLogger = LoggerFactory
    .Create(logging => logging.AddSimpleConsole(options => options.SingleLine = true))
    .CreateLogger("Startup");

var pluginAssemblies = PluginAssemblyLoader.LoadPluginAssemblies(configuration, builder.Environment.ContentRootPath, startupLogger);

// Register MCP server with HTTP streaming transport and auto-discover tools
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly, null);

foreach (var pluginAssembly in pluginAssemblies)
{
    mcpBuilder.WithToolsFromAssembly(pluginAssembly, null);
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register IHttpContextAccessor for tools to access HTTP context (session ID, etc.)
builder.Services.AddHttpContextAccessor();

// Register FileStorageService as a singleton so files persist across tool invocations
builder.Services.AddSingleton<FileStorageService>();

var app = builder.Build();

app.UseCors();

// Set session ID and API key for each request
app.Use(async (context, next) =>
{
    // Generate a session ID based on the request connection
    // This ensures all tool calls within a single MCP connection share the same file storage
    var sessionId = context.Request.Headers["X-Session-ID"].FirstOrDefault()
                    ?? context.Connection.Id
                    ?? Guid.NewGuid().ToString();

    FileStorageService.SetSessionId(sessionId);

    // Capture the Jiwa API key supplied by the client for this request
    var clientApiKey = context.Request.Headers["X-Jiwa-API-Key"].FirstOrDefault();
    JiwaMcpServer.Services.JiwaApiClient.CurrentApiKey.Value = clientApiKey;

    await next(context);
});

app.MapMcp("/mcp");

await app.RunAsync();
