using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class CompositeOcrService(IEnumerable<IOcrService> services) : IOcrService
{
    private readonly IOcrService[] _services = services.Where(x => x is not CompositeOcrService).ToArray();

    public async Task<OcrResult> ReadAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        foreach (var service in _services)
        {
            var result = await service.ReadAsync(fileName, mimeType, content, cancellationToken);
            if (result.Success)
            {
                return result;
            }
        }

        return new OcrResult(false, "No OCR provider succeeded.", Array.Empty<OcrPage>());
    }
}

public sealed class AzureDocumentIntelligenceOcrService(
    DocumentIntelligenceOptions options,
    IHttpClientFactory httpClientFactory,
    ILogger<AzureDocumentIntelligenceOcrService> logger) : IOcrService
{
    public async Task<OcrResult> ReadAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        if (!options.AzureDocumentIntelligence.Enabled || string.IsNullOrWhiteSpace(options.AzureDocumentIntelligence.Endpoint) || string.IsNullOrWhiteSpace(options.AzureDocumentIntelligence.ApiKey))
        {
            return new OcrResult(false, "Azure Document Intelligence is not configured.", Array.Empty<OcrPage>());
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(AzureDocumentIntelligenceOcrService));
            var endpoint = options.AzureDocumentIntelligence.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/documentintelligence/documentModels/{options.AzureDocumentIntelligence.ModelId}:analyze?api-version=2024-02-29-preview";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", options.AzureDocumentIntelligence.ApiKey);
            request.Content = new ByteArrayContent(content);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new OcrResult(false, $"Document Intelligence rejected request: {(int)response.StatusCode}", Array.Empty<OcrPage>());
            }

            var operationLocation = response.Headers.TryGetValues("operation-location", out var values)
                ? values.FirstOrDefault()
                : null;

            if (string.IsNullOrWhiteSpace(operationLocation))
            {
                return new OcrResult(false, "Document Intelligence did not return operation-location.", Array.Empty<OcrPage>());
            }

            for (var i = 0; i < 60; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                using var poll = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                poll.Headers.Add("Ocp-Apim-Subscription-Key", options.AzureDocumentIntelligence.ApiKey);
                using var pollResponse = await client.SendAsync(poll, cancellationToken);
                var payload = await pollResponse.Content.ReadAsStringAsync(cancellationToken);
                using var json = JsonDocument.Parse(payload);
                var root = json.RootElement;
                var status = root.GetProperty("status").GetString();

                if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    return new OcrResult(false, "Document Intelligence OCR failed.", Array.Empty<OcrPage>());
                }

                if (!string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!root.TryGetProperty("analyzeResult", out var analyzeResult) || !analyzeResult.TryGetProperty("pages", out var pagesElement))
                {
                    return new OcrResult(false, "Document Intelligence response missing pages.", Array.Empty<OcrPage>());
                }

                var pages = new List<OcrPage>();
                foreach (var pageElement in pagesElement.EnumerateArray())
                {
                    var pageNumber = pageElement.TryGetProperty("pageNumber", out var pageProp) ? pageProp.GetInt32() : pages.Count + 1;
                    var lines = pageElement.TryGetProperty("lines", out var linesElement)
                        ? linesElement.EnumerateArray().Select(line => line.GetProperty("content").GetString() ?? string.Empty)
                        : Enumerable.Empty<string>();
                    var text = string.Join(Environment.NewLine, lines);
                    pages.Add(new OcrPage(pageNumber, text));
                }

                return new OcrResult(true, null, pages);
            }

            return new OcrResult(false, "Document Intelligence OCR timeout.", Array.Empty<OcrPage>());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Azure OCR failed for {FileName}", fileName);
            return new OcrResult(false, ex.Message, Array.Empty<OcrPage>());
        }
    }
}

public sealed class TesseractOcrService(DocumentIntelligenceOptions options, ILogger<TesseractOcrService> logger) : IOcrService
{
    public async Task<OcrResult> ReadAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        if (!options.Tesseract.Enabled)
        {
            return new OcrResult(false, "Tesseract is disabled.", Array.Empty<OcrPage>());
        }

        if (!mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return new OcrResult(false, "Tesseract OCR supports image inputs only.", Array.Empty<OcrPage>());
        }

        var tempInput = Path.Combine(Path.GetTempPath(), $"ocr-{Guid.NewGuid():N}{Path.GetExtension(fileName)}");

        try
        {
            await File.WriteAllBytesAsync(tempInput, content, cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = options.Tesseract.ExecutablePath,
                Arguments = $"\"{tempInput}\" stdout -l {options.Tesseract.Language}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new OcrResult(false, "Unable to start tesseract process.", Array.Empty<OcrPage>());
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return new OcrResult(false, string.IsNullOrWhiteSpace(error) ? "Tesseract OCR failed." : error, Array.Empty<OcrPage>());
            }

            return new OcrResult(true, null, [new OcrPage(1, output)]);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tesseract OCR failed for {FileName}", fileName);
            return new OcrResult(false, ex.Message, Array.Empty<OcrPage>());
        }
        finally
        {
            if (File.Exists(tempInput))
            {
                File.Delete(tempInput);
            }
        }
    }
}
