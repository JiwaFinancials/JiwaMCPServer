using System.Threading.Channels;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<DocumentProcessingJob> _channel = Channel.CreateUnbounded<DocumentProcessingJob>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    public void Enqueue(DocumentProcessingJob job)
    {
        if (!_channel.Writer.TryWrite(job))
        {
            throw new InvalidOperationException("Unable to enqueue document processing job.");
        }
    }

    public async IAsyncEnumerable<DocumentProcessingJob> DequeueAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_channel.Reader.TryRead(out var job))
            {
                yield return job;
            }
        }
    }
}
