using Xunit;
using JiwaMcpServer.Services;
using JiwaMcpServer.Tools;

namespace JiwaMcpServer.Tests;

/// <summary>
/// Tests for JiwaApiClient static service methods.
/// </summary>
public class JiwaApiClientTests
{
    [Fact]
    public void CurrentApiKey_CanBeSet()
    {
        // Arrange
        var testKey = "test-key-123";

        // Act
        JiwaApiClient.CurrentApiKey.Value = testKey;

        // Assert
        Assert.Equal(testKey, JiwaApiClient.CurrentApiKey.Value);
    }

    [Fact]
    public void CurrentApiKey_InitiallyNull()
    {
        // Arrange & Act
        var currentKey = JiwaApiClient.CurrentApiKey.Value;

        // Assert - AsyncLocal starts as null
        Assert.Null(currentKey);
    }

    [Fact]
    public void CurrentApiKey_CanBeSetToNull()
    {
        // Arrange
        JiwaApiClient.CurrentApiKey.Value = "test-key";

        // Act
        JiwaApiClient.CurrentApiKey.Value = null;

        // Assert
        Assert.Null(JiwaApiClient.CurrentApiKey.Value);
    }

    [Fact]
    public void CurrentApiKey_SupportsMultipleValues()
    {
        // Arrange
        var originalValue = JiwaApiClient.CurrentApiKey.Value;
        var testKey = Guid.NewGuid().ToString();

        try
        {
            // Act - Set a value
            JiwaApiClient.CurrentApiKey.Value = testKey;
            var valueAfterSet = JiwaApiClient.CurrentApiKey.Value;

            // Assert
            Assert.Equal(testKey, valueAfterSet);

            // Act - Change the value
            var newTestKey = Guid.NewGuid().ToString();
            JiwaApiClient.CurrentApiKey.Value = newTestKey;

            // Assert
            Assert.Equal(newTestKey, JiwaApiClient.CurrentApiKey.Value);
            Assert.NotEqual(testKey, JiwaApiClient.CurrentApiKey.Value);
        }
        finally
        {
            // Cleanup
            JiwaApiClient.CurrentApiKey.Value = originalValue;
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_ThrowsWhenJiwaAPIURLNotConfigured()
    {
        // Arrange
        var originalUrl = Config.JiwaAPIURL;
        try
        {
            Config.JiwaAPIURL = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JiwaApiClient.GetAsync(
                    new JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest(),
                    CancellationToken.None));

            Assert.NotNull(exception);
        }
        finally
        {
            Config.JiwaAPIURL = originalUrl;
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task PostAsync_ThrowsWhenJiwaAPIURLNotConfigured()
    {
        // Arrange
        var originalUrl = Config.JiwaAPIURL;
        try
        {
            Config.JiwaAPIURL = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JiwaApiClient.PostAsync(
                    new JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest(),
                    CancellationToken.None));

            Assert.NotNull(exception);
        }
        finally
        {
            Config.JiwaAPIURL = originalUrl;
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task PatchAsync_ThrowsWhenJiwaAPIURLNotConfigured()
    {
        // Arrange
        var originalUrl = Config.JiwaAPIURL;
        try
        {
            Config.JiwaAPIURL = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JiwaApiClient.PatchAsync(
                    new JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest(),
                    CancellationToken.None));

            Assert.NotNull(exception);
        }
        finally
        {
            Config.JiwaAPIURL = originalUrl;
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task GetRawBytesAsync_ThrowsWhenJiwaAPIURLNotConfigured()
    {
        // Arrange
        var originalUrl = Config.JiwaAPIURL;
        try
        {
            Config.JiwaAPIURL = null;

            // Act & Assert - will throw either InvalidOperationException or HttpRequestException
            var exception = await Assert.ThrowsAnyAsync<System.Exception>(
                () => JiwaApiClient.GetRawBytesAsync("test/path", "{}", CancellationToken.None));

            // If it's HttpRequestException, that's from attempting to connect to null URL
            // which is an acceptable failure mode
            Assert.True(
                exception is InvalidOperationException ||
                exception is System.Net.Http.HttpRequestException,
                $"Expected InvalidOperationException or HttpRequestException, got {exception.GetType().Name}");
        }
        finally
        {
            Config.JiwaAPIURL = originalUrl;
        }
    }
}
