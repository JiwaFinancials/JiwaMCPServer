using JiwaMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JiwaMcpServer.Tools;

/// <summary>
/// Convenience base that resolves JiwaApiClient from the DI container.
/// MCP tool classes are instantiated per-call, so we resolve via IServiceProvider.
/// </summary>
public abstract class JiwaToolBase
{
    protected readonly JiwaApiClient ApiClient;

    protected JiwaToolBase(JiwaApiClient apiClient)
    {
        ApiClient = apiClient;
    }
}
