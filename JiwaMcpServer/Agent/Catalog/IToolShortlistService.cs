using JiwaMcpServer.Agent.Routing;

namespace JiwaMcpServer.Agent.Catalog;

public interface IToolShortlistService
{
    ToolRouteResult Shortlist(ToolRouteRequest request, CancellationToken ct = default);
}
