using JiwaMcpServer.Agent.Models;
using JiwaMcpServer.Agent.SchemaResolver;
using JiwaMcpServer.Agent.ToolInvoker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace JiwaMcpServer.Tests.Agent;

public class ReflectionToolInvokerTests
{
    [Fact]
    public async Task InvokeAsync_ContentBlocks_AreReturnedAsStructuredArtifacts()
    {
        var registry = new ToolRegistry([typeof(ArtifactTool).Assembly]);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var invoker = new ReflectionToolInvoker(registry, serviceProvider, NullLogger<ReflectionToolInvoker>.Instance);

        var result = await invoker.InvokeAsync("GenerateArtifact", "{}", CancellationToken.None);

        Assert.Equal("Created export.", result.Text);
        Assert.Single(result.Images);
        Assert.Equal("cHJldmlldy1pbWFnZQ==", result.Images[0].Base64Data);
        Assert.Single(result.Files);
        Assert.Equal("export.csv", result.Files[0].FileName);
        Assert.Equal("jiwa-file://files/export-123", result.Files[0].Uri);
    }

    [McpServerToolType]
    private sealed class ArtifactTool
    {
        [McpServerTool(Name = "GenerateArtifact")]
        [Description("Returns a generated export file.")]
        public IEnumerable<ContentBlock> GenerateArtifact()
        {
            return
            [
                new TextContentBlock { Text = "Created export." },
                ImageContentBlock.FromBytes(Encoding.UTF8.GetBytes("preview-image"), "image/png"),
                new ResourceLinkBlock
                {
                    Uri = "jiwa-file://files/export-123",
                    Name = "export.csv",
                    MimeType = "text/csv",
                    Size = 42,
                    Description = "Generated downloadable file."
                }
            ];
        }
    }
}
