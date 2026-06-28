using JiwaMcpServer.Services;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

/// <summary>
/// MCP tools for file upload, retrieval, and CSV querying.
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
    [McpServerTool(ReadOnly = false), Description("Upload a file with base64-encoded content. Returns a fileId for later reference. Supports text files up to 50 MB. Allowed MIME types: text/*, application/json, application/xml")]
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
}
