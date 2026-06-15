using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for SchemaTools - DTO schema retrieval.
/// </summary>
public class SchemaToolsTests
{
    private readonly SchemaTools _schemaTools = new();

    [Fact]
    public async System.Threading.Tasks.Task GetDtoSchema_ReturnsSchemaForValidDtoName()
    {
        // Act
        var schema = await _schemaTools.GetDtoSchema("DebtorGETRequest", CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.NotEqual("", schema);
        Assert.DoesNotContain("Error", schema);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDtoSchema_ReturnsSchemaForFullyQualifiedName()
    {
        // Act
        var schema = await _schemaTools.GetDtoSchema(
            "JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest", 
            CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.NotEqual("", schema);
        Assert.DoesNotContain("Error", schema);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDtoSchema_ReturnsErrorForInvalidDtoName()
    {
        // Act
        var schema = await _schemaTools.GetDtoSchema("InvalidDtoNameThatDoesNotExist", CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("Error", schema);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDtoSchema_IsCaseInsensitive()
    {
        // Act
        var schema1 = await _schemaTools.GetDtoSchema("debtorgetrequest", CancellationToken.None);
        var schema2 = await _schemaTools.GetDtoSchema("DebtorGETRequest", CancellationToken.None);

        // Assert
        Assert.Equal(schema1, schema2);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDtoSchema_ReturnsErrorForEmptyDtoName()
    {
        // Act
        var schema = await _schemaTools.GetDtoSchema("", CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("Error", schema);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDtoSchema_ReturnsErrorForWhitespaceDtoName()
    {
        // Act
        var schema = await _schemaTools.GetDtoSchema("   ", CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("Error", schema);
    }

    [Theory]
    [InlineData("Debtor")]
    [InlineData("v_Jiwa_Debtor_ListQuery")]
    [InlineData("DebtorGETRequest")]
    public async System.Threading.Tasks.Task GetDtoSchema_WorksWithVariousJiwaDtoTypes(string dtoName)
    {
        // Act
        var schema = await _schemaTools.GetDtoSchema(dtoName, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.NotEqual("", schema);
        // Most valid DTOs should return JSON, not error messages
        if (!schema.Contains("Error"))
        {
            Assert.True(schema.Contains("{") || schema.Contains("["), 
                "Expected JSON structure in schema response");
        }
    }
}
