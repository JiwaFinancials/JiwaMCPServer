using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Web;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace JiwaMcpServer.Services;

public static class JiwaApiClient
{
    /// <summary>
    /// Per-async-flow API key set by middleware from the X-Jiwa-API-Key request header.
    /// Falls back to Config.JiwaAPIKey when not set.
    /// </summary>
    public static readonly AsyncLocal<string?> CurrentApiKey = new AsyncLocal<string?>();

    private static string? ResolveApiKey()
    {
        var key = CurrentApiKey.Value;
        if (!string.IsNullOrWhiteSpace(key))
            return key;
        return Config.JiwaAPIKey;
    }

    /// <summary>
    /// Performs a GET with automatic re-authentication on 401.
    /// </summary>
    public static async Task<TResponse> GetAsync<TResponse>(ServiceStack.IReturn<TResponse> requestDTO, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            throw new InvalidOperationException("JiwaAPIURL is not configured.");
        }

        using (ServiceStack.JsonApiClient client = new ServiceStack.JsonApiClient(Config.JiwaAPIURL))
        {
            client.BearerToken = ResolveApiKey();
            return await client.GetAsync(requestDTO, ct);
        }
    }

    /// <summary>
    /// Performs a POST with automatic re-authentication on 401.
    /// </summary>
    public static async Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            throw new InvalidOperationException("JiwaAPIURL is not configured.");
        }

        using (ServiceStack.JsonApiClient client = new ServiceStack.JsonApiClient(Config.JiwaAPIURL))
        {
            client.BearerToken = ResolveApiKey();
            return await client.PostAsync<TResponse>(requestDto, ct);
        }
    }

    public static async Task<TResponse> PatchAsync<TResponse>(ServiceStack.IReturn<TResponse> requestDTO, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            throw new InvalidOperationException("JiwaAPIURL is not configured.");
        }

        using (JsonApiClient client = new JsonApiClient(Config.JiwaAPIURL))
        {
            client.BearerToken = ResolveApiKey();
            return await client.PatchAsync(requestDTO, ct);
        }
    }
}
