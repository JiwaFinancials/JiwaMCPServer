using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Services.DocumentIntelligence;

public sealed class CompositeDocumentExtractor(IEnumerable<IDocumentExtractor> extractors) : IDocumentExtractor
{
    private readonly IDocumentExtractor[] _extractors = extractors.Where(x => x is not CompositeDocumentExtractor).ToArray();

    public bool CanExtract(string fileName, string mimeType) => true;

    public Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        var extractor = _extractors.FirstOrDefault(x => x.CanExtract(fileName, mimeType));
        if (extractor is null)
        {
            throw new InvalidOperationException($"No extractor available for '{fileName}' ({mimeType}).");
        }

        return extractor.ExtractAsync(fileName, mimeType, content, cancellationToken);
    }
}

/// <summary>
/// Dependency-free PDF text extractor.
/// Handles both uncompressed and FlateDecode (zlib/deflate) compressed content streams,
/// which covers the vast majority of non-scanned PDFs.
/// Scanned/image-only PDFs set NeedsOcr=true so the OCR pipeline can handle them.
/// </summary>
public sealed class PdfDocumentExtractor : IDocumentExtractor
{
    private static readonly Regex HeadingLineRegex = new(@"^[A-Z][A-Z\s\-\d]{3,120}$", RegexOptions.Compiled);
    private static readonly Regex BtEtRegex = new(@"BT[\s\S]*?ET", RegexOptions.Compiled);
    private static readonly Regex TextTokenRegex = new(@"\(([^)\\]*(?:\\.[^)\\]*)*)\)\s*Tj|<([0-9A-Fa-f]+)>\s*Tj|\[([^\]]*)\]\s*TJ", RegexOptions.Compiled);

    private static readonly byte[] StreamMarker = "stream"u8.ToArray();
    private static readonly byte[] EndStreamMarker = "endstream"u8.ToArray();
    private static readonly byte[] FlateDecodeMarker = "FlateDecode"u8.ToArray();

    public bool CanExtract(string fileName, string mimeType)
        => mimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        var fragments = new List<string>();
        bool foundCompressedStreams = false;

        // 1. Decompress and parse every FlateDecode content stream
        foreach (var (streamBytes, isFlateDecode) in FindContentStreams(content))
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[]? sourceBytes = streamBytes;
            if (isFlateDecode)
            {
                foundCompressedStreams = true;
                // Try zlib (RFC 1950 — has 2-byte header), then raw deflate as fallback
                if (!TryZLibDecompress(streamBytes, out sourceBytes) &&
                    !TryDeflateDecompress(streamBytes, out sourceBytes))
                    continue;
            }

