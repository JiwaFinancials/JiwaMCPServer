using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.CRBatchTX;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using Microsoft.VisualBasic.FileIO;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
[BusinessTool(EntityType = "CreditorPurchase", Aliases = ["creditor purchase", "supplier purchase batch"], Tags = ["accounts payable", "supplier transactions", "creditor purchases"]) ]
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

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Get", Aliases = ["get supplier invoice", "show supplier bill", "get vendor bill", "get ap invoice", "get accounts payable invoice", "get creditor invoice"])]
    [McpServerTool, Description("Retrieve a supplier invoice (creditor purchase) by BatchID or visible supplier bill/vendor bill/AP invoice number. This tool handles supplier invoices, supplier bills, vendor bills, AP invoices, accounts payable invoices, and creditor invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetCreditorPurchase(CreditorPurchaseGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var batchId = requestDTO.BatchID?.Trim();

            try
            {
                var result = await JiwaApiClient.GetAsync(new CreditorPurchaseGETRequest { BatchID = batchId }, ct);
                return result.ToJson();
            }
            catch (WebServiceException ex) when (ex.StatusCode == 404 && !string.IsNullOrWhiteSpace(batchId))
            {
                var resolvedBatchId = await TryResolveCreditorPurchaseBatchIdAsync(batchId, ct);
                if (string.IsNullOrWhiteSpace(resolvedBatchId))
                    throw;

                var resolved = await JiwaApiClient.GetAsync(new CreditorPurchaseGETRequest { BatchID = resolvedBatchId }, ct);
                return resolved.ToJson();
            }
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Update", Aliases = ["update supplier invoice", "edit supplier bill", "update vendor bill", "update ap invoice", "update accounts payable invoice", "update creditor invoice"])]
    [McpServerTool, Description("Update a supplier invoice (creditor purchase). This tool handles supplier invoices, supplier bills, vendor bills, AP invoices, accounts payable invoices, and creditor invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> UpdateCreditorPurchase(CreditorPurchasePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Delete", Aliases = ["delete supplier invoice", "remove supplier bill", "delete vendor bill", "delete ap invoice", "delete accounts payable invoice", "delete creditor invoice"])]
    [McpServerTool, Description("Delete a supplier invoice (creditor purchase). This tool handles supplier invoices, supplier bills, vendor bills, AP invoices, accounts payable invoices, and creditor invoices. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
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

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Create", Aliases = ["create supplier invoice", "add supplier bill", "create vendor bill", "create ap invoice", "create accounts payable invoice", "create creditor invoice"])]
    [McpServerTool, Description("Create a supplier invoice (creditor purchase batch transaction). This tool handles supplier invoices, supplier bills, vendor bills, AP invoices, accounts payable invoices, and creditor invoices. This is not purchase order (PO) header creation; for PO creation use CreatePurchaseOrder or CreatePurchaseOrderWithLines in PurchaseOrderTools. For local file import requests, local paths under LocalFileSystem:AllowedRoots are accessible via FileTools (list_local_directory/read_local_file/query_local_structured_file): read or parse the file first, then map values into this request and call AddCreditorPurchaseLine for line rows. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> CreateCreditorPurchase(CreditorPurchasePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool(Name = "ImportCreditorPurchaseFromLocalCsv"), Description("Import a new creditor purchase from a local CSV path under LocalFileSystem:AllowedRoots. Accepts either a CSV file path or a folder path containing exactly one CSV file. This creates the creditor purchase batch and then adds one purchase line per CSV row. If creditor is omitted, the CSV must include one of: CreditorID, CreditorAccountNo, AccountNo, Supplier, or Creditor. Amount can be provided as SupplierTransAmount, HomeTransAmount, or Amount. Optional columns: RemitNo, ReceiptDate, DueDate, CurrencyID.")]
    public Task<string> ImportCreditorPurchaseFromLocalCsv(
        string path,
        string? creditor = null,
        string? description = null,
        DateTime? batchDate = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (!LocalFilePathResolver.TryResolveAllowedPath(path, out var fullPath, out var error))
                return new { error }.ToJson();

            var sourcePath = fullPath;
            if (Directory.Exists(fullPath))
            {
                var csvFiles = Directory.EnumerateFiles(fullPath, "*.csv", System.IO.SearchOption.TopDirectoryOnly).OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ToList();
                if (csvFiles.Count == 0)
                    return new { error = $"Directory '{fullPath}' does not contain any .csv files" }.ToJson();

                if (csvFiles.Count > 1)
                {
                    return new
                    {
                        error = $"Directory '{fullPath}' contains multiple CSV files. Provide a specific file path.",
                        files = csvFiles.Take(20).ToList()
                    }.ToJson();
                }

                sourcePath = csvFiles[0];
            }

            if (!File.Exists(sourcePath))
                return new { error = $"File '{sourcePath}' was not found" }.ToJson();

            if (!string.Equals(Path.GetExtension(sourcePath), ".csv", StringComparison.OrdinalIgnoreCase))
                return new { error = "Only .csv files are supported for ImportCreditorPurchaseFromLocalCsv" }.ToJson();

            var (rows, csvError) = TryReadCsvRows(sourcePath);
            if (csvError is not null)
                return new { error = csvError }.ToJson();

            if (rows.Count == 0)
                return new { error = $"File '{sourcePath}' contains no data rows" }.ToJson();

            var creditorValue = creditor?.Trim();
            if (string.IsNullOrWhiteSpace(creditorValue))
            {
                var creditorCandidates = rows
                    .Select(row => GetFirstNonEmpty(row, "CreditorID", "CreditorAccountNo", "AccountNo", "Supplier", "Creditor", "SupplierCode", "SupplierID"))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (creditorCandidates.Count == 0)
                {
                    return new
                    {
                        error = "Unable to determine supplier/creditor from CSV. Provide creditor parameter or include one of these columns: CreditorID, CreditorAccountNo, AccountNo, Supplier, Creditor"
                    }.ToJson();
                }

                if (creditorCandidates.Count > 1)
                {
                    return new
                    {
                        error = "CSV contains multiple suppliers/creditors. Split the file by supplier or provide a file with one supplier.",
                        creditors = creditorCandidates.Take(20).ToList()
                    }.ToJson();
                }

                creditorValue = creditorCandidates[0];
            }

            var resolvedCreditor = await TryResolveCreditorForImportAsync(creditorValue!, ct);
            if (resolvedCreditor is null)
                return new { error = $"Unable to resolve supplier/creditor '{creditorValue}'" }.ToJson();

            var preparedLines = new List<ImportedCreditorPurchaseLine>();
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (!TryPrepareImportLine(rows[rowIndex], rowIndex + 2, out var line, out var lineError))
                    return new { error = lineError }.ToJson();

                preparedLines.Add(line!);
            }

            if (preparedLines.Count == 0)
                return new { error = $"File '{sourcePath}' did not contain importable rows" }.ToJson();

            var createRequest = new CreditorPurchasePOSTRequest
            {
                BatchType = CreditorBatchType.CreditorPurchase,
                BatchDate = batchDate ?? DateTime.Today,
                Description = string.IsNullOrWhiteSpace(description) ? $"Imported from {Path.GetFileName(sourcePath)}" : description.Trim()
            };

            var createdPurchase = await JiwaApiClient.PostAsync(createRequest, ct);
            var batchId = createdPurchase.BatchID?.Trim();
            if (string.IsNullOrWhiteSpace(batchId))
                return new { error = "Creditor purchase was created but BatchID was not returned" }.ToJson();

            foreach (var line in preparedLines)
            {
                var lineRequest = new CreditorPurchaseLinePOSTRequest
                {
                    BatchID = batchId,
                    CreditorRecID = resolvedCreditor.CreditorID,
                    CreditorAccountNo = resolvedCreditor.AccountNo,
                    RemitNo = line.RemitNo,
                    SupplierTransAmount = line.SupplierTransAmount,
                    HomeTransAmount = line.HomeTransAmount,
                    ReceiptDate = line.ReceiptDate,
                    DueDate = line.DueDate,
                    CurrencyID = line.CurrencyID
                };

                await JiwaApiClient.PostAsync(lineRequest, ct);
            }

            return new
            {
                batchId,
                sourcePath,
                linesImported = preparedLines.Count,
                creditor = new
                {
                    id = resolvedCreditor.CreditorID,
                    accountNo = resolvedCreditor.AccountNo,
                    name = resolvedCreditor.Name
                }
            }.ToJson();
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

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Search", Aliases = ["supplier invoice history", "vendor bill history", "ap invoice history", "accounts payable invoice history", "creditor invoice history"])]
    [McpServerTool(Name = "ListSupplierPurchaseHistory", ReadOnly = true), Description("List or search supplier invoice history (creditor purchases) by supplier, invoice, product, or other fields. This tool handles supplier invoices, supplier bills, vendor bills, AP invoices, accounts payable invoices, and creditor invoices for reporting/history. Includes invoice numbers for purchases. Use this when the user asks for supplier purchase history or what was purchased from a supplier. This is reporting/history, not purchase order header creation. " +
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

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Search", Aliases = ["list supplier invoices", "list vendor bills", "list ap invoices", "list accounts payable invoices", "list creditor invoices"])]
    [McpServerTool(Name = "ListSupplierPurchases", ReadOnly = true), Description("List or search supplier invoices (creditor purchase batches) by supplier, batch, invoice, or other fields. This tool handles supplier invoices, supplier bills, vendor bills, AP invoices, accounts payable invoices, and creditor invoices. Suppliers are also known as creditors. Use this when the user asks to show supplier purchases. This tool is for creditor purchase batches, not purchase order (PO) header creation. " +
        "Treat visible supplier purchase batch/document/invoice numbers as the default user input and resolve them here first, then call GetCreditorPurchase with the internal BatchID. " +
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

    private sealed record ResolvedCreditor(string CreditorID, string? AccountNo, string? Name);

    private sealed record ImportedCreditorPurchaseLine(
        string? RemitNo,
        decimal? SupplierTransAmount,
        decimal? HomeTransAmount,
        DateTime? ReceiptDate,
        DateTime? DueDate,
        string? CurrencyID);

    private static (List<Dictionary<string, string>> Rows, string? Error) TryReadCsvRows(string fullPath)
    {
        try
        {
            using var parser = new TextFieldParser(fullPath);
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = false;

            if (parser.EndOfData)
                return (new List<Dictionary<string, string>>(), null);

            var headers = parser.ReadFields();
            if (headers is null || headers.Length == 0)
                return (new List<Dictionary<string, string>>(), "CSV header row is missing");

            var normalizedHeaders = headers
                .Select((header, index) => string.IsNullOrWhiteSpace(header) ? $"Column{index + 1}" : header.Trim())
                .ToArray();

            var rows = new List<Dictionary<string, string>>();
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                if (fields.All(field => string.IsNullOrWhiteSpace(field)))
                    continue;

                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < normalizedHeaders.Length; i++)
                {
                    row[normalizedHeaders[i]] = i < fields.Length ? fields[i].Trim() : string.Empty;
                }

                rows.Add(row);
            }

            return (rows, null);
        }
        catch (MalformedLineException ex)
        {
            return (new List<Dictionary<string, string>>(), $"Invalid CSV format in '{fullPath}' at line {ex.LineNumber}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (new List<Dictionary<string, string>>(), $"Failed to read CSV '{fullPath}': {ex.Message}");
        }
    }

    private static string? GetFirstNonEmpty(IReadOnlyDictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static bool TryPrepareImportLine(
        IReadOnlyDictionary<string, string> row,
        int csvLineNumber,
        out ImportedCreditorPurchaseLine? line,
        out string? error)
    {
        line = null;
        error = null;

        var remitNo = GetFirstNonEmpty(row, "RemitNo", "RemittanceNo", "InvoiceNo", "InvoiceNumber", "Reference");
        var currencyId = GetFirstNonEmpty(row, "CurrencyID", "Currency", "CurrencyCode");

        var hasSupplierAmount = TryParseOptionalDecimal(GetFirstNonEmpty(row, "SupplierTransAmount"), out var supplierTransAmount, out var supplierAmountError);
        if (!hasSupplierAmount)
        {
            error = $"Line {csvLineNumber}: {supplierAmountError}";
            return false;
        }

        var hasHomeAmount = TryParseOptionalDecimal(GetFirstNonEmpty(row, "HomeTransAmount"), out var homeTransAmount, out var homeAmountError);
        if (!hasHomeAmount)
        {
            error = $"Line {csvLineNumber}: {homeAmountError}";
            return false;
        }

        if (supplierTransAmount is null && homeTransAmount is null)
        {
            var hasAmount = TryParseOptionalDecimal(GetFirstNonEmpty(row, "Amount", "TransAmount", "Total"), out var amount, out var amountError);
            if (!hasAmount)
            {
                error = $"Line {csvLineNumber}: {amountError}";
                return false;
            }

            if (amount is not null)
            {
                supplierTransAmount = amount;
                homeTransAmount = amount;
            }
        }

        if (supplierTransAmount is null && homeTransAmount is null)
        {
            error = $"Line {csvLineNumber}: amount is required (SupplierTransAmount, HomeTransAmount, or Amount column)";
            return false;
        }

        var hasReceiptDate = TryParseOptionalDate(GetFirstNonEmpty(row, "ReceiptDate", "Date", "TransactionDate"), out var receiptDate, out var receiptDateError);
        if (!hasReceiptDate)
        {
            error = $"Line {csvLineNumber}: {receiptDateError}";
            return false;
        }

        var hasDueDate = TryParseOptionalDate(GetFirstNonEmpty(row, "DueDate"), out var dueDate, out var dueDateError);
        if (!hasDueDate)
        {
            error = $"Line {csvLineNumber}: {dueDateError}";
            return false;
        }

        line = new ImportedCreditorPurchaseLine(remitNo, supplierTransAmount, homeTransAmount, receiptDate, dueDate, currencyId);
        return true;
    }

    private static bool TryParseOptionalDecimal(string? raw, out decimal? value, out string? error)
    {
        value = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var parsed) ||
            decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Invalid decimal value '{raw}'";
        return false;
    }

    private static bool TryParseOptionalDate(string? raw, out DateTime? value, out string? error)
    {
        value = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedInvariant) ||
            DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out parsedInvariant))
        {
            value = parsedInvariant.Date;
            return true;
        }

        error = $"Invalid date value '{raw}'";
        return false;
    }

    private static async Task<ResolvedCreditor?> TryResolveCreditorForImportAsync(string userValue, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userValue))
            return null;

        var matches = new Dictionary<string, ResolvedCreditor>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in BuildSupplierCandidates(userValue))
        {
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery { CreditorID = candidate, Take = 2, Skip = 0 }, matches, ct);
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery { AccountNo = candidate, Take = 2, Skip = 0 }, matches, ct);
            await AddCreditorMatchesAsync(new v_Jiwa_CreditorSummaryQuery { Name = candidate, Take = 2, Skip = 0 }, matches, ct);

            if (matches.Count > 1)
                return null;
        }

        return matches.Count == 1 ? matches.Values.First() : null;
    }

    private static async Task AddCreditorMatchesAsync(v_Jiwa_CreditorSummaryQuery query, IDictionary<string, ResolvedCreditor> matches, CancellationToken ct)
    {
        var response = await JiwaApiClient.GetAsync(query, ct);
        foreach (var row in response.Results ?? [])
        {
            var creditorId = row.CreditorID?.Trim();
            if (string.IsNullOrWhiteSpace(creditorId))
                continue;

            if (!matches.ContainsKey(creditorId))
                matches[creditorId] = new ResolvedCreditor(creditorId, row.AccountNo?.Trim(), row.Name?.Trim());
        }
    }

    private static IReadOnlyList<string> BuildSupplierCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        var withoutPrefix = Regex.Replace(trimmed, "^(SUP|SUPPLIER|CR|CREDITOR)[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
        AddCandidate(withoutPrefix);

        return candidates;

        void AddCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (seen.Add(value))
                candidates.Add(value);
        }
    }

    private static async Task<string?> TryResolveCreditorPurchaseBatchIdAsync(string documentNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return null;

        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in BuildCreditorPurchaseCandidates(documentNumber))
        {
            await AddCreditorPurchaseMatchesAsync(new v_Jiwa_CreditorPurchasesQuery
            {
                ReceiptID = candidate,
                Take = 2,
                Skip = 0
            }, matches, ct);

            await AddCreditorPurchaseMatchesAsync(new v_Jiwa_CreditorPurchasesQuery
            {
                BatchNum = candidate,
                Take = 2,
                Skip = 0
            }, matches, ct);

            if (matches.Count > 1)
                return null;
        }

        return matches.Count == 1 ? matches.First() : null;
    }

    private static async Task AddCreditorPurchaseMatchesAsync(v_Jiwa_CreditorPurchasesQuery query, ISet<string> matches, CancellationToken ct)
    {
        var response = await JiwaApiClient.GetAsync(query, ct);
        foreach (var row in response.Results ?? [])
        {
            var receiptId = row.ReceiptID?.Trim();
            if (!string.IsNullOrWhiteSpace(receiptId))
                matches.Add(receiptId);
        }
    }

    private static IReadOnlyList<string> BuildCreditorPurchaseCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        var withoutPrefix = Regex.Replace(trimmed, "^(BATCH|RECEIPT|CP)[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
        AddCandidate(withoutPrefix);

        return candidates;

        void AddCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (seen.Add(value))
                candidates.Add(value);
        }
    }
}
