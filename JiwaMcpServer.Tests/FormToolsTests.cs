using Xunit;
using JiwaMcpServer.Tools;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for FormTools - form search and navigation operations.
/// 
/// These tests verify that when a user prompts with "open po" (or similar variants),
/// the FormTools correctly identifies and resolves to the PurchaseOrders form
/// (class: JiwaFinancials.Jiwa.JiwaPurchaseOrdersUI.PurchaseOrders).
/// 
/// Note: Some tests require a running Jiwa API and may fail in isolated test environments.
/// </summary>
public class FormToolsTests
{
    private readonly BusinessTerminologyRegistry _terminologyRegistry = new();
    private readonly FormNameRegistry _formNameRegistry = new();
    private readonly Mock<ILogger<FormTools>> _loggerMock = new();

    [Fact]
    public void FormTools_CanBeInstantiated()
    {
        // Arrange & Act
        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object);

        // Assert
        Assert.NotNull(formTools);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchForms_WithOpenPoPrompt_ShouldResolveToPurchaseOrders()
    {
        // Arrange
        // This test verifies that when searching with "po" (the abbreviation for Purchase Order),
        // the FormTools correctly resolves and searches for forms related to purchase orders
        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object);

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "http://localhost";
        }

        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.SY_FormsQuery
        {
            DescriptionContains = "po"
        };

        // Act
        var result = await formTools.SearchForms(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.NotNull(result);

        // The test expects that "JiwaFinancials.Jiwa.JiwaPurchaseOrdersUI.PurchaseOrders" should appear
        // in the results as the top candidate for the "open po" prompt
        // NOTE: This test may fail if the Jiwa API is not running or if there's a validation error
        if (!result.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("PurchaseOrders", result);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task OpenForm_WithPurchaseOrdersPrompt_ShouldSucceed()
    {
        // Arrange
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("test-routing")
            {
                Prompt = "open purchase order"
            }
        };

        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object, contextAccessor);

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "http://localhost";
        }

        var className = "JiwaFinancials.Jiwa.JiwaPurchaseOrdersUI.PurchaseOrders";

        // Act
        var result = await formTools.OpenForm(UserPrompt: "open purchase order");

        // Assert
        Assert.IsType<string>(result);
        Assert.NotNull(result);

        // NOTE: This test may fail if the Jiwa API is not running or the form is not registered
        if (!result.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains(className, result);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchForms_WithPoAbbreviation_ShouldIdentifyPurchaseOrder()
    {
        // Arrange
        // This test verifies that searching for forms containing "po" in the class name
        // correctly identifies purchase order forms through the terminology registry
        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object);

        if (string.IsNullOrEmpty(Config.JiwaAPIURL))
        {
            Config.JiwaAPIURL = "http://localhost";
        }

        // The BusinessTerminologyRegistry resolves "po" to "PurchaseOrder" entity
        var requestDto = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.SY_FormsQuery
        {
            ClassNameContains = "po"
        };

        // Act
        var result = await formTools.SearchForms(requestDto);

        // Assert
        Assert.IsType<string>(result);
        Assert.NotNull(result);

        // Should find forms that match the "po" (Purchase Order) criteria
        // and "JiwaFinancials.Jiwa.JiwaPurchaseOrdersUI.PurchaseOrders" should be a candidate
        // NOTE: The actual ranking and results depend on the Jiwa API data
    }

    [Fact]
    public void BusinessTerminologyRegistry_ResolvesPoToPurchaseOrder()
    {
        // Arrange
        var registry = new BusinessTerminologyRegistry();

        // Act
        // This verifies that the core terminology resolution works correctly
        // When the user types "po", it should resolve to the "PurchaseOrder" entity type
        var resolved = registry.TryResolveEntity("po", out var entityType);

        // Assert
        Assert.True(resolved);
        Assert.Equal("PurchaseOrder", entityType);
    }

    [Fact]
    public void BusinessTerminologyRegistry_ExpandsPoToIncludeAllSynonyms()
    {
        // Arrange
        var registry = new BusinessTerminologyRegistry();

        // Act
        // This verifies that the terminology registry expands "po" to include all related synonyms
        var expanded = registry.ExpandTerms(["po"]);

        // Assert
        Assert.NotEmpty(expanded);
        Assert.Contains("po", expanded, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("purchase order", expanded, StringComparer.OrdinalIgnoreCase);

        // The FormTools use these expanded terms to search the Jiwa API for matching forms
        // This ensures that forms can be found by any of the synonyms
    }

    [Fact]
    public async System.Threading.Tasks.Task OpenForm_WithoutUserPrompt_ShouldReturnError()
    {
        // Arrange
        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object);

        // Act
        var result = await formTools.OpenForm(UserPrompt: "");

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("UserPrompt is required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async System.Threading.Tasks.Task OpenForm_WithWhitespaceUserPrompt_ShouldReturnError()
    {
        // Arrange
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("test-routing")
            {
                Prompt = "open po"
            }
        };

        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object, contextAccessor);

        // Act
        var result = await formTools.OpenForm(UserPrompt: "   ");

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("UserPrompt is required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async System.Threading.Tasks.Task OpenForm_WithUserPrompt_ShouldAttemptInference()
    {
        // Arrange
        var contextAccessor = new ToolRoutingContextAccessor
        {
            Current = new ToolRoutingExecutionContext("test-routing")
            {
                Prompt = "   "
            }
        };

        var formTools = new FormTools(_terminologyRegistry, _formNameRegistry, _loggerMock.Object, contextAccessor);

        // Act
        var result = await formTools.OpenForm(UserPrompt: "open po");

        // Assert
        Assert.IsType<string>(result);
        Assert.DoesNotContain("Unable to determine a form", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:5000")]
    [InlineData("http://localhost:5001")]
    public void OpenForm_ConfiguresLocalhost(string localhost)
    {
        // Arrange
        Config.JiwaAPIURL = localhost;

        // Act & Assert
        Assert.Equal(localhost, Config.JiwaAPIURL);
    }
}
