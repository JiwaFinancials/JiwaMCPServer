using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingConfigurationTests
{
    [Fact]
    public void ToolRoutingOptions_BindsFromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ToolRouting:EnableDomainClassification"] = "true",
            ["ToolRouting:RetrievalTopK"] = "20",
            ["ToolRouting:SimilarityThreshold"] = "0.7",
            ["ToolRouting:MaxSelectedTools"] = "5"
        });

        var services = new ServiceCollection();
        services.Configure<ToolRoutingOptions>(config.GetSection(ToolRoutingOptions.SectionName));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ToolRoutingOptions>>().Value;
        Assert.True(options.EnableDomainClassification);
        Assert.Equal(20, options.RetrievalTopK);
        Assert.Equal(0.7, options.SimilarityThreshold, 3);
        Assert.Equal(5, options.MaxSelectedTools);
    }

    [Fact]
    public void ToolRoutingOptionsValidator_RejectsInvalidThreshold()
    {
        var validator = new ToolRoutingOptionsValidator();
        var result = validator.Validate(null, new ToolRoutingOptions { SimilarityThreshold = 2.0 });

        Assert.True(result.Failed);
        Assert.Contains("SimilarityThreshold", string.Join(' ', result.Failures));
    }

    [Fact]
    public void ToolRoutingOptionsValidator_RejectsSelectedToolsAboveRetrievalTopK()
    {
        var validator = new ToolRoutingOptionsValidator();
        var result = validator.Validate(null, new ToolRoutingOptions { RetrievalTopK = 2, MaxSelectedTools = 3 });

        Assert.True(result.Failed);
        Assert.Contains("MaxSelectedTools", string.Join(' ', result.Failures));
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
