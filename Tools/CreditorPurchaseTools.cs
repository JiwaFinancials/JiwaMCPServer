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
public class CreditorPurchaseTools(FileStorageService? fileStorage = null) : JiwaToolBase
{
    private readonly FileStorageService _fileStorage = fileStorage ?? new();

    [McpServerTool, Description("Activate a supplier invoice.")]
    public Task<string> ActivateCreditorPurchase(CreditorPurchaseACTIVATERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice custom field values.")]
    public Task<string> GetCreditorPurchaseCustomFieldValues(CreditorPurchaseCustomFieldValuesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice lines.")]
    public Task<string> GetCreditorPurchaseLines(CreditorPurchaseLinesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Add a line to a supplier invoice.")]
    public Task<string> AddCreditorPurchaseLine(CreditorPurchaseLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PostAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice line custom fields.")]
    public Task<string> GetCreditorPurchaseLineCustomFields(CreditorPurchaseLineCustomFieldsGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice document type.")]
    public Task<string> GetCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypeGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice document type.")]
    public Task<string> UpdateCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Delete a supplier invoice document type.")]
    public Task<string> DeleteCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypeDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, DocumentTypeID = requestDTO.DocumentTypeID }.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice documents.")]
    public Task<string> GetCreditorPurchaseDocuments(CreditorPurchaseDocumentsGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Add a document to a supplier invoice.")]
    public Task<string> AddCreditorPurchaseDocument(CreditorPurchaseDocumentPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PostAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice note type.")]
    public Task<string> GetCreditorPurchaseNoteType(CreditorPurchaseNoteTypeGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice note type.")]
    public Task<string> UpdateCreditorPurchaseNoteType(CreditorPurchaseNoteTypePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Delete a supplier invoice note type.")]
    public Task<string> DeleteCreditorPurchaseNoteType(CreditorPurchaseNoteTypeDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            await JiwaApiClient.DeleteAsync(requestDTO, ct);
            return new { Deleted = true, NoteTypeID = requestDTO.NoteTypeID }.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice notes.")]
    public Task<string> GetCreditorPurchaseNotes(CreditorPurchaseNotesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Add a note to a supplier invoice.")]
    public Task<string> AddCreditorPurchaseNote(CreditorPurchaseNotePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PostAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Get", Aliases = ["get supplier invoice", "show supplier bill", "get vendor bill", "get ap invoice", "get accounts payable invoice", "get creditor invoice"])]
    [McpServerTool, Description("Get supplier invoice details by BatchID or document number.")]
    public Task<string> GetCreditorPurchase(CreditorPurchaseGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedCreditorPurchaseBatchIdAsync(
                requestDTO.BatchID,
                ct,
                async (batchId, innerCt) => await JiwaApiClient.GetAsync(
                    new CreditorPurchaseGETRequest { BatchID = batchId },
                    innerCt));

            return result.ToJson();
        });

    private static Task<T> ExecuteWithResolvedCreditorPurchaseBatchIdAsync<T>(
        string? batchId,
        CancellationToken ct,
        Func<string, CancellationToken, Task<T>> executeAsync)
        => ExecuteWithResolvedIdentifierAsync(
            batchId,
            ct,
            executeAsync,
            TryResolveCreditorPurchaseBatchIdAsync);

    private static Task<TResponse> ExecuteWithResolvedCreditorPurchaseBatchRequestAsync<TRequest, TResponse>(
        TRequest requestDTO,
        string? batchId,
        CancellationToken ct,
        Func<TRequest, string, CancellationToken, Task<TResponse>> executeAsync)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(requestDTO);

        return ExecuteWithResolvedCreditorPurchaseBatchIdAsync(
            batchId,
            ct,
            async (resolvedBatchId, innerCt) => await executeAsync(requestDTO, resolvedBatchId, innerCt));
    }

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Resolve", Aliases = ["resolve supplier invoice number", "resolve creditor invoice number", "invoice no to batch id", "document number to batch id", "resolve batch number"])]
    [McpServerTool(Name = "ResolveCreditorPurchaseBatchId", ReadOnly = true), Description("Resolve a supplier invoice document number to BatchID.")]
    public Task<string> ResolveCreditorPurchaseBatchId(string documentNumber, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);

            var trimmedDocumentNumber = documentNumber.Trim();
            var resolvedBatchId = await TryResolveCreditorPurchaseBatchIdAsync(trimmedDocumentNumber, ct);
            if (string.IsNullOrWhiteSpace(resolvedBatchId))
            {
                return new
                {
                    success = false,
                    error = $"Unable to resolve supplier invoice document number '{trimmedDocumentNumber}' to a unique BatchID.",
                    hint = "Use ListSupplierPurchases with BatchNum or ReceiptID to locate the correct BatchID, then retry."
                }.ToJson();
            }

            return new
            {
                success = true,
                documentNo = trimmedDocumentNumber,
                batchID = resolvedBatchId
            }.ToJson();
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Update", Aliases = ["update supplier invoice", "edit supplier bill", "update vendor bill", "update ap invoice", "update accounts payable invoice", "update creditor invoice"])]
    [McpServerTool, Description("Update a supplier invoice.")]
    public Task<string> UpdateCreditorPurchase(CreditorPurchasePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var result = await ExecuteWithResolvedCreditorPurchaseBatchIdAsync(
                requestDTO.BatchID,
                ct,
                async (batchId, innerCt) =>
                {
                    requestDTO.BatchID = batchId;
                    return await JiwaApiClient.PatchAsync(requestDTO, innerCt);
                });

            return result.ToJson();
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Delete", Aliases = ["delete supplier invoice", "remove supplier bill", "delete vendor bill", "delete ap invoice", "delete accounts payable invoice", "delete creditor invoice"])]
    [McpServerTool, Description("Delete a supplier invoice.")]
    public Task<string> DeleteCreditorPurchase(CreditorPurchaseDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(requestDTO);

            var deletedBatchId = await ExecuteWithResolvedCreditorPurchaseBatchIdAsync(
                requestDTO.BatchID,
                ct,
                async (batchId, innerCt) =>
                {
                    requestDTO.BatchID = batchId;
                    await JiwaApiClient.DeleteAsync(requestDTO, innerCt);
                    return batchId;
                });

            return new { Deleted = true, BatchID = deletedBatchId }.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice custom fields.")]
    public Task<string> GetCreditorPurchaseCustomFields(CreditorPurchaseCustomFieldsGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice document types.")]
    public Task<string> GetCreditorPurchaseDocumentTypes(CreditorPurchaseDocumentTypesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Create a supplier invoice document type.")]
    public Task<string> CreateCreditorPurchaseDocumentType(CreditorPurchaseDocumentTypePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice note types.")]
    public Task<string> GetCreditorPurchaseNoteTypes(CreditorPurchaseNoteTypesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool, Description("Create a supplier invoice note type.")]
    public Task<string> CreateCreditorPurchaseNoteType(CreditorPurchaseNoteTypePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Create", Aliases = ["create supplier invoice", "add supplier bill", "create vendor bill", "create ap invoice", "create accounts payable invoice", "create creditor invoice"])]
    [McpServerTool, Description("Create a supplier invoice.")]
    public Task<string> CreateCreditorPurchase(CreditorPurchasePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson();
        });

    [McpServerTool(Name = "ImportCreditorPurchaseFromLocalCsv"), Description("Import a supplier invoice from a CSV file provided either as an uploaded attachment path or filename, or as a local path under LocalFileSystem:AllowedRoots; do not use this tool for PDF or image invoice attachments.")]
    public Task<string> ImportCreditorPurchaseFromLocalCsv(
        string path,
        string? creditor = null,
        string? description = null,
        DateTime? batchDate = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            List<Dictionary<string, string>> rows;
            string sourcePath;

            if (_fileStorage.TryResolveUploadedFileReference(path, out var uploadedFileId))
            {
                var uploaded = _fileStorage.ReadFileBinary(uploadedFileId);
                if (!uploaded.IsSuccess || uploaded.ContentBytes is null)
                    return new { error = uploaded.Error }.ToJson();

                sourcePath = string.IsNullOrWhiteSpace(uploaded.FileName) ? path : uploaded.FileName;
                if (!string.Equals(Path.GetExtension(sourcePath), ".csv", StringComparison.OrdinalIgnoreCase))
                    return new { error = "Only .csv files are supported for ImportCreditorPurchaseFromLocalCsv" }.ToJson();

                var uploadedRows = TryReadCsvRows(uploaded.ContentBytes, sourcePath);
                if (uploadedRows.Error is not null)
                    return new { error = uploadedRows.Error }.ToJson();

                rows = uploadedRows.Rows;
            }
            else
            {
                if (!LocalFilePathResolver.TryResolveAllowedPath(path, out var fullPath, out var error))
                    return new { error }.ToJson();

                sourcePath = fullPath;
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

                var localRows = TryReadCsvRows(sourcePath);
                if (localRows.Error is not null)
                    return new { error = localRows.Error }.ToJson();

                rows = localRows.Rows;
            }

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
            }

            ResolvedCreditor? defaultCreditor = null;
            if (!string.IsNullOrWhiteSpace(creditorValue))
            {
                defaultCreditor = await TryResolveCreditorForImportAsync(creditorValue!, ct);
                if (defaultCreditor is null)
                    return new { error = $"Unable to resolve supplier/creditor '{creditorValue}'" }.ToJson();
            }

            var resolvedCreditorCache = new Dictionary<string, ResolvedCreditor>(StringComparer.OrdinalIgnoreCase);

            var preparedLines = new List<ImportedCreditorPurchaseLine>();
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];

                if (!TryPrepareImportLine(row, rowIndex + 2, out var line, out var lineError))
                    return new { error = lineError }.ToJson();

                var lineCreditor = defaultCreditor;
                if (lineCreditor is null)
                {
                    var lineCreditorValue = GetFirstNonEmpty(row, "CreditorID", "CreditorAccountNo", "AccountNo", "Supplier", "Creditor", "SupplierCode", "SupplierID");
                    if (string.IsNullOrWhiteSpace(lineCreditorValue))
                    {
                        return new
                        {
                            error = $"Line {rowIndex + 2}: supplier/creditor is required (CreditorID, CreditorAccountNo, AccountNo, Supplier, Creditor, SupplierCode, or SupplierID column)"
                        }.ToJson();
                    }

                    if (!resolvedCreditorCache.TryGetValue(lineCreditorValue, out lineCreditor))
                    {
                        lineCreditor = await TryResolveCreditorForImportAsync(lineCreditorValue, ct);
                        if (lineCreditor is null)
                            return new { error = $"Line {rowIndex + 2}: Unable to resolve supplier/creditor '{lineCreditorValue}'" }.ToJson();

                        resolvedCreditorCache[lineCreditorValue] = lineCreditor;
                    }
                }

                preparedLines.Add(line! with { Creditor = lineCreditor });
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
                    CreditorRecID = line.Creditor!.CreditorID,
                    CreditorAccountNo = line.Creditor.AccountNo,
                    RemitNo = line.RemitNo,
                    SupplierTransAmount = line.SupplierTransAmount,
                    HomeTransAmount = line.HomeTransAmount,
                    ReceiptDate = line.ReceiptDate,
                    DueDate = line.DueDate,
                    CurrencyID = line.CurrencyID
                };

                await JiwaApiClient.PostAsync(lineRequest, ct);
            }

            var creditorsUsed = preparedLines
                .Select(line => line.Creditor!)
                .GroupBy(creditor => creditor.CreditorID, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(creditor => new
                {
                    id = creditor.CreditorID,
                    accountNo = creditor.AccountNo,
                    name = creditor.Name
                })
                .ToList();

            return new
            {
                batchId,
                sourcePath,
                linesImported = preparedLines.Count,
                creditors = creditorsUsed
            }.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice custom field value.")]
    public Task<string> GetCreditorPurchaseCustomFieldValue(CreditorPurchaseCustomFieldValueGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice custom field value.")]
    public Task<string> UpdateCreditorPurchaseCustomFieldValue(CreditorPurchaseCustomFieldValuePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PatchAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice line.")]
    public Task<string> GetCreditorPurchaseLine(CreditorPurchaseLineGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice line.")]
    public Task<string> UpdateCreditorPurchaseLine(CreditorPurchaseLinePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PatchAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Delete a supplier invoice line.")]
    public Task<string> DeleteCreditorPurchaseLine(CreditorPurchaseLineDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var deletedBatchId = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    await JiwaApiClient.DeleteAsync(request, innerCt);
                    return batchId;
                });

            return new
            {
                Deleted = true,
                BatchID = deletedBatchId,
                LineID = requestDTO.LineID
            }.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice document.")]
    public Task<string> GetCreditorPurchaseDocument(CreditorPurchaseDocumentGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice document.")]
    public Task<string> UpdateCreditorPurchaseDocument(CreditorPurchaseDocumentPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PatchAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Delete a supplier invoice document.")]
    public Task<string> DeleteCreditorPurchaseDocument(CreditorPurchaseDocumentDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var deletedBatchId = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    await JiwaApiClient.DeleteAsync(request, innerCt);
                    return batchId;
                });

            return new
            {
                Deleted = true,
                BatchID = deletedBatchId,
                DocumentID = requestDTO.DocumentID
            }.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice note.")]
    public Task<string> GetCreditorPurchaseNote(CreditorPurchaseNoteGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice note.")]
    public Task<string> UpdateCreditorPurchaseNote(CreditorPurchaseNotePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PatchAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Delete a supplier invoice note.")]
    public Task<string> DeleteCreditorPurchaseNote(CreditorPurchaseNoteDELETERequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var deletedBatchId = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    await JiwaApiClient.DeleteAsync(request, innerCt);
                    return batchId;
                });

            return new
            {
                Deleted = true,
                BatchID = deletedBatchId,
                NoteID = requestDTO.NoteID
            }.ToJson();
        });

    [McpServerTool, Description("Get supplier invoice line custom field values.")]
    public Task<string> GetCreditorPurchaseLineCustomFieldValues(CreditorPurchaseLineCustomFieldValuesGETManyRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Get a supplier invoice line custom field value.")]
    public Task<string> GetCreditorPurchaseLineCustomFieldValue(CreditorPurchaseLineCustomFieldValueGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.GetAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [McpServerTool, Description("Update a supplier invoice line custom field value.")]
    public Task<string> UpdateCreditorPurchaseLineCustomFieldValue(CreditorPurchaseLineCustomFieldValuePATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await ExecuteWithResolvedCreditorPurchaseBatchRequestAsync(
                requestDTO,
                requestDTO.BatchID,
                ct,
                async (request, batchId, innerCt) =>
                {
                    request.BatchID = batchId;
                    return await JiwaApiClient.PatchAsync(request, innerCt);
                });

            return result.ToJson();
        });

    [BusinessTool(EntityType = "CreditorPurchase", ActionType = "Search", Aliases = ["supplier invoice history", "vendor bill history", "ap invoice history", "accounts payable invoice history", "creditor invoice history"])]
    [McpServerTool(Name = "ListSupplierPurchaseHistory", ReadOnly = true), Description("List supplier invoice history.")]
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
    [McpServerTool(Name = "ListSupplierPurchases", ReadOnly = true), Description("List or search supplier invoices.")]
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
        string? CurrencyID,
        ResolvedCreditor? Creditor = null);

    private static (List<Dictionary<string, string>> Rows, string? Error) TryReadCsvRows(string fullPath)
    {
        try
        {
            using var parser = new TextFieldParser(fullPath);
            return TryReadCsvRows(parser, fullPath);
        }
        catch (Exception ex)
        {
            return (new List<Dictionary<string, string>>(), $"Failed to read CSV '{fullPath}': {ex.Message}");
        }
    }

    private static (List<Dictionary<string, string>> Rows, string? Error) TryReadCsvRows(byte[] content, string sourcePath)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var parser = new TextFieldParser(stream);
            return TryReadCsvRows(parser, sourcePath);
        }
        catch (Exception ex)
        {
            return (new List<Dictionary<string, string>>(), $"Failed to read CSV '{sourcePath}': {ex.Message}");
        }
    }

    private static (List<Dictionary<string, string>> Rows, string? Error) TryReadCsvRows(TextFieldParser parser, string sourcePath)
    {
        try
        {
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
            return (new List<Dictionary<string, string>>(), $"Invalid CSV format in '{sourcePath}' at line {ex.LineNumber}: {ex.Message}");
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
