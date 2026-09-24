using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public sealed class ToolExecutionContextUpdateRequest
{
    public string ConversationId { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public string ToolArgumentsJson { get; set; } = "{}";

    public string ToolResultText { get; set; } = string.Empty;

    public bool Successful { get; set; } = true;

    public WorkingContext? WorkingContext { get; set; }
}
