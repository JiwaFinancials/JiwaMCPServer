using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for SupplierTools - supplier/creditor operations.
/// </summary>
public class SupplierToolsTests
{
    private readonly SupplierTools _supplierTools = new();

    [Fact]
    public async System.Threading.Tasks.Task SearchSuppliers_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _supplierTools.SearchSuppliers(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSupplier_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.CreditorGETRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _supplierTools.GetSupplier(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchCreditorClassifications_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.CR_ClassificationQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _supplierTools.SearchCreditorClassifications(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchSuppliers_WithConfirmationToken_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery();
        var testToken = "test-token-123";

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _supplierTools.SearchSuppliers(
            requestDto, 
            confirmLargeResultSet: true, 
            confirmationToken: testToken);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchSuppliers_WithCancellation_ReturnsResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery();
        var cts = new CancellationTokenSource();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var task = _supplierTools.SearchSuppliers(requestDto, ct: cts.Token);

        // Assert
        Assert.NotNull(task);
    }
}
