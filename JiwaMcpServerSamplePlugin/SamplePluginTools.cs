using JiwaMcpServer.Agent.Catalog;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace JiwaMcpServerSamplePlugin;

[PlannerDomain("DocumentTools")]
[McpServerToolType]
public sealed class SamplePluginTools
{
    [McpServerTool(ReadOnly = true), Description("Returns basic diagnostic information from the sample plugin. Use this tool when checking plugin load state or validating an override hook.")]
    public string GetSamplePluginInfo()
    {
        return JsonSerializer.Serialize(new
        {
            success = true,
            plugin = "JiwaMcpServerSamplePlugin",
            message = "Sample plugin tool is loaded and available."
        });
    }
}
