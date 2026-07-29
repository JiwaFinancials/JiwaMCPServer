using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class OpenAiEmbeddingService(
    DocumentIntelligenceOptions options,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenAiEmbeddingService> logger,
    DeterministicEmbeddingService fallback) : IEmbeddingService
{
    public async Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var provider = options.Embeddings.Provider?.Trim().ToLowerInvariant();

        if (provider is not ("openai" or "azure-openai"))
        {
            return await fallback.CreateEmbeddingAsync(text, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(options.Embeddings.Endpoint) || string.IsNullOrWhiteSpace(options.Embeddings.ApiKey) || string.IsNullOrWhiteSpace(options.Embeddings.Model))
        {
            logger.LogWarning("OpenAI embeddings are selected but not fully configured. Falling back to deterministic embeddings.");
            return await fallback.CreateEmbeddingAsync(text, cancellationToken);
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(OpenAiEmbeddingService));
            var request = BuildRequest(provider, text);
            using var response = await client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Embedding API call failed ({StatusCode}). Falling back to deterministic embeddings.", (int)response.StatusCode);
                return await fallback.CreateEmbeddingAsync(text, cancellationToken);
            }

            using var json = JsonDocument.Parse(payload);
            var vector = json.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            if (vector.Length == 0)
            {
                return await fallback.CreateEmbeddingAsync(text, cancellationToken);
            }

            return vector;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Embedding generation failed. Falling back to deterministic embeddings.");
            return await fallback.CreateEmbeddingAsync(text, cancellationToken);
        }
    }

    private HttpRequestMessage BuildRequest(string provider, string text)
    {
        var endpoint = options.Embeddings.Endpoint.TrimEnd('/');
        string url;

        if (provider == "azure-openai")
        {
            url = $"{endpoint}/openai/deployments/{options.Embeddings.Model}/embeddings?api-version=2024-02-15-preview";
        }
        else
        {
            url = endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? $"{endpoint}/v1/embeddings"
                : "https://api.openai.com/v1/embeddings";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Embeddings.ApiKey);

        if (provider == "azure-openai")
        {
            request.Headers.Remove("Authorization");
            request.Headers.Add("api-key", options.Embeddings.ApiKey);
        }

        var body = JsonSerializer.Serialize(new
        {
            input = text,
            model = provider == "openai" ? options.Embeddings.Model : null
        });

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return request;
    }
}
