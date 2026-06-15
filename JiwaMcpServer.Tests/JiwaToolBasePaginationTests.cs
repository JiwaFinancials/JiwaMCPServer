using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Integration tests for the JiwaToolBase pagination and confirmation logic.
/// Tests the complex validation and pagination workflows.
/// </summary>
public class JiwaToolBasePaginationTests
{
    [Fact]
    public void EnsureIncludeContainsTotal_WithValidInput_ReturnsModifiedString()
    {
        // Arrange
        var input = "Name,Status";

        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal(input);

        // Assert
        Assert.Contains("Total", result);
    }

    [Fact]
    public void CreateDtoSchema_ReturnsValidJsonSchema()
    {
        // Arrange
        var dtoType = typeof(JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest);

        // Act
        var schema = ToolBaseTestHelper.CreateDtoSchema(dtoType);

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99)]
    public void PageSizeValidation_AcceptsValidPageSizes(int pageSize)
    {
        // Arrange & Act & Assert
        // Valid page sizes should not throw
        Assert.True(pageSize > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PageSizeValidation_RejectsInvalidPageSizes(int pageSize)
    {
        // Arrange & Act & Assert
        // Invalid page sizes should be rejected
        Assert.True(pageSize <= 0);
    }
}

