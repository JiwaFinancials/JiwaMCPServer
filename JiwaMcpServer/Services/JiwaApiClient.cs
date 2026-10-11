using ServiceStack;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    public static async Task<TResponse> PatchPartialAsync<TRequest, TResponse>(
        JsonObject changes,
        (string Name, string? Value)[] routeValues,
        CancellationToken ct = default)
        where TRequest : IReturn<TResponse>
    {
        ArgumentNullException.ThrowIfNull(changes);

        var relativeUrl = BuildRelativeUrl<TRequest>(routeValues);
        return await SendPatchAsync<TResponse>(relativeUrl, changes, ct);
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

    private static string BuildRelativeUrl<TRequest>((string Name, string? Value)[] routeValues)
    {
        var route = typeof(TRequest).GetCustomAttribute<RouteAttribute>()?.Path
            ?? throw new InvalidOperationException($"Request type '{typeof(TRequest).Name}' does not declare a [Route] attribute.");

        foreach (var (name, value) in routeValues)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("PATCH route values must have a name.");

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"PATCH route value '{name}' is required.");

            route = route.Replace("{" + name + "}", Uri.EscapeDataString(value), StringComparison.OrdinalIgnoreCase);
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(route, "{[^{}]+}"))
            throw new InvalidOperationException($"PATCH route '{route}' still contains unresolved parameters.");

        return route.TrimStart('/');
    }

    private static async Task<TResponse> SendPatchAsync<TResponse>(string relativeUrl, JsonObject requestBody, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            throw new InvalidOperationException("PATCH relativeUrl is required.");

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
            throw new InvalidOperationException("JiwaAPIURL is not configured.");

        using var httpClient = new System.Net.Http.HttpClient();
        var apiKey = ResolveApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        var baseUri = new Uri(Config.JiwaAPIURL.TrimEnd('/') + '/');
        using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Patch, new Uri(baseUri, relativeUrl))
        {
            Content = new System.Net.Http.StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };

        using var response = await httpClient.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = ExtractErrorDetail(responseText);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(detail)
                    ? $"PATCH {relativeUrl} failed with {(int)response.StatusCode} {response.ReasonPhrase}."
                    : $"PATCH {relativeUrl} failed with {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
        }

        if (string.IsNullOrWhiteSpace(responseText))
            throw new InvalidOperationException($"PATCH {relativeUrl} returned an empty response.");

        return responseText.FromJson<TResponse>();
    }

    private static string? ExtractErrorDetail(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        try
        {
            var root = JsonNode.Parse(responseText);
            return root?["message"]?.GetValue<string>()
                ?? root?["errorMessage"]?.GetValue<string>()
                ?? root?["responseStatus"]?["message"]?.GetValue<string>()
                ?? root?["ResponseStatus"]?["Message"]?.GetValue<string>()
                ?? responseText;
        }
        catch (JsonException)
        {
            return responseText;
        }
    }
}
