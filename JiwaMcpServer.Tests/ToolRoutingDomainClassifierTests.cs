using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingDomainClassifierTests
{
    [Fact]
    public async Task Classifier_ReturnsConfiguredDomainsWithConfidence()
    {
        var options = Options.Create(new ToolRoutingOptions { EnableDomainClassification = true, Domains = ["Procurement", "Finance"] });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var classifier = new HeuristicDomainClassifier(
            options,
            new StaticDomainCatalog(options),
            new ToolRoutingContextAccessor(),
            diagnostics,
            NullLogger<HeuristicDomainClassifier>.Instance);

        var result = await classifier.ClassifyAsync("Show me purchase orders from last month");

        Assert.Single(result.Domains);
        Assert.Equal("Procurement", result.Domains[0].Name);
        Assert.True(result.Domains[0].Confidence > 0.5);
    }

    [Fact]
    public async Task Classifier_FiltersToConfiguredDomains()
    {
        var options = Options.Create(new ToolRoutingOptions { EnableDomainClassification = true, Domains = ["Finance"] });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var classifier = new HeuristicDomainClassifier(
            options,
            new StaticDomainCatalog(options),
            new ToolRoutingContextAccessor(),
            diagnostics,
            NullLogger<HeuristicDomainClassifier>.Instance);

        var result = await classifier.ClassifyAsync("Search invoices");

        Assert.Single(result.Domains);
        Assert.Equal("Finance", result.Domains[0].Name);
    }

    [Fact]
    public async Task Classifier_ReturnsEmpty_WhenDisabled()
    {
        var options = Options.Create(new ToolRoutingOptions { EnableDomainClassification = false });
        var diagnostics = Options.Create(new ToolRoutingDiagnosticsOptions());
        var classifier = new HeuristicDomainClassifier(
            options,
            new StaticDomainCatalog(options),
            new ToolRoutingContextAccessor(),
            diagnostics,
            NullLogger<HeuristicDomainClassifier>.Instance);

        var result = await classifier.ClassifyAsync("anything");

        Assert.Empty(result.Domains);
    }
}
