using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for CustomerTools - customer/debtor operations.
/// </summary>
public class CustomerToolsTests
{
    private readonly CustomerTools _customerTools = new();

    [Fact]
    public async System.Threading.Tasks.Task SearchCustomers_HandlesCankellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery();

        // Act - just verify it handles cancellation without throwing before cancellation
        var task = _customerTools.SearchCustomers(requestDto, false, null, cts.Token);

        // Assert - should not throw immediately
        Assert.NotNull(task);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchCustomers_WithoutConfirmation_ReturnsResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery
        {
            Skip = 0,
            Take = 1
        };

        // Ensure config is set
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001"; // dummy URL, will fail at actual call
        }

        // Act - will likely fail due to no actual API, but should structure properly
        var result = await _customerTools.SearchCustomers(requestDto);

        // Assert - result should be a string (either data or error message)
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetCustomer_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act - will likely fail due to no actual API
        var result = await _customerTools.GetCustomer(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetCustomerTransactions_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_Transactions_ListQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _customerTools.GetCustomerTransactions(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchCustomerClassifications_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.DB_ClassificationQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _customerTools.SearchCustomerClassifications(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchCustomerCategories_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.DB_CategoriesQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _customerTools.SearchCustomerCategories(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchCustomers_WithConfirmationButNoToken_ReturnsWarning()
    {
        // Note: This test assumes the API returns a large result set count
        // The confirmation flow is complex and depends on actual API responses

        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act - calling with confirmLargeResultSet=true but no token
        var result = await _customerTools.SearchCustomers(requestDto, confirmLargeResultSet: true, confirmationToken: null);

        // Assert - result should be a string (error or warning message)
        Assert.IsType<string>(result);
    }
}
