using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public interface IDomainRouter
{
    IReadOnlyCollection<string> ResolveDomains(string userPrompt);

    IReadOnlyCollection<string> ResolveDomains(string userPrompt, IntentContext? intentContext, WorkingContext? workingContext);
}
