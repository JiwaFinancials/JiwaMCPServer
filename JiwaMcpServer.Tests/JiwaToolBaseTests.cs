using Xunit;
using JiwaMcpServer.Tools;
using System.Reflection;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for JiwaToolBase utility methods.
/// </summary>
public class JiwaToolBaseTests
{
    [Fact]
    public void ResolveJiwaDtoType_ThrowsWhenDtoTypeNameIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ToolBaseTestHelper.ResolveJiwaDtoType(null!));

        Assert.Contains("dtoTypeName is required", exception.Message);
    }

    [Fact]
    public void ResolveJiwaDtoType_ThrowsWhenDtoTypeNameIsEmpty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ToolBaseTestHelper.ResolveJiwaDtoType(""));

        Assert.Contains("dtoTypeName is required", exception.Message);
    }

    [Fact]
    public void ResolveJiwaDtoType_ThrowsWhenDtoTypeNameIsWhitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ToolBaseTestHelper.ResolveJiwaDtoType("   "));

        Assert.Contains("dtoTypeName is required", exception.Message);
    }

    [Fact]
    public void ResolveJiwaDtoType_FindsValidDtoByName()
    {
        // Act
        var dtoType = ToolBaseTestHelper.ResolveJiwaDtoType("DebtorGETRequest");

        // Assert
        Assert.NotNull(dtoType);
        Assert.Equal("DebtorGETRequest", dtoType.Name);
    }

    [Fact]
    public void ResolveJiwaDtoType_FindsValidDtoByFullyQualifiedName()
    {
        // Act
        var dtoType = ToolBaseTestHelper.ResolveJiwaDtoType("JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest");

        // Assert
        Assert.NotNull(dtoType);
        Assert.Equal("DebtorGETRequest", dtoType.Name);
    }

    [Fact]
    public void ResolveJiwaDtoType_IsCaseInsensitive()
    {
        // Act
        var dtoType1 = ToolBaseTestHelper.ResolveJiwaDtoType("debtorgetrequest");
        var dtoType2 = ToolBaseTestHelper.ResolveJiwaDtoType("DebtorGETRequest");
        var dtoType3 = ToolBaseTestHelper.ResolveJiwaDtoType("DEBTORGETREQUEST");

        // Assert
        Assert.Equal(dtoType1, dtoType2);
        Assert.Equal(dtoType2, dtoType3);
    }

    [Fact]
    public void ResolveJiwaDtoType_ThrowsWhenDtoTypeNotFound()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ToolBaseTestHelper.ResolveJiwaDtoType("NonExistentDtoType"));

        Assert.Contains("was not found in Jiwa DTOs", exception.Message);
        Assert.Contains("NonExistentDtoType", exception.Message);
    }

    [Fact]
    public void CreateDtoSchema_ReturnsJsonSchema()
    {
        // Act
        var schema = ToolBaseTestHelper.CreateDtoSchema(typeof(JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest));

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
        Assert.Contains("{", schema); // Should contain JSON
    }

    [Fact]
    public void GetJiwaDtoSchema_ReturnsSchemaBytTypeName()
    {
        // Act
        var schema = ToolBaseTestHelper.GetJiwaDtoSchema("DebtorGETRequest");

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
        Assert.Contains("{", schema);
    }

    [Fact]
    public void EnsureIncludeContainsTotal_AddsWhenMissing()
    {
        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal("Name,Status");

        // Assert
        Assert.Contains("Total", result);
        Assert.Contains("Name,Status", result);
    }

    [Fact]
    public void EnsureIncludeContainsTotal_DoesNotDuplicateWhenPresent()
    {
        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal("Name,Total,Status");

        // Assert
        Assert.Equal("Name,Total,Status", result);
    }

    [Fact]
    public void EnsureIncludeContainsTotal_IgnoresCaseWhenChecking()
    {
        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal("Name,total,Status");

        // Assert
        Assert.Equal("Name,total,Status", result);
    }

    [Fact]
    public void EnsureIncludeContainsTotal_ReturnsJustTotalWhenNull()
    {
        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal(null);

        // Assert
        Assert.Equal("Total", result);
    }

    [Fact]
    public void EnsureIncludeContainsTotal_ReturnsJustTotalWhenEmpty()
    {
        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal("");

        // Assert
        Assert.Equal("Total", result);
    }

    [Fact]
    public void EnsureIncludeContainsTotal_ReturnsJustTotalWhenWhitespace()
    {
        // Act
        var result = ToolBaseTestHelper.EnsureIncludeContainsTotal("   ");

        // Assert
        Assert.Equal("Total", result);
    }
}

/// <summary>
/// Helper class to access protected members of JiwaToolBase for testing.
/// </summary>
internal static class ToolBaseTestHelper
{
    // Get the private field JiwaDtosAssembly type reference
    private static readonly Type JiwaToolBaseType = typeof(JiwaToolBase);

    public static Type ResolveJiwaDtoType(string dtoTypeName)
    {
        var method = JiwaToolBaseType.GetMethod("ResolveJiwaDtoType", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        if (method == null)
            throw new InvalidOperationException("ResolveJiwaDtoType method not found");

        try
        {
            return (Type)method.Invoke(null, new object[] { dtoTypeName })!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public static string CreateDtoSchema(Type dtoType)
    {
        var method = JiwaToolBaseType.GetMethod("CreateDtoSchema",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        if (method == null)
            throw new InvalidOperationException("CreateDtoSchema method not found");

        try
        {
            return (string)method.Invoke(null, new object[] { dtoType })!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public static string GetJiwaDtoSchema(string dtoTypeName)
    {
        var method = JiwaToolBaseType.GetMethod("GetJiwaDtoSchema",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        if (method == null)
            throw new InvalidOperationException("GetJiwaDtoSchema method not found");

        try
        {
            return (string)method.Invoke(null, new object[] { dtoTypeName })!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public static string EnsureIncludeContainsTotal(string? include)
    {
        var method = JiwaToolBaseType.GetMethod("EnsureIncludeContainsTotal",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        if (method == null)
            throw new InvalidOperationException("EnsureIncludeContainsTotal method not found");

        try
        {
            return (string)method.Invoke(null, new object?[] { include })!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }
}
