namespace JiwaMcpServer.Agent.Models;

public sealed class WorkingContext
{
    public ActiveEntity? FocusedEntity { get; set; }

    public string CurrentDomain { get; set; } = string.Empty;

    public string ConversationSummary { get; set; } = string.Empty;

    public string LastIntent { get; set; } = string.Empty;

    public string LastToolName { get; set; } = string.Empty;
}
