using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Services;

internal static class StructuredResultHandleStore
{
    private const int MaxStoredResults = 1000;
    private static readonly ConcurrentDictionary<string, JsonNode> ResultsByHandle = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<string> HandleOrder = new();

    public static string Store(JsonNode structuredResult)
    {
        ArgumentNullException.ThrowIfNull(structuredResult);

        var resultHandle = Guid.NewGuid().ToString("N");
        ResultsByHandle[resultHandle] = structuredResult.DeepClone();
        HandleOrder.Enqueue(resultHandle);
        TrimExcess();
        return resultHandle;
    }

    public static JsonNode? Get(string resultHandle)
    {
        if (string.IsNullOrWhiteSpace(resultHandle)
            || !ResultsByHandle.TryGetValue(resultHandle, out var structuredResult))
        {
            return null;
        }

        return structuredResult.DeepClone();
    }

    private static void TrimExcess()
    {
        while (ResultsByHandle.Count > MaxStoredResults && HandleOrder.TryDequeue(out var resultHandle))
        {
            ResultsByHandle.TryRemove(resultHandle, out _);
        }
    }
}
