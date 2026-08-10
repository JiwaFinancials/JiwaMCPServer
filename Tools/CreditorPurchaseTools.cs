using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.CRBatchTX;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class CreditorPurchaseTools : JiwaToolBase
{
    [McpServerTool, Description("Activates a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> ActivateCreditorPurchase(CreditorPurchaseACTIVATERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of custom field values for a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseCustomFieldValues(CreditorPurchaseCustomFieldValuesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase lines. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseLines(CreditorPurchaseLinesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Appends a line to a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> AddCreditorPurchaseLine(CreditorPurchaseLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase line custom fields. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseLineCustomFields(CreditorPurchaseLineCustomFieldsGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase document type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypeGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase document type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Deletes a creditor purchase document type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypeDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, DocumentTypeID = requestDTO.DocumentTypeID }.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase documents. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseDocuments(CreditorPurchaseDocumentsGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Appends a document to a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> AddCreditorPurchaseDocument(CreditorPurchaseDocumentPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase note type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseNoteType(CreditorPurchaseNoteTypeGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase note type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseNoteType(CreditorPurchaseNoteTypePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Deletes a creditor purchase note type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteCreditorPurchaseNoteType(CreditorPurchaseNoteTypeDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, NoteTypeID = requestDTO.NoteTypeID }.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase notes. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseNotes(CreditorPurchaseNotesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Appends a note to a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> AddCreditorPurchaseNote(CreditorPurchaseNotePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchase(CreditorPurchaseGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchase(CreditorPurchasePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Deletes a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteCreditorPurchase(CreditorPurchaseDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, BatchID = requestDTO.BatchID }.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase custom fields. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseCustomFields(CreditorPurchaseCustomFieldsGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase document types. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseDocumentTypes(CreditorPurchaseDocumentTypesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Creates a new creditor purchase document type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of creditor purchase note types. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseNoteTypes(CreditorPurchaseNoteTypesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Creates a new creditor purchase note type. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateCreditorPurchaseNoteType(CreditorPurchaseNoteTypePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Creates a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateCreditorPurchase(CreditorPurchasePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase custom field value. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseCustomFieldValue(CreditorPurchaseCustomFieldValueGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase custom field value. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseCustomFieldValue(CreditorPurchaseCustomFieldValuePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase line. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseLine(CreditorPurchaseLineGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a line for a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseLine(CreditorPurchaseLinePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Deletes a line from a creditor purchase. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
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

    [McpServerTool, Description("Retrieves a creditor purchase document. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseDocument(CreditorPurchaseDocumentGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase document. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseDocument(CreditorPurchaseDocumentPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Deletes a creditor purchase document. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteCreditorPurchaseDocument(CreditorPurchaseDocumentDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new
            {
                Deleted = true,
                BatchID = requestDTO.BatchID,
                DocumentID = requestDTO.DocumentID
            }.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase note. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseNote(CreditorPurchaseNoteGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase note. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseNote(CreditorPurchaseNotePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Deletes a creditor purchase note. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> DeleteCreditorPurchaseNote(CreditorPurchaseNoteDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new
            {
                Deleted = true,
                BatchID = requestDTO.BatchID,
                NoteID = requestDTO.NoteID
            }.ToJson();
        });

    [McpServerTool, Description("Retrieves a list of custom field values for a creditor purchase line. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseLineCustomFieldValues(CreditorPurchaseLineCustomFieldValuesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Retrieves a creditor purchase line custom field value. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchaseLineCustomFieldValue(CreditorPurchaseLineCustomFieldValueGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Updates a creditor purchase line custom field value. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchaseLineCustomFieldValue(CreditorPurchaseLineCustomFieldValuePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool(Name = "ListSupplierPurchaseHistory", ReadOnly = true), Description("List or search supplier purchase history by supplier, invoice, product, or other fields. Includes invoice numbers for purchases. Use this when the user asks for supplier purchase history or what was purchased from a supplier. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token. " +
        "You can use the GetCreditorPurchase tool to retrieve full details for a specific creditor purchase if required.")]
    public Task<string> SearchCreditorPurchaseBatchInformation(
        v_Jiwa_CreditorPurchaseInformationQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    [McpServerTool(Name = "ListSupplierPurchases", ReadOnly = true), Description("List or search supplier purchases by supplier, batch, invoice, or other fields. Suppliers are also known as creditors. Use this when the user asks to show supplier purchases. " +
        "Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. " +
        "Supports pagination via skip and take parameters. A single call may return only a partial result set. " +
        "For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. " +
        "Then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchCreditorPurchaseBatches(
        v_Jiwa_CreditorPurchasesQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
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
