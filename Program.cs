using JiwaMcpServer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Metadata;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Remove default logging providers (including Console) so logs are not written to the console.
builder.Logging.ClearProviders();

//Read in a nd validate configuration settings for the Jiwa API URL and API key, which are required for the MCP tools to function. These should be stored in appsettings.json.
ConfigurationManager configuration = builder.Configuration;
Config.JiwaAPIURL = configuration.GetSection("JiwaAPIURL").Value;
Config.JiwaAPIKey = configuration.GetSection("JiwaAPIKey").Value;

if (string.IsNullOrWhiteSpace(Config.JiwaAPIURL))
{
    throw new InvalidOperationException("JiwaAPIURL is blank - check appsettings.json");
}

if (string.IsNullOrWhiteSpace(Config.JiwaAPIKey))
{
    throw new InvalidOperationException("JiwaAPIKey is blank - check appsettings.json");
}

// Register MCP server with stdio transport and auto-discover tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
