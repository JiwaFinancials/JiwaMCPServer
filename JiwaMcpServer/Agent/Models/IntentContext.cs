namespace JiwaMcpServer.Agent.Models;

public sealed class IntentContext
{
    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
