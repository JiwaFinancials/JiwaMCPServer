using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for SalesOrderTools - sales order operations.
/// </summary>
public class SalesOrderToolsTests
{
    private readonly SalesOrderTools _salesOrderTools = new();

    [Fact]
    public async System.Threading.Tasks.Task GetSalesOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrderGETRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.GetSalesOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateSalesOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrderPOSTRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.CreateSalesOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task ModifySalesOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrderPATCHRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.ModifySalesOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddAProductToASalesOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrderLinePOSTRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.AddAProductToASalesOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchSalesInformation_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.SearchSalesInformation(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchSalesOrders_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesOrdersQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.SearchSalesOrders(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchSalesInformation_WithConfirmation_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesInformationQuery();
        var testToken = "test-token-789";

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.SearchSalesInformation(
            requestDto,
            confirmLargeResultSet: true,
            confirmationToken: testToken);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchSalesOrders_WithConfirmation_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_SalesOrdersQuery();
        var testToken = "test-token-101112";

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _salesOrderTools.SearchSalesOrders(
            requestDto,
            confirmLargeResultSet: true,
            confirmationToken: testToken);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSalesOrder_WithCancellation_ReturnsResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrderGETRequest();
        var cts = new CancellationTokenSource();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var task = _salesOrderTools.GetSalesOrder(requestDto, cts.Token);

        // Assert
        Assert.NotNull(task);
    }
}
