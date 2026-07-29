using JiwaMcpServer.Services;
using JiwaMcpServer.Services.DocumentIntelligence;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public sealed class DocumentTools(
    IDocumentPipeline pipeline,
    FileStorageService fileStorage,
    IHttpContextAccessor? httpContextAccessor = null) : JiwaToolBase
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();

    /// <summary>Returns a tenant ID scoped to this session, used when the caller does not supply one.</summary>
    private string ResolveTenantId(string tenantId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
            return tenantId.Trim();

        // Fall back to the HTTP connection ID set by middleware, or the AsyncLocal session.
        var contextId = _httpContextAccessor.HttpContext?.Connection?.Id;
        return contextId ?? FileStorageService.GetCurrentSessionId();
    }

    [McpServerTool(ReadOnly = false), Description(
        "document.ingest — ALWAYS call this as the FIRST STEP whenever a PDF, Word document, spreadsheet, or image " +
        "has been uploaded and the user wants to analyse, extract, or query its content. " +
        "Supply sourceFileId with the fileId returned by upload_file. " +
        "If upload_file used clientSessionId, pass the same clientSessionId here to guarantee lookup in that session. " +
        "The pipeline extracts text (including from FlateDecode-compressed PDFs), runs OCR if the text layer is empty, " +
        "detects document type (Invoice/Statement/Contract/Report), extracts invoice fields " +
        "(invoiceNumber/vendor/totalAmount/taxAmount/currency), chunks the text, generates embeddings, and indexes everything. " +
        "Returns documentId which is required for document_search, document_extract_invoice, document_extract_tables, and document_get_summary. " +
        "tenantId is optional — omit it and the session ID will be used automatically.")]
    public Task<string> DocumentIngest(
        string sourceFileId = "",
        string clientSessionId = "",
        string tenantId = "",
        string fileName = "",
        string mimeType = "",
        string contentBase64 = "",
        string title = "",
        bool processAsync = false,
        bool forceReindex = false,
        string externalDocumentId = "")
    {
        return InvokeToolAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(clientSessionId))
                FileStorageService.SetSessionId(clientSessionId);
            else
                ApplyContextSession();
            var resolvedTenant = ResolveTenantId(tenantId);

            if (!string.IsNullOrWhiteSpace(sourceFileId))
            {
                var read = fileStorage.ReadFileBinary(sourceFileId);
                if (!read.IsSuccess || read.ContentBytes is null)
                {
                    // Cross-session continuity fallback: if source file was uploaded on a different
                    // MCP connection, try a global fileId lookup by capability token.
                    read = fileStorage.ReadFileBinaryAcrossSessions(sourceFileId);
                    if (!read.IsSuccess || read.ContentBytes is null)
                    {
                        return new
                        {
                            error = read.Error ?? "Unable to read source file for ingestion.",
                            hint = "If upload_file used clientSessionId, pass that same value to document_ingest.clientSessionId."
                        }.ToJson();
                    }
                }

                fileName = string.IsNullOrWhiteSpace(fileName) ? read.FileName ?? string.Empty : fileName;
                mimeType = string.IsNullOrWhiteSpace(mimeType) ? read.MimeType ?? string.Empty : mimeType;
                contentBase64 = Convert.ToBase64String(read.ContentBytes);
            }

            var response = await pipeline.IngestAsync(new DocumentIngestRequest(
                TenantId: resolvedTenant,
                FileName: fileName,
                MimeType: mimeType,
                ContentBase64: contentBase64,
                Title: string.IsNullOrWhiteSpace(title) ? null : title,
                ProcessAsync: processAsync,
                ExternalDocumentId: string.IsNullOrWhiteSpace(externalDocumentId) ? null : externalDocumentId,
                ForceReindex: forceReindex,
                SourceFileId: string.IsNullOrWhiteSpace(sourceFileId) ? null : sourceFileId),
                CancellationToken.None);

            return response.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description(
        "document.search — Semantic search over an ingested document. " +
        "Requires documentId from document_ingest. " +
        "Returns ranked text chunks with page numbers and a suggested answer. " +
        "tenantId is optional; omit to use the session default.")]
    public Task<string> DocumentSearch(
        string documentId,
        string query,
        string tenantId = "",
        int topK = 5,
        bool includeChunkText = true)
    {
        return InvokeToolAsync(async () =>
        {
            ApplyContextSession();
            var resolvedTenant = ResolveTenantId(tenantId);

            var response = await pipeline.SearchAsync(new DocumentSearchRequest(
                TenantId: resolvedTenant,
                Query: query,
                DocumentIds: string.IsNullOrWhiteSpace(documentId) ? null : [documentId],
                TopK: topK,
                IncludeChunkText: includeChunkText), CancellationToken.None);

            return response.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description(
        "document.getSummary — Return title, document type, page count, key dates, key entities, invoice metadata, and a condensed text summary. " +
        "Requires documentId from document_ingest. tenantId is optional.")]
    public Task<string> DocumentGetSummary(string documentId, string tenantId = "")
    {
        return InvokeToolAsync(async () =>
        {
            ApplyContextSession();
            var summary = await pipeline.GetSummaryAsync(ResolveTenantId(tenantId), documentId, CancellationToken.None);
            return summary is null ? new { error = "Document not found" }.ToJson() : summary.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description(
        "document.extractInvoice — Extract structured invoice fields: invoiceNumber, vendor, invoiceDate, totalAmount, taxAmount, currency. " +
        "Requires documentId from document_ingest. tenantId is optional.")]
    public Task<string> DocumentExtractInvoice(string documentId, string tenantId = "")
    {
        return InvokeToolAsync(async () =>
        {
            ApplyContextSession();
            var invoice = await pipeline.ExtractInvoiceAsync(ResolveTenantId(tenantId), documentId, CancellationToken.None);
            return invoice is null ? new { error = "Document not found" }.ToJson() : invoice.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description(
        "document.extractTables — Extract all structured tables (with headers, rows, and page references) from an ingested document. " +
        "Requires documentId from document_ingest. tenantId is optional.")]
    public Task<string> DocumentExtractTables(string documentId, string tenantId = "", int pageNumber = 0)
    {
        return InvokeToolAsync(async () =>
        {
            ApplyContextSession();
            var tables = await pipeline.ExtractTablesAsync(ResolveTenantId(tenantId), documentId, pageNumber <= 0 ? null : pageNumber, CancellationToken.None);
            return tables is null ? new { error = "Document not found" }.ToJson() : tables.ToJson();
        });
    }

    [McpServerTool(ReadOnly = false), Description(
        "document.delete — Delete an ingested document and all its chunks, embeddings, and metadata. " +
        "Requires documentId from document_ingest. tenantId is optional.")]
    public Task<string> DocumentDelete(string documentId, string tenantId = "")
    {
        return InvokeToolAsync(async () =>
        {
            ApplyContextSession();
            var response = await pipeline.DeleteAsync(ResolveTenantId(tenantId), documentId, CancellationToken.None);
            return response.ToJson();
        });
    }

    [McpServerTool(ReadOnly = false), Description(
        "document.reindex — Re-run the full extraction/OCR/chunking/embeddings/indexing pipeline for an existing document. " +
        "Use forceOcr=true to run OCR even if a text layer was already found. " +
        "Requires documentId from document_ingest. tenantId is optional.")]
    public Task<string> DocumentReindex(string documentId, string tenantId = "", bool forceOcr = false)
    {
        return InvokeToolAsync(async () =>
        {
            ApplyContextSession();
            var response = await pipeline.ReindexAsync(ResolveTenantId(tenantId), documentId, forceOcr, CancellationToken.None);
            return response.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description("Return request/response schema contracts for document.ingest, document.search, document.getSummary, document.extractInvoice, and document.extractTables")]
    public Task<string> DocumentContracts()
    {
        return InvokeToolAsync(async () =>
        {
            var schemas = new DocumentToolSchemaResponse(
            [
                new ToolSchema("document.ingest",
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["sourceFileId"] = new("string", "Preferred: fileId returned by upload_file", false),
                        ["tenantId"] = new("string", "Optional tenant identifier; defaults to session ID", false),
                        ["fileName"] = new("string", "Original file name (required if not using sourceFileId)", false),
                        ["mimeType"] = new("string", "MIME content type (required if not using sourceFileId)", false),
                        ["contentBase64"] = new("string", "Base64 file bytes (required if not using sourceFileId)", false),
                        ["title"] = new("string", "Optional display title", false),
                        ["processAsync"] = new("boolean", "Queue in background when true", false),
                        ["forceReindex"] = new("boolean", "Ignore duplicate/hash cache", false),
                        ["externalDocumentId"] = new("string", "Optional caller-provided document id", false)
                    },
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Server document ID", true),
                        ["tenantId"] = new("string", "Tenant identifier", true),
                        ["status"] = new("string", "Processing status", true),
                        ["message"] = new("string", "Status message", true),
                        ["pageCount"] = new("integer", "Page count after processing", false),
                        ["documentType"] = new("string", "Detected type", false),
                        ["invoice"] = new("object", "Extracted invoice fields", false),
                        ["updatedAt"] = new("string", "UTC timestamp", true)
                    }),
                new ToolSchema("document.search",
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier returned by document_ingest", true),
                        ["query"] = new("string", "Natural-language query", true),
                        ["tenantId"] = new("string", "Optional tenant identifier; defaults to session ID", false),
                        ["topK"] = new("integer", "Maximum result chunks", false),
                        ["includeChunkText"] = new("boolean", "Return chunk text bodies", false)
                    },
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["query"] = new("string", "Echoed query", true),
                        ["results"] = new("array", "Ranked chunks with page citations and score", true),
                        ["suggestedAnswer"] = new("string", "RAG-ready synthesis seed", true)
                    }),
                new ToolSchema("document.getSummary",
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier", true),
                        ["tenantId"] = new("string", "Optional tenant identifier; defaults to session ID", false)
                    },
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier", true),
                        ["title"] = new("string", "Document title", true),
                        ["documentType"] = new("string", "Detected type", true),
                        ["pageCount"] = new("integer", "Page count", true),
                        ["status"] = new("string", "Processing status", true),
                        ["invoice"] = new("object", "Invoice metadata", false),
                        ["keyDates"] = new("array", "Detected dates", true),
                        ["keyEntities"] = new("array", "Detected entities", true),
                        ["summaryText"] = new("string", "Condensed text summary", true),
                        ["metadata"] = new("object", "Flattened metadata map", true)
                    }),
                new ToolSchema("document.extractInvoice",
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier", true),
                        ["tenantId"] = new("string", "Optional tenant identifier; defaults to session ID", false)
                    },
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier", true),
                        ["isInvoice"] = new("boolean", "Whether invoice signals were found", true),
                        ["invoice"] = new("object", "invoiceNumber/vendor/invoiceDate/totalAmount/taxAmount/currency", false),
                        ["evidence"] = new("array", "Evidence snippets", true)
                    }),
                new ToolSchema("document.extractTables",
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier", true),
                        ["tenantId"] = new("string", "Optional tenant identifier; defaults to session ID", false),
                        ["pageNumber"] = new("integer", "Optional filter by page", false)
                    },
                    new Dictionary<string, ToolSchemaProperty>
                    {
                        ["documentId"] = new("string", "Document identifier", true),
                        ["tableCount"] = new("integer", "Total tables returned", true),
                        ["tables"] = new("array", "Table rows/headers with page references", true)
                    })
            ]);

            return schemas.ToJson();
        });
    }

    private void ApplyContextSession()
    {
        var contextId = _httpContextAccessor.HttpContext?.Connection?.Id;
        if (!string.IsNullOrWhiteSpace(contextId))
        {
            FileStorageService.SetSessionId(contextId);
        }
    }
}
