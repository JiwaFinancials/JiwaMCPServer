using System.Text;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class SemanticChunker(DocumentIntelligenceOptions options)
{
    private static readonly Regex WhiteSpace = new("\\s+", RegexOptions.Compiled);

    public IReadOnlyList<DocumentChunk> Chunk(string documentId, IReadOnlyList<DocumentPage> pages)
    {
        if (pages.Count == 0)
        {
            return Array.Empty<DocumentChunk>();
        }

        var segments = pages
            .OrderBy(p => p.PageNumber)
            .SelectMany(ExtractSegments)
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .ToList();

        if (segments.Count == 0)
        {
            return Array.Empty<DocumentChunk>();
        }

        var chunks = new List<DocumentChunk>();
        var chunkBuffer = new List<Segment>();
        var chunkTokenCount = 0;

        foreach (var segment in segments)
        {
            var segmentTokens = EstimateTokens(segment.Text);
            if (chunkBuffer.Count > 0 && chunkTokenCount + segmentTokens > options.ChunkMaxTokens)
            {
                chunks.Add(CreateChunk(documentId, chunkBuffer, chunks.Count + 1));
                chunkBuffer = CreateOverlap(chunkBuffer);
                chunkTokenCount = chunkBuffer.Sum(x => EstimateTokens(x.Text));
            }

            chunkBuffer.Add(segment);
            chunkTokenCount += segmentTokens;

            if (chunkTokenCount >= options.ChunkTargetTokens && chunkTokenCount >= options.ChunkMinTokens)
            {
                chunks.Add(CreateChunk(documentId, chunkBuffer, chunks.Count + 1));
                chunkBuffer = CreateOverlap(chunkBuffer);
                chunkTokenCount = chunkBuffer.Sum(x => EstimateTokens(x.Text));
            }
        }

        if (chunkBuffer.Count > 0)
        {
            chunks.Add(CreateChunk(documentId, chunkBuffer, chunks.Count + 1));
        }

        return chunks;
    }

    private static IEnumerable<Segment> ExtractSegments(DocumentPage page)
    {
        foreach (var heading in page.Headings)
        {
            yield return new Segment(page.PageNumber, heading, SegmentType.Heading, Array.Empty<DocumentTable>());
        }

        var paragraphs = WhiteSpace.Replace(page.Text, " ")
            .Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var paragraph in paragraphs)
        {
            yield return new Segment(page.PageNumber, paragraph, SegmentType.Paragraph, Array.Empty<DocumentTable>());
        }

        foreach (var table in page.Tables)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(table.Caption))
            {
                sb.AppendLine(table.Caption);
            }

            if (table.Headers.Count > 0)
            {
                sb.AppendLine(string.Join(" | ", table.Headers));
            }

            foreach (var row in table.Rows)
            {
                sb.AppendLine(string.Join(" | ", row));
            }

            yield return new Segment(page.PageNumber, sb.ToString(), SegmentType.Table, [table]);
        }
    }

    private List<Segment> CreateOverlap(IReadOnlyList<Segment> segments)
    {
        var overlap = new List<Segment>();
        var tokenCount = 0;

        for (var i = segments.Count - 1; i >= 0; i--)
        {
            var segment = segments[i];
            overlap.Insert(0, segment);
            tokenCount += EstimateTokens(segment.Text);

            if (tokenCount >= options.ChunkOverlapTokens)
            {
                break;
            }
        }

        return overlap;
    }

    private static DocumentChunk CreateChunk(string documentId, IReadOnlyList<Segment> segments, int sequence)
    {
        var startPage = segments.Min(x => x.PageNumber);
        var endPage = segments.Max(x => x.PageNumber);
        var headings = segments.Where(x => x.Type == SegmentType.Heading).Select(x => x.Text).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToArray();
        var tables = segments.SelectMany(x => x.Tables).ToArray();
        var text = string.Join(Environment.NewLine + Environment.NewLine, segments.Select(x => x.Text));

        return new DocumentChunk(
            ChunkId: $"{documentId}-chunk-{sequence:D5}",
            DocumentId: documentId,
            StartPage: startPage,
            EndPage: endPage,
            Text: text,
            Tables: tables,
            Headings: headings,
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["startPage"] = startPage.ToString(),
                ["endPage"] = endPage.ToString(),
                ["sequence"] = sequence.ToString()
            });
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
    }

    private sealed record Segment(int PageNumber, string Text, SegmentType Type, IReadOnlyList<DocumentTable> Tables);

    private enum SegmentType
    {
        Heading,
        Paragraph,
        Table
    }
}
