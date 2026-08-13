using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.Tools;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolSelectionMatchingTests
{
    private static readonly IReadOnlyList<BusinessToolDescriptor> Descriptors;
    private static readonly ToolDescriptionComposer DescriptionComposer = new(new BusinessTerminologyRegistry());

    static ToolSelectionMatchingTests()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        Descriptors = extractor.ExtractFromAssemblies([typeof(SupplierTools).Assembly]);
    }

    [Theory]
    [InlineData("create supplier", "CreateSupplier")]
    [InlineData("create vendor", "CreateSupplier")]
    [InlineData("add creditor", "CreateSupplier")]
    [InlineData("create supplier invoice", "CreateCreditorPurchase")]
    [InlineData("create AP invoice", "CreateCreditorPurchase")]
    [InlineData("vendor bill", "CreateCreditorPurchase")]
    [InlineData("raise PO", "CreatePurchaseOrder")]
    public void Query_MapsToExpectedCanonicalTool(string query, string expectedTool)
    {
        var selected = SelectBestTool(query);
        Assert.Equal(expectedTool, Canonicalize(selected.ToolName));
    }

    [Fact]
    public void Query_ShowPoNumber_MapsToPurchaseOrderLookupPath()
    {
        var selected = SelectBestTool("show PO 100123");
        Assert.Contains(Canonicalize(selected.ToolName), new[] { "ListPurchaseOrders", "GetPurchaseOrderDetails" });
    }

    [Fact]
    public void Query_ShowStockItem_MapsToInventoryLookupPath()
    {
        var selected = SelectBestTool("show stock item");
        Assert.Contains(Canonicalize(selected.ToolName), new[] { "ListProducts", "GetProductDetails" });
    }

    private static BusinessToolDescriptor SelectBestTool(string query)
    {
        var queryTokens = Tokenize(query);
        var queryLower = query.Trim().ToLowerInvariant();

        var ranked = Descriptors
            .Where(x => !string.Equals(x.ToolName, "GetAvailableBusinessActions", StringComparison.OrdinalIgnoreCase))
            .Select(descriptor => new
            {
                Descriptor = descriptor,
                Score = ScoreDescriptor(descriptor, queryLower, queryTokens)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Descriptor.ToolName, StringComparer.OrdinalIgnoreCase)
            .First();

        return ranked.Descriptor;
    }

    private static int ScoreDescriptor(BusinessToolDescriptor descriptor, string queryLower, IReadOnlySet<string> queryTokens)
    {
        var enrichedDescription = DescriptionComposer.Compose(descriptor).ToLowerInvariant();
        var searchable = $"{descriptor.ToolName} {descriptor.Metadata.SearchText} {enrichedDescription}".ToLowerInvariant();

        var score = 0;
        foreach (var token in queryTokens)
        {
            if (searchable.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 3;
        }

        if (searchable.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
            score += 8;

        var queryAction = InferAction(queryTokens);
        if (!string.IsNullOrWhiteSpace(queryAction))
        {
            if (string.Equals(descriptor.Metadata.ActionType, queryAction, StringComparison.OrdinalIgnoreCase))
                score += 8;
            else
                score -= 2;
        }

        if (queryTokens.Contains("supplier") || queryTokens.Contains("vendor") || queryTokens.Contains("creditor"))
        {
            var isInvoiceIntent = queryTokens.Contains("invoice") || queryTokens.Contains("bill") || queryTokens.Contains("ap");

            if (string.Equals(descriptor.Metadata.EntityType, "CreditorPurchase", StringComparison.OrdinalIgnoreCase) && !isInvoiceIntent)
                score -= 10;

            if (string.Equals(descriptor.Metadata.EntityType, "Supplier", StringComparison.OrdinalIgnoreCase) && !isInvoiceIntent)
                score += 8;
        }

        if (queryTokens.Contains("po") || queryTokens.Contains("purchase") && queryTokens.Contains("order"))
        {
            if (string.Equals(descriptor.Metadata.EntityType, "PurchaseOrder", StringComparison.OrdinalIgnoreCase))
                score += 12;
            else
                score -= 4;
        }

        if (string.Equals(descriptor.ToolName, "CreateCreditorPurchase", StringComparison.OrdinalIgnoreCase)
            && (queryLower.Contains("vendor bill") || queryLower.Contains("supplier invoice") || queryLower.Contains("ap invoice")))
        {
            score += 12;
        }

        return score;
    }

    private static string Canonicalize(string toolName)
        => toolName switch
        {
            "CreatePO" => "CreatePurchaseOrder",
            "GetPO" => "GetPurchaseOrderDetails",
            "GetPurchaseOrder" => "GetPurchaseOrderDetails",
            "AddItemToPO" => "AddItemToPurchaseOrder",
            "CreateSO" => "CreateSalesOrder",
            "AddItemToSO" => "AddItemToSalesOrder",
            _ => toolName
        };

    private static string? InferAction(IReadOnlySet<string> queryTokens)
    {
        if (queryTokens.Contains("create") || queryTokens.Contains("add") || queryTokens.Contains("raise") || queryTokens.Contains("new"))
            return "Create";

        if (queryTokens.Contains("show") || queryTokens.Contains("get") || queryTokens.Contains("view"))
            return "Get";

        if (queryTokens.Contains("list") || queryTokens.Contains("search") || queryTokens.Contains("find"))
            return "Search";

        if (queryTokens.Contains("update") || queryTokens.Contains("edit") || queryTokens.Contains("change"))
            return "Update";

        if (queryTokens.Contains("delete") || queryTokens.Contains("remove"))
            return "Delete";

        return null;
    }

    private static IReadOnlySet<string> Tokenize(string text)
    {
        var tokens = text
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', '-', '/', ',', '.', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return tokens;
    }
}
