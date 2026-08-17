using JiwaMcpServer.ToolMetadata;
using Xunit;

namespace JiwaMcpServer.Tests;

public class BusinessTerminologyRegistryResolutionTests
{
    private readonly BusinessTerminologyRegistry _registry = new();

    [Theory]
    [InlineData("po", "PurchaseOrder")]
    [InlineData("so", "SalesOrder")]
    [InlineData("creditor", "Supplier")]
    [InlineData("creditor purchase", "CreditorPurchase")]
    [InlineData("debtor", "Customer")]
    public void TryResolveEntity_MapsBusinessTermsToCanonicalEntity(string term, string expectedEntity)
    {
        var resolved = _registry.TryResolveEntity(term, out var entityType);

        Assert.True(resolved);
        Assert.Equal(expectedEntity, entityType);
    }

    [Fact]
    public void ExpandTerms_Po_ExpandsToPurchaseOrderSynonyms()
    {
        var expanded = _registry.ExpandTerms(["po"]);

        Assert.Contains("po", expanded, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("purchase order", expanded, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("supplier order", expanded, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("procurement order", expanded, StringComparer.OrdinalIgnoreCase);
    }
}
