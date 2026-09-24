using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public interface IToolRoutingContextService
{
    WorkingContext ResolveForRouting(ToolRouteRequest request);

    WorkingContext UpdateAfterToolExecution(ToolExecutionContextUpdateRequest request);
}
