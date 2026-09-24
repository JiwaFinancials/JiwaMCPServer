using JiwaMcpServer.Agent.Routing;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Services;

public sealed class ToolResultStore : IToolResultStore
{
    private const int MaxStoredResultsPerConversation = 10;
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly ConcurrentDictionary<string, ConcurrentQueue<StoredToolResult>> _resultsByConversation = new(StringComparer.OrdinalIgnoreCase);

    public void Store(ToolExecutionContextUpdateRequest request)
    {
        if (request is null
            || !request.Successful
            || string.IsNullOrWhiteSpace(request.ConversationId)
            || string.IsNullOrWhiteSpace(request.ToolName)
            || string.IsNullOrWhiteSpace(request.ToolResultText)
            || !TryParseStructuredResult(request.ToolResultText, out var structuredResult))
        {
            return;
        }

        var queue = _resultsByConversation.GetOrAdd(request.ConversationId, _ => new ConcurrentQueue<StoredToolResult>());
        queue.Enqueue(new StoredToolResult(request.ToolName, structuredResult!));

        while (queue.Count > MaxStoredResultsPerConversation && queue.TryDequeue(out _))
        {
        }
    }

    public JsonNode? GetLatestStructuredResult(string conversationId, string? toolName = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId)
            || !_resultsByConversation.TryGetValue(conversationId, out var queue))
        {
            return null;
        }

        var snapshots = queue.ToArray();
        for (var index = snapshots.Length - 1; index >= 0; index--)
        {
            var snapshot = snapshots[index];
            if (!string.IsNullOrWhiteSpace(toolName)
                && !snapshot.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return snapshot.StructuredResult.DeepClone();
        }

        return null;
    }

    private static bool TryParseStructuredResult(string toolResultText, out JsonNode? structuredResult)
    {
        structuredResult = null;

        try
        {
            using var document = JsonDocument.Parse(toolResultText, JsonOptions);
            structuredResult = ExtractExportableNode(document.RootElement);
            return structuredResult is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonNode? ExtractExportableNode(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetPropertyCaseInsensitive(element, "results", out var resultsElement)
                && resultsElement.ValueKind == JsonValueKind.Array)
            {
                return JsonNode.Parse(resultsElement.GetRawText());
            }

            if (TryGetPropertyCaseInsensitive(element, "result", out var resultElement)
                && resultElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                return JsonNode.Parse(resultElement.GetRawText());
            }
        }

        return element.ValueKind is JsonValueKind.Object or JsonValueKind.Array
            ? JsonNode.Parse(element.GetRawText())
            : null;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement propertyValue)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = property.Value;
                    return true;
                }
            }
        }

        propertyValue = default;
        return false;
    }

    private sealed record StoredToolResult(string ToolName, JsonNode StructuredResult);
}
