using System.Text;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JiwaMcpServer.Tests;

public class ToolRoutingRerankerTests
{
    [Fact]
    public async Task Reranker_PrefersSemanticInvoiceMatch()
    {
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance);

        var candidates = BuildInvoiceCandidates();
        var result = await reranker.RerankAsync("search invoices", candidates, 1);

        Assert.Single(result);
        Assert.Equal("SearchInvoices", result[0].Name);
        Assert.Contains("semantic", result[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reranker_BoostsPurchaseOrderIntentOverCreditorPurchase()
    {
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("CreateCreditorPurchase", 0.92, new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["supplier invoice", "vendor bill", "ap invoice"], ["accounts payable"])),
            new("CreatePurchaseOrder", 0.83, new ToolCatalogEntry("CreatePurchaseOrder", "Create a purchase order.", "Procurement", "PurchaseOrder", "Create purchase order", ["create po", "purchase order", "po", "raise po"], ["procurement"]))
        ];

        var result = await reranker.RerankAsync("Create a PO", candidates, 1);

        Assert.Single(result);
        Assert.Equal("CreatePurchaseOrder", result[0].Name);
        Assert.Contains("purchase-order", result[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reranker_PrefersNonCsvToolForPdfAttachment()
    {
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.pdf", "application/pdf");
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance,
            fileStorage);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("ImportCreditorPurchaseFromLocalCsv", 0.96, new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename; do not use this tool for PDF or image invoice attachments.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"])),
            new("CreateCreditorPurchase", 0.91, new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["supplier invoice", "vendor bill", "creditor purchase"], ["accounts payable"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase from the attached file", candidates, 1);

        Assert.Single(result);
        Assert.Equal("CreateCreditorPurchase", result[0].Name);
        Assert.Contains("document", result[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reranker_AvoidsCsvToolForGenericAttachmentPromptWithoutFileMetadata()
    {
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("ImportCreditorPurchaseFromLocalCsv", 0.98, new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"])),
            new("CreateCreditorPurchase", 0.74, new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice from an uploaded file.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["supplier invoice", "creditor purchase", "attached file"], ["accounts payable"])),
            new("CreateSupplier", 0.70, new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor"], ["procurement"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase from the attached file, create a new creditor if necessary", candidates, 2);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, tool => tool.Name == "CreateCreditorPurchase");
        Assert.Contains(result, tool => tool.Name == "CreateSupplier");
        Assert.DoesNotContain(result, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
    }

    [Fact]
    public async Task Reranker_DoesNotPreferSupplierForNewCreditorPurchasePrompt()
    {
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("CreateSupplier", 0.93, new ToolCatalogEntry("CreateSupplier", "Create a supplier or creditor.", "Procurement", "Supplier", "Create supplier", ["create supplier", "create creditor", "new creditor"], ["procurement"])),
            new("CreateCreditorPurchase", 0.82, new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["creditor purchase", "supplier invoice", "vendor bill"], ["accounts payable"]))
        ];

        var result = await reranker.RerankAsync(@"Import the file in C:\Users\scott\Documents\AIChat as a new creditor purchase", candidates, 1);

        Assert.Single(result);
        Assert.Equal("CreateCreditorPurchase", result[0].Name);
        Assert.Contains("supplier-invoice", result[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reranker_PrefersPdfProcessingAndCreditorPurchaseToolsForPdfAttachment()
    {
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.pdf", "application/pdf");
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance,
            fileStorage);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("ImportCreditorPurchaseFromLocalCsv", 0.99, new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename; do not use this tool for PDF or image invoice attachments.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"])),
            new("DocumentExtractInvoice", 0.74, new ToolCatalogEntry("DocumentExtractInvoice", "Extract invoice fields from an uploaded PDF or image document.", "Documents", "Document", "Extract invoice", ["invoice extraction", "pdf", "ocr", "uploaded document"], ["document analysis"])),
            new("CreateCreditorPurchaseFromAttachment", 0.78, new ToolCatalogEntry("CreateCreditorPurchaseFromAttachment", "Create a supplier invoice from an attached PDF or uploaded document.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["creditor purchase", "supplier invoice", "attached file", "uploaded document"], ["accounts payable"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase from the attached file, create a new creditor if necessary", candidates, 2);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, tool => tool.Name == "DocumentExtractInvoice");
        Assert.Contains(result, tool => tool.Name == "CreateCreditorPurchaseFromAttachment");
        Assert.DoesNotContain(result, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
        Assert.All(result, tool => Assert.Contains("pdf", tool.Reason, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reranker_AppendsDocumentAttachmentToolsBeyondSelectionLimit()
    {
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.pdf", "application/pdf");
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance,
            fileStorage);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("CreateCreditorPurchaseFromAttachment", 0.88, new ToolCatalogEntry("CreateCreditorPurchaseFromAttachment", "Create a supplier invoice from an attached PDF or uploaded document.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["creditor purchase", "supplier invoice", "attached file", "uploaded document"], ["accounts payable"])),
            new("DocumentIngest", 0.54, new ToolCatalogEntry("DocumentIngest", "Ingest an uploaded document for search and extraction.", "Documents", "Document", "Ingest document", ["document", "pdf", "uploaded document", "source file"], ["document analysis", "ocr"])),
            new("DocumentExtractInvoice", 0.53, new ToolCatalogEntry("DocumentExtractInvoice", "Extract invoice fields from an uploaded PDF or image document.", "Documents", "Document", "Extract invoice", ["invoice extraction", "pdf", "ocr", "uploaded document"], ["document analysis"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase from the attached file", candidates, 1);

        Assert.Equal(3, result.Count);
        Assert.Equal("CreateCreditorPurchaseFromAttachment", result[0].Name);
        Assert.Contains(result, tool => tool.Name == "DocumentIngest");
        Assert.Contains(result, tool => tool.Name == "DocumentExtractInvoice");
        Assert.Contains(result.Where(tool => tool.Name != "CreateCreditorPurchaseFromAttachment"), tool =>
            tool.Reason.Contains("Attachment workflow requirement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reranker_UsesUploadedPdfContextWithoutAttachmentPhrase()
    {
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.pdf", "application/pdf");
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance,
            fileStorage);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("CreateCreditorPurchaseFromAttachment", 0.88, new ToolCatalogEntry("CreateCreditorPurchaseFromAttachment", "Create a supplier invoice from an attached PDF or uploaded document.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["creditor purchase", "supplier invoice", "attached file", "uploaded document"], ["accounts payable"])),
            new("DocumentIngest", 0.54, new ToolCatalogEntry("DocumentIngest", "Ingest an uploaded document for search and extraction.", "Documents", "Document", "Ingest document", ["document", "pdf", "uploaded document", "source file"], ["document analysis", "ocr"])),
            new("DocumentExtractInvoice", 0.53, new ToolCatalogEntry("DocumentExtractInvoice", "Extract invoice fields from an uploaded PDF or image document.", "Documents", "Document", "Extract invoice", ["invoice extraction", "pdf", "ocr", "uploaded document"], ["document analysis"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase from this", candidates, 1);

        Assert.Equal(3, result.Count);
        Assert.Equal("CreateCreditorPurchaseFromAttachment", result[0].Name);
        Assert.Contains(result, tool => tool.Name == "DocumentIngest");
        Assert.Contains(result, tool => tool.Name == "DocumentExtractInvoice");
    }

    [Fact]
    public async Task Reranker_UsesUploadedPdfContextAcrossSessions()
    {
        var fileStorage = new FileStorageService();
        FileStorageService.SetSessionId(Guid.NewGuid().ToString("n"));
        var uploadResult = fileStorage.UploadFile("kehnk1ad.pdf", "application/pdf", Convert.ToBase64String(Encoding.UTF8.GetBytes("test-content")));
        Assert.True(uploadResult.IsSuccess);

        FileStorageService.SetSessionId(Guid.NewGuid().ToString("n"));
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance,
            fileStorage);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("CreateCreditorPurchase", 0.84, new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["supplier invoice", "creditor purchase"], ["accounts payable"])),
            new("DocumentIngest", 0.52, new ToolCatalogEntry("DocumentIngest", "Ingest an uploaded document for search and extraction.", "Documents", "Document", "Ingest document", ["document", "pdf", "uploaded document", "source file"], ["document analysis", "ocr"])),
            new("DocumentExtractInvoice", 0.51, new ToolCatalogEntry("DocumentExtractInvoice", "Extract invoice fields from an uploaded PDF or image document.", "Documents", "Document", "Extract invoice", ["invoice extraction", "pdf", "ocr", "uploaded document"], ["document analysis"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase", candidates, 1);

        Assert.Equal(3, result.Count);
        Assert.Equal("CreateCreditorPurchase", result[0].Name);
        Assert.Contains(result, tool => tool.Name == "DocumentIngest");
        Assert.Contains(result, tool => tool.Name == "DocumentExtractInvoice");
    }

    [Fact]
    public async Task Reranker_AppendsCsvAttachmentToolsBeyondSelectionLimit()
    {
        var fileStorage = CreateFileStorageWithUploadedFile("invoice.csv", "text/csv");
        var reranker = new DeterministicToolReranker(
            new ToolRoutingContextAccessor(),
            Options.Create(new ToolRoutingDiagnosticsOptions()),
            NullLogger<DeterministicToolReranker>.Instance,
            fileStorage);

        IReadOnlyList<ToolCandidate> candidates =
        [
            new("CreateCreditorPurchase", 0.84, new ToolCatalogEntry("CreateCreditorPurchase", "Create a supplier invoice from uploaded data.", "Procurement", "CreditorPurchase", "Create creditor purchase", ["supplier invoice", "creditor purchase", "attached file"], ["accounts payable"])),
            new("QueryCsv", 0.52, new ToolCatalogEntry("QueryCsv", "Query a CSV file using natural language questions.", "Files", "File", "Query csv", ["csv", "uploaded file", "source file"], ["file", "spreadsheet"])),
            new("ImportCreditorPurchaseFromLocalCsv", 0.51, new ToolCatalogEntry("ImportCreditorPurchaseFromLocalCsv", "Import a supplier invoice from a CSV file provided as an uploaded attachment path or filename.", "Procurement", "CreditorPurchase", "Import creditor purchase", ["csv", "uploaded file", "supplier invoice"], ["accounts payable"]))
        ];

        var result = await reranker.RerankAsync("create a creditor purchase from the attached file", candidates, 1);

        Assert.Equal(2, result.Count);
        Assert.Equal("CreateCreditorPurchase", result[0].Name);
        Assert.Contains(result, tool => tool.Name == "QueryCsv");
        Assert.DoesNotContain(result, tool => tool.Name == "ImportCreditorPurchaseFromLocalCsv");
    }

    private static IReadOnlyList<ToolCandidate> BuildInvoiceCandidates()
    {
        return
        [
            new ToolCandidate("SearchInvoices", 0.91, new ToolCatalogEntry("SearchInvoices", "Search invoices", "Finance", "Invoices", "Search invoices", ["invoice"], ["finance"])),
            new ToolCandidate("GetCustomer", 0.60, new ToolCatalogEntry("GetCustomer", "Get customer", "CRM", "Customer", "Get customer", ["customer"], ["crm"]))
        ];
    }

    private static FileStorageService CreateFileStorageWithUploadedFile(string fileName, string mimeType)
    {
        var fileStorage = new FileStorageService();
        FileStorageService.SetSessionId(Guid.NewGuid().ToString("n"));
        var uploadResult = fileStorage.UploadFile(fileName, mimeType, Convert.ToBase64String(Encoding.UTF8.GetBytes("test-content")));
        Assert.True(uploadResult.IsSuccess);
        return fileStorage;
    }
}
