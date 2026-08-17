using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.Tools;
using Xunit;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for CreditorPurchaseTools - creditor purchase operations.
/// </summary>
public class CreditorPurchaseToolsTests
{
    private readonly CreditorPurchaseTools _creditorPurchaseTools = new();

    public static IEnumerable<object[]> ReturnsStringCases()
    {
        yield return Case(nameof(CreditorPurchaseTools.ActivateCreditorPurchase), new CreditorPurchaseACTIVATERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseCustomFieldValues), new CreditorPurchaseCustomFieldValuesGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseLines), new CreditorPurchaseLinesGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.AddCreditorPurchaseLine), new CreditorPurchaseLinePOSTRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseLineCustomFields), new CreditorPurchaseLineCustomFieldsGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseDocumentType), new CreditorPurchaseDocumentTypeGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseDocumentType), new CreditorPurchaseDocumentTypePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.DeleteCreditorPurchaseDocumentType), new CreditorPurchaseDocumentTypeDELETERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseDocuments), new CreditorPurchaseDocumentsGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.AddCreditorPurchaseDocument), new CreditorPurchaseDocumentPOSTRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseNoteType), new CreditorPurchaseNoteTypeGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseNoteType), new CreditorPurchaseNoteTypePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.DeleteCreditorPurchaseNoteType), new CreditorPurchaseNoteTypeDELETERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseNotes), new CreditorPurchaseNotesGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.AddCreditorPurchaseNote), new CreditorPurchaseNotePOSTRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchase), new CreditorPurchaseGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchase), new CreditorPurchasePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.DeleteCreditorPurchase), new CreditorPurchaseDELETERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseCustomFields), new CreditorPurchaseCustomFieldsGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseDocumentTypes), new CreditorPurchaseDocumentTypesGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.CreateCreditorPurchaseDocumentType), new CreditorPurchaseDocumentTypePOSTRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseNoteTypes), new CreditorPurchaseNoteTypesGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.CreateCreditorPurchaseNoteType), new CreditorPurchaseNoteTypePOSTRequest());
        yield return Case(nameof(CreditorPurchaseTools.CreateCreditorPurchase), new CreditorPurchasePOSTRequest());
        yield return Case(nameof(CreditorPurchaseTools.ImportCreditorPurchaseFromLocalCsv), "C:\\Users\\scott\\Documents\\AIChat", null, null, null, CancellationToken.None);
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseCustomFieldValue), new CreditorPurchaseCustomFieldValueGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseCustomFieldValue), new CreditorPurchaseCustomFieldValuePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseLine), new CreditorPurchaseLineGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseLine), new CreditorPurchaseLinePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.DeleteCreditorPurchaseLine), new CreditorPurchaseLineDELETERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseDocument), new CreditorPurchaseDocumentGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseDocument), new CreditorPurchaseDocumentPATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.DeleteCreditorPurchaseDocument), new CreditorPurchaseDocumentDELETERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseNote), new CreditorPurchaseNoteGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseNote), new CreditorPurchaseNotePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.DeleteCreditorPurchaseNote), new CreditorPurchaseNoteDELETERequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseLineCustomFieldValues), new CreditorPurchaseLineCustomFieldValuesGETManyRequest());
        yield return Case(nameof(CreditorPurchaseTools.GetCreditorPurchaseLineCustomFieldValue), new CreditorPurchaseLineCustomFieldValueGETRequest());
        yield return Case(nameof(CreditorPurchaseTools.UpdateCreditorPurchaseLineCustomFieldValue), new CreditorPurchaseLineCustomFieldValuePATCHRequest());
        yield return Case(nameof(CreditorPurchaseTools.SearchCreditorPurchaseBatchInformation), new v_Jiwa_CreditorPurchaseInformationQuery(), false, null, CancellationToken.None);
        yield return Case(nameof(CreditorPurchaseTools.SearchCreditorPurchaseBatches), new v_Jiwa_CreditorPurchasesQuery(), false, null, CancellationToken.None);
    }

    [Theory]
    [MemberData(nameof(ReturnsStringCases))]
    public async Task PublicMethods_ReturnStringResult(string methodName, object?[] arguments)
    {
        EnsureApiUrl();

        var result = await InvokeToolMethodAsync(methodName, arguments);

        Assert.IsType<string>(result);
    }

    [Fact]
    public void GetCreditorPurchase_WithCancellation_ReturnsTask()
    {
        var requestDto = new CreditorPurchaseGETRequest();
        var cts = new CancellationTokenSource();

        EnsureApiUrl();

        var task = _creditorPurchaseTools.GetCreditorPurchase(requestDto, cts.Token);

        Assert.NotNull(task);
        Assert.IsAssignableFrom<Task<string>>(task);
    }

    [Fact]
    public async Task ImportCreditorPurchaseFromLocalCsv_UploadedAttachmentPathOutsideAllowlist_ReadsUploadedCsv()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");
        var attachedPath = @"C:\Users\scott\Downloads\creditor-purchase.csv";
        var fileStorage = new FileStorageService();
        var tools = new CreditorPurchaseTools(fileStorage);

        try
        {
            Directory.CreateDirectory(tempRoot);
            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };
            FileStorageService.SetSessionId($"creditor-import-{Guid.NewGuid():N}");

            var csv = "CreditorID,Amount\r\n";
            var upload = fileStorage.UploadFile("creditor-purchase.csv", "text/csv", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csv)));
            Assert.True(upload.IsSuccess);

            var result = await tools.ImportCreditorPurchaseFromLocalCsv(attachedPath);

            Assert.DoesNotContain("outside allowed roots", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("contains no data rows", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task ImportCreditorPurchaseFromLocalCsv_UploadedPdfPathOutsideAllowlist_ReturnsCsvValidationError()
    {
        var originalRoots = Config.LocalFileSystemAllowedRoots;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"JiwaMcpServerTests-{Guid.NewGuid():N}");
        var attachedPath = @"C:\Users\scott\Downloads\invoice.pdf";
        var fileStorage = new FileStorageService();
        var tools = new CreditorPurchaseTools(fileStorage);

        try
        {
            Directory.CreateDirectory(tempRoot);
            Config.LocalFileSystemAllowedRoots = new[] { tempRoot };
            FileStorageService.SetSessionId($"creditor-import-{Guid.NewGuid():N}");

            var upload = fileStorage.UploadFile("invoice.pdf", "application/pdf", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("pdf bytes")));
            Assert.True(upload.IsSuccess);

            var result = await tools.ImportCreditorPurchaseFromLocalCsv(attachedPath);

            Assert.DoesNotContain("outside allowed roots", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Only .csv files are supported for ImportCreditorPurchaseFromLocalCsv", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Config.LocalFileSystemAllowedRoots = originalRoots;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    private static object[] Case(string methodName, params object?[] arguments)
    {
        if (arguments.Length == 1)
        {
            arguments = [arguments[0], CancellationToken.None];
        }

        return new object[] { methodName, arguments };
    }

    private static void EnsureApiUrl()
    {
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }
    }

    private Task<string> InvokeToolMethodAsync(string methodName, object?[] arguments)
    {
        var method = typeof(CreditorPurchaseTools).GetMethod(methodName);
        Assert.NotNull(method);

        var invocation = method!.Invoke(_creditorPurchaseTools, arguments);
        return Assert.IsAssignableFrom<Task<string>>(invocation);
    }
}
