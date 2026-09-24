namespace JiwaMcpServer.Agent.Models;

public sealed class DomainRoutingResult
{
    public IReadOnlyCollection<string> Domains { get; init; } = [];

    public IReadOnlyCollection<string> ToolNames { get; init; } = [];
}
