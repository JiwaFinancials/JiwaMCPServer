using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace JiwaMcpServerSamplePlugin;

[McpServerToolType]
public sealed class SamplePluginTools
{
    [McpServerTool(ReadOnly = true), Description("Returns basic diagnostic information from the sample plugin.")]
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
