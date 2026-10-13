using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Services;

internal static class TabularResultMetadataHelper
{
    private static readonly string[] ExportFormats = ["csv", "xlsx"];

    public static string EnrichJson(string result)
    {
        if (string.IsNullOrWhiteSpace(result))
            return result;

        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(result);
        }
        catch (JsonException)
        {
            return result;
        }

        if (rootNode is not JsonObject rootObject || !TryGetTotalCount(rootObject, out var totalCount))
            return result;

        var availableExports = DetectAvailableExports(rootObject);
        rootObject["metadata"] = new JsonObject
        {
            ["totalCount"] = totalCount,
            ["resultSetSize"] = GetResultSetSize(totalCount),
            ["recommendedOutput"] = GetRecommendedOutput(totalCount),
            ["shouldOfferDownload"] = totalCount > 100,
            ["availableExports"] = CreateStringArray(availableExports)
        };
        rootObject["assistantInstructions"] = CreateStringArray(BuildAssistantInstructions(totalCount, availableExports));

        return rootObject.ToJsonString();
    }

    private static bool TryGetTotalCount(JsonObject rootObject, out int totalCount)
    {
        totalCount = 0;

        if (!TryGetCollectionNode(rootObject, out var collectionNode))
            return false;

        totalCount = TryGetIntPropertyCaseInsensitive(rootObject, "total", out var declaredTotal)
            ? declaredTotal
            : collectionNode.Count;

        return true;
    }

    private static IReadOnlyList<string> DetectAvailableExports(JsonObject rootObject)
        => TryGetCollectionNode(rootObject, out _)
            ? ExportFormats
            : [];

    private static bool TryGetCollectionNode(JsonObject rootObject, out JsonArray collectionNode)
    {
        if (TryGetPropertyCaseInsensitive(rootObject, "results", out var resultsNode)
            && resultsNode is JsonArray resultsArray)
        {
            collectionNode = resultsArray;
            return true;
        }

        if (TryGetPropertyCaseInsensitive(rootObject, "result", out var resultNode)
            && resultNode is JsonArray resultArray)
        {
            collectionNode = resultArray;
            return true;
        }

        collectionNode = null!;
        return false;
    }

    private static string GetResultSetSize(int totalCount)
        => totalCount switch
        {
            <= 100 => "small",
            <= 500 => "medium",
            _ => "large"
        };

    private static string GetRecommendedOutput(int totalCount)
        => totalCount switch
        {
            <= 100 => "chat",
            <= 500 => "preview",
            _ => "file"
        };

    private static string[] BuildAssistantInstructions(int totalCount, IReadOnlyList<string> availableExports)
    {
        var instructions = new List<string>
        {
            $"Result set contains {totalCount} records."
        };

        if (totalCount <= 100)
        {
            instructions.Add("Displaying all records in chat is acceptable.");
            instructions.Add("Answer directly with the matching records instead of offering export or asking whether to show a sample.");
            return [.. instructions];
        }

        if (totalCount <= 500)
        {
            instructions.Add("Show only a short preview (for example, the first 10 rows).");
            instructions.Add("Do not paste the full dataset into chat.");
            instructions.Add("For grouped counts, totals, or summaries, use an appropriate server-side aggregation tool with the stored result handle rather than counting rows from the preview.");
            AddMediumResultExportInstruction(instructions, availableExports);
            instructions.Add("Provide pagination if supported.");
            return [.. instructions];
        }

        instructions.Add("Do not render the entire dataset in chat.");
        instructions.Add("Show only a very small preview (for example, the first 10 rows).");
        instructions.Add("Do not paste the full dataset into chat.");
        instructions.Add("For grouped counts, totals, or summaries, use an appropriate server-side aggregation tool with the stored result handle rather than counting rows from the preview.");
        AddLargeResultExportInstruction(instructions, availableExports);
        instructions.Add("Avoid generating thousands of rows in the response.");
        return [.. instructions];
    }

    private static void AddMediumResultExportInstruction(ICollection<string> instructions, IReadOnlyList<string> availableExports)
    {
        var instruction = availableExports.Count switch
        {
            0 => null,
            1 when availableExports[0].Equals("csv", StringComparison.OrdinalIgnoreCase) => "Offer CSV download.",
            1 when availableExports[0].Equals("xlsx", StringComparison.OrdinalIgnoreCase) => "Offer XLSX download.",
            _ => "Offer CSV and XLSX downloads."
        };

        if (!string.IsNullOrWhiteSpace(instruction))
            instructions.Add(instruction);
    }

    private static void AddLargeResultExportInstruction(ICollection<string> instructions, IReadOnlyList<string> availableExports)
    {
        var instruction = availableExports.Count switch
        {
            0 => null,
            1 when availableExports[0].Equals("csv", StringComparison.OrdinalIgnoreCase) => "Recommend CSV download.",
            1 when availableExports[0].Equals("xlsx", StringComparison.OrdinalIgnoreCase) => "Recommend XLSX download.",
            _ => "Recommend CSV or XLSX download."
        };

        if (!string.IsNullOrWhiteSpace(instruction))
            instructions.Add(instruction);
    }

    private static bool TryGetPropertyCaseInsensitive(JsonObject rootObject, string propertyName, out JsonNode? propertyValue)
    {
        foreach (var property in rootObject)
        {
            if (property.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                propertyValue = property.Value;
                return true;
            }
        }

        propertyValue = null;
        return false;
    }

    private static bool TryGetIntPropertyCaseInsensitive(JsonObject rootObject, string propertyName, out int value)
    {
        value = 0;

        if (!TryGetPropertyCaseInsensitive(rootObject, propertyName, out var propertyValue))
            return false;

        if (propertyValue is not JsonValue jsonValue)
            return false;

        return jsonValue.TryGetValue<int>(out value);
    }

    private static JsonArray CreateStringArray(IEnumerable<string> values)
        => new(values.Select(static value => (JsonNode?)JsonValue.Create(value)).ToArray());
}
