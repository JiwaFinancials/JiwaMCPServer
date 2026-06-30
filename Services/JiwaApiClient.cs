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

    public static async Task DeleteAsync(ServiceStack.IReturnVoid requestDTO, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            throw new InvalidOperationException("JiwaAPIURL is not configured.");
        }

        using (JsonApiClient client = new JsonApiClient(Config.JiwaAPIURL))
        {
            client.BearerToken = ResolveApiKey();
            await client.DeleteAsync(requestDTO, ct);
        }
    }

    /// <summary>
    /// Performs a GET to a relative URL with a raw JSON body and returns the response bytes and content-type.
    /// </summary>
    public static async Task<(byte[] Bytes, string? ContentType)> GetRawBytesAsync(string relativeUrl, string jsonBody, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            throw new InvalidOperationException("JiwaAPIURL is not configured.");
        }

        using var httpClient = new System.Net.Http.HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ResolveApiKey()}");
        var baseUri = new Uri(Config.JiwaAPIURL.TrimEnd('/') + '/');
        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, new Uri(baseUri, relativeUrl))
        {
            Content = new System.Net.Http.StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        return (bytes, contentType);
    }
}
