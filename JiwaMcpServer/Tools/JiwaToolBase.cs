using JiwaMcpServer.Services;
using ServiceStack;
using ServiceStack.Metadata;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiwaMcpServer.Tools;

/// <summary>
/// Convenience base that resolves JiwaApiClient from the DI container.
/// MCP tool classes are instantiated per-call, so we resolve via IServiceProvider.
/// </summary>
public abstract class JiwaToolBase
{
    private const int InlineSearchResultThreshold = 100;
    private const int PreviewSearchResultCount = 10;
    private static readonly Assembly JiwaDtosAssembly = typeof(JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest).Assembly;
    private static readonly JsonSerializerOptions ToolJsonOptions = new() { WriteIndented = true };
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

    protected static string CreateSearchResponseJson<T>(IReadOnlyCollection<T> results, int pageSize)
    {
        var allResults = results.ToList();
        if (allResults.Count <= InlineSearchResultThreshold)
        {
            var inlineResponse = new
            {
                total = allResults.Count,
                returned = allResults.Count,
                pageSize,
                truncated = false,
                results = allResults
            };

            return JsonSerializer.Serialize(inlineResponse);
        }

        var previewResults = allResults.Take(PreviewSearchResultCount).ToList();
        var structuredResult = JsonSerializer.SerializeToNode(allResults) ?? new JsonArray();
        var resultHandle = StructuredResultHandleStore.Store(structuredResult);
        var response = new
        {
            text = $"Showing the first {previewResults.Count} of {allResults.Count} records. The full result set is stored on the server for follow-up actions and exports.",
            total = allResults.Count,
            returned = previewResults.Count,
            pageSize,
            truncated = true,
            results = previewResults,
            resultHandle,
            message = $"Showing only the first {previewResults.Count} records in chat. Use the stored result handle for full exports or follow-up actions."
        };

        return JsonSerializer.Serialize(response);
    }

    protected static string CreateSearchResponseJson<T>(IReadOnlyCollection<T> results, int pageSize, string? requestedFields)
        => CreateSearchResponseJson(results, pageSize, requestedFields, defaultFields: null);

    protected static string CreateSearchResponseJson<T>(IReadOnlyCollection<T> results, int pageSize, string? requestedFields, IReadOnlyCollection<string>? defaultFields)
        => CreateSearchResponseJson(ApplyRequestedFieldSelection(results, ResolveRequestedFields(requestedFields, defaultFields)), pageSize);

    protected static JsonObject AggregateStructuredResultsByField(JsonNode? structuredResult, string groupByField, string? groupByTransform = null)
    {
        if (string.IsNullOrWhiteSpace(groupByField))
            throw new ArgumentException("groupByField is required.", nameof(groupByField));

        var rows = structuredResult switch
        {
            JsonArray array => array,
            JsonObject obj when obj.TryGetPropertyValue("results", out var results) && results is JsonArray resultsArray => resultsArray,
            _ => null
        };

        if (rows is null)
            throw new InvalidOperationException("Structured result must be a JSON array or an object containing a 'results' array.");

        var normalizedTransform = string.IsNullOrWhiteSpace(groupByTransform) ? "none" : groupByTransform.Trim();
        var groups = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row is not JsonObject obj)
                continue;

            var key = ExtractAggregationKey(obj, groupByField, normalizedTransform);
            if (string.IsNullOrWhiteSpace(key))
                key = "(blank)";

            groups[key] = groups.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        var summaryGroups = new JsonArray();
        foreach (var group in groups)
        {
            summaryGroups.Add(new JsonObject
            {
                ["key"] = group.Key,
                ["count"] = group.Value
            });
        }

