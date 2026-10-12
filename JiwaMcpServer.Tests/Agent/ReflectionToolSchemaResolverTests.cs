using JiwaMcpServer.Agent;
using JiwaMcpServer.Agent.SchemaResolver;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tests.Agent;

public class ReflectionToolSchemaResolverTests
{
    private readonly ReflectionToolSchemaResolver _resolver = new(
        new ToolRegistry([typeof(AgentServiceExtensions).Assembly]),
        NullLogger<ReflectionToolSchemaResolver>.Instance);

    [Fact]
    public async Task ResolveAsync_DocumentTool_ExportsDetailedRequestSchema()
    {
        var schemas = await _resolver.ResolveAsync(["AddCreditorPurchaseDocument"]);

        var schema = Assert.IsType<JsonObject>(schemas["AddCreditorPurchaseDocument"].ParametersSchema);
        var rootProperties = Assert.IsType<JsonObject>(schema["properties"]);
        var requestDto = Assert.IsType<JsonObject>(rootProperties["requestDTO"]);
        var requestProperties = Assert.IsType<JsonObject>(requestDto["properties"]);

        Assert.Contains("batchID", requestProperties.Select(p => p.Key));
        Assert.Contains("documentType", requestProperties.Select(p => p.Key));
        Assert.Contains("fileBinary", requestProperties.Select(p => p.Key));
    }

    [Fact]
    public async Task ResolveAsync_LineTool_ExportsArrayCollectionsAsArrays()
    {
        var schemas = await _resolver.ResolveAsync(["AddCreditorPurchaseLine"]);

        var schema = Assert.IsType<JsonObject>(schemas["AddCreditorPurchaseLine"].ParametersSchema);
        var requestDto = Assert.IsType<JsonObject>(Assert.IsType<JsonObject>(schema["properties"])["requestDTO"]);
        var requestProperties = Assert.IsType<JsonObject>(requestDto["properties"]);
        var dispersals = Assert.IsType<JsonObject>(requestProperties["dispersals"]);

        Assert.Equal("array", dispersals["type"]?.GetValue<string>());
        Assert.NotNull(dispersals["items"]);
    }

    [Fact]
    public async Task ResolveAsync_DeleteProduct_AllowsInventoryIdOrPartNo()
    {
        var schemas = await _resolver.ResolveAsync(["DeleteProduct"]);

        var schema = Assert.IsType<JsonObject>(schemas["DeleteProduct"].ParametersSchema);
        var requestDto = Assert.IsType<JsonObject>(Assert.IsType<JsonObject>(schema["properties"])["requestDTO"]);
        var requestProperties = Assert.IsType<JsonObject>(requestDto["properties"]);

        Assert.Contains("inventoryID", requestProperties.Select(p => p.Key));
        Assert.Contains("partNo", requestProperties.Select(p => p.Key));
    }

    [Fact]
    public async Task ResolveAsync_GetCustomer_AllowsDebtorIdOrIdentifier()
    {
        var schemas = await _resolver.ResolveAsync(["GetCustomer"]);

        var schema = Assert.IsType<JsonObject>(schemas["GetCustomer"].ParametersSchema);
        var requestDto = Assert.IsType<JsonObject>(Assert.IsType<JsonObject>(schema["properties"])["requestDTO"]);
        var requestProperties = Assert.IsType<JsonObject>(requestDto["properties"]);

        Assert.Contains("debtorID", requestProperties.Select(p => p.Key));
        Assert.Contains("identifier", requestProperties.Select(p => p.Key));
    }

    [Fact]
    public async Task ResolveAsync_ModifyCustomer_AllowsDebtorIdOrIdentifier()
    {
        var schemas = await _resolver.ResolveAsync(["ModifyCustomer"]);

        var schema = Assert.IsType<JsonObject>(schemas["ModifyCustomer"].ParametersSchema);
        var requestDto = Assert.IsType<JsonObject>(Assert.IsType<JsonObject>(schema["properties"])["requestDTO"]);
        var requestProperties = Assert.IsType<JsonObject>(requestDto["properties"]);

        Assert.Contains("debtorID", requestProperties.Select(p => p.Key));
        Assert.Contains("identifier", requestProperties.Select(p => p.Key));
        Assert.Contains("changes", requestProperties.Select(p => p.Key));
    }

    [Fact]
    public async Task ResolveAsync_DeleteCustomer_AllowsDebtorIdOrIdentifier()
    {
        var schemas = await _resolver.ResolveAsync(["DeleteCustomer"]);

        var schema = Assert.IsType<JsonObject>(schemas["DeleteCustomer"].ParametersSchema);
        var requestDto = Assert.IsType<JsonObject>(Assert.IsType<JsonObject>(schema["properties"])["requestDTO"]);
        var requestProperties = Assert.IsType<JsonObject>(requestDto["properties"]);

        Assert.Contains("debtorID", requestProperties.Select(p => p.Key));
        Assert.Contains("identifier", requestProperties.Select(p => p.Key));
    }
}
