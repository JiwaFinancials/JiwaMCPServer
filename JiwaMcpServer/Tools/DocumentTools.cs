using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Models;
using JiwaMcpServer.Options;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolDescriptions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tools;

[PlannerDomain("DocumentTools")]
[McpServerToolType]
public sealed class DocumentTools(
    IDocumentProcessingService documentProcessingService,
    IFileExportService fileExportService,
    IFileStorageService fileStorageService,
    IToolResultStore toolResultStore,
    IOptions<DocumentProcessingOptions> documentOptions,
    ILogger<DocumentTools> logger) : JiwaToolBase
{
    private const int EmbeddedResourceThresholdBytes = 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IDocumentProcessingService _documentProcessingService = documentProcessingService;
    private readonly IFileExportService _fileExportService = fileExportService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IToolResultStore _toolResultStore = toolResultStore;
    private readonly DocumentProcessingOptions _documentOptions = documentOptions.Value;
    private readonly ILogger<DocumentTools> _logger = logger;

    [McpServerTool(ReadOnly = true), Description(DocumentToolDescriptions.ExtractDocumentData)]
    public Task<string> ExtractDocumentData(
        [Description("The FileId returned by the document upload workflow.")] string fileId,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var extractedDocument = await LoadExtractedDocumentAsync(fileId, ct);
            _logger.LogInformation("Completed extraction for file {FileId}", fileId);
            return JsonSerializer.Serialize(extractedDocument, JsonOptions);
        });

    [McpServerTool(ReadOnly = true), Description(DocumentToolDescriptions.ExtractLocalDocumentData)]
    public Task<string> ExtractLocalDocumentData(
        [Description("A local Windows file or directory path under LocalFileSystem:AllowedRoots.")] string localPath,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var resolved = LocalFilePathResolver.ResolveSingleFilePath(localPath, Config.LocalFileSystemAllowedRoots, Config.LocalFileSystemMaxReadBytes);
            var contentType = DocumentFormatMappings.GetMimeType(_documentProcessingService.DetectFileType(resolved.ResolvedPath));

            await using var stream = new FileStream(resolved.ResolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var extractedDocument = await _documentProcessingService.ExtractDocumentAsync(stream, Path.GetFileName(resolved.ResolvedPath), contentType, ct);

            var payload = new
            {
                resolved.OriginalPath,
                ResolvedPath = resolved.ResolvedPath,
                resolved.InferredFromDirectory,
                extractedDocument.FileName,
                extractedDocument.FileType,
                extractedDocument.Metadata,
                extractedDocument.ExtractedText,
                extractedDocument.StructuredData
            };

            _logger.LogInformation("Completed extraction for local path {Path}; resolved file {ResolvedPath}", localPath, resolved.ResolvedPath);
            return JsonSerializer.Serialize(payload, JsonOptions);
        });

    [McpServerTool, Description(DocumentToolDescriptions.UploadLocalDocument)]
    public Task<string> UploadLocalDocument(
        [Description("A local Windows file path, or a local directory containing exactly one supported file.")] string localPath,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var resolved = LocalFilePathResolver.ResolveSingleFilePath(localPath, Config.LocalFileSystemAllowedRoots, Config.LocalFileSystemMaxReadBytes);
            var fileName = Path.GetFileName(resolved.ResolvedPath);
            var contentType = DocumentFormatMappings.GetMimeType(_documentProcessingService.DetectFileType(resolved.ResolvedPath));

            await using var stream = new FileStream(resolved.ResolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var storedFile = await _fileStorageService.SaveAsync(stream, fileName, contentType, ct);

            var payload = new
            {
                resolved.OriginalPath,
                ResolvedPath = resolved.ResolvedPath,
                resolved.InferredFromDirectory,
                storedFile.FileId,
                storedFile.FileName,
                storedFile.ContentType,
                storedFile.Length
            };

            _logger.LogInformation("Uploaded local file {ResolvedPath} as FileId {FileId}", resolved.ResolvedPath, storedFile.FileId);
            return JsonSerializer.Serialize(payload, JsonOptions);
        });

    [McpServerTool(ReadOnly = true), Description(DocumentToolDescriptions.QueryUploadedDocument)]
    public Task<string> QueryUploadedDocument(
        [Description("The FileId returned by the document upload workflow.")] string fileId,
        [Description("The question to answer using the uploaded document.")] string question,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new InvalidOperationException("A question is required.");
            }

            var extractedDocument = await LoadExtractedDocumentAsync(fileId, ct);
            var answer = BuildDeterministicDocumentAnswer(extractedDocument, question);

            var payload = new
            {
                FileId = fileId,
                extractedDocument.FileName,
                extractedDocument.FileType,
                extractedDocument.Metadata,
                Question = question,
                Answer = answer
            };

            return JsonSerializer.Serialize(payload, JsonOptions);
        });

    [McpServerTool, Description(DocumentToolDescriptions.ConvertDocument)]
    public async Task<IEnumerable<ContentBlock>> ConvertDocument(DocumentConversionRequest request, CancellationToken ct = default)
    {
        try
        {
            var metadata = await _fileStorageService.GetMetadataAsync(request.FileId, ct);
            await using var stream = await _fileStorageService.OpenReadAsync(request.FileId, ct);
            var export = await _documentProcessingService.ConvertDocumentAsync(stream, metadata.FileName, metadata.ContentType, request.TargetFormat, ct);
            var storedExport = await SaveGeneratedFileAsync(export, ct);
            return CreateFileBlocks(storedExport, export);
        }
        catch (Exception ex)
        {
            return [new TextContentBlock { Text = $"Error converting document {request.FileId}: {ex.Message}" }];
        }
    }

    [McpServerTool, Description(DocumentToolDescriptions.CreateDataExport)]
    public async Task<IEnumerable<ContentBlock>> CreateDataExport(FileExportRequest request, CancellationToken ct = default)
    {
        try
        {
            var data = ResolveExportData(request);
            var export = await _fileExportService.CreateExportAsync(data, request.Format, request.FileName, ct);
            var storedExport = await SaveGeneratedFileAsync(export, ct);
            return CreateFileBlocks(storedExport, export);
        }
        catch (Exception ex)
        {
            return [new TextContentBlock { Text = $"Error creating export: {ex.Message}" }];
        }
    }

    private JsonNode ResolveExportData(FileExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        JsonNode data;
        if (request.Data is JsonElement dataElement
            && dataElement.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            data = JsonNode.Parse(dataElement.GetRawText()) ?? new JsonArray();
        }
        else if (!string.IsNullOrWhiteSpace(request.ClientSessionId)
            && (request.UseLatestStructuredToolResult || !string.IsNullOrWhiteSpace(request.SourceToolName)))
        {
            data = _toolResultStore.GetLatestStructuredResult(request.ClientSessionId, request.SourceToolName)
                ?? throw new InvalidOperationException("No stored structured tool result is available for this export request.");
        }
        else
        {
            throw new InvalidOperationException("CreateDataExport requires either Data or a stored structured tool result reference.");
        }

        return ApplyFieldSelection(data, request.Fields);
    }

    private static JsonNode ApplyFieldSelection(JsonNode data, string[]? fields)
    {
        var selectedFields = fields?
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field => field.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedFields is not { Length: > 0 })
            return data.DeepClone();

        return data switch
        {
            JsonArray array => SelectFieldsFromArray(array, selectedFields),
            JsonObject obj => SelectFieldsFromObject(obj, selectedFields, allowMissingFields: false),
            _ => throw new InvalidOperationException("Field selection is only supported for object or array export data.")
        };
    }

    private static JsonArray SelectFieldsFromArray(JsonArray array, IReadOnlyList<string> selectedFields)
    {
        var availableFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in array)
        {
            if (item is JsonObject obj)
            {
                foreach (var property in obj)
                    availableFields.Add(property.Key);
            }
        }

        var missingFields = selectedFields.Where(field => !availableFields.Contains(field)).ToArray();
        if (missingFields.Length > 0)
            throw new InvalidOperationException($"Unknown export field(s): {string.Join(", ", missingFields)}.");

        var filtered = new JsonArray();
        foreach (var item in array)
        {
            filtered.Add(item is JsonObject obj
                ? SelectFieldsFromObject(obj, selectedFields, allowMissingFields: true)
                : item?.DeepClone());
        }

        return filtered;
    }

    private static JsonObject SelectFieldsFromObject(JsonObject obj, IReadOnlyList<string> selectedFields, bool allowMissingFields)
    {
        var filtered = new JsonObject();
        var missingFields = new List<string>();

        foreach (var field in selectedFields)
        {
            var property = obj.FirstOrDefault(candidate => candidate.Key.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (property.Key is null)
            {
                if (!allowMissingFields)
                    missingFields.Add(field);
                continue;
            }

            filtered[property.Key] = property.Value?.DeepClone();
        }

        if (missingFields.Count > 0)
            throw new InvalidOperationException($"Unknown export field(s): {string.Join(", ", missingFields)}.");

        return filtered;
    }

    private async Task<ExtractedDocument> LoadExtractedDocumentAsync(string fileId, CancellationToken ct)
    {
        var metadata = await _fileStorageService.GetMetadataAsync(fileId, ct);
        _logger.LogInformation("Loading uploaded document {FileId} ({FileType})", fileId, metadata.FileType);
        await using var stream = await _fileStorageService.OpenReadAsync(fileId, ct);
        return await _documentProcessingService.ExtractDocumentAsync(stream, metadata.FileName, metadata.ContentType, ct);
    }

    private async Task<StoredFileReference> SaveGeneratedFileAsync(ExportFileResult export, CancellationToken ct)
    {
        await using var stream = new MemoryStream(export.Content, writable: false);
        var storedFile = await _fileStorageService.SaveAsync(stream, export.FileName, export.ContentType, ct);
        _logger.LogInformation("Generated export {FileId} ({FileName})", storedFile.FileId, storedFile.FileName);
        return storedFile;
    }

    private IEnumerable<ContentBlock> CreateFileBlocks(StoredFileReference storedFile, ExportFileResult export)
    {
        var blocks = new List<ContentBlock>
        {
            new TextContentBlock
            {
                Text = $"Created '{storedFile.FileName}' ({storedFile.Length} bytes). FileId: {storedFile.FileId}. Resource URI: {storedFile.ResourceUri}. Temporary retention: {_documentOptions.RetentionHours} hour(s)."
            },
            new ResourceLinkBlock
            {
                Uri = storedFile.ResourceUri,
                Name = storedFile.FileName,
                Title = storedFile.FileName,
                Description = "Generated downloadable file.",
                MimeType = storedFile.ContentType,
                Size = storedFile.Length
            }
        };

        if (export.ContentLength <= EmbeddedResourceThresholdBytes)
        {
            blocks.Add(new EmbeddedResourceBlock
            {
                Resource = BlobResourceContents.FromBytes(export.Content, storedFile.ResourceUri, storedFile.ContentType)
            });
        }

        return blocks;
    }

    private static string BuildDeterministicDocumentAnswer(ExtractedDocument extractedDocument, string question)
    {
        var questionTokens = Regex.Matches(question, "[A-Za-z0-9]+")
            .Select(match => match.Value)
            .Where(token => token.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (questionTokens.Count == 0)
            return "Unable to determine an answer from the document because the question does not contain searchable terms.";

        var lines = (extractedDocument.ExtractedText ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        var matchingLines = lines
            .Where(line => questionTokens.Any(token => line.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Take(5)
            .ToList();

        if (matchingLines.Count == 0)
            return "The requested value is not present in the uploaded document.";

        var builder = new StringBuilder();
        builder.AppendLine("Matched document lines:");
        foreach (var line in matchingLines)
        {
            builder.AppendLine($"- {line}");
        }

        return builder.ToString().TrimEnd();
    }

    }
