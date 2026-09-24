using JiwaMcpServer.Tools;
using System.Text.Json;

namespace JiwaMcpServer.Tests.Tools;

public class JiwaToolBaseTests
{
    [Fact]
    public void CreateSearchResponseJson_ReturnsAllResultsWithoutTruncation()
    {
        var items = Enumerable.Range(1, 15)
            .Select(index => new { PartNo = $"P{index:000}", Description = new string('x', 10) })
            .ToList();

        var json = ToolBaseHarness.Render(items, 100);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(15, doc.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(15, doc.RootElement.GetProperty("returned").GetInt32());
        Assert.False(doc.RootElement.GetProperty("truncated").GetBoolean());
        Assert.Equal(15, doc.RootElement.GetProperty("results").GetArrayLength());
        Assert.False(doc.RootElement.TryGetProperty("message", out _));
    }

    private sealed class ToolBaseHarness : JiwaToolBase
    {
        public static string Render<T>(IReadOnlyCollection<T> results, int pageSize)
            => CreateSearchResponseJson(results, pageSize);
    }
}