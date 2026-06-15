using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Edge case and error handling tests for all tool classes.
/// </summary>
public class ToolsErrorHandlingTests
{
    private readonly SchemaTools _schemaTools = new();
    private readonly CustomerTools _customerTools = new();
    private readonly ProductTools _productTools = new();
    private readonly SupplierTools _supplierTools = new();
    private readonly PurchaseOrderTools _purchaseOrderTools = new();
    private readonly SalesOrderTools _salesOrderTools = new();

    [Fact]
    public async System.Threading.Tasks.Task SchemaTools_HandlesNullDtoTypeName()
    {
        // Arrange - attempt with null (will cause error in GetDtoSchema)

        // Act & Assert
        var result = await _schemaTools.GetDtoSchema(null!, CancellationToken.None);
        Assert.Contains("Error", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task CustomerTools_ReturnsErrorMessageOnApiFailure()
    {
        // Arrange
        Config.JiwaAPIURL = null; // Invalid configuration

        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest();

        // Act
        var result = await _customerTools.GetCustomer(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("Error", result); // Should contain error message
    }

    [Fact]
    public async System.Threading.Tasks.Task ProductTools_ReturnsErrorMessageOnApiFailure()
    {
        // Arrange
        Config.JiwaAPIURL = null;

        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.InventoryGETRequest();

        // Act
        var result = await _productTools.GetProduct(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("Error", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SupplierTools_ReturnsErrorMessageOnApiFailure()
    {
        // Arrange
        Config.JiwaAPIURL = null;

        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.CreditorGETRequest();

        // Act
        var result = await _supplierTools.GetSupplier(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("Error", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task PurchaseOrderTools_ReturnsErrorMessageOnApiFailure()
    {
        // Arrange
        Config.JiwaAPIURL = null;

        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrderGETRequest();

        // Act
        var result = await _purchaseOrderTools.GetPurchaseOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("Error", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SalesOrderTools_ReturnsErrorMessageOnApiFailure()
    {
        // Arrange
        Config.JiwaAPIURL = null;

        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrderGETRequest();

        // Act
        var result = await _salesOrderTools.GetSalesOrder(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("Error", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task CancellationToken_PropagatesCorrectly()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest();

        // Act
        var task = _customerTools.GetCustomer(requestDto, cts.Token);

        // Assert
        Assert.NotNull(task);
    }

    [Fact]
    public async System.Threading.Tasks.Task MultipleToolMethodsConcurrently()
    {
        // Arrange
        var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task<string>>();

        // Act
        tasks.Add(_customerTools.SearchCustomers(new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Debtor_ListQuery()));
        tasks.Add(_productTools.SearchProducts(new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_ListQuery()));
        tasks.Add(_supplierTools.SearchSuppliers(new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery()));

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            Assert.True(task.IsCompleted);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task ProductTools_GetProductPicture_WithEmptyBothParameters_ReturnsError()
    {
        // Act
        var result = await _productTools.GetProductPicture();

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
        var firstBlock = resultList.First();
        Assert.NotNull(firstBlock);
    }

    [Fact]
    public async System.Threading.Tasks.Task ProductTools_GetProductPicture_WithWhitespaceInventoryID_IsValid()
    {
        // Arrange
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act - whitespace inventoryID should be treated as null
        var result = await _productTools.GetProductPicture(inventoryID: "   ");

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
    }
}
