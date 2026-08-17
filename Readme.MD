# Jiwa MCP Server

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that exposes [Jiwa Financials](https://jiwa.com.au) business data to AI agents and MCP-compatible clients. Built with .NET 10 and ASP.NET Core, it uses the **streamable HTTP** transport.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Available Tools](#available-tools)
- [Large Result Set Confirmation](#large-result-set-confirmation)
- [Warehouse Context Synchronization](#warehouse-context-synchronization)
- [Configuration](#configuration)
  - [Tool Routing and Diagnostics](#tool-routing-and-diagnostics)
  - [Per-Request API Key](#per-request-api-key)
- [Building and Running](#building-and-running)
- [Testing](#testing)
- [Using with VS Code and GitHub Copilot](#using-with-vs-code-and-github-copilot)
- [Connecting an MCP Client](#connecting-an-mcp-client)
- [Deploying to a Server](#deploying-to-a-server)

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Access to a running Jiwa Financials API instance
- A valid Jiwa API key

---

## Project Structure

```
JiwaMcpServer/
├── JiwaMcpServer.csproj
├── Program.cs
├── Config.cs
├── appsettings.json
├── Tools/
│   ├── JiwaToolBase.cs
│   ├── BusinessDiscoveryTools.cs (Business capability catalog/discovery)
│   ├── FileTools.cs              (File upload, local file access, CSV/XML/JSON/Excel queries)
│   ├── DocumentTools.cs          (Document intelligence + RAG: ingest/search/extract)
│   ├── CustomerTools.cs
│   ├── FormTools.cs              (Prompt-aware form lookup and record-aware open requests)
│   ├── ProductTools.cs
│   ├── SalesOrderTools.cs
│   ├── PurchaseOrderTools.cs
│   ├── CreditorPurchaseTools.cs
│   ├── SupplierTools.cs
│   ├── WarehouseTools.cs
│   └── SchemaTools.cs
├── ToolMetadata/                (Business metadata, terminology, aliases, form mappings)
├── ToolRouting/                 (Retrieval-augmented routing, diagnostics, health endpoints)
├── Services/
│   ├── JiwaApiClient.cs
│   ├── FileStorageService.cs
│   └── DocumentIntelligence/
│       ├── DocumentIntelligenceOptions.cs  (Configuration binding)
│       ├── DocumentModels.cs               (Domain records & DTOs)
│       ├── Interfaces.cs                   (All service contracts)
│       ├── DocumentPipeline.cs             (Core orchestrator + background worker)
│       ├── SemanticChunker.cs              (Overlap-aware token chunker)
│       ├── Extractors.cs                   (PDF / DOCX / XLSX / CSV / Text / Image)
│       ├── OcrServices.cs                  (Azure DI + Tesseract + composite)
│       ├── OpenAiEmbeddingService.cs       (OpenAI / Azure OpenAI + deterministic fallback)
│       ├── InMemoryDocumentRepository.cs   (Default in-process store)
│       ├── InMemoryVectorStore.cs          (Cosine-similarity in-memory vector search)
│       ├── InfrastructureServices.cs       (Malware scanner, audit logger, deterministic embedder)
│       ├── DocumentProcessingQueue.cs      (Unbounded background channel)
│       ├── ServiceCollectionExtensions.cs  (AddDocumentIntelligence())
│       └── DatabaseSchema.sql              (PostgreSQL + pgvector production schema)
└── JiwaMcpServer.Tests/
    ├── JiwaMcpServer.Tests.csproj
    ├── DocumentPipelineTests.cs
    ├── FormToolsTests.cs
    └── ToolRouting*Tests.cs
```

---

## Available Tools

### FileTools
| Tool | Description |
|---|---|
| `upload_file` | Upload and store a base64-encoded file. Returns a unique `fileId` for reference. Supports text files up to 50 MB; allowed MIME types: `text/*`, `application/json`, `application/xml`, `application/sql`, `application/pdf`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `image/png`, `image/jpeg`, `image/gif`, `image/webp`, and `application/octet-stream` for `.sql`/`.pdf`/`.docx`/`.xlsx`/`.png`/`.jpg`/`.jpeg`/`.gif`/`.webp` files. |
| `read_uploaded_file` | Retrieve the content of a previously uploaded file by its `fileId`, or omit the ID to read the most recently uploaded file in this session. |
| `query_csv` | Execute natural language queries on CSV files (e.g., "How many rows?", "Show me the Name column"). If no `fileId` is provided, queries the most recently uploaded CSV file in this session. Supports field extraction, counting, and summarization. |
| `query_excel` | Execute natural language queries on Excel (`.xlsx`) files by `fileId`, uploaded-file reference, or allowed local path. Optional `sheetName` and `range` parameters target specific worksheet data. If omitted, it uses the latest uploaded Excel file in the session and the worksheet used range. |
| `describe_excel` | Describe an Excel workbook by `fileId`, uploaded-file reference, or allowed local path. Returns sheet list, used ranges, selected sheet/range, inferred headers, and data row count to help craft better follow-up questions. |
| `read_excel_rows` | Read Excel rows deterministically for import workflows by `fileId`, uploaded-file reference, or allowed local path. Returns `headers`, `rows`, `totalRows`, `returnedRows`, and `hasMore`, with optional `sheetName`, `range`, `skip`, and `take` paging controls. |
| `query_xml` | Execute natural language queries on XML files (e.g., "How many elements?", "Find the config element"). If no `fileId` is provided, queries the most recently uploaded XML file in this session. Supports element search, structure summarization, and top 10 result limiting. |
| `query_json` | Execute natural language queries on JSON files (e.g., "How many keys?", "Show me the settings property"). If no `fileId` is provided, queries the most recently uploaded JSON file in this session. Supports key search, value extraction, array/object navigation, and top 10 result limiting. |
| `SaveLocalFile` | Save or create a local text file (`.csv`, `.txt`, `.json`, `.xml`) under an allowed root. Use this for export/save-to-disk requests. Output is limited by `LocalFileSystem:MaxWriteBytes`. |
| `list_local_directory` | List files/folders from an allowed local or UNC **directory** path. Access is restricted to configured `LocalFileSystem:AllowedRoots`. |
| `read_local_file` | Read text content from an allowed local or UNC **file** path, or from an uploaded-file reference/filename when the file is already staged in the current session. If the path is a folder, use `list_local_directory` instead. Output is limited by `LocalFileSystem:MaxReadBytes` and may be truncated. |
| `query_local_structured_file` | Query a local structured file directly by allowed path, or by uploaded-file reference/filename, with natural language. Supports `.csv`, `.xml`, `.json`, and `.xlsx`. |

**Latest File Convenience:** You don't need to copy/paste `fileId` values! All file query tools (`query_csv`, `query_excel`, `query_xml`, `query_json`) and `read_uploaded_file` will automatically use the most recently uploaded file in your session if you omit or leave empty the `fileId` parameter. This enables natural workflows like:
1. Upload a CSV file
2. Ask "show me the file I just uploaded" (implicitly uses latest)
3. Ask "how many rows?" (implicitly queries the latest file)

For deterministic import pipelines (for example debtor/customer imports), use `read_excel_rows` instead of natural-language query tools so row data is returned in a predictable paged structure.

**Session Isolation:** Files are stored per client connection, while file IDs and uploaded-file references can be reused across related follow-up tools in the same workflow.

### BusinessDiscoveryTools
| Tool | Description |
|---|---|
| `GetAvailableBusinessActions` | Return the business tool catalog grouped by area (customers, suppliers, supplier invoices, purchase orders, sales orders, inventory, warehouses, documents, and files). Useful when a client needs a concise capability map before choosing a tool. |

---

### DocumentTools — Document Intelligence & RAG

Full ingestion-to-retrieval pipeline for PDFs, DOCX, XLSX, CSV, plain text, and images.
All document operations are scoped to a `tenantId`, which defaults to the session ID when omitted, providing strict isolation.

| Tool | Description |
|---|---|
| `document_ingest` | Ingest a document via base64 or a previously uploaded `sourceFileId`. Runs extraction → OCR fallback → metadata classification → chunking → embeddings → vector indexing. Returns `documentId` and status. |
| `document_search` | Semantic (vector) search across indexed documents. Returns ranked chunks with page citations and a synthesised answer seed. |
| `document_get_summary` | Return title, type, page count, key dates, key entities, invoice metadata, and a condensed text summary. Cached for 15 minutes. |
| `document_extract_invoice` | Extract structured invoice fields: `invoiceNumber`, `vendor`, `invoiceDate`, `totalAmount`, `taxAmount`, `currency`. |
| `document_extract_tables` | Return all extracted tables (headers + rows) with page references. Optional `pageNumber` filter. |
| `document_delete` | Delete document, all chunks, embeddings, and metadata. |
| `document_reindex` | Re-run the full processing pipeline for an existing document (useful after enabling OCR). |
| `document_contracts` | Return the complete JSON request/response schema for all document tools. |

#### Example: Invoice Question-Answering

```
1. upload_file("invoice.pdf", "application/pdf", <base64>)  → fileId="abc123"
2. document_ingest(tenantId="acme", sourceFileId="abc123", processAsync=false)
   → documentId="d-001", status=Indexed, invoice.totalAmount=408.86
3. document_search(tenantId="acme", query="invoice total", topK=3)
   → result[0].text="The invoice total is $408.86 (Page 1)"
   → suggestedAnswer="Top match (Page 1-1): The invoice total is $408.86..."
```

#### Processing Pipeline

```
upload_file
  └─► document_ingest
        ├─► File validation (size, MIME, malware scan)
        ├─► Duplicate hash check
        ├─► Extraction  (PDF text layer / DOCX / XLSX / CSV / plain text)
        ├─► OCR fallback (Azure Document Intelligence → Tesseract, when extraction < 40 chars)
        ├─► Metadata classification  (Invoice / Statement / Contract / Manual / Report)
        ├─► Invoice field extraction  (regex on full text)
        ├─► Semantic chunking  (1000–1500 token target, 150–200 token overlap)
        ├─► Embedding generation  (OpenAI / Azure OpenAI → deterministic fallback)
        └─► Vector indexing  (in-memory cosine similarity, or pgvector in production)
```

#### Configuration (`appsettings.json → DocumentIntelligence`)

```json
"DocumentIntelligence": {
  "MaxDocumentSizeBytes": 83886080,
  "MaxPages": 1200,
  "EnableBackgroundProcessing": true,
  "ChunkTargetTokens": 1200,
  "ChunkMinTokens": 1000,
  "ChunkMaxTokens": 1500,
  "ChunkOverlapTokens": 180,
  "MaxSearchResults": 10,
  "EmbeddingDimensions": 1536,
  "MinimumExtractedCharactersBeforeOcr": 40,
  "Embeddings": {
    "Provider": "disabled",
    "Endpoint": "",
    "ApiKey": "",
    "Model": "text-embedding-3-small"
  },
  "AzureDocumentIntelligence": {
    "Enabled": false,
    "Endpoint": "",
    "ApiKey": "",
    "ModelId": "prebuilt-read"
  },
  "Tesseract": {
    "Enabled": false,
    "ExecutablePath": "tesseract",
    "Language": "eng"
  }
}
```

`Provider` accepts `"openai"`, `"azure-openai"`, or `"disabled"` (deterministic fallback used when disabled — suitable for development and testing).

#### Production Vector Store Options

| Store | Notes |
|---|---|
| **PostgreSQL pgvector** | See `Services/DocumentIntelligence/DatabaseSchema.sql`. Replace `InMemoryVectorStore` with a pgvector implementation via `IVectorStore`. Recommended for self-hosted deployments. |
| **Azure AI Search** | Implement `IVectorStore` with the Azure AI Search SDK. Supports hybrid keyword + vector search. |
| **Chroma / Qdrant** | Implement `IVectorStore` against their HTTP APIs. Both support tenant namespacing. |
| **In-memory** (default) | Cosine similarity over all tenant chunks. Sufficient for development and low-volume production. |

#### Sequence Diagram — End-to-End Invoice Q&A

```
User           Chat UI       MCP Bridge     MCP Server (DocumentTools)     LLM
 │                │               │                    │                    │
 │  upload PDF    │               │                    │                    │
 │──────────────►│  upload_file   │                    │                    │
 │               │───────────────►│  upload_file       │                    │
 │               │               │───────────────────►│ FileStorageService │
 │               │               │◄─── fileId ────────│                    │
 │               │               │                    │                    │
 │  ingest doc   │               │                    │                    │
 │               │───────────────►│  document_ingest   │                    │
 │               │               │───────────────────►│ Extract → OCR      │
 │               │               │                    │ Chunk → Embed      │
 │               │               │◄── documentId ─────│ Index              │
 │               │               │                    │                    │
 │  ask question │               │                    │                    │
 │──────────────►│               │                    │                    │
 │               │  ask LLM      │                    │                    │
 │               │───────────────────────────────────────────────────────►│
 │               │               │                    │                    │
 │               │               │    document_search │                    │
 │               │               │◄───────────────────────────────────────│
 │               │               │───────────────────►│ Vector search      │
 │               │               │◄── ranked chunks ──│ + citations        │
 │               │               │                    │                    │
 │               │  chunks sent to LLM                │                    │
 │               │───────────────────────────────────────────────────────►│
 │               │◄──── "The invoice total is $408.86 (Page 1)" ──────────│
 │◄──────────────│               │                    │                    │
```

#### Future Extensibility

- **Swap extractors**: Implement `IDocumentExtractor` and register it before `CompositeDocumentExtractor` in DI.
- **Production vector store**: Implement `IVectorStore` (pgvector, Azure AI Search, Qdrant, Chroma) and register in `AddDocumentIntelligence()`.
- **Production embeddings**: Set `Embeddings:Provider` to `openai` or `azure-openai` and provide credentials — no code changes required.
- **Production OCR**: Set `AzureDocumentIntelligence:Enabled=true` or `Tesseract:Enabled=true` for scanned PDF support.
- **Audit trail**: Replace `LoggerAuditLogger` with a database-backed `IDocumentAuditLogger`.
- **Multi-format pages**: Add per-page DOCX page-break detection; improve PDF page boundary detection.
- **Reranking**: Add a cross-encoder reranking step in `DocumentPipeline.SearchAsync` before returning results.
- **Incremental indexing**: Only reindex chunks whose source page text changed (diffing against stored page hashes).

---

### CustomerTools
| Tool | Description |
|---|---|
| `SearchCustomers` | Search for customers (debtors/accounts) by field. |
| `GetCustomer` | Get full details for a customer by internal `DebtorID`, or pass a visible customer/account code or name and the tool will auto-resolve it. |
| `GetCustomerTransactions` | Retrieve outstanding customer transactions. |
| `SearchCustomerClassifications` | Retrieve available customer/debtor classifications. |
| `SearchCustomerCategories` | Retrieve available customer/debtor categories. |

### ProductTools
| Tool | Description |
|---|---|
| `SearchProducts` | Search for products (inventory items) by field. |
| `GetProduct` | Get full details for a product. |
| `GetStockOnHand` | Get stock-on-hand quantities for products. |
| `SearchProductClassifications` | Retrieve available product classifications. |
| `SearchProductCategories` | Retrieve available product categories. |
| `GetProductPicture` | Retrieve a product image by `InventoryID` or `PartNo`. |

### FormTools
| Tool | Description |
|---|---|
| `ListForms` | Search forms and plugins by description, class name, prompt terms, and configured aliases. `FormNameMappings` can bias common requests such as `po` or `inventory` to the expected Jiwa class. |
| `OpenForm` | Resolve a Jiwa form/plugin from natural language and optionally a document number, then return the `className` and `drillDownID` needed to open a specific record when resolution succeeds. |

### SalesOrderTools
| Tool | Description |
|---|---|
| `GetSalesOrderDetails` | Get full details for a sales order (sales invoice) by internal `InvoiceID`, or pass a visible sales order/invoice number and the tool will auto-resolve it. |
| `CreateSalesOrder` | Create a sales order. |
| `CreateSO` | Alias for `CreateSalesOrder`. |
| `UpdateSalesOrder` | Modify an existing sales order. |
| `AddItemToSalesOrder` | Add a product line to an existing sales order. |
| `AddItemToSO` | Alias for `AddItemToSalesOrder`. |
| `ResolveSalesOrderId` | Resolve a visible sales order/invoice document number to internal `InvoiceID`. |
| `ListSalesHistory` | Search detailed sales information, including sold part numbers. |
| `ListSalesOrders` | Search sales order headers; use this first to resolve visible sales order/invoice numbers to internal `InvoiceID` before calling `GetSalesOrderDetails`. |

### PurchaseOrderTools
| Tool | Description |
|---|---|
| `GetPurchaseOrderDetails` | Get full details for a purchase order using internal `PurchaseOrderID` (`OrderID`), or pass a visible PO/document number and the tool will auto-resolve it. |
| `GetPurchaseOrder` | Alias for `GetPurchaseOrderDetails`. |
| `GetPO` | Alias for `GetPurchaseOrderDetails`. |
| `CreatePurchaseOrder` | Create a purchase order header. |
| `CreatePO` | Alias for `CreatePurchaseOrder`. |
| `CreatePurchaseOrderWithLines` | Create a purchase order header and add multiple part lines in one call; when configured, missing parts are skipped and the PO is still created. |
| `CreatePOWithLines` | Alias for `CreatePurchaseOrderWithLines`. |
| `UpdatePurchaseOrder` | Modify an existing purchase order. |
| `AddItemToPurchaseOrder` | Add a product line to an existing purchase order. |
| `AddItemToPO` | Alias for `AddItemToPurchaseOrder`. |
| `ResolvePurchaseOrderId` | Resolve a visible purchase order document number to internal `PurchaseOrderID` (`OrderID`). |
| `ListPurchaseHistory` | Search current and historical purchase detail, including purchased part numbers; use `GetPurchaseOrderDetails` to retrieve full details for a specific purchase order. |
| `ListPurchaseOrders` | Search current and historical purchase order header data. Use this first when the user provides a PO/document number (for example, `PO 100191`) so you can resolve `OrderNo` to `OrderID` before calling `GetPurchaseOrderDetails`. |
| `DeletePurchaseOrder` | Delete a purchase order. |

`ListPurchaseHistory` and `ListPurchaseOrders` both support pagination via `skip` and `take`, and may require large-result confirmation before returning the full result set.

### CreditorPurchaseTools
Creditor purchase tools cover creditor purchase headers, lines, documents, notes, custom fields, type records, and batch searches.

| Tool | Description |
|---|---|
| `ActivateCreditorPurchase` | Activate a creditor purchase. |
| `CreateCreditorPurchase` | Create a creditor purchase. For file-driven imports, either map the data yourself after using FileTools, or use `ImportCreditorPurchaseFromLocalCsv` when the source is a CSV. |
| `ImportCreditorPurchaseFromLocalCsv` | Create a new creditor purchase from either an uploaded CSV attachment/reference or a local CSV path under `LocalFileSystem:AllowedRoots` (file path or folder with a single CSV). Creates the purchase and adds one line per row, resolving the creditor from the parameter or CSV data. |
| `ResolveCreditorPurchaseBatchId` | Resolve a visible supplier invoice/batch document number to internal `BatchID`. |
| `GetCreditorPurchase` | Retrieve a creditor purchase by internal `BatchID`, or pass visible supplier purchase batch/document/invoice numbers and the tool will auto-resolve where possible. |
| `UpdateCreditorPurchase` | Update a creditor purchase. |
| `DeleteCreditorPurchase` | Delete a creditor purchase. |
| `GetCreditorPurchaseLines` | Retrieve creditor purchase lines. |
| `AddCreditorPurchaseLine` | Append a line to a creditor purchase. |
| `GetCreditorPurchaseLine` | Retrieve a creditor purchase line. |
| `UpdateCreditorPurchaseLine` | Update a creditor purchase line. |
| `DeleteCreditorPurchaseLine` | Delete a creditor purchase line. |
| `GetCreditorPurchaseDocuments` | Retrieve creditor purchase documents. |
| `AddCreditorPurchaseDocument` | Append a document to a creditor purchase. |
| `GetCreditorPurchaseDocument` | Retrieve a creditor purchase document. |
| `UpdateCreditorPurchaseDocument` | Update a creditor purchase document. |
| `DeleteCreditorPurchaseDocument` | Delete a creditor purchase document. |
| `GetCreditorPurchaseNotes` | Retrieve creditor purchase notes. |
| `AddCreditorPurchaseNote` | Append a note to a creditor purchase. |
| `GetCreditorPurchaseNote` | Retrieve a creditor purchase note. |
| `UpdateCreditorPurchaseNote` | Update a creditor purchase note. |
| `DeleteCreditorPurchaseNote` | Delete a creditor purchase note. |
| `GetCreditorPurchaseCustomFields` | Retrieve creditor purchase custom field definitions. |
| `GetCreditorPurchaseCustomFieldValue` | Retrieve a creditor purchase custom field value. |
| `UpdateCreditorPurchaseCustomFieldValue` | Update a creditor purchase custom field value. |
| `GetCreditorPurchaseLineCustomFields` | Retrieve creditor purchase line custom field definitions. |
| `GetCreditorPurchaseLineCustomFieldValues` | Retrieve creditor purchase line custom field values. |
| `GetCreditorPurchaseLineCustomFieldValue` | Retrieve a creditor purchase line custom field value. |
| `UpdateCreditorPurchaseLineCustomFieldValue` | Update a creditor purchase line custom field value. |
| `GetCreditorPurchaseDocumentTypes` | Retrieve creditor purchase document types. |
| `CreateCreditorPurchaseDocumentType` | Create a creditor purchase document type. |
| `GetCreditorPurchaseDocumentType` | Retrieve a creditor purchase document type. |
| `UpdateCreditorPurchaseDocumentType` | Update a creditor purchase document type. |
| `DeleteCreditorPurchaseDocumentType` | Delete a creditor purchase document type. |
| `GetCreditorPurchaseNoteTypes` | Retrieve creditor purchase note types. |
| `CreateCreditorPurchaseNoteType` | Create a creditor purchase note type. |
| `GetCreditorPurchaseNoteType` | Retrieve a creditor purchase note type. |
| `UpdateCreditorPurchaseNoteType` | Update a creditor purchase note type. |
| `DeleteCreditorPurchaseNoteType` | Delete a creditor purchase note type. |
| `SearchCreditorPurchaseBatchInformation` | Search creditor purchase batch information, including invoice numbers. |
| `SearchCreditorPurchaseBatches` | Search creditor purchase batches; use this first to resolve visible supplier purchase batch/document/invoice numbers to internal `BatchID` before calling `GetCreditorPurchase`. |

### SupplierTools
| Tool | Description |
|---|---|
| `SearchSuppliers` | Search suppliers (creditors) by field. |
| `GetSupplier` | Get full details for a supplier by internal `CreditorID`, or pass a visible supplier/creditor code or name and the tool will auto-resolve it. |
| `CreateSupplier` | Create a supplier. |
| `ModifySupplier` | Modify a supplier. |
| `DeleteSupplier` | Delete a supplier. |
| `SearchCreditorClassifications` | Retrieve available supplier/creditor classifications. |

### WarehouseTools
| Tool | Description |
|---|---|
| `SearchWarehouses` | Search logical warehouses by field. |
| `GetCurrentLogicalWarehouse` | Get the current logical warehouse for the session. |
| `ChangeCurrentLogicalWarehouse` | Change the current logical warehouse for the session. |

### SchemaTools
| Tool | Description |
|---|---|
| `GetDtoSchema` | Return the DTO schema for any Jiwa DTO type by name. |

---

## Large Result Set Confirmation

Most search/list tools support safe handling of large datasets.

When a query exceeds the warning threshold, call flow is:

1. Call tool with `confirmLargeResultSet=false` (default).
2. If result is large, the tool returns a warning and a short-lived `confirmationToken`.
3. Call the same tool again with `confirmLargeResultSet=true` and that token.

This avoids unintentionally returning very large payloads in a single tool call.

This is especially relevant for `SearchPurchaseInformation` and `SearchPurchaseOrders`, which can span current and historical purchasing data.

---

## Warehouse Context Synchronization

The Jiwa system maintains warehouse context at **three** distinct levels:

1. **API Server Context** (Jiwa REST API): Warehouse state for API requests
2. **MCP Server Context** (JiwaMcpServer): Stateless per-request API calls
3. **Jiwa Client Context** (AI Chat): `Manager.CurrentLogicalWarehouse` in the Jiwa client application

### The Synchronization Challenge

When you use `ChangeCurrentLogicalWarehouse`:

| Step | What Happens | State |
|------|--------------|-------|
| 1. Call tool | MCP Server updates Jiwa API | API warehouse ✓ |
| 2. Tool returns | Response includes new warehouse ID | Response data ready |
| 3. **REQUIRED ACTION** | **AI Chat must update** `Manager.CurrentLogicalWarehouse` | AI Chat warehouse needs update |
| 4. Call GetCurrentLogicalWarehouse | Verify and get full details | Confirmation |

**The Critical Issue:** The MCP Server cannot directly update `Manager.CurrentLogicalWarehouse` because it's in a different process (the Jiwa client application). The AI Chat must be instructed to do this.

### How ChangeCurrentLogicalWarehouse Response Works

The tool returns a structured response with the new warehouse context:

```json
{
  "success": true,
  "newWarehouseId": "WAREHOUSE_ID_12345",
  "newWarehouseDetails": { /* warehouse data */ }
}
```

### Required Workflow for AI Chat

1. **Call** `ChangeCurrentLogicalWarehouse(newWarehouseId)` → **API updated** ✓
2. **Receive response** with `newWarehouseId`
3. **Update Jiwa Client** `Manager.CurrentLogicalWarehouse = newWarehouseId` ← **Critical Step**
4. **Call** `GetCurrentLogicalWarehouse` → **Verify and get details** ✓
5. **Inform user** "Warehouse changed to [warehouse name]"

### Why Both Updates Are Necessary

- **API Context Update**: Ensures future API calls use the correct warehouse
- **Manager Update**: Ensures the Jiwa AI Chat client displays and uses the correct warehouse

If either is missed, the contexts diverge and operations use the wrong warehouse.

---

## Configuration

Configuration is stored in `appsettings.json` in the application directory.

```json
{
  "JiwaAPIURL": "https://your-jiwa-instance.example.com",
  "JiwaAPIKey": "",
  "PageSize": 100,
  "Plugins": {
    "Enabled": true,
    "Folder": "Plugins",
    "Assemblies": []
  },
  "LocalFileSystem": {
    "AllowedRoots": [
      "C:\\Allowed\\Folder",
      "\\\\fileserver\\share\\finance"
    ],
    "MaxReadBytes": 262144,
    "MaxWriteBytes": 262144
  },
  "DocumentIntelligence": {
    "MaxDocumentSizeBytes": 83886080,
    "MaxPages": 1200,
    "EnableBackgroundProcessing": true,
    "ChunkTargetTokens": 1200,
    "ChunkMinTokens": 1000,
    "ChunkMaxTokens": 1500,
    "ChunkOverlapTokens": 180,
    "MaxSearchResults": 10,
    "EmbeddingDimensions": 1536,
    "MinimumExtractedCharactersBeforeOcr": 40,
    "Embeddings": {
      "Provider": "disabled",
      "Endpoint": "",
      "ApiKey": "",
      "Model": "text-embedding-3-small"
    },
    "AzureDocumentIntelligence": {
      "Enabled": false,
      "Endpoint": "",
      "ApiKey": "",
      "ModelId": "prebuilt-read"
    },
    "Tesseract": {
      "Enabled": false,
      "ExecutablePath": "tesseract",
      "Language": "eng"
    }
  },
  "FormNameMappings": {
    "po": "JiwaFinancials.Jiwa.JiwaPurchaseOrdersUI.PurchaseOrders",
    "purchase order": "JiwaFinancials.Jiwa.JiwaPurchaseOrdersUI.PurchaseOrders",
    "inventory": "JiwaFinancials.Jiwa.JiwaInventoryUI.InventoryMaintenanceForm"
  },
  "ToolRouting": {
    "EnableRouting": true,
    "EnableDomainClassification": true,
    "RetrievalTopK": 20,
    "MaxSelectedTools": 5,
    "SimilarityThreshold": 0.2,
    "EmbeddingDimensions": 768
  },
  "ToolRoutingDiagnostics": {
    "EnableDetailedLogging": true,
    "EnablePromptLogging": true,
    "EnableMetrics": true,
    "EnableDiagnosticEndpoint": true,
    "RestrictDiagnosticEndpointToLocalhost": true,
    "DiagnosticApiKeyHeaderName": "X-Tool-Routing-Diagnostics-Key",
    "DiagnosticApiKey": ""
  }
}
```

| Setting | Description |
|---|---|
| `JiwaAPIURL` | The base URL of your Jiwa Financials REST API. |
| `JiwaAPIKey` | *(Optional)* A server-side fallback Jiwa API key. When set, it is used for any request that does not supply its own key via the `X-Jiwa-API-Key` header. Can be left blank if all clients supply their own key. |
| `PageSize` | *(Optional)* Default page size used by paginated search/list tools. Default is `100`. |
| `Plugins` | *(Optional)* Plugin loading settings. `Folder` is scanned for `*.dll` assemblies at startup, and `Assemblies` can include explicit relative or absolute DLL paths. |
| `LocalFileSystem:AllowedRoots` | *(Optional)* Absolute local and/or UNC root paths that local path tools are allowed to access. If empty, local file tools are disabled. |
| `LocalFileSystem:MaxReadBytes` | *(Optional)* Maximum bytes returned by one `read_local_file` call. Default is `262144` (256 KB). |
| `LocalFileSystem:MaxWriteBytes` | *(Optional)* Maximum UTF-8 payload size accepted by `SaveLocalFile`. Default is `262144` (256 KB). |
| `DocumentIntelligence` | *(Optional)* Document ingestion, OCR, chunking, embeddings, and RAG settings. `EnableBackgroundProcessing` queues work, `Chunk*` controls chunking, `MinimumExtractedCharactersBeforeOcr` controls the OCR fallback threshold, and `Embeddings`/`AzureDocumentIntelligence`/`Tesseract` configure the retrieval and OCR backends. |
| `FormNameMappings` | *(Optional)* Friendly prompt aliases for `ListForms` and `OpenForm`. Map short business terms such as `po` or `inventory` to Jiwa form class names. |
| `ToolRouting` | *(Optional)* Retrieval-augmented MCP tool selection settings. Controls whether routing is enabled, whether domain classification runs, candidate pool sizes, and selection thresholds. |
| `ToolRoutingDiagnostics` | *(Optional)* Structured logging, metrics, and diagnostics endpoint settings for tool routing observability. |

> **Note:** `JiwaAPIURL` is required. `JiwaAPIKey`, `PageSize`, `Plugins`, `LocalFileSystem`, `DocumentIntelligence`, `FormNameMappings`, `ToolRouting`, and `ToolRoutingDiagnostics` are optional — see [Per-Request API Key](#per-request-api-key) below.

### Tool Routing and Diagnostics

When an MCP client sends `X-User-Prompt` or `X-Tool-Query`, the server can pre-filter the MCP tool list before it reaches the cloud model. Each routed request also returns `X-Tool-Routing-Id`, which you can use to correlate logs and diagnostics.

| Endpoint / Header | Purpose |
|---|---|
| `GET /health/tool-routing` | Health check for the routing pipeline and its embedding/vector services. |
| `GET /diagnostics/tool-routing/latest` | Return the latest captured routing decision. Controlled by `ToolRoutingDiagnostics` settings and, by default, restricted to localhost. |
| `X-Tool-Routing-Id` | Response header that identifies the routing decision for a request. |
| `X-Tool-Query` / `X-User-Prompt` | Request headers a client can send to help the server select the most relevant tool subset. |

### Plugin Tools

Customers can extend the server by dropping plugin DLLs into the configured plugin folder (default: `Plugins` under the app directory). On startup, JiwaMcpServer loads those assemblies and registers any MCP tools found in them.

To expose tools from a plugin assembly:

1. Reference the same MCP SDK packages used by JiwaMcpServer.
2. Add tool classes marked with `[McpServerToolType]`.
3. Add methods marked with `[McpServerTool]`.
4. Copy the built plugin DLL (and its dependencies) into the `Plugins` folder, or list explicit DLL paths in `Plugins:Assemblies`.

A project named JiwaMcpServerSamplePlugin is now included that illustrates how to create a plugin based tool.

### Per-Request API Key

In addition to (or instead of) a server-side `JiwaAPIKey` in `appsettings.json`, clients can supply a Jiwa API key on a per-request basis using the `X-Jiwa-API-Key` HTTP header:

```
X-Jiwa-API-Key: <your-jiwa-api-key>
```

This allows each MCP client (or each user) to authenticate to Jiwa with their own API key, without sharing a single server-level credential. The priority is:

1. **`X-Jiwa-API-Key` header** — used if present and non-empty.
2. **`JiwaAPIKey` in `appsettings.json`** — used as a fallback if no header is supplied.

If neither is set, calls to the Jiwa API will be made without a bearer token and will fail unless the Jiwa endpoint allows anonymous access.

### Changing the Port

By default the server listens on `http://localhost:5000`. To change this, set the `ASPNETCORE_URLS` environment variable or add a `urls` entry to `appsettings.json`:

```json
{
  "JiwaAPIURL": "https://your-jiwa-instance.example.com",
  "JiwaAPIKey": "your-api-key-here",
  "PageSize": 100,
  "urls": "http://0.0.0.0:8080"
}
```

---

## Building and Running

### Run from source

```powershell
# Clone the repository
git clone https://github.com/JiwaFinancials/JiwaMcpServer.git
cd JiwaMcpServer

# Edit appsettings.json with your Jiwa API URL and key
notepad appsettings.json

# Run
dotnet run
```

The server will start and listen on `http://localhost:5000` by default. The MCP endpoint is available at:

```
http://localhost:5000/mcp
```

### Publish a self-contained executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

Replace `win-x64` with your target runtime (e.g. `linux-x64`, `osx-x64`). Copy `appsettings.json` alongside the executable and edit it with your settings before running.

---

## Testing

A dedicated test project is included at `JiwaMcpServer.Tests/` with coverage for tool behavior, pagination, and error handling.

Run all tests:

```powershell
dotnet test JiwaMcpServer.Tests/JiwaMcpServer.Tests.csproj
```

---

## Using with VS Code and GitHub Copilot

This section walks through configuring VS Code and GitHub Copilot to use the Jiwa MCP Server as a tool source in Copilot Chat (Agent mode).

### Prerequisites

- [Visual Studio Code](https://code.visualstudio.com/) with the [GitHub Copilot](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot) extension (v1.99 or later)
- A GitHub Copilot subscription (Individual, Business, or Enterprise)
- The Jiwa MCP Server running locally or on a reachable host (see [Building and Running](#building-and-running))
### Step 1 — Start the MCP Server

Start the server before configuring VS Code so you can verify connectivity later.

```powershell
cd C:\path\to\JiwaMcpServer
dotnet run
```

Confirm it is listening:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 2 — Register the Server in VS Code

VS Code looks for MCP server definitions in two places. Choose whichever suits your workflow:

#### Option A — Workspace config (per-project, do not commit if it contains tokens)

Create or edit `.vscode/mcp.json` in your workspace root:

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
      }
    }
  }
}
```

> ⚠️ This file will contain your Jiwa API key. Add it to `.gitignore` to avoid accidentally committing it:
> ```
> .vscode/mcp.json
> ```

#### Option B — User config (applies to all your VS Code workspaces)

Open the VS Code command palette (`Ctrl+Shift+P`) and run:

```
MCP: Open User Configuration
```

This opens your global `mcp.json`. Add the same entry:

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
      }
    }
  }
}
```

> **Tip:** Replace `<your-jiwa-api-key>` with your personal Jiwa API key.

### Step 3 — Enable the Server in Copilot Chat

1. Open the Copilot Chat panel (`Ctrl+Alt+I`).
2. Switch to **Agent mode** by clicking the mode selector at the top of the chat input and choosing **Agent**.
3. Click the **Tools** button (🔧) at the bottom of the chat input to open the tool picker.
4. Locate **jiwa** in the list and ensure it is checked/enabled.

You should see all available Jiwa tools listed (SearchCustomers, GetProduct, CreateSalesOrder, etc.).

### Step 4 — Verify the Connection

In Copilot Chat (Agent mode) type a test prompt such as:

```
List all available Jiwa MCP tools
```

or try a real query:

```
Search for customers with the name "Smith" using the Jiwa tools
```

Copilot will call the appropriate tool and return results from your Jiwa system.

### Step 5 — (Optional) Auto-start the Server

To avoid manually starting the server each time, you can configure a VS Code task that launches it automatically.

Create or edit `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Start Jiwa MCP Server",
      "type": "shell",
      "command": "dotnet run --project ${workspaceFolder}/JiwaMcpServer.csproj",
      "isBackground": true,
      "problemMatcher": {
        "pattern": {
          "regexp": "^$"
        },
        "background": {
          "activeOnStart": true,
          "beginsPattern": "Building\\.\\.\\.",
          "endsPattern": "Now listening on:"
        }
      },
      "presentation": {
        "reveal": "silent",
        "panel": "dedicated"
      }
    }
  ]
}
```

Then reference it as a pre-launch dependency in `.vscode/launch.json` or trigger it manually via **Terminal → Run Task → Start Jiwa MCP Server**.

### Troubleshooting

| Problem | Solution |
|---|---|
| Tools not appearing in Copilot Chat | Ensure the server is running and accessible at the configured URL. Open a browser to `http://localhost:5000/mcp` — you should receive an MCP response, not a connection error. |
| `ERR_CONNECTION_REFUSED` | The server is not running or is listening on a different port. Check the console output and verify `appsettings.json`. |
| Jiwa API returns 401 / Unauthorized |
| Copilot asks for confirmation before every tool call | This is expected behaviour in Agent mode. You can approve individual calls or select **Always Allow** for trusted tools. |
| Server returns 500 errors | Check that `JiwaAPIURL` in `appsettings.json` is correct, that a Jiwa API key is being supplied (see above), and that your Jiwa instance is reachable from the machine running the MCP server. |

---

## Connecting an MCP Client

The server uses the **streamable HTTP** transport. The MCP endpoint is:

```
http://<host>:<port>/mcp
```

Requests may optionally include the `X-Jiwa-API-Key` header with a Jiwa API key:

```
X-Jiwa-API-Key: <your-jiwa-api-key>
```

The `X-Jiwa-API-Key` header is required if no server-side fallback key is configured in `appsettings.json`. See [Per-Request API Key](#per-request-api-key) for details.

### GitHub Copilot (VS Code)

See the dedicated [Using with VS Code and GitHub Copilot](#using-with-vs-code-and-github-copilot) section above for full step-by-step instructions.

For a quick reference, add the following to `.vscode/mcp.json` in your workspace (or your user-level `mcp.json`):

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
      }
    }
  }
}
```

### GitHub Copilot (Visual Studio)

Add the following to your `mcp.json` (typically at `%USERPROFILE%\.vs\mcp.json` or the solution's `.vs\mcp.json`):

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
      }
    }
  }
}
```

### MCP Inspector (for testing)

You can test the server with the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```powershell
npx -y @modelcontextprotocol/inspector http://localhost:5000/mcp
```

---

## Deploying to a Server

### Windows (as a Windows Service)

1. Publish the application:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true -o C:\Services\JiwaMcpServer
   ```

2. Copy and edit `appsettings.json` in the publish folder with your settings.

3. Register as a Windows Service:
   ```powershell
   sc.exe create JiwaMcpServer binPath="C:\Services\JiwaMcpServer\JiwaMcpServer.exe" start=auto
   sc.exe start JiwaMcpServer
   ```

### Linux (as a systemd service)

1. Publish the application:
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained true -o /opt/jiwamcpserver
   ```

2. Edit `/opt/jiwamcpserver/appsettings.json` with your settings.

3. Create a systemd unit file at `/etc/systemd/system/jiwamcpserver.service`:
   ```ini
   [Unit]
   Description=Jiwa MCP Server
   After=network.target

   [Service]
   WorkingDirectory=/opt/jiwamcpserver
   ExecStart=/opt/jiwamcpserver/JiwaMcpServer
   Restart=always
   RestartSec=10
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

   [Install]
   WantedBy=multi-user.target
   ```

4. Enable and start the service:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable jiwamcpserver
   sudo systemctl start jiwamcpserver
   ```

### Behind a Reverse Proxy (nginx / IIS)

When hosting behind a reverse proxy, configure it to forward requests to the MCP endpoint. Example nginx snippet:

```nginx
location /mcp {
    proxy_pass         http://localhost:5000/mcp;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection keep-alive;
    proxy_set_header   Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_buffering    off;
}
```

> **Tip:** Disable proxy response buffering (`proxy_buffering off`) to ensure streaming responses are forwarded immediately to the client.
