using JiwaMcpServer.Agent.Routing;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Services;

public sealed class ToolResultStore : IToolResultStore
{
    private const int MaxStoredResultsPerConversation = 10;
    private const int MaxGlobalStoredResults = 50;
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly ConcurrentDictionary<string, ConcurrentQueue<StoredToolResult>> _resultsByConversation = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<StoredToolResult> _latestStructuredResults = new();

    public void Store(ToolExecutionContextUpdateRequest request)
    {
        if (request is null
            || !request.Successful
            || string.IsNullOrWhiteSpace(request.ToolName)
            || string.IsNullOrWhiteSpace(request.ToolResultText)
            || !TryParseStructuredResult(request.ToolResultText, out var structuredResult))
        {
            return;
        }

        var result = new StoredToolResult(request.ToolName, structuredResult!.DeepClone());

        if (!string.IsNullOrWhiteSpace(request.ConversationId))
        {
            var queue = _resultsByConversation.GetOrAdd(request.ConversationId, _ => new ConcurrentQueue<StoredToolResult>());
            queue.Enqueue(result);

            while (queue.Count > MaxStoredResultsPerConversation && queue.TryDequeue(out _))
            {
            }
        }

        _latestStructuredResults.Enqueue(result);
        while (_latestStructuredResults.Count > MaxGlobalStoredResults && _latestStructuredResults.TryDequeue(out _))
        {
        }
    }

    public string StoreStructuredResult(JsonNode structuredResult)
        => StructuredResultHandleStore.Store(structuredResult);

    public JsonNode? GetStructuredResultByHandle(string resultHandle)
        => StructuredResultHandleStore.Get(resultHandle);

    public JsonNode? GetLatestStructuredResult(string conversationId, string? toolName = null)
    {
        var normalizedToolName = NormalizeToolName(toolName);

        if (!string.IsNullOrWhiteSpace(conversationId)
            && _resultsByConversation.TryGetValue(conversationId, out var queue))
        {
            var snapshots = queue.ToArray();
            for (var index = snapshots.Length - 1; index >= 0; index--)
            {
                var snapshot = snapshots[index];
                if (!string.IsNullOrWhiteSpace(normalizedToolName)
                    && !MatchesToolName(snapshot.ToolName, normalizedToolName))
                {
                    continue;
                }

                return snapshot.StructuredResult.DeepClone();
            }
        }

        var globalSnapshots = _latestStructuredResults.ToArray();
        for (var index = globalSnapshots.Length - 1; index >= 0; index--)
        {
            var snapshot = globalSnapshots[index];
            if (!string.IsNullOrWhiteSpace(normalizedToolName)
                && !MatchesToolName(snapshot.ToolName, normalizedToolName))
            {
                continue;
            }

            return snapshot.StructuredResult.DeepClone();
        }

        return null;
    }

    private static bool MatchesToolName(string actualToolName, string expectedToolName)
    {
        var normalizedActual = NormalizeToolName(actualToolName);
        var normalizedExpected = NormalizeToolName(expectedToolName);

        return string.Equals(normalizedActual, normalizedExpected, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeToolName(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return string.Empty;

        var normalized = toolName.Trim();
        var lastSegment = normalized.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (!string.IsNullOrWhiteSpace(lastSegment))
            normalized = lastSegment;

        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    private static bool TryParseStructuredResult(string toolResultText, out JsonNode? structuredResult)
    {
        structuredResult = null;

        try
        {
            using var document = JsonDocument.Parse(toolResultText, JsonOptions);
            if (TryGetPropertyCaseInsensitive(document.RootElement, "resultHandle", out var resultHandleElement)
                && resultHandleElement.ValueKind == JsonValueKind.String)
            {
                var resultHandle = resultHandleElement.GetString();
                structuredResult = StructuredResultHandleStore.Get(resultHandle ?? string.Empty);
                if (structuredResult is not null)
                {
                    return true;
                }
            }

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
