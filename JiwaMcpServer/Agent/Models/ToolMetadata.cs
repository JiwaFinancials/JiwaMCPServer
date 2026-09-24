namespace JiwaMcpServer.Agent.Models;

public sealed class ToolMetadata
{
    public string ToolName { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string[] SupportedActions { get; set; } = [];
}