            var decoded = Encoding.Latin1.GetString(sourceBytes!);
            var text = ExtractBtEtText(decoded);
            if (!string.IsNullOrWhiteSpace(text))
                fragments.Add(text);
        }

        // 2. For PDFs with no compressed streams, scan raw bytes for uncompressed BT/ET text.
        //    Skip this pass when FlateDecode streams were present — their compressed bytes
        //    contain arbitrary byte values that could match BT/ET by coincidence.
        if (!foundCompressedStreams)
        {
            var rawText = ExtractBtEtText(Encoding.Latin1.GetString(content));
            if (!string.IsNullOrWhiteSpace(rawText))
                fragments.Add(rawText);
        }

        var full = Regex.Replace(string.Join(" ", fragments).Trim(), @"\s{2,}", " ");

        if (string.IsNullOrWhiteSpace(full))
        {
            var empty = new DocumentPage(string.Empty, 1, string.Empty, false, Array.Empty<string>(), Array.Empty<DocumentTable>());
            return Task.FromResult(new ExtractionResult([empty], 0, Array.Empty<DocumentTable>(),
                new Dictionary<string, string> { ["extractor"] = "NativePdf", ["pageCount"] = "1" }, NeedsOcr: true));
        }

        // Split into logical pages (~3 000 chars each)
        const int pageChunkSize = 3000;
        var pages = new List<DocumentPage>();
        for (int offset = 0, pageNum = 1; offset < full.Length; offset += pageChunkSize, pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slice = full.Substring(offset, Math.Min(pageChunkSize, full.Length - offset)).Trim();
            var headings = slice
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Length <= 120 && HeadingLineRegex.IsMatch(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20).ToArray();
            var tables = ExtractorHelpers.ExtractSimpleTables(slice, pageNum, "PDF");
            pages.Add(new DocumentPage(string.Empty, pageNum, slice, false, headings, tables));
        }

        // NeedsOcr is only true when no text at all was found.
        // The pipeline's MinimumExtractedCharactersBeforeOcr config controls supplemental OCR.
        return Task.FromResult(new ExtractionResult(
            Pages: pages,
            ExtractedCharacterCount: full.Length,
            Tables: pages.SelectMany(x => x.Tables).ToArray(),
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["extractor"] = "NativePdf",
                ["pageCount"] = pages.Count.ToString()
            },
            NeedsOcr: false));
    }

    private static IEnumerable<(byte[] Data, bool IsFlateDecode)> FindContentStreams(byte[] pdf)
    {
        int pos = 0;

        while (pos < pdf.Length)
        {
            int streamKeyword = ByteIndexOf(pdf, StreamMarker, pos);
            if (streamKeyword < 0) yield break;

            int afterKeyword = streamKeyword + StreamMarker.Length;
            if (afterKeyword >= pdf.Length) yield break;

            int dataStart;
            if (pdf[afterKeyword] == '\r' && afterKeyword + 1 < pdf.Length && pdf[afterKeyword + 1] == '\n')
                dataStart = afterKeyword + 2;
            else if (pdf[afterKeyword] == '\n')
                dataStart = afterKeyword + 1;
            else { pos = streamKeyword + 1; continue; }

            int lookBackStart = Math.Max(0, streamKeyword - 512);
            bool isFlateDecode = ByteIndexOf(pdf, FlateDecodeMarker, lookBackStart, streamKeyword) >= 0;

            int endStreamPos = ByteIndexOf(pdf, EndStreamMarker, dataStart);
            if (endStreamPos < 0) yield break;

            int dataEnd = endStreamPos;
            if (dataEnd > 0 && pdf[dataEnd - 1] == '\n') dataEnd--;
            if (dataEnd > 0 && pdf[dataEnd - 1] == '\r') dataEnd--;

            if (dataEnd > dataStart)
            {
                var slice = new byte[dataEnd - dataStart];
                Buffer.BlockCopy(pdf, dataStart, slice, 0, slice.Length);
                yield return (slice, isFlateDecode);
            }

            pos = endStreamPos + EndStreamMarker.Length;
        }
    }

    /// <summary>Simple byte-array KMP-style search, safe for use inside iterators.</summary>
    private static int ByteIndexOf(byte[] haystack, byte[] needle, int startIndex = 0, int endIndex = -1)
    {
        int limit = endIndex < 0 ? haystack.Length - needle.Length : Math.Min(endIndex, haystack.Length) - needle.Length;
        for (int i = startIndex; i <= limit; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    private static bool TryZLibDecompress(byte[] data, out byte[]? result)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            result = output.ToArray();
            return result.Length > 0;
        }
        catch { result = null; return false; }
    }

    private static bool TryDeflateDecompress(byte[] data, out byte[]? result)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            result = output.ToArray();
            return result.Length > 0;
        }
        catch { result = null; return false; }
    }

    private string ExtractBtEtText(string streamContent)
    {
        var sb = new StringBuilder();
        foreach (Match block in BtEtRegex.Matches(streamContent))
        {
            foreach (Match token in TextTokenRegex.Matches(block.Value))
            {
                if (token.Groups[1].Success)
                    sb.Append(UnescapePdfString(token.Groups[1].Value)).Append(' ');
                else if (token.Groups[2].Success)
                    sb.Append(HexToString(token.Groups[2].Value)).Append(' ');
                else if (token.Groups[3].Success)
                {
                    foreach (Match sub in Regex.Matches(token.Groups[3].Value, @"\(([^)\\]*(?:\\.[^)\\]*)*)\)"))
                        sb.Append(UnescapePdfString(sub.Groups[1].Value));
                    sb.Append(' ');
                }
            }
        }
        return sb.ToString();
    }

    private static string UnescapePdfString(string s)
        => s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t")
            .Replace("\\(", "(").Replace("\\)", ")").Replace("\\\\", "\\");

    private static string HexToString(string hex)
    {
        var sb = new StringBuilder(hex.Length / 2);
        for (var i = 0; i + 1 < hex.Length; i += 2)
            if (byte.TryParse(hex.AsSpan(i, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                sb.Append((char)b);
        return sb.ToString();
    }
}

public sealed class DocxDocumentExtractor : IDocumentExtractor
{
    public bool CanExtract(string fileName, string mimeType)
    {
        return mimeType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var word = WordprocessingDocument.Open(stream, false);
        var body = word.MainDocumentPart?.Document.Body;

        if (body is null)
        {
            return Task.FromResult(new ExtractionResult(Array.Empty<DocumentPage>(), 0, Array.Empty<DocumentTable>(), new Dictionary<string, string>(), true));
        }

        var paragraphs = body.Descendants<Paragraph>()
            .Select(x => x.InnerText?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var text = string.Join(Environment.NewLine, paragraphs);
        var tables = ExtractWordTables(body);
        var page = new DocumentPage(string.Empty, 1, text, false, ExtractLikelyHeadings(paragraphs), tables);

        return Task.FromResult(new ExtractionResult(
            Pages: [page],
            ExtractedCharacterCount: text.Length,
            Tables: tables,
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["extractor"] = "OpenXml",
                ["pageCount"] = "1"
            },
            NeedsOcr: string.IsNullOrWhiteSpace(text)));
    }

    private static IReadOnlyList<DocumentTable> ExtractWordTables(Body body)
    {
        var output = new List<DocumentTable>();
        var index = 0;

        foreach (var table in body.Descendants<Table>())
        {
            var rows = table.Descendants<TableRow>()
                .Select(row => row.Descendants<TableCell>().Select(cell => cell.InnerText.Trim()).ToArray())
                .Where(cells => cells.Length > 0)
                .ToArray();

            if (rows.Length == 0)
            {
                continue;
            }

            var headers = rows[0];
            var dataRows = rows.Skip(1).Select(r => (IReadOnlyList<string>)r).ToArray();
            output.Add(new DocumentTable(1, $"DOCX Table {++index}", headers, dataRows));
        }

        return output;
    }

    private static IReadOnlyList<string> ExtractLikelyHeadings(IEnumerable<string> lines)
    {
        return lines.Where(line => line.Length is > 3 and < 120)
            .Where(line => line.All(c => !char.IsLetter(c) || char.IsUpper(c) || char.IsWhiteSpace(c) || c == '-' || c == ':'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .ToArray();
    }
}

public sealed class SpreadsheetDocumentExtractor : IDocumentExtractor
{
    public bool CanExtract(string fileName, string mimeType)
    {
        return mimeType.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || mimeType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ExtractCsv(content));
        }

        return Task.FromResult(ExtractXlsx(content));
    }

    private static ExtractionResult ExtractCsv(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var headers = lines.FirstOrDefault()?.Split(',', StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
        var rows = lines.Skip(1).Select(line => (IReadOnlyList<string>)line.Split(',', StringSplitOptions.TrimEntries)).ToArray();

        var table = new DocumentTable(1, "CSV Data", headers, rows);
        var page = new DocumentPage(string.Empty, 1, text, false, Array.Empty<string>(), [table]);

        return new ExtractionResult([page], text.Length, [table], new Dictionary<string, string>
        {
            ["extractor"] = "Csv",
            ["pageCount"] = "1"
        }, NeedsOcr: false);
    }

    private static ExtractionResult ExtractXlsx(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);
        var pages = new List<DocumentPage>();
        var tables = new List<DocumentTable>();
        var textBuilder = new StringBuilder();
        var sheetIndex = 0;

        foreach (var sheet in workbook.Worksheets)
        {
            sheetIndex++;
            var range = sheet.RangeUsed();
            if (range is null)
            {
                continue;
            }

            var rows = range.RowsUsed().ToArray();
            if (rows.Length == 0)
            {
                continue;
            }

            var header = rows[0].Cells().Select(cell => cell.GetString().Trim()).ToArray();
            var dataRows = rows.Skip(1)
                .Select(r => (IReadOnlyList<string>)r.Cells().Select(cell => cell.GetString().Trim()).ToArray())
                .ToArray();

            var table = new DocumentTable(sheetIndex, $"Worksheet {sheet.Name}", header, dataRows);
            tables.Add(table);

            textBuilder.AppendLine($"Sheet: {sheet.Name}");
            textBuilder.AppendLine(string.Join(" | ", header));
            foreach (var row in dataRows.Take(200))
            {
                textBuilder.AppendLine(string.Join(" | ", row));
            }

            pages.Add(new DocumentPage(string.Empty, sheetIndex, textBuilder.ToString(), false, [sheet.Name], [table]));
            textBuilder.Clear();
        }

        var fullText = string.Join(Environment.NewLine + Environment.NewLine, pages.Select(p => p.Text));
        return new ExtractionResult(pages, fullText.Length, tables, new Dictionary<string, string>
        {
            ["extractor"] = "ClosedXML",
            ["pageCount"] = pages.Count.ToString()
        }, NeedsOcr: false);
    }
}

public sealed class PlainTextDocumentExtractor : IDocumentExtractor
{
    public bool CanExtract(string fileName, string mimeType)
    {
        if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(content);
        var headings = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Length > 2 && line.Length < 100 && line == line.ToUpperInvariant())
            .Take(12)
            .ToArray();

        var tables = ExtractorHelpers.ExtractSimpleTables(text, 1, "Text");

        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            using var json = JsonDocument.Parse(text);
            headings = ["JSON Document"];
        }

        var page = new DocumentPage(string.Empty, 1, text, false, headings, tables);
        return Task.FromResult(new ExtractionResult([page], text.Length, tables, new Dictionary<string, string>
        {
            ["extractor"] = "PlainText",
            ["pageCount"] = "1"
        }, NeedsOcr: false));
    }
}

