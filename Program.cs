using JiwaMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

//Uncomment below and comment the ClearProviders() call further down to enable logging to the console.
//builder.Logging.AddConsole(options =>
//{
//    options.LogToStandardErrorThreshold = LogLevel.Trace;
//});

// Remove default logging providers (including Console) so logs are not written to the console.
builder.Logging.ClearProviders();

// Register Jiwa API client
builder.Services.AddHttpClient<JiwaApiClient>(client =>
{
    // Use a valid absolute URI for the API base address
    client.BaseAddress = new Uri("https://api.jiwa.com.au");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Retrieve bearer token from environment variables or configuration
builder.Services.AddSingleton<JiwaApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<JiwaApiClient>>();
    var httpClient = httpClientFactory.CreateClient(nameof(JiwaApiClient));

    var token = Environment.GetEnvironmentVariable("JIWA_BEARER_TOKEN")
        ?? builder.Configuration["Jiwa:BearerToken"];

    if (string.IsNullOrWhiteSpace(token))
    {
        throw new InvalidOperationException("JIWA_BEARER_TOKEN environment variable or Jiwa:BearerToken configuration is not set.");
    }

    return new JiwaApiClient(httpClient, logger, token);
});

// Register MCP server with stdio transport and auto-discover tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
