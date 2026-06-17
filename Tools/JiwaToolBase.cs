using JiwaMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Metadata;
using System.Collections.Concurrent;
using System.Reflection;

namespace JiwaMcpServer.Tools;

/// <summary>
/// Convenience base that resolves JiwaApiClient from the DI container.
/// MCP tool classes are instantiated per-call, so we resolve via IServiceProvider.
/// </summary>
public abstract class JiwaToolBase
{
    private static readonly Assembly JiwaDtosAssembly = typeof(JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest).Assembly;
    private const string JiwaDtoNamespacePrefix = "JiwaFinancials.Jiwa.JiwaServiceModel";
    private static readonly ConcurrentDictionary<string, (string QueryFingerprint, DateTimeOffset ExpiresAt)> PendingLargeResultSetConfirmations = new();
    private static readonly ConcurrentDictionary<string, DateTimeOffset> ConfirmedLargeResultSetQueries = new();

    protected static string CreateDtoSchema(Type dtoType)
    {
        JsonMetadataHandler jsonMetadataHandler = new JsonMetadataHandler();
        return jsonMetadataHandler.CreateResponse(dtoType);
    }

    protected static Type ResolveJiwaDtoType(string dtoTypeName)
    {
        if (string.IsNullOrWhiteSpace(dtoTypeName))
            throw new ArgumentException("dtoTypeName is required.", nameof(dtoTypeName));

        var matches = JiwaDtosAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                t.Namespace != null &&
                t.Namespace.StartsWith(JiwaDtoNamespacePrefix, StringComparison.Ordinal) &&
                (
                    string.Equals(t.FullName, dtoTypeName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Name, dtoTypeName, StringComparison.OrdinalIgnoreCase)
                ))
            .ToList();

        if (matches.Count == 0)
            throw new ArgumentException($"DTO type '{dtoTypeName}' was not found in Jiwa DTOs.", nameof(dtoTypeName));

        if (matches.Count > 1)
            throw new ArgumentException($"Multiple DTO types named '{dtoTypeName}' were found. Use fully qualified type name.", nameof(dtoTypeName));

        return matches[0];
    }

    protected static string GetJiwaDtoSchema(string dtoTypeName)
    {
        var dtoType = ResolveJiwaDtoType(dtoTypeName);
        return CreateDtoSchema(dtoType);
    }

    protected static async Task<List<T>> GetAllQueryResultsAsync<T>(QueryDb<T> requestDTO, int pageSize, CancellationToken ct = default)
    {
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var originalTake = requestDTO.Take;
        var originalSkip = requestDTO.Skip;

        try
        {
            var allResults = new List<T>();
            requestDTO.Take = pageSize;
            requestDTO.Skip = 0;

            while (true)
            {
                var response = await JiwaApiClient.GetAsync(requestDTO, ct);

                if (response.Results == null || response.Results.Count == 0)
                    break;

                allResults.AddRange(response.Results);

                if (response.Total > 0 && allResults.Count >= response.Total)
                    break;

                if (response.Results.Count < pageSize)
                    break;

                requestDTO.Skip += pageSize;
            }

            return allResults;
        }
        finally
        {
            requestDTO.Take = originalTake;
            requestDTO.Skip = originalSkip;
        }
    }

    protected static async Task<string?> ValidateLargeResultSetConfirmationAsync<T>(
        QueryDb<T> requestDTO,
        bool confirmLargeResultSet,
        string? confirmationToken,
        CancellationToken ct,
        int warningThreshold = 100,
        TimeSpan? confirmationTokenLifetime = null)
    {
        var originalTake = requestDTO.Take;
        var originalSkip = requestDTO.Skip;
        var originalInclude = requestDTO.Include;
        var tokenLifetime = confirmationTokenLifetime ?? TimeSpan.FromMinutes(5);

        try
        {
            requestDTO.Take = 0;
            requestDTO.Skip = 0;
            requestDTO.Include = EnsureIncludeContainsTotal(requestDTO.Include);

            var queryFingerprint = requestDTO.ToJson();
            var countResponse = await JiwaApiClient.GetAsync(requestDTO, ct);

            if (countResponse.Total <= warningThreshold)
                return null;

            var now = DateTimeOffset.UtcNow;
            RemoveExpiredConfirmationTokens(now);

            if (ConfirmedLargeResultSetQueries.TryGetValue(queryFingerprint, out var confirmedUntil) && confirmedUntil > now)
                return null;

            if (!confirmLargeResultSet)
            {
                var issuedToken = Guid.NewGuid().ToString("N");
                var expiresAt = now.Add(tokenLifetime);
                PendingLargeResultSetConfirmations[issuedToken] = (queryFingerprint, expiresAt);

                return $"WARNING: This query will return {countResponse.Total} records which exceeds the threshold of {warningThreshold}. " +
                       $"Please confirm with the user before proceeding. To proceed, call this tool again with confirmLargeResultSet=true and confirmationToken='{issuedToken}'. " +
                       $"This token expires in {(int)tokenLifetime.TotalMinutes} minutes.";
            }

            if (string.IsNullOrWhiteSpace(confirmationToken) ||
                !PendingLargeResultSetConfirmations.TryRemove(confirmationToken, out var pendingConfirmation) ||
                pendingConfirmation.ExpiresAt <= now ||
                !string.Equals(pendingConfirmation.QueryFingerprint, queryFingerprint, StringComparison.Ordinal))
            {
                return "ERROR: Missing or invalid confirmation token for large result set. Call again with confirmLargeResultSet=false to receive a new token.";
            }

            ConfirmedLargeResultSetQueries[queryFingerprint] = pendingConfirmation.ExpiresAt;
            return null;
        }
        catch (WebServiceException ex)
        {
            return $"ERROR: Unable to validate large result set confirmation ({ex.StatusCode} {ex.StatusDescription}): {ex.ErrorMessage}";
        }
        catch (Exception ex)
        {
            return $"ERROR: Unable to validate large result set confirmation: {ex.Message}";
        }
        finally
        {
            requestDTO.Take = originalTake;
            requestDTO.Skip = originalSkip;
            requestDTO.Include = originalInclude;
        }
    }

    protected static string EnsureIncludeContainsTotal(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
            return "Total";

        return include.Contains("Total", StringComparison.OrdinalIgnoreCase)
            ? include
            : $"{include},Total";
    }

    private static void RemoveExpiredConfirmationTokens(DateTimeOffset now)
    {
        foreach (var pending in PendingLargeResultSetConfirmations)
        {
            if (pending.Value.ExpiresAt <= now)
                PendingLargeResultSetConfirmations.TryRemove(pending.Key, out _);
        }

        foreach (var confirmed in ConfirmedLargeResultSetQueries)
        {
            if (confirmed.Value <= now)
                ConfirmedLargeResultSetQueries.TryRemove(confirmed.Key, out _);
        }
    }

    protected static async Task<string> InvokeToolAsync(Func<Task<string>> action)
    {
        try
        {
            return await action();
        }
        catch (WebServiceException ex)
        {
            return $"Error: {ex.StatusCode} {ex.StatusDescription} - {ex.ErrorMessage}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
