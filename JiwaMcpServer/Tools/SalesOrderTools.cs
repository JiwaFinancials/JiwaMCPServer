using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolDescriptions;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tools;

[PlannerDomain("SalesOrderTools")]
[McpServerToolType]
public class SalesOrderTools : JiwaToolBase
{
    private static readonly string[] DefaultProductSalesFields = ["PartNo", "Description", "QuantityDelivered", "LocalLineTotalInc"];
    private static readonly string[] DefaultProductSalesTimelineFields = ["InvoiceDate", "QuantityDelivered", "LocalLineTotalInc", "IsCreditNote"];
    private static readonly JsonSerializerOptions CompactJsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "GetSalesOrderDetails"), Description(SalesOrderToolDescriptions.GetSalesOrder)]
    public Task<string> GetSalesOrder(SalesOrderGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [McpServerTool(Name = "CreateSalesOrder"), Description(SalesOrderToolDescriptions.CreateSalesOrder)]
    public Task<string> CreateSalesOrder(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [McpServerTool(Name = "CreateSO"), Description(SalesOrderToolDescriptions.CreateSalesOrderAlias)]
    public Task<string> CreateSO(SalesOrderPOSTRequest requestDTO, CancellationToken ct = default)
        => CreateSalesOrder(requestDTO, ct);

    [McpServerTool(Name = "UpdateSalesOrder"), Description(SalesOrderToolDescriptions.ModifySalesOrder)]
    public Task<string> ModifySalesOrder(SalesOrderPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);
            return result.ToJson<SalesOrder>();
        });

    [McpServerTool(Name = "AddItemToSalesOrder"), Description(SalesOrderToolDescriptions.AddItemToSalesOrder)]
    public Task<string> AddAProductToASalesOrder(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(requestDTO.InvoiceHistoryID))
            {
                if (string.IsNullOrWhiteSpace(requestDTO.InvoiceID))
                {
                    return new
                    {
                        success = false,
                        error = "InvoiceID is required when InvoiceHistoryID is not provided."
                    }.ToJson();
                }

                var currentHistory = await ResolveCurrentHistoryAsync(requestDTO.InvoiceID, ct);
                if (string.IsNullOrWhiteSpace(currentHistory?.InvoiceHistoryID))
                {
                    return new
                    {
                        success = false,
                        error = $"I couldn't determine the current history for sales order '{requestDTO.InvoiceID}'."
                    }.ToJson();
                }

                requestDTO.InvoiceHistoryID = currentHistory.InvoiceHistoryID;
            }

            var result = await JiwaApiClient.PostAsync(requestDTO, ct);
            return result.ToJson<SalesOrderLine>();
        });

    [McpServerTool(Name = "AddItemToSO"), Description(SalesOrderToolDescriptions.AddItemToSalesOrderAlias)]
    public Task<string> AddItemToSO(SalesOrderLinePOSTRequest requestDTO, CancellationToken ct = default)
        => AddAProductToASalesOrder(requestDTO, ct);

    private async Task<SalesOrderHistory?> ResolveCurrentHistoryAsync(string invoiceId, CancellationToken ct)
    {
        var salesOrder = await JiwaApiClient.GetAsync(new SalesOrderGETRequest { InvoiceID = invoiceId }, ct);
        return salesOrder?.Histories?
            .Where(history => history != null && !string.IsNullOrWhiteSpace(history.InvoiceHistoryID))
            .OrderByDescending(history => history.HistoryNo ?? int.MinValue)
            .FirstOrDefault();
    }

    [McpServerTool(Name = "ProductSales", ReadOnly = true), Description(SalesOrderToolDescriptions.ProductSales)]
    public Task<string> SearchProductSales(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize, requestDTO.Fields, DefaultProductSalesFields);
        });

    [McpServerTool(Name = "SummarizeProductSales", ReadOnly = true), Description(SalesOrderToolDescriptions.SummarizeProductSales)]
    public Task<string> SummarizeProductSales(
        [Description("Optional resultHandle returned by a previous ProductSales call. Use this whenever possible so the full stored sales dataset is summarized instead of a preview.")] string? resultHandle = null,
        [Description("How to rank products. Supported values include QuantityDelivered, quantity sold, LocalLineTotalInc, sales value, and revenue.")] string metric = "QuantityDelivered",
        [Description("How many products to return. Use 1 for a single best seller or a higher number such as 10 for a top-products list.")] int top = 10,
        [Description("Optional full sales query to execute when resultHandle is not supplied. This tool fetches and summarizes the full matching sales dataset server-side before returning the top products.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery? requestDTO = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            JsonNode? structuredResult;
            if (!string.IsNullOrWhiteSpace(resultHandle))
            {
                structuredResult = StructuredResultHandleStore.Get(resultHandle);
                if (structuredResult is null)
                    throw new InvalidOperationException("The supplied resultHandle is not available.");
            }
            else
            {
                requestDTO ??= new v_Jiwa_SalesInformationQuery();
                requestDTO.Fields = EnsureRequestedFields(requestDTO.Fields, DefaultProductSalesFields);
                var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
                structuredResult = JsonSerializer.SerializeToNode(allResults) ?? new JsonArray();
            }

            return CreateProductSalesSummaryJson(structuredResult, metric, top);
        });

    [McpServerTool(Name = "SummarizeProductSalesTimeline", ReadOnly = true), Description(SalesOrderToolDescriptions.SummarizeProductSalesTimeline)]
    public Task<string> SummarizeProductSalesTimeline(
        [Description("Optional resultHandle returned by a previous ProductSales call. Use this whenever possible so the full stored sales dataset is grouped instead of a preview.")] string? resultHandle = null,
        [Description("Which metric to total in each time bucket. Supported values include QuantityDelivered, quantity sold, LocalLineTotalInc, sales value, and revenue.")] string metric = "LocalLineTotalInc",
        [Description("Which time bucket to group by. Supported values include year, quarter, and month.")] string period = "month",
        [Description("Optional full sales query to execute when resultHandle is not supplied. This tool fetches and groups the full matching sales dataset server-side before returning the timeline.")] JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery? requestDTO = null,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            JsonNode? structuredResult;
            if (!string.IsNullOrWhiteSpace(resultHandle))
            {
                structuredResult = StructuredResultHandleStore.Get(resultHandle);
                if (structuredResult is null)
                    throw new InvalidOperationException("The supplied resultHandle is not available.");
            }
            else
            {
                requestDTO ??= new v_Jiwa_SalesInformationQuery();
                requestDTO.Fields = EnsureRequestedFields(requestDTO.Fields, DefaultProductSalesTimelineFields);

                var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
                if (!string.IsNullOrEmpty(confirmationMessage))
                    return confirmationMessage;

                var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
                structuredResult = JsonSerializer.SerializeToNode(allResults) ?? new JsonArray();
            }

            return CreateProductSalesTimelineSummaryJson(structuredResult, metric, period);
        });

    [McpServerTool(Name = "ListSalesOrders", ReadOnly = true), Description(SalesOrderToolDescriptions.QuerySalesOrders)]
    public Task<string> SearchSalesOrders(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesOrdersQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return CreateSearchResponseJson(allResults, Config.PageSize);
        });

    private static string CreateProductSalesSummaryJson(JsonNode? structuredResult, string? metric, int top)
    {
        if (top <= 0)
            throw new InvalidOperationException("top must be greater than zero.");

        var rows = ExtractRows(structuredResult);
        var rankingMetric = ResolveRankingMetric(metric);
        var aggregates = new Dictionary<string, ProductSalesAggregate>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.OfType<JsonObject>())
        {
            var partNo = GetStringValue(row, "PartNo");
            var description = GetStringValue(row, "Description");
            var key = !string.IsNullOrWhiteSpace(partNo)
                ? partNo
                : !string.IsNullOrWhiteSpace(description)
                    ? description
                    : "(unspecified product)";

            if (!aggregates.TryGetValue(key, out var aggregate))
            {
                aggregate = new ProductSalesAggregate
                {
                    PartNo = partNo,
                    Description = description
                };
                aggregates[key] = aggregate;
            }

            if (string.IsNullOrWhiteSpace(aggregate.PartNo) && !string.IsNullOrWhiteSpace(partNo))
                aggregate.PartNo = partNo;

            if (string.IsNullOrWhiteSpace(aggregate.Description) && !string.IsNullOrWhiteSpace(description))
                aggregate.Description = description;

            aggregate.TotalQuantityDelivered += GetDecimalValue(row, "QuantityDelivered");
            aggregate.TotalSalesValue += GetSignedSalesValue(row);
            aggregate.SalesLineCount++;
        }

        var rankedProducts = aggregates.Values
            .OrderByDescending(aggregate => rankingMetric.Selector(aggregate))
            .ThenBy(aggregate => aggregate.PartNo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(aggregate => aggregate.Description, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToList();

        var topProducts = new JsonArray();
        for (var index = 0; index < rankedProducts.Count; index++)
        {
            var aggregate = rankedProducts[index];
            topProducts.Add(new JsonObject
            {
                ["rank"] = index + 1,
                ["partNo"] = string.IsNullOrWhiteSpace(aggregate.PartNo) ? null : aggregate.PartNo,
                ["description"] = string.IsNullOrWhiteSpace(aggregate.Description) ? null : aggregate.Description,
                ["quantityDelivered"] = aggregate.TotalQuantityDelivered,
                ["salesValue"] = aggregate.TotalSalesValue,
                ["salesLineCount"] = aggregate.SalesLineCount
            });
        }

        var assistantInstructions = new JsonArray
        {
            "Use only the values present in topProducts when answering the user.",
            "If a requested value is not present in this tool result, say you cannot determine it from the available data.",
            "Do not infer, estimate, or invent missing product codes, descriptions, quantities, or sales values."
        };

        var summary = new JsonObject
        {
            ["answerStatus"] = "complete",
            ["answerText"] = CreateProductSalesAnswerText(rankingMetric, rankedProducts, rows.Count, aggregates.Count),
            ["metric"] = rankingMetric.Name,
            ["metricLabel"] = rankingMetric.Label,
            ["sourceRowCount"] = rows.Count,
            ["groupedProductCount"] = aggregates.Count,
            ["returned"] = rankedProducts.Count,
            ["topProducts"] = topProducts,
            ["assistantInstructions"] = assistantInstructions
        };

        return summary.ToJsonString(CompactJsonOptions);
    }

    private static string CreateProductSalesTimelineSummaryJson(JsonNode? structuredResult, string? metric, string? period)
    {
        var rows = ExtractRows(structuredResult);
        var timelineMetric = ResolveTimelineMetric(metric);
        var timeBucket = ResolveTimeBucket(period);
        var aggregates = new SortedDictionary<string, SalesTimelineAggregate>(StringComparer.OrdinalIgnoreCase);
        var includedRowCount = 0;

        foreach (var row in rows.OfType<JsonObject>())
        {
            if (!TryGetDateTimeOffsetValue(row, "InvoiceDate", out var invoiceDate))
                continue;

            var bucket = timeBucket.BucketSelector(invoiceDate);
            if (!aggregates.TryGetValue(bucket.Key, out var aggregate))
            {
                aggregate = new SalesTimelineAggregate
                {
                    Period = bucket.Key,
                    Label = bucket.Label,
                    SortKey = bucket.SortKey
                };
                aggregates[bucket.Key] = aggregate;
            }

            aggregate.Value += timelineMetric.Selector(row);
            aggregate.SalesLineCount++;
            includedRowCount++;
        }

        var periodTotals = new JsonArray();
        foreach (var aggregate in aggregates.Values.OrderBy(item => item.SortKey))
        {
            periodTotals.Add(new JsonObject
            {
                ["period"] = aggregate.Period,
                ["label"] = aggregate.Label,
                ["value"] = aggregate.Value,
                ["salesLineCount"] = aggregate.SalesLineCount
            });
        }

        var assistantInstructions = new JsonArray
        {
            "Use only the values present in periodTotals when answering the user.",
            "If the user asked for a chart, build it only from periodTotals because these values were aggregated from the full dataset or stored result handle.",
            "Do not generate charts, trends, or totals from preview rows."
        };

        var summary = new JsonObject
        {
            ["answerStatus"] = "complete",
            ["answerText"] = CreateProductSalesTimelineAnswerText(timelineMetric, timeBucket, aggregates.Count, includedRowCount, rows.Count),
            ["metric"] = timelineMetric.Name,
            ["metricLabel"] = timelineMetric.Label,
            ["period"] = timeBucket.Name,
            ["periodLabel"] = timeBucket.Label,
            ["sourceRowCount"] = rows.Count,
            ["includedRowCount"] = includedRowCount,
            ["returned"] = aggregates.Count,
            ["periodTotals"] = periodTotals,
            ["assistantInstructions"] = assistantInstructions
        };

        return summary.ToJsonString(CompactJsonOptions);
    }

    private static JsonArray ExtractRows(JsonNode? structuredResult)
        => structuredResult switch
        {
            JsonArray array => array,
            JsonObject obj when TryGetPropertyCaseInsensitive(obj, "results", out var results) && results is JsonArray resultArray => resultArray,
            _ => throw new InvalidOperationException("Structured result must be a JSON array or an object containing a 'results' array.")
        };

    private static string CreateProductSalesAnswerText(
        (string Name, string Label, Func<ProductSalesAggregate, decimal> Selector) rankingMetric,
        IReadOnlyList<ProductSalesAggregate> rankedProducts,
        int sourceRowCount,
        int groupedProductCount)
    {
        if (rankedProducts.Count == 0)
        {
            return $"No product sales matched the query. The summary scanned {sourceRowCount} sales rows and found 0 grouped products.";
        }

        var leadingProduct = rankedProducts[0];
        var leadingIdentifier = !string.IsNullOrWhiteSpace(leadingProduct.PartNo)
            ? leadingProduct.PartNo
            : !string.IsNullOrWhiteSpace(leadingProduct.Description)
                ? leadingProduct.Description
                : "(unspecified product)";

        return $"Computed top {rankedProducts.Count} products by {rankingMetric.Label} from {sourceRowCount} sales rows grouped into {groupedProductCount} products. The leading product is {leadingIdentifier}.";
    }

    private static string CreateProductSalesTimelineAnswerText(
        (string Name, string Label, Func<JsonObject, decimal> Selector) timelineMetric,
        (string Name, string Label, Func<DateTimeOffset, TimeBucket> BucketSelector) timeBucket,
        int periodCount,
        int includedRowCount,
        int sourceRowCount)
    {
        if (periodCount == 0)
            return $"No sales rows with a valid InvoiceDate were available to compute {timelineMetric.Label} by {timeBucket.Label}. The tool scanned {sourceRowCount} sales rows.";

        return $"Computed {timelineMetric.Label} by {timeBucket.Label} across {periodCount} periods from {includedRowCount} sales rows.";
    }

    private static string EnsureRequestedFields(string? requestedFields, IReadOnlyCollection<string> requiredFields)
    {
        var mergedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(requestedFields))
        {
            foreach (var field in requestedFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mergedFields.Add(field);
        }

        foreach (var field in requiredFields)
            mergedFields.Add(field);

        return string.Join(", ", mergedFields);
    }

    private static (string Name, string Label, Func<ProductSalesAggregate, decimal> Selector) ResolveRankingMetric(string? metric)
    {
        var normalizedMetric = metric?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedMetric))
            return ("QuantityDelivered", "quantity sold", aggregate => aggregate.TotalQuantityDelivered);

        return normalizedMetric.ToLowerInvariant() switch
        {
            "quantitydelivered" or "quantity sold" or "qty sold" or "qty" or "quantity" => ("QuantityDelivered", "quantity sold", aggregate => aggregate.TotalQuantityDelivered),
            "locallinetotalinc" or "sales value" or "revenue" or "sales" or "value" => ("LocalLineTotalInc", "sales value", aggregate => aggregate.TotalSalesValue),
            _ => throw new InvalidOperationException($"Unsupported metric '{metric}'. Use QuantityDelivered or LocalLineTotalInc.")
        };
    }

    private static (string Name, string Label, Func<JsonObject, decimal> Selector) ResolveTimelineMetric(string? metric)
    {
        var normalizedMetric = metric?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedMetric))
            return ("LocalLineTotalInc", "sales value", GetSignedSalesValue);

        return normalizedMetric.ToLowerInvariant() switch
        {
            "quantitydelivered" or "quantity sold" or "qty sold" or "qty" or "quantity" => ("QuantityDelivered", "quantity sold", row => GetDecimalValue(row, "QuantityDelivered")),
            "locallinetotalinc" or "sales value" or "revenue" or "sales" or "value" => ("LocalLineTotalInc", "sales value", GetSignedSalesValue),
            _ => throw new InvalidOperationException($"Unsupported metric '{metric}'. Use QuantityDelivered or LocalLineTotalInc.")
        };
    }

    private static (string Name, string Label, Func<DateTimeOffset, TimeBucket> BucketSelector) ResolveTimeBucket(string? period)
    {
        var normalizedPeriod = period?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPeriod))
            return ("month", "month", CreateMonthBucket);

        return normalizedPeriod.ToLowerInvariant() switch
        {
            "year" or "annual" or "yearly" => ("year", "year", CreateYearBucket),
            "quarter" or "quarterly" => ("quarter", "quarter", CreateQuarterBucket),
            "month" or "monthly" => ("month", "month", CreateMonthBucket),
            _ => throw new InvalidOperationException($"Unsupported period '{period}'. Use year, quarter, or month.")
        };
    }

    private static TimeBucket CreateYearBucket(DateTimeOffset value)
        => new(
            value.Year.ToString(CultureInfo.InvariantCulture),
            value.Year.ToString(CultureInfo.InvariantCulture),
            new DateTimeOffset(value.Year, 1, 1, 0, 0, 0, value.Offset));

    private static TimeBucket CreateQuarterBucket(DateTimeOffset value)
    {
        var quarter = ((value.Month - 1) / 3) + 1;
        var firstMonthOfQuarter = ((quarter - 1) * 3) + 1;
        return new TimeBucket(
            $"{value.Year.ToString(CultureInfo.InvariantCulture)}-Q{quarter}",
            $"{value.Year.ToString(CultureInfo.InvariantCulture)}-Q{quarter}",
            new DateTimeOffset(value.Year, firstMonthOfQuarter, 1, 0, 0, 0, value.Offset));
    }

    private static TimeBucket CreateMonthBucket(DateTimeOffset value)
        => new(
            value.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            value.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, value.Offset));

    private static decimal GetSignedSalesValue(JsonObject obj)
    {
        var salesValue = GetDecimalValue(obj, "LocalLineTotalInc");
        return TryGetBooleanValue(obj, "IsCreditNote", out var isCreditNote) && isCreditNote && salesValue > 0m
            ? -salesValue
            : salesValue;
    }

    private static bool TryGetBooleanValue(JsonObject obj, string propertyName, out bool value)
    {
        value = false;
        if (!TryGetPropertyCaseInsensitive(obj, propertyName, out var propertyValue) || propertyValue is null)
            return false;

        if (propertyValue is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out var boolValue))
            {
                value = boolValue;
                return true;
            }

            if (jsonValue.TryGetValue<int>(out var intValue))
            {
                value = intValue != 0;
                return true;
            }

            if (jsonValue.TryGetValue<long>(out var longValue))
            {
                value = longValue != 0L;
                return true;
            }

            if (jsonValue.TryGetValue<string>(out var stringValue))
            {
                if (bool.TryParse(stringValue, out var parsedBool))
                {
                    value = parsedBool;
                    return true;
                }

                if (int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                {
                    value = parsedInt != 0;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetDateTimeOffsetValue(JsonObject obj, string propertyName, out DateTimeOffset value)
    {
        value = default;
        if (!TryGetPropertyCaseInsensitive(obj, propertyName, out var propertyValue) || propertyValue is null)
            return false;

        var rawValue = propertyValue.GetValueKind() == JsonValueKind.Null
            ? string.Empty
            : propertyValue.ToString().Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        if (DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value))
            return true;

        if (rawValue.StartsWith("/Date(", StringComparison.Ordinal) && rawValue.EndsWith(")/", StringComparison.Ordinal))
        {
            var ticksSection = rawValue[6..^2];
            var separatorIndex = ticksSection.IndexOfAny(['+', '-']);
            if (separatorIndex > 0)
                ticksSection = ticksSection[..separatorIndex];

            if (long.TryParse(ticksSection, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
            {
                value = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonObject obj, string propertyName, out JsonNode? propertyValue)
    {
        foreach (var property in obj)
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

    private static string GetStringValue(JsonObject obj, string propertyName)
    {
        if (!TryGetPropertyCaseInsensitive(obj, propertyName, out var propertyValue) || propertyValue is null)
            return string.Empty;

        return propertyValue.GetValueKind() == JsonValueKind.Null
            ? string.Empty
            : propertyValue.ToString().Trim();
    }

    private static decimal GetDecimalValue(JsonObject obj, string propertyName)
    {
        if (!TryGetPropertyCaseInsensitive(obj, propertyName, out var propertyValue) || propertyValue is null)
            return 0m;

        if (propertyValue is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<decimal>(out var decimalValue))
                return decimalValue;

            if (jsonValue.TryGetValue<double>(out var doubleValue))
                return Convert.ToDecimal(doubleValue);

            if (jsonValue.TryGetValue<int>(out var intValue))
                return intValue;

            if (jsonValue.TryGetValue<long>(out var longValue))
                return longValue;

            if (jsonValue.TryGetValue<string>(out var stringValue)
                && decimal.TryParse(stringValue, out var parsedDecimal))
            {
                return parsedDecimal;
            }
        }

        return 0m;
    }

    private sealed class ProductSalesAggregate
    {
        public string PartNo { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal TotalQuantityDelivered { get; set; }

        public decimal TotalSalesValue { get; set; }

        public int SalesLineCount { get; set; }
    }

    private sealed class SalesTimelineAggregate
    {
        public string Period { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public DateTimeOffset SortKey { get; set; }

        public decimal Value { get; set; }

        public int SalesLineCount { get; set; }
    }

    private sealed record TimeBucket(string Key, string Label, DateTimeOffset SortKey);
}