        return new JsonObject
        {
            ["groupByField"] = groupByField,
            ["groupByTransform"] = normalizedTransform,
            ["totalRows"] = rows.Count,
            ["groups"] = summaryGroups
        };
    }

    private static string ExtractAggregationKey(JsonObject row, string groupByField, string groupByTransform)
    {
        var match = row.FirstOrDefault(candidate => candidate.Key.Equals(groupByField, StringComparison.OrdinalIgnoreCase));
        var rawValue = match.Value?.ToString();
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        var normalizedValue = rawValue.Trim();
        return groupByTransform switch
        {
            "firstLetter" when normalizedValue.Length > 0 => normalizedValue[0].ToString(),
            "firstLetterUpper" when normalizedValue.Length > 0 => normalizedValue[0].ToString().ToUpperInvariant(),
            "lower" => normalizedValue.ToLowerInvariant(),
            "upper" => normalizedValue.ToUpperInvariant(),
            "trim" => normalizedValue,
            _ => normalizedValue
        };
    }

    private static IReadOnlyCollection<object?> ApplyRequestedFieldSelection<T>(IReadOnlyCollection<T> results, string? requestedFields)
    {
        var materializedResults = results.Cast<object?>().ToList();
        var selectedFields = ParseRequestedFields(requestedFields);
        if (selectedFields.Length == 0 || materializedResults.Count == 0)
            return materializedResults;

        var serializedResults = JsonSerializer.SerializeToNode(materializedResults) as JsonArray;
        if (serializedResults is null)
            return materializedResults;

        var filteredResults = new List<object?>(serializedResults.Count);
        var matchedAnyField = false;
        foreach (var item in serializedResults)
        {
            if (item is not JsonObject obj)
            {
                filteredResults.Add(item?.DeepClone());
                continue;
            }

            var filteredObject = new JsonObject();
            foreach (var field in selectedFields)
            {
                var property = obj.FirstOrDefault(candidate => candidate.Key.Equals(field, StringComparison.OrdinalIgnoreCase));
                if (property.Key is null)
                    continue;

                filteredObject[property.Key] = property.Value?.DeepClone();
                matchedAnyField = true;
            }

            filteredResults.Add(filteredObject.Count > 0 ? filteredObject : obj.DeepClone());
        }

        return matchedAnyField ? filteredResults : materializedResults;
    }

    private static string ResolveRequestedFields(string? requestedFields, IReadOnlyCollection<string>? defaultFields)
    {
        if (!string.IsNullOrWhiteSpace(requestedFields))
            return requestedFields;

        if (defaultFields is null || defaultFields.Count == 0)
            return string.Empty;

        return string.Join(", ", defaultFields.Where(field => !string.IsNullOrWhiteSpace(field)));
    }

    private static string[] ParseRequestedFields(string? requestedFields)
        => string.IsNullOrWhiteSpace(requestedFields)
            ? []
            : requestedFields
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

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
        catch (WebServiceException ex) when (ex.StatusCode >= 400)
        {
            // The count-only validation probe is best-effort only. Some Jiwa endpoints reject it (for example 404 on a
            // query that otherwise supports the actual fetch), and the real query should still be allowed to run.
            return null;
        }
        catch (WebServiceException ex)
        {
            return $"ERROR: Unable to validate large result set confirmation ({ex.StatusCode} {ex.StatusDescription}): {ex.ErrorMessage}";
        }
        catch (HttpRequestException)
        {
            // Same as above: if the probe cannot reach the service, avoid blocking the actual query.
            return null;
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

    protected static JsonObject PreparePatchChanges(JsonObject? changes, params string[] identifierPropertyNames)
    {
        if (changes is null || changes.Count == 0)
            throw new InvalidOperationException("At least one field change is required for PATCH operations.");

        var identifierNames = new HashSet<string>(identifierPropertyNames, StringComparer.OrdinalIgnoreCase);
        var sanitizedChanges = new JsonObject();
        foreach (var change in changes)
        {
            if (identifierNames.Contains(change.Key))
            {
                throw new InvalidOperationException($"Do not include identifier '{change.Key}' inside Changes. Provide identifiers using the dedicated parameter instead.");
            }

            sanitizedChanges[change.Key] = change.Value?.DeepClone();
        }

        return sanitizedChanges;
    }

    protected static string CreatePatchResponseJson(string entityName, JsonObject appliedChanges, object updatedRecord, params (string Name, string? Value)[] identifiers)
    {
        var identifierObject = new JsonObject();
        foreach (var (name, value) in identifiers)
            identifierObject[name] = value;

        var changedFieldNames = new JsonArray();
        foreach (var change in appliedChanges)
            changedFieldNames.Add(change.Key);

        var fullResult = JsonSerializer.SerializeToNode(updatedRecord) ?? new JsonObject();
        var resultHandle = StructuredResultHandleStore.Store(fullResult);
        var response = new JsonObject
        {
            ["status"] = "updated",
            ["entity"] = entityName,
            ["identifiers"] = identifierObject,
            ["changedFields"] = appliedChanges.DeepClone(),
            ["changedFieldNames"] = changedFieldNames,
            ["resultHandle"] = resultHandle,
            ["message"] = $"Updated {entityName}. Only changed fields are shown here. Use the stored result handle or the corresponding GET tool if you need the full current record."
        };

        return JsonSerializer.Serialize(response, ToolJsonOptions);
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
            var result = await action();
            return TabularResultMetadataHelper.EnrichJson(result);
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
