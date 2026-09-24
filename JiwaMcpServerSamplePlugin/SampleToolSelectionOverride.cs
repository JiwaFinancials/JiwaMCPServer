using JiwaMcpServer.Agent.Routing;

namespace JiwaMcpServerSamplePlugin;

public sealed class SampleToolSelectionOverride : IToolSelectionOverride
{
    public void Apply(ToolSelectionContext context)
    {
        if (!context.Prompt.Contains("sample plugin", StringComparison.OrdinalIgnoreCase)
            && !context.Prompt.Contains("plugin diagnostic", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        context.PreferTool("GetSamplePluginInfo");
        context.AddAdjustment("SampleToolSelectionOverride promoted GetSamplePluginInfo for a plugin diagnostic prompt.");
    }
}
