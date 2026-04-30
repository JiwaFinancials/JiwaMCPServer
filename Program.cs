using JiwaMcpServer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Read and validate configuration settings for the Jiwa API URL and API key
ConfigurationManager configuration = builder.Configuration;
Config.JiwaAPIURL = configuration.GetSection("JiwaAPIURL").Value;
Config.JiwaAPIKey = configuration.GetSection("JiwaAPIKey").Value;
Config.BearerTokens = configuration.GetSection("BearerTokens").Get<HashSet<string>>()
    ?? throw new InvalidOperationException("BearerTokens is missing - check appsettings.json");

if (string.IsNullOrWhiteSpace(Config.JiwaAPIURL))
{
    throw new InvalidOperationException("JiwaAPIURL is blank - check appsettings.json");
}

if (Config.BearerTokens.Count == 0)
{
    throw new InvalidOperationException("BearerTokens is empty - check appsettings.json");
}

// Register MCP server with HTTP streaming transport and auto-discover tools
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

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

// Enforce bearer token authentication
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
        !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
        !Config.BearerTokens.Contains(authHeader.ToString()["Bearer ".Length..].Trim()))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }
    await next(context);
});

app.MapMcp("/mcp");

await app.RunAsync();
