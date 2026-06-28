using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Http;

namespace JiwaMcpServer.Services;

/// <summary>
/// Manages file uploads and retrieval with session isolation, TTL cleanup, and security validations.
/// Files are stored in-memory per session and automatically cleaned up after the TTL expires.
/// </summary>
public class FileStorageService
{
    /// <summary>
    /// Key used to store session ID in HttpContext.Items for request-scoped access
    /// </summary>
    private const string HttpContextSessionIdKey = "FileStorageSessionId";

    /// <summary>
    /// Maximum allowed upload size: 50 MB
    /// </summary>
    public const long MaxUploadSizeBytes = 50 * 1024 * 1024;

    /// <summary>
    /// Default time-to-live for uploaded files: 30 minutes
    /// </summary>
    public static readonly TimeSpan DefaultFileTimeToLive = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Allowed MIME types for upload
    /// </summary>
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "text/html",
        "text/xml",
        "text/json",
        "application/json",
        "application/xml"
    };

    /// <summary>
    /// File metadata stored per session
    /// </summary>
    private class FileMetadata
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public long SizeBytes { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    /// <summary>
    /// Session-scoped storage: sessionId -> (fileId -> FileMetadata)
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, FileMetadata>> _sessionStorage =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks the most recently uploaded file ID per session: sessionId -> fileId
    /// Used to support "latest file" convenience feature for read and query operations.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _lastUploadedFilePerSession =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the current session ID from AsyncLocal context. Falls back to a default if not set.
    /// </summary>
    private static readonly AsyncLocal<string?> CurrentSessionId = new();

    /// <summary>
    /// Sets the session ID for the current async context.
    /// </summary>
    public static void SetSessionId(string sessionId)
    {
        CurrentSessionId.Value = sessionId;
    }

    /// <summary>
    /// Gets the current session ID. Generates a new one if not set.
    /// </summary>
    private static string GetCurrentSessionId()
    {
        // Always return AsyncLocal value if it's set
        if (!string.IsNullOrWhiteSpace(CurrentSessionId.Value))
            return CurrentSessionId.Value;

        // Fallback: use a stable ID based on thread (for unit tests and cases where middleware didn't run)
        return $"session-{Thread.CurrentThread.ManagedThreadId}";
    }

    /// <summary>
    /// Gets the most recently uploaded file ID for the current session.
    /// Returns null if no files have been uploaded in this session.
    /// </summary>
    public string? GetLastUploadedFileId()
    {
        var sessionId = GetCurrentSessionId();
        _lastUploadedFilePerSession.TryGetValue(sessionId, out var lastFileId);
        return lastFileId;
    }

    /// <summary>
    /// Validates MIME type against the allowed list.
    /// </summary>
    private static bool IsAllowedMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return false;

        // Allow any text/* type
        if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check against explicit whitelist
        return AllowedMimeTypes.Contains(mimeType);
    }

    /// <summary>
    /// Generates a stable unique file ID based on content hash and timestamp.
    /// </summary>
    private static string GenerateFileId()
    {
        using (var sha256 = SHA256.Create())
        {
            // Include timestamp to ensure uniqueness across multiple uploads
            var input = Encoding.UTF8.GetBytes($"{Guid.NewGuid():N}-{DateTimeOffset.UtcNow.Ticks}");
            var hash = sha256.ComputeHash(input);
            return Convert.ToHexString(hash).Substring(0, 16).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Uploads a file with base64-encoded content.
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <param name="contentBase64">Base64-encoded file content</param>
    /// <returns>Result containing fileId or error message</returns>
    public UploadFileResult UploadFile(string fileName, string mimeType, string contentBase64)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(fileName))
            return UploadFileResult.CreateError("fileName is required");

        if (string.IsNullOrWhiteSpace(mimeType))
            return UploadFileResult.CreateError("mimeType is required");

        if (string.IsNullOrWhiteSpace(contentBase64))
            return UploadFileResult.CreateError("contentBase64 is required");

        // Validate MIME type
        if (!IsAllowedMimeType(mimeType))
            return UploadFileResult.CreateError($"MIME type '{mimeType}' is not allowed. Allowed types: text/*, application/json, application/xml");

        // Decode base64
        byte[] content;
        try
        {
            content = Convert.FromBase64String(contentBase64);
        }
        catch (FormatException)
        {
            return UploadFileResult.CreateError("contentBase64 is not valid base64 encoding");
        }

        // Validate size
        if (content.Length == 0)
            return UploadFileResult.CreateError("File content cannot be empty");

        if (content.Length > MaxUploadSizeBytes)
            return UploadFileResult.CreateError($"File size ({content.Length} bytes) exceeds maximum ({MaxUploadSizeBytes} bytes)");

        // Generate file ID and metadata
        var fileId = GenerateFileId();
        var sessionId = GetCurrentSessionId();
        var now = DateTimeOffset.UtcNow;

        var metadata = new FileMetadata
        {
            FileId = fileId,
            FileName = fileName,
            MimeType = mimeType,
            Content = content,
            SizeBytes = content.Length,
            UploadedAt = now,
            ExpiresAt = now.Add(DefaultFileTimeToLive)
        };

        // Store in session
        var sessionFiles = _sessionStorage.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, FileMetadata>());
        sessionFiles.TryAdd(fileId, metadata);

        // Track this as the latest uploaded file for the session
        _lastUploadedFilePerSession[sessionId] = fileId;

        return UploadFileResult.CreateSuccess(fileId);
    }

    /// <summary>
    /// Reads an uploaded file by ID.
    /// </summary>
    /// <param name="fileId">The file ID</param>
    /// <returns>File content as string (for text files) or error</returns>
    public ReadFileResult ReadFile(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return ReadFileResult.CreateError("fileId is required");

        var sessionId = GetCurrentSessionId();

        // Cleanup expired files first
        CleanupExpiredFiles(sessionId);

        if (!_sessionStorage.TryGetValue(sessionId, out var sessionFiles))
            return ReadFileResult.CreateError($"File '{fileId}' not found (no files in session)");

        if (!sessionFiles.TryGetValue(fileId, out var metadata))
            return ReadFileResult.CreateError($"File '{fileId}' not found");

        // Attempt to read as text
        try
        {
            var text = Encoding.UTF8.GetString(metadata.Content);
            return ReadFileResult.CreateSuccess(text, metadata.FileName, metadata.MimeType);
        }
        catch (Exception ex)
        {
            return ReadFileResult.CreateError($"Cannot read file '{fileId}' as text: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads an uploaded file by optional ID, defaulting to the most recently uploaded file if ID is not provided.
    /// </summary>
    /// <param name="fileId">The file ID, or null/empty to use the most recently uploaded file</param>
    /// <returns>File content as string (for text files) or error</returns>
    public ReadFileResult ReadFileOrLatest(string? fileId)
    {
        // If fileId is provided, use it directly
        if (!string.IsNullOrWhiteSpace(fileId))
            return ReadFile(fileId);

        // Otherwise, use the most recently uploaded file
        var lastFileId = GetLastUploadedFileId();
        if (string.IsNullOrWhiteSpace(lastFileId))
            return ReadFileResult.CreateError("No fileId provided and no recent files in this session");

        return ReadFile(lastFileId);
    }

    /// <summary>
    /// Queries a CSV file by file ID with a natural language question.
    /// </summary>
    /// <param name="fileId">The file ID</param>
    /// <param name="question">Natural language question about the CSV</param>
    /// <returns>Query result with matching rows or summary</returns>
    public QueryCsvResult QueryCsv(string fileId, string question)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return QueryCsvResult.CreateError("fileId is required");

        if (string.IsNullOrWhiteSpace(question))
            return QueryCsvResult.CreateError("question is required");

        var sessionId = GetCurrentSessionId();

        // Cleanup expired files first
        CleanupExpiredFiles(sessionId);

        if (!_sessionStorage.TryGetValue(sessionId, out var sessionFiles))
            return QueryCsvResult.CreateError($"File '{fileId}' not found");

        if (!sessionFiles.TryGetValue(fileId, out var metadata))
            return QueryCsvResult.CreateError($"File '{fileId}' not found");

        // Verify it's a text file
        if (!metadata.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            return QueryCsvResult.CreateError($"File '{fileId}' is not a text file (type: {metadata.MimeType})");

        // Parse CSV
        ParseCsvResult csvResult;
        try
        {
            var text = Encoding.UTF8.GetString(metadata.Content);
            csvResult = ParseCsv(text);
        }
        catch (Exception ex)
        {
            return QueryCsvResult.CreateError($"Failed to parse CSV: {ex.Message}");
        }

        if (!csvResult.IsSuccess)
            return QueryCsvResult.CreateError(csvResult.Error ?? "Unknown CSV parse error");

        // Execute query
        var queryResult = ExecuteSimpleQuery(csvResult.Rows ?? new(), csvResult.Headers ?? new(), question);
        return queryResult;
    }

    /// <summary>
    /// Queries a CSV file by optional ID with a natural language question, 
    /// defaulting to the most recently uploaded file if ID is not provided.
    /// </summary>
    /// <param name="fileId">The file ID, or null/empty to use the most recently uploaded file</param>
    /// <param name="question">Natural language question about the CSV</param>
    /// <returns>Query result with matching rows or summary</returns>
    public QueryCsvResult QueryCsvOrLatest(string? fileId, string question)
    {
        // If fileId is provided, use it directly
        if (!string.IsNullOrWhiteSpace(fileId))
            return QueryCsv(fileId, question);

        // Otherwise, use the most recently uploaded file
        var lastFileId = GetLastUploadedFileId();
        if (string.IsNullOrWhiteSpace(lastFileId))
            return QueryCsvResult.CreateError("No fileId provided and no recent files in this session");

        return QueryCsv(lastFileId, question);
    }

    /// <summary>
    /// Queries an XML file by file ID with a natural language question.
    /// </summary>
    /// <param name="fileId">The file ID of the XML file</param>
    /// <param name="question">Natural language question about the XML</param>
    /// <returns>Query result with matching elements or structure summary</returns>
    public QueryXmlResult QueryXml(string fileId, string question)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return QueryXmlResult.CreateError("fileId is required");

        if (string.IsNullOrWhiteSpace(question))
            return QueryXmlResult.CreateError("question is required");

        var sessionId = GetCurrentSessionId();

        // Cleanup expired files first
        CleanupExpiredFiles(sessionId);

        if (!_sessionStorage.TryGetValue(sessionId, out var sessionFiles))
            return QueryXmlResult.CreateError($"File '{fileId}' not found");

        if (!sessionFiles.TryGetValue(fileId, out var metadata))
            return QueryXmlResult.CreateError($"File '{fileId}' not found");

        // Verify it's a text file
        if (!metadata.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
            !metadata.MimeType.Contains("xml", StringComparison.OrdinalIgnoreCase))
            return QueryXmlResult.CreateError($"File '{fileId}' is not an XML file (type: {metadata.MimeType})");

        // Parse XML
        ParseXmlResult xmlResult;
        try
        {
            var text = Encoding.UTF8.GetString(metadata.Content);
            xmlResult = ParseXml(text);
        }
        catch (Exception ex)
        {
            return QueryXmlResult.CreateError($"Failed to parse XML: {ex.Message}");
        }

        if (!xmlResult.IsSuccess)
            return QueryXmlResult.CreateError(xmlResult.Error ?? "Unknown XML parse error");

        // Execute query
        var queryResult = ExecuteXmlQuery(xmlResult.Document!, question);
        return queryResult;
    }

    /// <summary>
    /// Queries an XML file by optional ID with a natural language question,
    /// defaulting to the most recently uploaded file if ID is not provided.
    /// </summary>
    /// <param name="fileId">The file ID, or null/empty to use the most recently uploaded file</param>
    /// <param name="question">Natural language question about the XML</param>
    /// <returns>Query result with matching elements or structure summary</returns>
    public QueryXmlResult QueryXmlOrLatest(string? fileId, string question)
    {
        // If fileId is provided, use it directly
        if (!string.IsNullOrWhiteSpace(fileId))
            return QueryXml(fileId, question);

        // Otherwise, use the most recently uploaded file
        var lastFileId = GetLastUploadedFileId();
        if (string.IsNullOrWhiteSpace(lastFileId))
            return QueryXmlResult.CreateError("No fileId provided and no recent files in this session");

        return QueryXml(lastFileId, question);
    }

    /// <summary>
    /// Queries a JSON file by file ID with a natural language question.
    /// </summary>
    /// <param name="fileId">The file ID of the JSON file</param>
    /// <param name="question">Natural language question about the JSON</param>
    /// <returns>Query result with matching keys or structure summary</returns>
    public QueryJsonResult QueryJson(string fileId, string question)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return QueryJsonResult.CreateError("fileId is required");

        if (string.IsNullOrWhiteSpace(question))
            return QueryJsonResult.CreateError("question is required");

        var sessionId = GetCurrentSessionId();

        // Cleanup expired files first
        CleanupExpiredFiles(sessionId);

        if (!_sessionStorage.TryGetValue(sessionId, out var sessionFiles))
            return QueryJsonResult.CreateError($"File '{fileId}' not found");

        if (!sessionFiles.TryGetValue(fileId, out var metadata))
            return QueryJsonResult.CreateError($"File '{fileId}' not found");

        // Verify it's a text file or JSON
        if (!metadata.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
            !metadata.MimeType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return QueryJsonResult.CreateError($"File '{fileId}' is not a JSON file (type: {metadata.MimeType})");

        // Parse JSON
        ParseJsonResult jsonResult;
        try
        {
            var text = Encoding.UTF8.GetString(metadata.Content);
            jsonResult = ParseJson(text);
        }
        catch (Exception ex)
        {
            return QueryJsonResult.CreateError($"Failed to parse JSON: {ex.Message}");
        }

        if (!jsonResult.IsSuccess)
            return QueryJsonResult.CreateError(jsonResult.Error ?? "Unknown JSON parse error");

        // Execute query
        var queryResult = ExecuteJsonQuery(jsonResult.Document!.Value, question);
        return queryResult;
    }

    /// <summary>
    /// Queries a JSON file by optional ID with a natural language question,
    /// defaulting to the most recently uploaded file if ID is not provided.
    /// </summary>
    /// <param name="fileId">The file ID, or null/empty to use the most recently uploaded file</param>
    /// <param name="question">Natural language question about the JSON</param>
    /// <returns>Query result with matching keys or structure summary</returns>
    public QueryJsonResult QueryJsonOrLatest(string? fileId, string question)
    {
        // If fileId is provided, use it directly
        if (!string.IsNullOrWhiteSpace(fileId))
            return QueryJson(fileId, question);

        // Otherwise, use the most recently uploaded file
        var lastFileId = GetLastUploadedFileId();
        if (string.IsNullOrWhiteSpace(lastFileId))
            return QueryJsonResult.CreateError("No fileId provided and no recent files in this session");

        return QueryJson(lastFileId, question);
    }

    /// <summary>
    /// Parses CSV content and returns headers and rows.
    /// </summary>
    private static ParseCsvResult ParseCsv(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length < 1)
            return ParseCsvResult.CreateError("CSV is empty");

        // Parse headers
        var headers = ParseCsvLine(lines[0]);
        if (headers.Count == 0)
            return ParseCsvResult.CreateError("CSV has no headers");

        // Parse rows
        var rows = new List<Dictionary<string, string>>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);

            // Skip malformed rows
            if (values.Count != headers.Count)
                continue;

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < headers.Count; j++)
            {
                row[headers[j]] = values[j];
            }

            rows.Add(row);
        }

        return ParseCsvResult.CreateSuccess(headers, rows);
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted values.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                // Handle escaped quotes
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    /// <summary>
    /// Parses XML content into an XmlDocument for querying.
    /// </summary>
    private static ParseXmlResult ParseXml(string content)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);
            return ParseXmlResult.CreateSuccess(doc);
        }
        catch (XmlException ex)
        {
            return ParseXmlResult.CreateError($"XML parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ParseXmlResult.CreateError($"Failed to parse XML: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses JSON content into a JsonElement for querying.
    /// </summary>
    private static ParseJsonResult ParseJson(string content)
    {
        try
        {
            var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
            using var doc = JsonDocument.Parse(content, options);
            return ParseJsonResult.CreateSuccess(doc.RootElement.Clone());
        }
        catch (JsonException ex)
        {
            return ParseJsonResult.CreateError($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ParseJsonResult.CreateError($"Failed to parse JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a simple query on CSV data.
    /// Supports basic filtering and field extraction.
    /// </summary>
    private static QueryCsvResult ExecuteSimpleQuery(
        List<Dictionary<string, string>> rows,
        List<string> headers,
        string question)
    {
        if (rows.Count == 0)
            return QueryCsvResult.CreateSuccess(new List<string> { "No data rows found in CSV" });

        // Simple query strategies:
        // 1. Count matching rows
        // 2. Extract specific columns
        // 3. Filter by field value
        // 4. Summary statistics

        question = question.ToLowerInvariant();
        var results = new List<string>();

        // Strategy: "count" or "how many"
        if (question.Contains("count", StringComparison.OrdinalIgnoreCase) ||
            question.Contains("how many", StringComparison.OrdinalIgnoreCase))
        {
            return QueryCsvResult.CreateSuccess(new List<string> { $"Total rows: {rows.Count}" });
        }

        // Strategy: field extraction - "show" or "list"
        var matchingHeaders = headers.Where(h =>
            question.Contains(h, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matchingHeaders.Count > 0)
        {
            // Extract and show matching columns
            results.Add($"Showing column(s): {string.Join(", ", matchingHeaders)}");

            // Show top 10 rows
            var limit = Math.Min(10, rows.Count);
            for (int i = 0; i < limit; i++)
            {
                var rowData = string.Join(" | ",
                    matchingHeaders.Select(h => $"{h}: {rows[i].GetValueOrDefault(h, "N/A")}"));
                results.Add($"Row {i + 1}: {rowData}");
            }

            if (rows.Count > 10)
                results.Add($"... and {rows.Count - 10} more rows");

            return QueryCsvResult.CreateSuccess(results);
        }

        // Fallback: return summary
        results.Add($"CSV Summary:");
        results.Add($"  Rows: {rows.Count}");
        results.Add($"  Columns: {string.Join(", ", headers)}");
        results.Add($"First row sample: {string.Join(" | ", headers.Select(h => rows[0].GetValueOrDefault(h, "N/A")))}");

        return QueryCsvResult.CreateSuccess(results);
    }

    /// <summary>
    /// Executes a simple query on XML data.
    /// Supports element/attribute search, XPath-like queries, and basic summarization.
    /// </summary>
    private static QueryXmlResult ExecuteXmlQuery(XmlDocument document, string question)
    {
        var results = new List<string>();
        question = question.ToLowerInvariant();

        // Strategy: count nodes
        if (question.Contains("count") || question.Contains("how many"))
        {
            var allNodes = document.GetElementsByTagName("*");
            results.Add($"Total elements: {allNodes.Count}");
            return QueryXmlResult.CreateSuccess(results);
        }

        // Strategy: search for element by name
        var searchTerms = question.Split(new[] { ' ', ',', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToList();

        var matchingNodes = new List<XmlNode>();
        foreach (var term in searchTerms)
        {
            var nodes = document.GetElementsByTagName(term);
            foreach (XmlNode node in nodes)
            {
                if (!matchingNodes.Contains(node))
                    matchingNodes.Add(node);
            }
        }

        if (matchingNodes.Count > 0)
        {
            results.Add($"Found {matchingNodes.Count} matching element(s)");
            var limit = Math.Min(10, matchingNodes.Count);
            for (int i = 0; i < limit; i++)
            {
                var node = matchingNodes[i];
                var elementInfo = $"<{node.Name}>";
                if (!string.IsNullOrWhiteSpace(node.InnerText))
                {
                    var text = node.InnerText.Trim();
                    if (text.Length > 100)
                        text = text.Substring(0, 100) + "...";
                    elementInfo += $" {text}";
                }
                results.Add(elementInfo);
            }

            if (matchingNodes.Count > 10)
                results.Add($"... and {matchingNodes.Count - 10} more elements");

            return QueryXmlResult.CreateSuccess(results);
        }

        // Fallback: return XML structure summary
        var root = document.DocumentElement;
        results.Add("XML Structure:");
        results.Add($"  Root element: <{root?.Name}>");
        results.Add($"  Total elements: {document.GetElementsByTagName("*").Count}");

        var uniqueTags = new HashSet<string>();
        if (document.DocumentElement?.ChildNodes != null)
        {
            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                    uniqueTags.Add(node.Name);
            }
        }

        if (uniqueTags.Count > 0)
            results.Add($"  Child element types: {string.Join(", ", uniqueTags.Take(10))}");

        return QueryXmlResult.CreateSuccess(results);
    }

    /// <summary>
    /// Executes a simple query on JSON data.
    /// Supports key search, value extraction, and basic structure summarization.
    /// </summary>
    private static QueryJsonResult ExecuteJsonQuery(JsonElement element, string question)
    {
        var results = new List<string>();
        question = question.ToLowerInvariant();

        // Strategy: count properties
        if (question.Contains("count") || question.Contains("how many"))
        {
            var count = element.ValueKind == JsonValueKind.Object
                ? element.EnumerateObject().Count()
                : element.ValueKind == JsonValueKind.Array
                ? element.GetArrayLength()
                : 0;
            results.Add($"Total items: {count}");
            return QueryJsonResult.CreateSuccess(results);
        }

        // Strategy: search for keys
        var searchTerms = question.Split(new[] { ' ', ',', '?', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToList();

        var matchingKeys = new Dictionary<string, JsonElement>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var term in searchTerms)
                {
                    if (property.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!matchingKeys.ContainsKey(property.Name))
                            matchingKeys[property.Name] = property.Value;
                    }
                }
            }
        }

        if (matchingKeys.Count > 0)
        {
            results.Add($"Found {matchingKeys.Count} matching key(s)");
            foreach (var kvp in matchingKeys.Take(10))
            {
                var value = FormatJsonValue(kvp.Value);
                results.Add($"  {kvp.Key}: {value}");
            }

            if (matchingKeys.Count > 10)
                results.Add($"... and {matchingKeys.Count - 10} more keys");

            return QueryJsonResult.CreateSuccess(results);
        }

        // Fallback: return JSON structure summary
        results.Add("JSON Structure:");
        if (element.ValueKind == JsonValueKind.Object)
        {
            var propCount = element.EnumerateObject().Count();
            results.Add($"  Type: Object with {propCount} properties");
            foreach (var property in element.EnumerateObject().Take(5))
            {
                results.Add($"    {property.Name}: {property.Value.ValueKind}");
            }
            if (propCount > 5)
                results.Add($"    ... and {propCount - 5} more properties");
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var length = element.GetArrayLength();
            results.Add($"  Type: Array with {length} item(s)");
            var limit = Math.Min(5, length);
            for (int i = 0; i < limit; i++)
            {
                results.Add($"    [{i}]: {element[i].ValueKind}");
            }
            if (length > 5)
                results.Add($"    ... and {length - 5} more items");
        }
        else
        {
            results.Add($"  Type: {element.ValueKind}");
            var value = FormatJsonValue(element);
            if (value.Length > 100)
                value = value.Substring(0, 100) + "...";
            results.Add($"  Value: {value}");
        }

        return QueryJsonResult.CreateSuccess(results);
    }

    /// <summary>
    /// Formats a JSON element value as a readable string.
    /// </summary>
    private static string FormatJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => element.GetBoolean().ToString(),
            JsonValueKind.Null => "null",
            JsonValueKind.Array => $"Array[{element.GetArrayLength()}]",
            JsonValueKind.Object => $"Object[{element.EnumerateObject().Count()}]",
            _ => element.ValueKind.ToString()
        };
    }

    /// <summary>
    /// Removes expired files from a session.
    /// </summary>
    private void CleanupExpiredFiles(string sessionId)
    {
        if (!_sessionStorage.TryGetValue(sessionId, out var sessionFiles))
            return;

        var now = DateTimeOffset.UtcNow;
        var expiredIds = sessionFiles
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var expiredId in expiredIds)
        {
            sessionFiles.TryRemove(expiredId, out _);
        }

        // Remove empty session
        if (sessionFiles.IsEmpty)
            _sessionStorage.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Result of file upload operation
    /// </summary>
    public class UploadFileResult
    {
        public bool IsSuccess { get; set; }
        public string? FileId { get; set; }
        public string? Error { get; set; }

        public static UploadFileResult CreateSuccess(string fileId) =>
            new() { IsSuccess = true, FileId = fileId };

        public static UploadFileResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of file read operation
    /// </summary>
    public class ReadFileResult
    {
        public bool IsSuccess { get; set; }
        public string? Content { get; set; }
        public string? FileName { get; set; }
        public string? MimeType { get; set; }
        public string? Error { get; set; }

        public static ReadFileResult CreateSuccess(string content, string fileName, string mimeType) =>
            new() { IsSuccess = true, Content = content, FileName = fileName, MimeType = mimeType };

        public static ReadFileResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of CSV query operation
    /// </summary>
    public class QueryCsvResult
    {
        public bool IsSuccess { get; set; }
        public List<string>? Results { get; set; }
        public string? Error { get; set; }

        public static QueryCsvResult CreateSuccess(List<string> results) =>
            new() { IsSuccess = true, Results = results };

        public static QueryCsvResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of CSV parsing
    /// </summary>
    private class ParseCsvResult
    {
        public bool IsSuccess { get; set; }
        public List<string>? Headers { get; set; }
        public List<Dictionary<string, string>>? Rows { get; set; }
        public string? Error { get; set; }

        public static ParseCsvResult CreateSuccess(List<string> headers, List<Dictionary<string, string>> rows) =>
            new() { IsSuccess = true, Headers = headers, Rows = rows };

        public static ParseCsvResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of XML parsing
    /// </summary>
    private class ParseXmlResult
    {
        public bool IsSuccess { get; set; }
        public XmlDocument? Document { get; set; }
        public string? Error { get; set; }

        public static ParseXmlResult CreateSuccess(XmlDocument document) =>
            new() { IsSuccess = true, Document = document };

        public static ParseXmlResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of JSON parsing
    /// </summary>
    private class ParseJsonResult
    {
        public bool IsSuccess { get; set; }
        public JsonElement? Document { get; set; }
        public string? Error { get; set; }

        public static ParseJsonResult CreateSuccess(JsonElement document) =>
            new() { IsSuccess = true, Document = document };

        public static ParseJsonResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of XML query operation
    /// </summary>
    public class QueryXmlResult
    {
        public bool IsSuccess { get; set; }
        public List<string>? Results { get; set; }
        public string? Error { get; set; }

        public static QueryXmlResult CreateSuccess(List<string> results) =>
            new() { IsSuccess = true, Results = results };

        public static QueryXmlResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }

    /// <summary>
    /// Result of JSON query operation
    /// </summary>
    public class QueryJsonResult
    {
        public bool IsSuccess { get; set; }
        public List<string>? Results { get; set; }
        public string? Error { get; set; }

        public static QueryJsonResult CreateSuccess(List<string> results) =>
            new() { IsSuccess = true, Results = results };

        public static QueryJsonResult CreateError(string error) =>
            new() { IsSuccess = false, Error = error };
    }
}
