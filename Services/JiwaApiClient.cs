using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace JiwaMcpServer.Services;

public class JiwaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JiwaApiClient> _logger;
    private readonly string _bearerToken;

    public JiwaApiClient(HttpClient httpClient, ILogger<JiwaApiClient> logger,
        string bearerToken)
    {
        _httpClient = httpClient;
        _logger = logger;
        _bearerToken = bearerToken;
    }

    /// <summary>
    /// Authenticates against the Jiwa API and caches the session token.
    /// Uses double-check locking to avoid redundant logins.
    /// </summary>
    private Task EnsureAuthenticatedAsync(CancellationToken ct = default)
    {
        // With bearer token auth we only need to ensure a token was provided.
        if (string.IsNullOrWhiteSpace(_bearerToken))
        {
            throw new InvalidOperationException("JIWA_BEARER_TOKEN environment variable is not set.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates the cached session so the next call will re-authenticate.
    /// </summary>
    // No session invalidation needed when using a static bearer token.
    public void InvalidateSession() { }

    /// <summary>
    /// Performs a GET with automatic re-authentication on 401.
    /// </summary>
    public async Task<JsonElement> GetAsync(string endpoint, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);
        var request = BuildRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            InvalidateSession();
            await EnsureAuthenticatedAsync(ct);
            request = BuildRequest(HttpMethod.Get, endpoint);
            response = await _httpClient.SendAsync(request, ct);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    /// <summary>
    /// Performs a GET with autoquery parameter support and automatic re-authentication on 401.
    /// </summary>
    public async Task<JsonElement> GetAutoQueryAsync(string endpoint, string fields, string query, string OrderBy, string OrderByDesc, CancellationToken ct = default)
    {
        return await GetAsync($"/Queries/DebtorList?fields={fields}&{query}&OrderBy={OrderBy}&OrderByDesc={OrderByDesc}", ct);
    }

    /// <summary>
    /// Performs a POST with automatic re-authentication on 401.
    /// </summary>
    public async Task<JsonElement> PostAsync(string endpoint, object body, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);
        var response = await SendWithBodyAsync(HttpMethod.Post, endpoint, body, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            InvalidateSession();
            await EnsureAuthenticatedAsync(ct);
            response = await SendWithBodyAsync(HttpMethod.Post, endpoint, body, ct);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    /// <summary>
    /// Performs a PATCH with automatic re-authentication on 401.
    /// </summary>
    public async Task<JsonElement> PatchAsync(string endpoint, object body, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);
        var response = await SendWithBodyAsync(HttpMethod.Patch, endpoint, body, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            InvalidateSession();
            await EnsureAuthenticatedAsync(ct);
            response = await SendWithBodyAsync(HttpMethod.Patch, endpoint, body, ct);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private HttpRequestMessage BuildRequest(HttpMethod method, string endpoint)
    {
        // Resolve to an absolute URI where possible so logs show the full target URL
        Uri? absoluteUri = null;
        if (_httpClient.BaseAddress is not null)
        {
            absoluteUri = new Uri(_httpClient.BaseAddress, endpoint);
        }
        else
        {
            absoluteUri = new Uri(endpoint, UriKind.RelativeOrAbsolute);
        }

        _logger.LogInformation("Preparing HTTP {Method} {Uri}", method, absoluteUri);

        var request = new HttpRequestMessage(method, absoluteUri);
        if (!string.IsNullOrWhiteSpace(_bearerToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
        }
        return request;
    }

    private async Task<HttpResponseMessage> SendWithBodyAsync(
        HttpMethod method, string endpoint, object body, CancellationToken ct)
    {
        var request = BuildRequest(method, endpoint);
        var serialized = JsonSerializer.Serialize(body);
        _logger.LogInformation("Request body for {Method} {Uri}: {Body}", method, request.RequestUri, serialized);
        request.Content = new StringContent(
            serialized,
            Encoding.UTF8,
            "application/json");
        return await _httpClient.SendAsync(request, ct);
    }
}