public sealed class ImageDocumentExtractor : IDocumentExtractor
{
    public bool CanExtract(string fileName, string mimeType)
    {
        return mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractionResult> ExtractAsync(string fileName, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        var page = new DocumentPage(string.Empty, 1, string.Empty, false, Array.Empty<string>(), Array.Empty<DocumentTable>());
        return Task.FromResult(new ExtractionResult([page], 0, Array.Empty<DocumentTable>(), new Dictionary<string, string>
        {
            ["extractor"] = "Image",
            ["pageCount"] = "1"
        }, NeedsOcr: true));
    }
}

internal static class ExtractorHelpers
{
    public static IReadOnlyList<DocumentTable> ExtractSimpleTables(string text, int pageNumber, string captionPrefix)
    {
        var lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var candidateRows = lines
            .Where(line => line.Contains('|') || (line.Count(char.IsWhiteSpace) > 3 && line.Any(char.IsDigit)))
            .Take(300)
            .Select(line => line.Contains('|')
                ? line.Split('|', StringSplitOptions.TrimEntries)
                : Regex.Split(line.Trim(), "\\s{2,}", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            .Where(parts => parts.Length >= 2)
            .ToList();

        if (candidateRows.Count < 2)
        {
            return Array.Empty<DocumentTable>();
        }

        var headers = candidateRows[0];
        var rows = candidateRows.Skip(1).Select(r => (IReadOnlyList<string>)r).ToArray();
        return [new DocumentTable(pageNumber, $"{captionPrefix} Table", headers, rows)];
    }
}
