using Xunit;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for the Config class which manages API configuration.
/// </summary>
public class ConfigTests
{
    [Fact]
    public void JiwaAPIURL_CanBeSet()
    {
        // Arrange
        var expectedUrl = "https://api.example.com";

        // Act
        Config.JiwaAPIURL = expectedUrl;

        // Assert
        Assert.Equal(expectedUrl, Config.JiwaAPIURL);
    }

    [Fact]
    public void JiwaAPIKey_CanBeSet()
    {
        // Arrange
        var expectedKey = "test-api-key-12345";

        // Act
        Config.JiwaAPIKey = expectedKey;

        // Assert
        Assert.Equal(expectedKey, Config.JiwaAPIKey);
    }

    [Fact]
    public void JiwaAPIURL_CanBeNull()
    {
        // Act
        Config.JiwaAPIURL = null;

        // Assert
        Assert.Null(Config.JiwaAPIURL);
    }

    [Fact]
    public void JiwaAPIKey_CanBeNull()
    {
        // Act
        Config.JiwaAPIKey = null;

        // Assert
        Assert.Null(Config.JiwaAPIKey);
    }

    [Theory]
    [InlineData("https://api.local:8080")]
    [InlineData("http://localhost")]
    [InlineData("https://api.jiwa.com/service")]
    public void JiwaAPIURL_AcceptsVariousValidUrls(string url)
    {
        // Act
        Config.JiwaAPIURL = url;

        // Assert
        Assert.Equal(url, Config.JiwaAPIURL);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void JiwaAPIURL_CanBeSetToEmptyOrWhitespace(string url)
    {
        // Act
        Config.JiwaAPIURL = url;

        // Assert
        Assert.Equal(url, Config.JiwaAPIURL);
    }
}
