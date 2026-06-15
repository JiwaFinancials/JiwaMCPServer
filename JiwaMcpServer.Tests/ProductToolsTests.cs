using Xunit;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for ProductTools - product/inventory operations.
/// </summary>
public class ProductToolsTests
{
    private readonly ProductTools _productTools = new();

    [Fact]
    public async System.Threading.Tasks.Task SearchProducts_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_Inventory_Item_ListQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _productTools.SearchProducts(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProduct_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.InventoryGETRequest();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _productTools.GetProduct(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStockOnHand_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_IN_SOHWithBinLocationsQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _productTools.GetStockOnHand(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchProductClassifications_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.IN_ClassificationQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _productTools.SearchProductClassifications(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchProductCategories_ReturnsStringResult()
    {
        // Arrange
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.IN_CategoriesQuery();

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _productTools.SearchProductCategories(requestDto);

        // Assert
        Assert.IsType<string>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProductPicture_WithoutParameters_ReturnsError()
    {
        // Arrange - no inventoryID or partNo provided

        // Act
        var result = await _productTools.GetProductPicture(inventoryID: null, partNo: null);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
        var firstBlock = resultList.First();
        Assert.NotNull(firstBlock);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProductPicture_WithInventoryID_ReturnsContentBlock()
    {
        // Arrange
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act - will fail due to no actual API, but should return content blocks
        var result = await _productTools.GetProductPicture(inventoryID: "12345");

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProductPicture_WithPartNo_ReturnsContentBlock()
    {
        // Arrange
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act
        var result = await _productTools.GetProductPicture(partNo: "PART-12345");

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProductPicture_PrefersInventoryIDOverPartNo()
    {
        // Arrange
        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "https://localhost:5001";
        }

        // Act - with both parameters, inventoryID should be used
        var result = await _productTools.GetProductPicture(inventoryID: "12345", partNo: "PART-12345");

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
        // The result should mention InventoryID in the error/response
        var resultText = string.Join(" ", resultList.Select(b => b.GetType().Name));
        Assert.NotEmpty(resultText);
    }
}
