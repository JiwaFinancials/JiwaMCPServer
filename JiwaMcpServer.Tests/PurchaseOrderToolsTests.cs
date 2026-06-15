using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for PurchaseOrderTools - purchase order operations.
/// </summary>
public class PurchaseOrderToolsTests
{
    private readonly PurchaseOrderTools _purchaseOrderTools = new();

    [Fact]
    public async System.Threading.Tasks.Task GetPurchaseOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrderGETRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.GetPurchaseOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreatePurchaseOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrderPOSTRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.CreatePurchaseOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task ModifyPurchaseOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrderPATCHRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.ModifyPurchaseOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddAProductToAPurchaseOrder_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrderLinePOSTRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.AddAProductToAPurchaseOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchPurchaseInformation_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_PurchaseInformationQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.SearchPurchaseInformation(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchPurchaseOrders_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_PurchaseOrdersQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.SearchPurchaseOrders(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchPurchaseInformation_WithConfirmation_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_PurchaseInformationQuery();
        var testToken = "test-token-123";

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.SearchPurchaseInformation(
            requestDto,
            confirmLargeResultSet: true,
            confirmationToken: testToken);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchPurchaseOrders_WithConfirmation_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_PurchaseOrdersQuery();
        var testToken = "test-token-456";

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _purchaseOrderTools.SearchPurchaseOrders(
            requestDto,
            confirmLargeResultSet: true,
            confirmationToken: testToken);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetPurchaseOrder_WithCancellation_ReturnsResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrderGETRequest();
        var cts = new CancellationTokenSource();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var task = _purchaseOrderTools.GetPurchaseOrder(requestDto, cts.Token);

        // Assert
        Assert.NotNull(task);
    }
}
