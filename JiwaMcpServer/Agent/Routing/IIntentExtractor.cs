using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public interface IIntentExtractor
{
    IntentContext Extract(string userPrompt, WorkingContext? workingContext = null);
}
