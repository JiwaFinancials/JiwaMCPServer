using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public interface IToolRouter
{
    Task<ToolRouteResult> ResolveAsync(ToolRouteRequest request, CancellationToken ct = default);

    Task<WorkingContext> UpdateContextAsync(ToolExecutionContextUpdateRequest request, CancellationToken ct = default);
}
