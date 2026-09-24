using JiwaMcpServer.Agent;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.SchemaResolver;

namespace JiwaMcpServer.Tests.Agent;

public class ToolCatalogTests
{
    private readonly ToolRegistry _registry = CreateRegistry();
    private readonly IToolCatalog _catalog;

    public ToolCatalogTests()
    {
        _catalog = JiwaToolCatalogFactory.Create(_registry);
    }

    [Fact]
    public void GetDomains_ContainsActiveRoutingDomains()
    {
        var domains = _catalog.GetDomains().Select(d => d.Name).ToList();

        Assert.Contains("InventoryTools", domains);
        Assert.Contains("CustomerTools", domains);
        Assert.Contains("CreditorPurchaseTools", domains);
        Assert.Contains("DocumentTools", domains);
        Assert.DoesNotContain("FinanceTools", domains);
    }

    [Fact]
    public void GetDomains_EachDomainHasDescriptionAndCapabilities()
    {
        foreach (var domain in _catalog.GetDomains())
        {
            Assert.False(string.IsNullOrWhiteSpace(domain.Description),
                $"Domain '{domain.Name}' has no description");
            Assert.NotEmpty(domain.Capabilities);
            Assert.NotEmpty(_catalog.GetToolsForDomains([domain.Name]));
        }
    }

    [Fact]
    public void GetAllTools_MatchesReflectedRegistry()
    {
        var catalogTools = _catalog.GetAllTools().Select(t => t.Name).OrderBy(name => name).ToList();
        var registryTools = _registry.Tools.Keys.OrderBy(name => name).ToList();

        Assert.Equal(registryTools, catalogTools);
    }

    [Theory]
    [InlineData("QueryInventory", "InventoryTools")]
    [InlineData("QueryCustomers", "CustomerTools")]
    [InlineData("AddCreditorPurchaseDocument", "CreditorPurchaseTools")]
    [InlineData("UploadLocalDocument", "DocumentTools")]
    public void GetAllTools_ContainsExpectedToolInDomain(string toolName, string expectedDomain)
    {
        var tool = _catalog.GetAllTools().FirstOrDefault(t => t.Name == toolName);

        Assert.NotNull(tool);
        Assert.Equal(expectedDomain, tool!.Domain);
    }

    [Fact]
    public void GetToolsForDomains_CreditorPurchaseDomain_ContainsSupplierPurchaseTools()
    {
        var creditorPurchaseTools = _catalog.GetToolsForDomains(["CreditorPurchaseTools"]).Select(t => t.Name).ToList();

        Assert.Contains("AddCreditorPurchaseDocument", creditorPurchaseTools);
        Assert.Contains("GetCreditorPurchaseDocument", creditorPurchaseTools);
    }

    [Fact]
    public void GetToolsForDomains_InventoryDomain_ContainsQueryInventory()
    {
        var tools = _catalog.GetToolsForDomains(["InventoryTools"]);

        Assert.Contains(tools, t => t.Name == "QueryInventory");
    }

    [Fact]
    public void GetToolsForDomains_MultiDomain_ReturnsToolsFromAllDomains()
    {
        var tools = _catalog.GetToolsForDomains(["InventoryTools", "DocumentTools"]).ToList();

        Assert.Contains(tools, t => t.Domain == "InventoryTools");
        Assert.Contains(tools, t => t.Domain == "DocumentTools");
        Assert.DoesNotContain(tools, t => t.Domain == "CustomerTools");
    }

    [Fact]
    public void GetToolsForDomains_EmptyList_ReturnsEmpty()
    {
        var tools = _catalog.GetToolsForDomains([]);
        Assert.Empty(tools);
    }

    [Fact]
    public void GetToolsForDomains_UnknownDomain_ReturnsEmpty()
    {
        var tools = _catalog.GetToolsForDomains(["NonExistentDomain"]);
        Assert.Empty(tools);
    }

    [Fact]
    public void GetToolsForDomains_IsCaseInsensitive()
    {
        var lower = _catalog.GetToolsForDomains(["inventorytools"]);
        var upper = _catalog.GetToolsForDomains(["INVENTORYTOOLS"]);
        var proper = _catalog.GetToolsForDomains(["InventoryTools"]);

        Assert.Equal(proper.Count, lower.Count);
        Assert.Equal(proper.Count, upper.Count);
    }

    private static ToolRegistry CreateRegistry() => new([typeof(AgentServiceExtensions).Assembly]);
}
