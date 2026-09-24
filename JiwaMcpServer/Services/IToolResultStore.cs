using JiwaMcpServer.Agent.Routing;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Services;

public interface IToolResultStore
{
    void Store(ToolExecutionContextUpdateRequest request);

    JsonNode? GetLatestStructuredResult(string conversationId, string? toolName = null);
}
