namespace JiwaMcpServer.ToolDescriptions;

public static class DocumentToolDescriptions
{
    public const string ExtractDocumentData = """
Extract text, metadata, and structured content from an uploaded document using FileId.

IMPORTANT:
• Authoritative source for uploaded document content.
• Use whenever the answer depends on an uploaded file.
• Supports CSV, XLSX, JSON, XML, PDF, DOCX.

Use for:
• Reading uploaded documents
• Extracting text, tables, records, worksheets, or document content
• Inspecting uploaded files
• Preparing data for analysis, conversion, or export

Returns extracted text, metadata, and structured content.
""";

    public const string ExtractLocalDocumentData = """
Extract text, metadata, and structured content from a local file path.

IMPORTANT:
• Use when the user provides a local file path instead of a FileId.
• Supports CSV, XLSX, JSON, XML, PDF, DOCX.
• File must be within LocalFileSystem:AllowedRoots.
• File size limits apply.

Use for:
• Reading local documents
• Analysing local files
• Import workflows based on local files

Returns extracted text, metadata, structured content, and the resolved file path.
""";

    public const string UploadLocalDocument = """
Upload a local file and return a FileId.

IMPORTANT:
• Use when another tool requires a FileId but the user provides a local file path.
• Supports CSV, XLSX, JSON, XML, PDF, DOCX.
• File must be within LocalFileSystem:AllowedRoots.
• File size limits apply.

Returns FileId, file metadata, and the resolved file path.
""";

    public const string QueryUploadedDocument = """
Answer questions about an uploaded document using FileId.

IMPORTANT:
• Use when the user wants answers, summaries, analysis, filtering, interpretation, or insights.
• Use instead of ExtractDocumentData when the user wants conclusions rather than raw content.
• Supports CSV, XLSX, JSON, XML, PDF, DOCX.

Use for:
• Summaries
• Search and filtering
• Finding duplicates or missing values
• Extracting entities or records
• Follow-up questions about uploaded files
""";

    public const string ConvertDocument = """
Convert an uploaded document into another format.

IMPORTANT:
• Requires a FileId.
• Use only when the user explicitly requests file conversion.
• Supports CSV, XLSX, JSON, XML, PDF, DOCX.

Returns a downloadable converted file.
""";

    public const string CreateDataExport = """
Create a downloadable file from structured data.

IMPORTANT:
• Use whenever the user wants a downloadable file rather than inline results.
• Supports CSV, XLSX, JSON, XML, PDF, DOCX.
• Can export supplied data or the latest structured tool result from the current chat session.

Use for:
• Data exports
• Excel spreadsheets
• CSV downloads
• PDF reports
• JSON or XML exports
• Downloadable datasets

Returns a downloadable file resource.
""";
}