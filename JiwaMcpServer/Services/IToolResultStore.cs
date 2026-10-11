using JiwaMcpServer.Agent.Routing;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Services;

public interface IToolResultStore
{
    void Store(ToolExecutionContextUpdateRequest request);

    string StoreStructuredResult(JsonNode structuredResult);

    JsonNode? GetStructuredResultByHandle(string resultHandle);

    JsonNode? GetLatestStructuredResult(string conversationId, string? toolName = null);
}
