using JiwaMcpServer.Services;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Text;

namespace JiwaMcpServer.Tools;

/// <summary>
/// MCP tools for file upload, retrieval, and structured file querying.
/// Supports the Jiwa Chat client's file handling workflow.
/// </summary>
[McpServerToolType]
public class FileTools : JiwaToolBase
{
    private readonly FileStorageService _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FileTools(FileStorageService fileStorage, IHttpContextAccessor? httpContextAccessor = null)
    {
        _fileStorage = fileStorage;
        _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
    }

    /// <summary>
    /// Gets the session ID to use for file operations.
    /// Prefers the HTTP context connection ID if available (for web requests),
    /// otherwise returns null to let the service use its default AsyncLocal value.
    /// </summary>
    private string? GetSessionIdFromContext()
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.Connection?.Id != null)
            {
                return httpContext.Connection.Id;
            }
        }
        catch
        {
            // Ignore errors accessing HttpContext (e.g., in unit tests)
        }

        // Return null to indicate "use default session management" (AsyncLocal value)
        return null;
    }

    /// <summary>
    /// Uploads a file with base64-encoded content.
    /// The client calls this tool to store files for later reference.
    /// </summary>
    /// <param name="fileName">The name of the file being uploaded (e.g., "data.csv")</param>
    /// <param name="mimeType">The MIME type of the file (e.g., "text/csv")</param>
    /// <param name="contentBase64">The file content encoded as base64</param>
    /// <param name="clientSessionId">Optional session ID for grouping files. If provided, subsequent read/query calls with the same ID will be able to access files uploaded in this call</param>
    /// <returns>JSON response with fileId or error message</returns>
    [McpServerTool(ReadOnly = false), Description("Upload a file with base64-encoded content. Returns a fileId for later reference. Supports text files up to 50 MB. Allowed MIME types: text/*, application/json, application/xml, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<string> UploadFile(
        string fileName,
        string mimeType,
        string contentBase64,
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            var result = _fileStorage.UploadFile(fileName, mimeType, contentBase64);

            if (result.IsSuccess)
            {
                return new
                {
                    fileId = result.FileId
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Reads an uploaded file and returns its content as text.
    /// If fileId is not provided or is empty, reads the most recently uploaded file in this session.
    /// Use this to retrieve files previously uploaded via upload_file.
    /// </summary>
    /// <param name="fileId">The file ID returned from upload_file, or empty/null to use the most recent file</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with file content or error message</returns>
    [McpServerTool(ReadOnly = true), Description("Read an uploaded file by its fileId and return the content as text. If fileId is omitted or empty, reads the most recently uploaded file from this session. Returns an error if the file is not found or cannot be read as text")]
    public Task<string> ReadUploadedFile(
        string fileId = "",
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            var result = _fileStorage.ReadFileOrLatest(
                string.IsNullOrWhiteSpace(fileId) ? null : fileId);

            if (result.IsSuccess)
            {
                return new
                {
                    fileName = result.FileName,
                    mimeType = result.MimeType,
                    content = result.Content
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Queries a CSV file using natural language questions.
    /// If fileId is not provided or is empty, queries the most recently uploaded file in this session.
    /// Automatically parses the CSV and extracts relevant data based on the question.
    /// Supports field extraction, row filtering, and basic summarization.
    /// </summary>
    /// <param name="fileId">The file ID of the CSV file, or empty/null to use the most recent file</param>
    /// <param name="question">Natural language question about the CSV data (e.g., "How many rows?", "Show me the Name column")</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with query results or error message</returns>
    [McpServerTool(ReadOnly = true), Description("Query a CSV file using natural language questions. If fileId is omitted or empty, queries the most recently uploaded file from this session. Automatically parses CSV content and returns matching data. Supports questions like 'How many rows?', 'Show me the Name column', or 'List all values'. Returns top 10 matching rows")]
    public Task<string> QueryCsv(
        string fileId = "",
        string question = "",
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            var result = _fileStorage.QueryCsvOrLatest(
                string.IsNullOrWhiteSpace(fileId) ? null : fileId, 
                question);

            if (result.IsSuccess)
            {
                return new
                {
                    results = result.Results ?? new List<string>()
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Queries an Excel file using natural language questions.
    /// Accepts either an uploaded fileId or a local path to an .xlsx file under allowed roots.
    /// If pathOrFileId is omitted, queries the most recently uploaded Excel file in this session.
    /// </summary>
    /// <param name="pathOrFileId">Uploaded fileId or local .xlsx path under allowed roots</param>
    /// <param name="question">Natural language question about the Excel data (e.g., "How many rows?", "Show me the Name column")</param>
    /// <param name="sheetName">Optional worksheet name. Defaults to first worksheet</param>
    /// <param name="range">Optional range address (e.g., A1:D20). Defaults to used range</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with query results or error message</returns>
    [McpServerTool(ReadOnly = true), Description("Query an Excel (.xlsx) file using natural language questions. Accepts either fileId or local path (under LocalFileSystem:AllowedRoots). Optional sheetName and range let you target specific worksheet data. If pathOrFileId is empty, uses the latest uploaded Excel file in this session")]
    public Task<string> QueryExcel(
        string pathOrFileId = "",
        string question = "",
        string sheetName = "",
        string range = "",
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            string? fileIdForQuery = null;

            if (!string.IsNullOrWhiteSpace(pathOrFileId))
            {
                var looksLikePath = Path.IsPathRooted(pathOrFileId) ||
                                    pathOrFileId.Contains(Path.DirectorySeparatorChar) ||
                                    pathOrFileId.Contains(Path.AltDirectorySeparatorChar);

                if (looksLikePath)
                {
                    if (!TryResolveAllowedPath(pathOrFileId, out var fullPath, out var pathError))
                    {
                        return new { error = pathError }.ToJson();
                    }

                    if (!File.Exists(fullPath))
                    {
                        return new { error = $"File '{fullPath}' was not found" }.ToJson();
                    }

                    if (!fullPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        return new { error = "Only .xlsx files are supported for query_excel path input" }.ToJson();
                    }

                    var fileBytes = await File.ReadAllBytesAsync(fullPath);
                    var uploadResult = _fileStorage.UploadFile(
                        Path.GetFileName(fullPath),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Convert.ToBase64String(fileBytes));

                    if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.FileId))
                    {
                        return new { error = uploadResult.Error ?? "Failed to stage local Excel file for querying" }.ToJson();
                    }

                    fileIdForQuery = uploadResult.FileId;
                }
                else
                {
                    fileIdForQuery = pathOrFileId;
                }
            }

            var result = _fileStorage.QueryExcelOrLatest(
                fileIdForQuery,
                question,
                string.IsNullOrWhiteSpace(sheetName) ? null : sheetName,
                string.IsNullOrWhiteSpace(range) ? null : range);

            if (result.IsSuccess)
            {
                return new
                {
                    results = result.Results ?? new List<string>()
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Describes an Excel workbook, including sheets, used ranges, and selected sheet/range context.
    /// Accepts either an uploaded fileId or a local path to an .xlsx file under allowed roots.
    /// </summary>
    /// <param name="pathOrFileId">Uploaded fileId or local .xlsx path under allowed roots</param>
    /// <param name="sheetName">Optional worksheet name. Defaults to first worksheet</param>
    /// <param name="range">Optional range address (e.g., A1:D20). Defaults to used range</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with workbook description or error message</returns>
    [McpServerTool(ReadOnly = true), Description("Describe an Excel (.xlsx) workbook by fileId or local path under LocalFileSystem:AllowedRoots. Returns sheet list, used ranges, selected sheet/range, headers, and row counts")]
    public Task<string> DescribeExcel(
        string pathOrFileId = "",
        string sheetName = "",
        string range = "",
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            string? fileIdForDescribe = null;

            if (!string.IsNullOrWhiteSpace(pathOrFileId))
            {
                var looksLikePath = Path.IsPathRooted(pathOrFileId) ||
                                    pathOrFileId.Contains(Path.DirectorySeparatorChar) ||
                                    pathOrFileId.Contains(Path.AltDirectorySeparatorChar);

                if (looksLikePath)
                {
                    if (!TryResolveAllowedPath(pathOrFileId, out var fullPath, out var pathError))
                    {
                        return new { error = pathError }.ToJson();
                    }

                    if (!File.Exists(fullPath))
                    {
                        return new { error = $"File '{fullPath}' was not found" }.ToJson();
                    }

                    if (!fullPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        return new { error = "Only .xlsx files are supported for describe_excel path input" }.ToJson();
                    }

                    var fileBytes = await File.ReadAllBytesAsync(fullPath);
                    var uploadResult = _fileStorage.UploadFile(
                        Path.GetFileName(fullPath),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Convert.ToBase64String(fileBytes));

                    if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.FileId))
                    {
                        return new { error = uploadResult.Error ?? "Failed to stage local Excel file for description" }.ToJson();
                    }

                    fileIdForDescribe = uploadResult.FileId;
                }
                else
                {
                    fileIdForDescribe = pathOrFileId;
                }
            }

            var result = _fileStorage.DescribeExcelOrLatest(
                fileIdForDescribe,
                string.IsNullOrWhiteSpace(sheetName) ? null : sheetName,
                string.IsNullOrWhiteSpace(range) ? null : range);

            if (result.IsSuccess)
            {
                return new
                {
                    summary = result.Summary
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Reads deterministic Excel rows for import workflows.
    /// Accepts either an uploaded fileId or a local path to an .xlsx file under allowed roots.
    /// </summary>
    /// <param name="pathOrFileId">Uploaded fileId or local .xlsx path under allowed roots</param>
    /// <param name="sheetName">Optional worksheet name. Defaults to first worksheet</param>
    /// <param name="range">Optional range address (e.g., A1:D200). Defaults to used range</param>
    /// <param name="skip">Number of data rows to skip after headers</param>
    /// <param name="take">Maximum number of rows to return (capped at 1000)</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with headers, rows, and paging metadata</returns>
    [McpServerTool(ReadOnly = true), Description("Read Excel (.xlsx) rows deterministically for import by fileId or local path under LocalFileSystem:AllowedRoots. Supports optional sheetName, range, skip, and take paging")]
    public Task<string> ReadExcelRows(
        string pathOrFileId = "",
        string sheetName = "",
        string range = "",
        int skip = 0,
        int take = 200,
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            string? fileIdForRead = null;

            if (!string.IsNullOrWhiteSpace(pathOrFileId))
            {
                var looksLikePath = Path.IsPathRooted(pathOrFileId) ||
                                    pathOrFileId.Contains(Path.DirectorySeparatorChar) ||
                                    pathOrFileId.Contains(Path.AltDirectorySeparatorChar);

                if (looksLikePath)
                {
                    if (!TryResolveAllowedPath(pathOrFileId, out var fullPath, out var pathError))
                    {
                        return new { error = pathError }.ToJson();
                    }

                    if (!File.Exists(fullPath))
                    {
                        return new { error = $"File '{fullPath}' was not found" }.ToJson();
                    }

                    if (!fullPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        return new { error = "Only .xlsx files are supported for read_excel_rows path input" }.ToJson();
                    }

                    var fileBytes = await File.ReadAllBytesAsync(fullPath);
                    var uploadResult = _fileStorage.UploadFile(
                        Path.GetFileName(fullPath),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Convert.ToBase64String(fileBytes));

                    if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.FileId))
                    {
                        return new { error = uploadResult.Error ?? "Failed to stage local Excel file for row reading" }.ToJson();
                    }

                    fileIdForRead = uploadResult.FileId;
                }
                else
                {
                    fileIdForRead = pathOrFileId;
                }
            }

            var result = _fileStorage.ReadExcelRowsOrLatest(
                fileIdForRead,
                string.IsNullOrWhiteSpace(sheetName) ? null : sheetName,
                string.IsNullOrWhiteSpace(range) ? null : range,
                skip,
                take);

            if (result.IsSuccess)
            {
                return new
                {
                    data = result.Data
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Queries an XML file using natural language questions.
    /// If fileId is not provided or is empty, queries the most recently uploaded file in this session.
    /// Automatically parses the XML and extracts relevant data based on the question.
    /// Supports element search, attribute extraction, and basic summarization.
    /// </summary>
    /// <param name="fileId">The file ID of the XML file, or empty/null to use the most recent file</param>
    /// <param name="question">Natural language question about the XML data (e.g., "How many elements?", "Find the config element")</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with query results or error message</returns>
    [McpServerTool(ReadOnly = true), Description("Query an XML file using natural language questions. If fileId is omitted or empty, queries the most recently uploaded file from this session. Automatically parses XML and returns matching elements. Supports questions like 'How many elements?', 'Find [element_name]', or 'Show structure'. Returns top 10 matching elements")]
    public Task<string> QueryXml(
        string fileId = "",
        string question = "",
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            var result = _fileStorage.QueryXmlOrLatest(
                string.IsNullOrWhiteSpace(fileId) ? null : fileId,
                question);

            if (result.IsSuccess)
            {
                return new
                {
                    results = result.Results ?? new List<string>()
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    /// <summary>
    /// Queries a JSON file using natural language questions.
    /// If fileId is not provided or is empty, queries the most recently uploaded file in this session.
    /// Automatically parses the JSON and extracts relevant data based on the question.
    /// Supports key search, value extraction, and basic summarization.
    /// </summary>
    /// <param name="fileId">The file ID of the JSON file, or empty/null to use the most recent file</param>
    /// <param name="question">Natural language question about the JSON data (e.g., "How many keys?", "What is the value property?", "Find all settings")</param>
    /// <param name="clientSessionId">Optional session ID. If provided, ensures access to files from that session</param>
    /// <returns>JSON response with query results or error message</returns>
    [McpServerTool(ReadOnly = true), Description("Query a JSON file using natural language questions. If fileId is omitted or empty, queries the most recently uploaded file from this session. Automatically parses JSON and returns matching keys/values. Supports questions like 'How many keys?', 'Show me [key_name]', or 'What is the structure?'. Returns top 10 matching properties")]
    public Task<string> QueryJson(
        string fileId = "",
        string question = "",
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            var result = _fileStorage.QueryJsonOrLatest(
                string.IsNullOrWhiteSpace(fileId) ? null : fileId,
                question);

            if (result.IsSuccess)
            {
                return new
                {
                    results = result.Results ?? new List<string>()
                }.ToJson();
            }

            return new
            {
                error = result.Error
            }.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description("List files and folders under an allowed local or UNC directory path. Access is restricted to LocalFileSystem:AllowedRoots in appsettings.json")]
    public Task<string> ListLocalDirectory(
        string path,
        bool includeFiles = true,
        bool includeDirectories = true,
        int maxEntries = 200)
    {
        return InvokeToolAsync(async () =>
        {
            if (!includeFiles && !includeDirectories)
            {
                return new { error = "At least one of includeFiles or includeDirectories must be true" }.ToJson();
            }

            if (maxEntries <= 0)
            {
                return new { error = "maxEntries must be greater than 0" }.ToJson();
            }

            if (!TryResolveAllowedPath(path, out var fullPath, out var error))
            {
                return new { error }.ToJson();
            }

            if (!Directory.Exists(fullPath))
            {
                return new { error = $"Directory '{fullPath}' was not found" }.ToJson();
            }

            var entries = new List<object>();
            var remaining = maxEntries;

            if (includeDirectories)
            {
                foreach (var directory in Directory.EnumerateDirectories(fullPath).Take(remaining))
                {
                    entries.Add(new
                    {
                        type = "directory",
                        name = Path.GetFileName(directory),
                        path = directory
                    });
                }

                remaining = maxEntries - entries.Count;
            }

            if (includeFiles && remaining > 0)
            {
                foreach (var file in Directory.EnumerateFiles(fullPath).Take(remaining))
                {
                    var info = new FileInfo(file);
                    entries.Add(new
                    {
                        type = "file",
                        name = Path.GetFileName(file),
                        path = file,
                        sizeBytes = info.Length
                    });
                }
            }

            return new
            {
                path = fullPath,
                entriesReturned = entries.Count,
                maxEntries,
                entries
            }.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description("Read a text file from an allowed local or UNC path. Access is restricted to LocalFileSystem:AllowedRoots in appsettings.json")]
    public Task<string> ReadLocalFile(
        string path,
        int? maxBytes = null)
    {
        return InvokeToolAsync(async () =>
        {
            if (!TryResolveAllowedPath(path, out var fullPath, out var error))
            {
                return new { error }.ToJson();
            }

            if (!File.Exists(fullPath))
            {
                return new { error = $"File '{fullPath}' was not found" }.ToJson();
            }

            var configuredMaxBytes = Config.LocalFileSystemMaxReadBytes > 0 ? Config.LocalFileSystemMaxReadBytes : 256 * 1024;
            var effectiveMaxBytes = Math.Min(maxBytes ?? configuredMaxBytes, configuredMaxBytes);
            if (effectiveMaxBytes <= 0)
            {
                return new { error = "maxBytes must be greater than 0" }.ToJson();
            }

            var fileInfo = new FileInfo(fullPath);
            var bytesToRead = (int)Math.Min(fileInfo.Length, effectiveMaxBytes);
            var buffer = new byte[bytesToRead];
            var totalRead = 0;

            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                while (totalRead < bytesToRead)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(totalRead, bytesToRead - totalRead));
                    if (read == 0)
                    {
                        break;
                    }

                    totalRead += read;
                }
            }

            var text = Encoding.UTF8.GetString(buffer, 0, totalRead);
            var isTruncated = fileInfo.Length > totalRead;

            return new
            {
                path = fullPath,
                sizeBytes = fileInfo.Length,
                readBytes = totalRead,
                truncated = isTruncated,
                maxBytes = effectiveMaxBytes,
                content = text
            }.ToJson();
        });
    }

    [McpServerTool(ReadOnly = true), Description("Query a local structured file by path using natural language. Supports .csv, .xml, .json, and .xlsx under LocalFileSystem:AllowedRoots")]
    public Task<string> QueryLocalStructuredFile(
        string path,
        string question,
        string? clientSessionId = null)
    {
        return InvokeToolAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new { error = "question is required" }.ToJson();
            }

            // Set the session ID: prefer explicit clientSessionId, then HTTP context, then use AsyncLocal default
            if (!string.IsNullOrWhiteSpace(clientSessionId))
            {
                FileStorageService.SetSessionId(clientSessionId);
            }
            else
            {
                var contextSessionId = GetSessionIdFromContext();
                if (contextSessionId != null)
                {
                    FileStorageService.SetSessionId(contextSessionId);
                }
                // else: let AsyncLocal value persist (set by middleware or test setup)
            }

            if (!TryResolveAllowedPath(path, out var fullPath, out var error))
            {
                return new { error }.ToJson();
            }

            if (!File.Exists(fullPath))
            {
                return new { error = $"File '{fullPath}' was not found" }.ToJson();
            }

            if (!TryGetStructuredFileQueryInfo(fullPath, out var queryType, out var mimeType, out error))
            {
                return new { error }.ToJson();
            }

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var uploadResult = _fileStorage.UploadFile(
                Path.GetFileName(fullPath),
                mimeType!,
                Convert.ToBase64String(fileBytes));

            if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.FileId))
            {
                return new { error = uploadResult.Error ?? "Failed to stage local file for querying" }.ToJson();
            }

            var fileId = uploadResult.FileId;

            if (string.Equals(queryType, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var result = _fileStorage.QueryCsv(fileId, question);
                return result.IsSuccess
                    ? new { results = result.Results ?? new List<string>() }.ToJson()
                    : new { error = result.Error }.ToJson();
            }

            if (string.Equals(queryType, "xml", StringComparison.OrdinalIgnoreCase))
            {
                var result = _fileStorage.QueryXml(fileId, question);
                return result.IsSuccess
                    ? new { results = result.Results ?? new List<string>() }.ToJson()
                    : new { error = result.Error }.ToJson();
            }

            if (string.Equals(queryType, "json", StringComparison.OrdinalIgnoreCase))
            {
                var result = _fileStorage.QueryJson(fileId, question);
                return result.IsSuccess
                    ? new { results = result.Results ?? new List<string>() }.ToJson()
                    : new { error = result.Error }.ToJson();
            }

            if (string.Equals(queryType, "excel", StringComparison.OrdinalIgnoreCase))
            {
                var result = _fileStorage.QueryExcel(fileId, question);
                return result.IsSuccess
                    ? new { results = result.Results ?? new List<string>() }.ToJson()
                    : new { error = result.Error }.ToJson();
            }

            return new { error = "Unsupported file type for local structured query" }.ToJson();
        });
    }

    private static bool TryGetStructuredFileQueryInfo(string fullPath, out string queryType, out string mimeType, out string error)
    {
        queryType = string.Empty;
        mimeType = string.Empty;
        error = string.Empty;

        var extension = Path.GetExtension(fullPath);

        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            queryType = "csv";
            mimeType = "text/csv";
            return true;
        }

        if (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
        {
            queryType = "xml";
            mimeType = "application/xml";
            return true;
        }

        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            queryType = "json";
            mimeType = "application/json";
            return true;
        }

        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            queryType = "excel";
            mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return true;
        }

        error = $"Unsupported file extension '{extension}'. Supported extensions: .csv, .xml, .json, .xlsx";
        return false;
    }

    private static bool TryResolveAllowedPath(string requestedPath, out string fullPath, out string error)
    {
        fullPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            error = "path is required";
            return false;
        }

        string[] allowedRoots = Config.LocalFileSystemAllowedRoots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedRoots.Length == 0)
        {
            error = "Local file access is disabled. Configure LocalFileSystem:AllowedRoots in appsettings.json";
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(requestedPath);
        }
        catch (Exception ex)
        {
            error = $"Invalid path '{requestedPath}': {ex.Message}";
            return false;
        }

        var resolvedPath = fullPath;
        var isAllowed = allowedRoots
            .Select(TryNormalizeRoot)
            .Where(normalizedRoot => !string.IsNullOrEmpty(normalizedRoot))
            .Any(normalizedRoot => IsPathWithinRoot(resolvedPath, normalizedRoot!));

        if (!isAllowed)
        {
            error = $"Path '{fullPath}' is outside allowed roots";
            return false;
        }

        return true;
    }

    private static string? TryNormalizeRoot(string root)
    {
        try
        {
            return Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPathWithinRoot(string fullPath, string normalizedRoot)
    {
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
