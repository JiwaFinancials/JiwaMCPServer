using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.Tools;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tests.Tools;

public class SalesOrderToolsTests
{
    [Fact]
    public async Task SummarizeProductSales_FromStoredResult_ReturnsBestSellerByQuantity()
    {
        var toolResultStore = new ToolResultStore();
        var handle = toolResultStore.StoreStructuredResult(new JsonArray
        {
            new JsonObject { ["PartNo"] = "A100", ["Description"] = "Widget A", ["QuantityDelivered"] = 2, ["LocalLineTotalInc"] = 20.5m },
            new JsonObject { ["PartNo"] = "A100", ["Description"] = "Widget A", ["QuantityDelivered"] = 3, ["LocalLineTotalInc"] = 30.0m },
            new JsonObject { ["PartNo"] = "B200", ["Description"] = "Widget B", ["QuantityDelivered"] = 4, ["LocalLineTotalInc"] = 100.0m }
        });

        var tool = new SalesOrderTools();
        var json = await tool.SummarizeProductSales(resultHandle: handle, metric: "quantity sold", top: 1);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("complete", doc.RootElement.GetProperty("answerStatus").GetString());
        Assert.Equal("QuantityDelivered", doc.RootElement.GetProperty("metric").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("sourceRowCount").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("groupedProductCount").GetInt32());
        Assert.Contains("leading product is A100", doc.RootElement.GetProperty("answerText").GetString(), StringComparison.OrdinalIgnoreCase);

        var bestProduct = doc.RootElement.GetProperty("topProducts")[0];
        Assert.Equal(1, bestProduct.GetProperty("rank").GetInt32());
        Assert.Equal("A100", bestProduct.GetProperty("partNo").GetString());
        Assert.Equal("Widget A", bestProduct.GetProperty("description").GetString());
        Assert.Equal(5m, bestProduct.GetProperty("quantityDelivered").GetDecimal());
        Assert.Equal(50.5m, bestProduct.GetProperty("salesValue").GetDecimal());
    }

    [Fact]
    public async Task SummarizeProductSales_FromStoredResult_ReturnsTopProductsByRevenue()
    {
        var toolResultStore = new ToolResultStore();
        var handle = toolResultStore.StoreStructuredResult(new JsonArray
        {
            new JsonObject { ["partNo"] = "A100", ["description"] = "Widget A", ["quantityDelivered"] = 2, ["localLineTotalInc"] = 20.5m },
            new JsonObject { ["partNo"] = "B200", ["description"] = "Widget B", ["quantityDelivered"] = 4, ["localLineTotalInc"] = 100.0m },
            new JsonObject { ["partNo"] = "C300", ["description"] = "Widget C", ["quantityDelivered"] = 1, ["localLineTotalInc"] = 140.0m }
        });

        var tool = new SalesOrderTools();
        var json = await tool.SummarizeProductSales(resultHandle: handle, metric: "revenue", top: 2);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("LocalLineTotalInc", doc.RootElement.GetProperty("metric").GetString());
        var topProducts = doc.RootElement.GetProperty("topProducts");
        Assert.Equal(2, topProducts.GetArrayLength());
        Assert.Equal("C300", topProducts[0].GetProperty("partNo").GetString());
        Assert.Equal("B200", topProducts[1].GetProperty("partNo").GetString());
        Assert.True(doc.RootElement.TryGetProperty("assistantInstructions", out var instructions));
        Assert.Contains(instructions.EnumerateArray().Select(item => item.GetString()), item => item?.Contains("Do not infer, estimate, or invent", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task SummarizeProductSalesTimeline_FromStoredResult_ReturnsSalesValueByYear()
    {
        var toolResultStore = new ToolResultStore();
        var handle = toolResultStore.StoreStructuredResult(new JsonArray
        {
            new JsonObject { ["InvoiceDate"] = "2023-12-20T00:00:00+10:00", ["LocalLineTotalInc"] = 50.0m, ["IsCreditNote"] = false },
            new JsonObject { ["InvoiceDate"] = "2024-01-05T00:00:00+10:00", ["LocalLineTotalInc"] = 100.0m, ["IsCreditNote"] = false },
            new JsonObject { ["InvoiceDate"] = "2024-02-10T00:00:00+10:00", ["LocalLineTotalInc"] = 25.0m, ["IsCreditNote"] = true }
        });

        var tool = new SalesOrderTools();
        var json = await tool.SummarizeProductSalesTimeline(resultHandle: handle, metric: "sales value", period: "year");
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("complete", doc.RootElement.GetProperty("answerStatus").GetString());
        Assert.Equal("LocalLineTotalInc", doc.RootElement.GetProperty("metric").GetString());
        Assert.Equal("year", doc.RootElement.GetProperty("period").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("sourceRowCount").GetInt32());
        Assert.Equal(3, doc.RootElement.GetProperty("includedRowCount").GetInt32());

        var totals = doc.RootElement.GetProperty("periodTotals");
        Assert.Equal(2, totals.GetArrayLength());
        Assert.Equal("2023", totals[0].GetProperty("period").GetString());
        Assert.Equal(50.0m, totals[0].GetProperty("value").GetDecimal());
        Assert.Equal("2024", totals[1].GetProperty("period").GetString());
        Assert.Equal(75.0m, totals[1].GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task SummarizeProductSalesTimeline_FromStoredResult_ReturnsQuantityByMonth()
    {
        var toolResultStore = new ToolResultStore();
        var handle = toolResultStore.StoreStructuredResult(new JsonArray
        {
            new JsonObject { ["InvoiceDate"] = "2024-01-05T00:00:00+10:00", ["QuantityDelivered"] = 2 },
            new JsonObject { ["InvoiceDate"] = "2024-01-20T00:00:00+10:00", ["QuantityDelivered"] = 3 },
            new JsonObject { ["InvoiceDate"] = "2024-02-10T00:00:00+10:00", ["QuantityDelivered"] = 4 },
            new JsonObject { ["InvoiceDate"] = "not-a-date", ["QuantityDelivered"] = 9 }
        });

        var tool = new SalesOrderTools();
        var json = await tool.SummarizeProductSalesTimeline(resultHandle: handle, metric: "quantity sold", period: "month");
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("QuantityDelivered", doc.RootElement.GetProperty("metric").GetString());
        Assert.Equal("month", doc.RootElement.GetProperty("period").GetString());
        Assert.Equal(4, doc.RootElement.GetProperty("sourceRowCount").GetInt32());
        Assert.Equal(3, doc.RootElement.GetProperty("includedRowCount").GetInt32());

        var totals = doc.RootElement.GetProperty("periodTotals");
        Assert.Equal(2, totals.GetArrayLength());
        Assert.Equal("2024-01", totals[0].GetProperty("period").GetString());
        Assert.Equal(5m, totals[0].GetProperty("value").GetDecimal());
        Assert.Equal("2024-02", totals[1].GetProperty("period").GetString());
        Assert.Equal(4m, totals[1].GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task SummarizeProductSales_RejectsUnsupportedMetric()
    {
        var toolResultStore = new ToolResultStore();
        var handle = toolResultStore.StoreStructuredResult(new JsonArray
        {
            new JsonObject { ["PartNo"] = "A100", ["Description"] = "Widget A", ["QuantityDelivered"] = 2, ["LocalLineTotalInc"] = 20.5m }
        });

        var tool = new SalesOrderTools();
        var json = await tool.SummarizeProductSales(resultHandle: handle, metric: "margin", top: 1);
        Assert.Contains("Unsupported metric", json, StringComparison.Ordinal);
    }
}
