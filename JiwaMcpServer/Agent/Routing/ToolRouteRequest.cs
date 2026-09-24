using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public sealed class ToolRouteRequest
{
    public string ConversationId { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public string ConversationSummary { get; set; } = string.Empty;

    public string ConversationContext { get; set; } = string.Empty;

    public WorkingContext? WorkingContext { get; set; }

    public int MaxTools { get; set; } = 20;
}
