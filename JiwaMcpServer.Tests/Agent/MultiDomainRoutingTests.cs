using JiwaMcpServer.Agent;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.SchemaResolver;

namespace JiwaMcpServer.Tests.Agent;

public class MultiDomainRoutingTests
{
    private readonly IToolCatalog _catalog = JiwaToolCatalogFactory.Create(new ToolRegistry([typeof(AgentServiceExtensions).Assembly]));

    [Theory]
    [InlineData("InventoryTools")]
    [InlineData("CustomerTools")]
    [InlineData("CreditorPurchaseTools")]
    [InlineData("DocumentTools")]
    public void EachDomain_HasAtLeastOneTool(string domain)
    {
        var tools = _catalog.GetToolsForDomains([domain]);
        Assert.NotEmpty(tools);
    }

    [Fact]
    public void AllDomainsSelected_ReturnsAllTools()
    {
        var allDomains = _catalog.GetDomains().Select(d => d.Name);
        var tools = _catalog.GetToolsForDomains(allDomains);
        var allTools = _catalog.GetAllTools();

        Assert.Equal(allTools.Count, tools.Count);
    }

    [Fact]
    public void InventoryAndCreditorPurchaseTools_ReturnsCorrectSubset()
    {
        var tools = _catalog.GetToolsForDomains(["InventoryTools", "CreditorPurchaseTools"]).ToList();

        Assert.Contains(tools, t => t.Domain == "InventoryTools");
        Assert.Contains(tools, t => t.Domain == "CreditorPurchaseTools");
        Assert.DoesNotContain(tools, t => t.Domain == "DocumentTools");
    }

    [Fact]
    public void DocumentsAndCreditorPurchaseTools_ReturnsCorrectSubset()
    {
        var tools = _catalog.GetToolsForDomains(["DocumentTools", "CreditorPurchaseTools"]).ToList();

        Assert.Contains(tools, t => t.Domain == "DocumentTools");
        Assert.Contains(tools, t => t.Domain == "CreditorPurchaseTools");
        Assert.DoesNotContain(tools, t => t.Domain == "InventoryTools");
    }

    [Fact]
    public void CreditorPurchaseToolsDomain_ContainsCreditorPurchaseDocumentTools()
    {
        var tools = _catalog.GetToolsForDomains(["CreditorPurchaseTools"]).Select(t => t.Name).ToList();

        Assert.Contains("AddCreditorPurchaseDocument", tools);
        Assert.Contains("GetCreditorPurchaseDocument", tools);
    }

    [Fact]
    public void CustomerToolsDomain_ContainsCustomerDiscoveryTools()
    {
        var tools = _catalog.GetToolsForDomains(["CustomerTools"]).Select(t => t.Name).ToList();

        Assert.Contains("QueryCustomers", tools);
        Assert.Contains("GetCustomer", tools);
    }

    [Fact]
    public void NoDuplicateToolsWhenSameDomainSpecifiedTwice()
    {
        var single = _catalog.GetToolsForDomains(["InventoryTools"]);
        var doubled = _catalog.GetToolsForDomains(["InventoryTools", "InventoryTools"]);

        Assert.Equal(single.Count, doubled.Count);
    }
}
