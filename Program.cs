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

var app = builder.Build();

app.UseCors();

// Capture the Jiwa API key supplied by the client for this request
app.Use(async (context, next) =>
{
    var clientApiKey = context.Request.Headers["X-Jiwa-API-Key"].FirstOrDefault();
    JiwaMcpServer.Services.JiwaApiClient.CurrentApiKey.Value = clientApiKey;
    await next(context);
});

app.MapMcp("/mcp");

await app.RunAsync();
