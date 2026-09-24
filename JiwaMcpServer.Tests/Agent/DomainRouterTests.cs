using JiwaMcpServer.Agent.Routing;

namespace JiwaMcpServer.Tests.Agent;

public class DomainRouterTests
{
    private readonly IDomainRouter _router = new DomainRouter();

    [Fact]
    public void ResolveDomains_CustomerRouting_ReturnsCustomerTools()
    {
        var domains = _router.ResolveDomains("show customer 1001 details");

        Assert.Contains("CustomerTools", domains);
    }

    [Fact]
    public void ResolveDomains_InventoryRouting_ReturnsInventoryTools()
    {
        var domains = _router.ResolveDomains("find products with low stock");

        Assert.Contains("InventoryTools", domains);
    }

    [Fact]
    public void ResolveDomains_PurchasingRouting_ReturnsCreditorPurchaseTools()
    {
        var domains = _router.ResolveDomains("show supplier purchases this month");

        Assert.Contains("CreditorPurchaseTools", domains);
    }

    [Fact]
    public void ResolveDomains_FinanceKeywords_RouteToCreditorPurchaseTools()
    {
        var domains = _router.ResolveDomains("list all overdue invoices");

        Assert.Contains("CreditorPurchaseTools", domains);
    }

    [Fact]
    public void ResolveDomains_DocumentRouting_ReturnsDocumentTools()
    {
        var domains = _router.ResolveDomains("upload this pdf file");

        Assert.Contains("DocumentTools", domains);
    }

    [Fact]
    public void ResolveDomains_MultiDomainRouting_ReturnsExpectedDomains()
    {
        var invoiceByCustomer = _router.ResolveDomains("Customer invoices");
        var purchasesByPart = _router.ResolveDomains("Show purchases of part T1170");

        Assert.Contains("CustomerTools", invoiceByCustomer);
        Assert.Contains("CreditorPurchaseTools", invoiceByCustomer);

        Assert.Contains("InventoryTools", purchasesByPart);
        Assert.Contains("CreditorPurchaseTools", purchasesByPart);
    }

    [Fact]
    public void ResolveDomains_DocumentSupportRouting_ImportProductsFromCsv_ReturnsInventoryAndDocumentTools()
    {
        var domains = _router.ResolveDomains("Import products from CSV");

        Assert.Contains("InventoryTools", domains);
        Assert.Contains("DocumentTools", domains);
    }

    [Fact]
    public void ResolveDomains_DocumentSupportRouting_ExportProductsToExcel_ReturnsInventoryAndDocumentTools()
    {
        var domains = _router.ResolveDomains("Export products to Excel");

        Assert.Contains("InventoryTools", domains);
        Assert.Contains("DocumentTools", domains);
    }

    [Fact]
    public void ResolveDomains_DocumentSupportRouting_ExportProductsWithoutFormat_StillReturnsDocumentTools()
    {
        var domains = _router.ResolveDomains("Export products");

        Assert.Contains("InventoryTools", domains);
        Assert.Contains("DocumentTools", domains);
    }

    [Fact]
    public void ResolveDomains_NoMatchRouting_ReturnsEmpty()
    {
        var domains = _router.ResolveDomains("hello there");

        Assert.Empty(domains);
    }
}
