using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Agent.Catalog;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolDescriptions;

namespace JiwaMcpServer.Tools;

[PlannerDomain("CreditorPurchaseTools")]
[McpServerToolType]
public class CreditorPurchaseTools : JiwaToolBase
{
    [McpServerTool, Description(CreditorToolDescriptions.ActivateCreditorPurchase)]
    public Task<string> ActivateCreditorPurchase(CreditorPurchaseACTIVATERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseCustomFieldValues)]
    //public Task<string> GetCreditorPurchaseCustomFieldValues(CreditorPurchaseCustomFieldValuesGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    [McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseLines)]
    public Task<string> GetCreditorPurchaseLines(CreditorPurchaseLinesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description(CreditorToolDescriptions.AddCreditorPurchaseLine)]
    public Task<string> AddCreditorPurchaseLine(CreditorPurchaseLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseLineCustomFields)]
    //public Task<string> GetCreditorPurchaseLineCustomFields(CreditorPurchaseLineCustomFieldsGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseDocumentType)]
    //public Task<string> GetCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypeGETRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseDocumentType)]
    //public Task<string> ModifyCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypePATCHRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.DeleteCreditorPurchaseDocumentType)]
    //public Task<string> DeleteCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypeDELETERequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        await JiwaApiClient.DeleteAsync(requestDTO, ct);
    //        return new { Deleted = true, DocumentTypeID = requestDTO.DocumentTypeID }.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseDocuments)]
    //public Task<string> GetCreditorPurchaseDocuments(CreditorPurchaseDocumentsGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.AddCreditorPurchaseDocument)]
    //public Task<string> AddCreditorPurchaseDocument(CreditorPurchaseDocumentPOSTRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PostAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseNoteType)]
    //public Task<string> GetCreditorPurchaseNoteType(CreditorPurchaseNoteTypeGETRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseNoteType)]
    //public Task<string> ModifyCreditorPurchaseNoteType(CreditorPurchaseNoteTypePATCHRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.DeleteCreditorPurchaseNoteType)]
    //public Task<string> DeleteCreditorPurchaseNoteType(CreditorPurchaseNoteTypeDELETERequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        await JiwaApiClient.DeleteAsync(requestDTO, ct);
    //        return new { Deleted = true, NoteTypeID = requestDTO.NoteTypeID }.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseNotes)]
    //public Task<string> GetCreditorPurchaseNotes(CreditorPurchaseNotesGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.AddCreditorPurchaseNote)]
    //public Task<string> AddCreditorPurchaseNote(CreditorPurchaseNotePOSTRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PostAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    [McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchase)]
    public Task<string> GetCreditorPurchase(CreditorPurchaseGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchase)]
    public Task<string> ModifyCreditorPurchase(CreditorPurchasePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description(CreditorToolDescriptions.DeleteCreditorPurchase)]
    public Task<string> DeleteCreditorPurchase(CreditorPurchaseDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, BatchID = requestDTO.BatchID }.ToJson();
        });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseCustomFields)]
    //public Task<string> GetCreditorPurchaseCustomFields(CreditorPurchaseCustomFieldsGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseDocumentTypes)]
    //public Task<string> GetCreditorPurchaseDocumentTypes(CreditorPurchaseDocumentTypesGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.CreateCreditorPurchaseDocumentType)]
    //public Task<string> CreateCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypePOSTRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PostAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseNoteTypes)]
    //public Task<string> GetCreditorPurchaseNoteTypes(CreditorPurchaseNoteTypesGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.CreateCreditorPurchaseNoteType)]
    //public Task<string> CreateCreditorPurchaseNoteType(CreditorPurchaseNoteTypePOSTRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PostAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    [McpServerTool, Description(CreditorToolDescriptions.CreateCreditorPurchase)]
    public Task<string> CreateCreditorPurchase(CreditorPurchasePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseCustomFieldValue)]
    //public Task<string> GetCreditorPurchaseCustomFieldValue(CreditorPurchaseCustomFieldValueGETRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseCustomFieldValue)]
    //public Task<string> ModifyCreditorPurchaseCustomFieldValue(CreditorPurchaseCustomFieldValuePATCHRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    [McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseLine)]
    public Task<string> GetCreditorPurchaseLine(CreditorPurchaseLineGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseLine)]
    public Task<string> ModifyCreditorPurchaseLine(CreditorPurchaseLinePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description(CreditorToolDescriptions.DeleteCreditorPurchaseLine)]
    public Task<string> DeleteCreditorPurchaseLine(CreditorPurchaseLineDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new
            {
                Deleted = true,
                BatchID = requestDTO.BatchID,
                LineID = requestDTO.LineID
            }.ToJson();
        });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseDocument)]
    //public Task<string> GetCreditorPurchaseDocument(CreditorPurchaseDocumentGETRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseDocument)]
    //public Task<string> ModifyCreditorPurchaseDocument(CreditorPurchaseDocumentPATCHRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.DeleteCreditorPurchaseDocument)]
    //public Task<string> DeleteCreditorPurchaseDocument(CreditorPurchaseDocumentDELETERequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        await JiwaApiClient.DeleteAsync(requestDTO, ct);
    //        return new
    //        {
    //            Deleted = true,
    //            BatchID = requestDTO.BatchID,
    //            DocumentID = requestDTO.DocumentID
    //        }.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseNote)]
    //public Task<string> GetCreditorPurchaseNote(CreditorPurchaseNoteGETRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseNote)]
    //public Task<string> ModifyCreditorPurchaseNote(CreditorPurchaseNotePATCHRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.DeleteCreditorPurchaseNote)]
    //public Task<string> DeleteCreditorPurchaseNote(CreditorPurchaseNoteDELETERequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        await JiwaApiClient.DeleteAsync(requestDTO, ct);
    //        return new
    //        {
    //            Deleted = true,
    //            BatchID = requestDTO.BatchID,
    //            NoteID = requestDTO.NoteID
    //        }.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseLineCustomFieldValues)]
    //public Task<string> GetCreditorPurchaseLineCustomFieldValues(CreditorPurchaseLineCustomFieldValuesGETManyRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.GetCreditorPurchaseLineCustomFieldValue)]
    //public Task<string> GetCreditorPurchaseLineCustomFieldValue(CreditorPurchaseLineCustomFieldValueGETRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.GetAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    //[McpServerTool, Description(CreditorToolDescriptions.ModifyCreditorPurchaseLineCustomFieldValue)]
    //public Task<string> ModifyCreditorPurchaseLineCustomFieldValue(CreditorPurchaseLineCustomFieldValuePATCHRequest requestDTO, CancellationToken ct = default)
    //    => InvokeToolAsync(async () =>
    //    {
    //        var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
    //        return result.ToJson();
    //    });

    [McpServerTool(ReadOnly = true), Description(CreditorToolDescriptions.QuerySupplierPurchaseHistory)]
    public Task<string> QuerySupplierPurchaseHistory(
        [Description("Search and filter criteria for historical supplier purchasing activity, such as suppliers, products, invoice references, and date ranges.")] v_Jiwa_CreditorPurchaseInformationQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching supplier purchase history result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same supplier purchase history query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [McpServerTool(ReadOnly = true), Description(CreditorToolDescriptions.QuerySupplierPurchases)]
    public Task<string> QuerySupplierPurchases(
        [Description("Search and filter criteria for supplier purchase batches, invoices, BatchIDs, supplier references, statuses, and date ranges.")] v_Jiwa_CreditorPurchasesQuery requestDTO,
        [Description("Set to true only after confirming that you want a large matching supplier purchase result set returned.")] bool confirmLargeResultSet = false,
        [Description("Echo back the confirmation token returned by a previous large-result warning when retrying the same supplier purchase query.")] string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });
}
