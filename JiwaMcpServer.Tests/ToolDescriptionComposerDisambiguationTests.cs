using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.Tools;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolDescriptionComposerDisambiguationTests
{
    [Fact]
    public void Compose_CreatePurchaseOrder_IncludesNotInvoiceHint()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var composer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var descriptor = extractor
            .ExtractFromAssemblies([typeof(PurchaseOrderTools).Assembly])
            .Single(x => x.ToolName == "CreatePurchaseOrder");

        var description = composer.Compose(descriptor);

        Assert.Contains("purchase order (PO)", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a supplier invoice", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_CreateCreditorPurchase_IncludesNotPoHint()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var composer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var descriptor = extractor
            .ExtractFromAssemblies([typeof(CreditorPurchaseTools).Assembly])
            .Single(x => x.ToolName == "CreateCreditorPurchase");

        var description = composer.Compose(descriptor);

        Assert.Contains("supplier invoice", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a purchase order", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_DocumentIngest_IncludesUploadedDocumentWorkflowHint()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var composer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var descriptor = extractor
            .ExtractFromAssemblies([typeof(DocumentTools).Assembly])
            .Single(x => x.ToolName == "DocumentIngest");

        var description = composer.Compose(descriptor);

        Assert.Contains("uploaded pdf", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("documentid", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_DocumentExtractInvoice_IncludesDocumentIngestHint()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var composer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());

        var descriptor = extractor
            .ExtractFromAssemblies([typeof(DocumentTools).Assembly])
            .Single(x => x.ToolName == "DocumentExtractInvoice");

        var description = composer.Compose(descriptor);
        var normalizedDescription = description.Replace(" ", string.Empty);

        Assert.Contains("afterdocumentingest", normalizedDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("documentid", description, StringComparison.OrdinalIgnoreCase);
    }
}
