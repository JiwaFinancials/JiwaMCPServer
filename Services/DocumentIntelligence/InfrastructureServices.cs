using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class PassThroughMalwareScanner : IMalwareScanner
{
    public Task<MalwareScanResult> ScanAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MalwareScanResult(true, null));
    }
}

public sealed class LoggerAuditLogger(ILogger<LoggerAuditLogger> logger) : IDocumentAuditLogger
{
    public Task WriteAsync(string tenantId, string action, string documentId, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("DocumentAudit tenant={TenantId} action={Action} document={DocumentId} message={Message}", tenantId, action, documentId, message);
        return Task.CompletedTask;
    }
}

public sealed class DeterministicEmbeddingService(DocumentIntelligenceOptions options) : IEmbeddingService
{
    public Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var dimensions = Math.Max(64, options.EmbeddingDimensions);
        var output = new float[dimensions];
        var utf8 = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(utf8);

        for (var i = 0; i < output.Length; i++)
        {
            var hashByte = hash[i % hash.Length];
            var charByte = utf8.Length == 0 ? (byte)0 : utf8[i % utf8.Length];
            var value = ((hashByte ^ charByte) - 127f) / 128f;
            output[i] = value;
        }

        return Task.FromResult<IReadOnlyList<float>>(output);
    }
}
