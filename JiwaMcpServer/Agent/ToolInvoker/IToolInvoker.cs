using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.ToolInvoker;

/// <summary>
/// Invokes a registered MCP tool by name, passing JSON-serialised arguments,
/// and returns its structured result.
/// </summary>
public interface IToolInvoker
{
    Task<ToolInvocationResult> InvokeAsync(string toolName, string argumentsJson, CancellationToken ct = default);
}
