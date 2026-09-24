using JiwaMcpServer.Models;
using JiwaMcpServer.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace JiwaMcpServer;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/files/upload", UploadAsync)
            .DisableAntiforgery()
            .WithName("UploadDocument")
            .WithSummary("Stream an uploaded document into temporary storage")
            .WithDescription("Accepts either multipart/form-data uploads or a raw request body with the X-File-Name header. Returns a FileId for subsequent MCP document tool calls.");

        app.MapGet("/files/{fileId}", DownloadAsync)
            .WithName("DownloadDocument")
            .WithSummary("Download a stored temporary file")
            .WithDescription("Returns a previously uploaded or generated file by FileId.");

        return app;
    }

    private static async Task<IResult> UploadAsync(HttpRequest request, IFileStorageService storageService, CancellationToken ct)
    {
        if (request.ContentLength is <= 0)
        {
            return Results.BadRequest(new { Error = "Request body is empty." });
        }

        if (request.HasFormContentType)
        {
            return await UploadMultipartAsync(request, storageService, ct);
        }

        if (!request.Headers.TryGetValue("X-File-Name", out var fileNameValues) || string.IsNullOrWhiteSpace(fileNameValues.FirstOrDefault()))
        {
            return Results.BadRequest(new { Error = "Provide a multipart file upload or send a raw body with the X-File-Name header." });
        }

        var storedFile = await storageService.SaveAsync(request.Body, fileNameValues.First()!, request.ContentType ?? "application/octet-stream", ct);
        return Results.Ok(ToUploadedFileReference(storedFile));
    }

    private static async Task<IResult> UploadMultipartAsync(HttpRequest request, IFileStorageService storageService, CancellationToken ct)
    {
        var boundary = GetBoundary(request.ContentType);
        var reader = new MultipartReader(boundary, request.Body);

        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync(ct)) is not null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) || !HasFileContentDisposition(disposition))
            {
                continue;
            }

            var fileName = HeaderUtilities.RemoveQuotes(disposition.FileNameStar.HasValue ? disposition.FileNameStar : disposition.FileName).Value;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Results.BadRequest(new { Error = "Uploaded file name is missing." });
            }

            var storedFile = await storageService.SaveAsync(section.Body, fileName, section.ContentType ?? "application/octet-stream", ct);
            return Results.Ok(ToUploadedFileReference(storedFile));
        }

        return Results.BadRequest(new { Error = "No file section was found in the upload request." });
    }

    private static async Task<IResult> DownloadAsync(string fileId, IFileStorageService storageService, CancellationToken ct)
    {
        if (!await storageService.ExistsAsync(fileId, ct))
        {
            return Results.NotFound(new { Error = $"File '{fileId}' was not found." });
        }

        var metadata = await storageService.GetMetadataAsync(fileId, ct);
        var stream = await storageService.OpenReadAsync(fileId, ct);
        return Results.File(stream, metadata.ContentType, metadata.FileName, enableRangeProcessing: true);
    }

    private static UploadedFileReference ToUploadedFileReference(StoredFileReference storedFile)
        => new()
        {
            FileId = storedFile.FileId,
            FileName = storedFile.FileName,
            ContentType = storedFile.ContentType,
            Length = storedFile.Length
        };

    private static string GetBoundary(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new InvalidOperationException("A multipart content type is required.");
        }

        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidOperationException("The multipart boundary is missing.");
        }

        return boundary;
    }

    private static bool HasFileContentDisposition(ContentDispositionHeaderValue disposition)
        => disposition.DispositionType.Equals("form-data") && (!StringSegment.IsNullOrEmpty(disposition.FileName) || !StringSegment.IsNullOrEmpty(disposition.FileNameStar));
}
