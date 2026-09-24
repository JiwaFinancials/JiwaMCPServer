using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Agent.Routing;

public sealed class ToolRoutingContextService : IToolRoutingContextService
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly IToolCatalog _toolCatalog;
    private readonly IIntentExtractor _intentExtractor;
    private readonly ConcurrentDictionary<string, WorkingContext> _contexts = new(StringComparer.OrdinalIgnoreCase);

    public ToolRoutingContextService(IToolCatalog toolCatalog, IIntentExtractor intentExtractor)
    {
        _toolCatalog = toolCatalog;
        _intentExtractor = intentExtractor;
    }

    public WorkingContext ResolveForRouting(ToolRouteRequest request)
    {
        var safeRequest = request ?? new ToolRouteRequest();
        var persisted = GetPersistedContext(safeRequest.ConversationId);
        var merged = MergeContexts(persisted, safeRequest.WorkingContext);

        var requestSummary = FirstNonEmpty(safeRequest.ConversationSummary, safeRequest.WorkingContext?.ConversationSummary);
        merged.ConversationSummary = !string.IsNullOrWhiteSpace(requestSummary)
            ? Summarize(requestSummary)
            : string.IsNullOrWhiteSpace(merged.ConversationSummary)
                ? SummarizeLegacyConversationContext(safeRequest.ConversationContext)
                : merged.ConversationSummary;

        if (string.IsNullOrWhiteSpace(merged.CurrentDomain) && merged.FocusedEntity is not null)
            merged.CurrentDomain = RoutingVocabulary.ResolveDomainForEntityType(merged.FocusedEntity.EntityType);

        PersistIfConversationScoped(safeRequest.ConversationId, merged);
        return CloneContext(merged)!;
    }

    public WorkingContext UpdateAfterToolExecution(ToolExecutionContextUpdateRequest request)
    {
        var safeRequest = request ?? new ToolExecutionContextUpdateRequest();
        var current = MergeContexts(GetPersistedContext(safeRequest.ConversationId), safeRequest.WorkingContext);
        var intent = _intentExtractor.Extract(safeRequest.UserMessage, current);
        var tool = _toolCatalog.GetAllTools().FirstOrDefault(candidate => candidate.Name.Equals(safeRequest.ToolName, StringComparison.OrdinalIgnoreCase));
        var focusedEntity = safeRequest.Successful
            ? ResolveFocusedEntity(tool, safeRequest.ToolArgumentsJson, safeRequest.ToolResultText, current.FocusedEntity)
            : current.FocusedEntity;

        var resolvedDomain = FirstNonEmpty(
            tool?.Metadata.Domain,
            tool?.Domain,
            current.CurrentDomain,
            focusedEntity is null ? string.Empty : RoutingVocabulary.ResolveDomainForEntityType(focusedEntity.EntityType));

        var updated = new WorkingContext
        {
            FocusedEntity = focusedEntity,
            CurrentDomain = resolvedDomain,
            LastIntent = DescribeIntent(intent),
            LastToolName = safeRequest.ToolName ?? string.Empty,
            ConversationSummary = BuildSummary(current.ConversationSummary, safeRequest.UserMessage, intent, focusedEntity, resolvedDomain, safeRequest.ToolName)
        };

        PersistIfConversationScoped(safeRequest.ConversationId, updated);
        return CloneContext(updated)!;
    }

    private static WorkingContext MergeContexts(WorkingContext? persisted, WorkingContext? incoming)
    {
        if (persisted is null && incoming is null)
            return new WorkingContext();

        var merged = CloneContext(persisted) ?? new WorkingContext();
        if (incoming is null)
            return merged;

        if (incoming.FocusedEntity is not null)
            merged.FocusedEntity = CloneEntity(incoming.FocusedEntity);

        if (!string.IsNullOrWhiteSpace(incoming.CurrentDomain))
            merged.CurrentDomain = RoutingVocabulary.ResolveCanonicalDomain(incoming.CurrentDomain);

        if (!string.IsNullOrWhiteSpace(incoming.ConversationSummary))
            merged.ConversationSummary = Summarize(incoming.ConversationSummary);

        if (!string.IsNullOrWhiteSpace(incoming.LastIntent))
            merged.LastIntent = incoming.LastIntent;

        if (!string.IsNullOrWhiteSpace(incoming.LastToolName))
            merged.LastToolName = incoming.LastToolName;

        return merged;
    }

    private static WorkingContext? CloneContext(WorkingContext? source)
    {
        if (source is null)
            return null;

        return new WorkingContext
        {
            FocusedEntity = CloneEntity(source.FocusedEntity),
            CurrentDomain = source.CurrentDomain,
            ConversationSummary = source.ConversationSummary,
            LastIntent = source.LastIntent,
            LastToolName = source.LastToolName
        };
    }

    private static ActiveEntity? CloneEntity(ActiveEntity? source)
    {
        if (source is null)
            return null;

        return new ActiveEntity
        {
            EntityType = source.EntityType,
            EntityId = source.EntityId,
            DisplayName = source.DisplayName
        };
    }

    private WorkingContext? GetPersistedContext(string? conversationId)
        => string.IsNullOrWhiteSpace(conversationId)
            ? null
            : _contexts.TryGetValue(conversationId, out var context)
                ? CloneContext(context)
                : null;

    private void PersistIfConversationScoped(string? conversationId, WorkingContext context)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return;

        _contexts[conversationId] = CloneContext(context) ?? new WorkingContext();
    }

    private ActiveEntity? ResolveFocusedEntity(ToolDefinition? tool, string? argumentsJson, string? resultText, ActiveEntity? currentEntity)
    {
        if (IsToolFailure(resultText))
            return CloneEntity(currentEntity);

        var entityType = FirstNonEmpty(tool?.Metadata.EntityType, currentEntity?.EntityType);
        using var resultDocument = TryParseJson(resultText);
        using var argumentDocument = TryParseJson(argumentsJson);

        var primaryNode = TrySelectPrimaryNode(resultDocument?.RootElement);
        var id = ExtractEntityId(primaryNode, entityType)
            ?? ExtractEntityId(argumentDocument?.RootElement, entityType)
            ?? currentEntity?.EntityId
            ?? string.Empty;

        var displayName = ExtractDisplayName(primaryNode, entityType)
            ?? ExtractDisplayName(argumentDocument?.RootElement, entityType)
            ?? currentEntity?.DisplayName
            ?? string.Empty;

        var resolvedEntityType = FirstNonEmpty(
            entityType,
            ExtractEntityType(primaryNode),
            ExtractEntityType(argumentDocument?.RootElement));

        if (string.IsNullOrWhiteSpace(resolvedEntityType) && string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(displayName))
            return CloneEntity(currentEntity);

        return new ActiveEntity
        {
            EntityType = resolvedEntityType,
            EntityId = id,
            DisplayName = displayName
        };
    }

    private static JsonDocument? TryParseJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            return JsonDocument.Parse(text, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement? TrySelectPrimaryNode(JsonElement? root)
    {
        if (root is null)
            return null;

        if (root.Value.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(root.Value, "results", out var results)
                && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in results.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        return item;
                }
            }

            if (TryGetProperty(root.Value, "result", out var result)
                && result.ValueKind == JsonValueKind.Object)
            {
                return result;
            }

            return root;
        }

        if (root.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                    return item;
            }
        }

        return root;
    }

    private static string ExtractEntityType(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
            return string.Empty;

        foreach (var property in element.Value.EnumerateObject())
        {
            if (!property.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)
                && !property.Name.EndsWith("Number", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cleaned = Regex.Replace(property.Name, "(ID|Number)$", string.Empty, RegexOptions.IgnoreCase);
            var entityType = RoutingVocabulary.ResolveEntityType(cleaned);
            if (!string.IsNullOrWhiteSpace(entityType))
                return entityType;
        }

        return string.Empty;
    }

    private static string? ExtractEntityId(JsonElement? element, string? entityType)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
            return null;

        var preferredKeys = new List<string>();
        var resolvedEntityType = RoutingVocabulary.ResolveEntityType(entityType);
        if (!string.IsNullOrWhiteSpace(resolvedEntityType))
        {
            preferredKeys.Add($"{resolvedEntityType}ID");
            preferredKeys.Add($"{resolvedEntityType}Id");
            preferredKeys.Add($"{resolvedEntityType}Number");
        }

        if (resolvedEntityType.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            preferredKeys.InsertRange(0, ["DebtorID", "CustomerNumber", "DebtorNumber"]);

        if (resolvedEntityType.Equals("CreditorPurchase", StringComparison.OrdinalIgnoreCase))
            preferredKeys.InsertRange(0, ["BatchID", "InvoiceNumber"]);

        if (resolvedEntityType.Equals("PurchaseOrder", StringComparison.OrdinalIgnoreCase))
            preferredKeys.InsertRange(0, ["PurchaseOrderID", "PurchaseOrderNumber", "PONumber"]);

        foreach (var key in preferredKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (TryGetProperty(element.Value, key, out var value))
            {
                var stringValue = GetScalarValue(value);
                if (!string.IsNullOrWhiteSpace(stringValue))
                    return stringValue;
            }
        }

        foreach (var property in element.Value.EnumerateObject())
        {
            if (!property.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)
                && !property.Name.EndsWith("Number", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = GetScalarValue(property.Value);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? ExtractDisplayName(JsonElement? element, string? entityType)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
            return null;

        var preferredKeys = new List<string>();
        var resolvedEntityType = RoutingVocabulary.ResolveEntityType(entityType);
        if (!string.IsNullOrWhiteSpace(resolvedEntityType))
        {
            preferredKeys.Add($"{resolvedEntityType}Name");
            preferredKeys.Add($"{resolvedEntityType}Description");
        }

        preferredKeys.AddRange(["DisplayName", "Name", "Description", "Title", "Company", "CompanyName", "DebtorName", "CustomerName", "SupplierName", "CreditorName", "Code"]);

        foreach (var key in preferredKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!TryGetProperty(element.Value, key, out var value))
                continue;

            var stringValue = GetScalarValue(value);
            if (!string.IsNullOrWhiteSpace(stringValue))
                return stringValue;
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? GetScalarValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };

    private static string DescribeIntent(IntentContext intent)
    {
        var action = FirstNonEmpty(intent.Action, "Unknown");
        var entityType = FirstNonEmpty(intent.EntityType, "Entity");
        var parameterKeys = intent.Parameters.Keys.Take(2).ToList();
        return parameterKeys.Count == 0
            ? $"{action} {entityType}"
            : $"{action} {entityType} ({string.Join(", ", parameterKeys)})";
    }

    private static string BuildSummary(string existingSummary, string userMessage, IntentContext intent, ActiveEntity? focusedEntity, string currentDomain, string? toolName)
    {
        var sentences = new List<string>();
        if (focusedEntity is not null && !string.IsNullOrWhiteSpace(focusedEntity.EntityType))
        {
            var label = string.IsNullOrWhiteSpace(focusedEntity.DisplayName)
                ? focusedEntity.EntityType.ToLowerInvariant()
                : $"{focusedEntity.EntityType.ToLowerInvariant()} {focusedEntity.DisplayName}";
            var identity = string.IsNullOrWhiteSpace(focusedEntity.EntityId) ? label : $"{label} ({focusedEntity.EntityId})";
            sentences.Add($"User is working with {identity}.");
        }

        if (!string.IsNullOrWhiteSpace(currentDomain))
            sentences.Add($"Current domain: {RoutingVocabulary.HumanizePascalCase(currentDomain.Replace("Tools", string.Empty, StringComparison.OrdinalIgnoreCase)).ToLowerInvariant()}.");

        if (!string.IsNullOrWhiteSpace(intent.Action) || !string.IsNullOrWhiteSpace(intent.EntityType))
        {
            var detail = intent.Parameters.Count == 0
                ? string.Empty
                : $" ({string.Join(", ", intent.Parameters.Keys.Take(2))})";
            sentences.Add($"Current task: {FirstNonEmpty(intent.Action, "review").ToLowerInvariant()} {FirstNonEmpty(intent.EntityType, focusedEntity?.EntityType, "record").ToLowerInvariant()}{detail}.");
        }

        var requestedArtifact = DescribeRequestedArtifact(userMessage);
        if (!string.IsNullOrWhiteSpace(requestedArtifact))
            sentences.Add($"Requested output: {requestedArtifact}.");

        if (!string.IsNullOrWhiteSpace(toolName))
            sentences.Add($"Last tool: {toolName}.");

        if (sentences.Count == 0 && !string.IsNullOrWhiteSpace(existingSummary))
            sentences.Add(existingSummary);
        else if (sentences.Count == 0 && !string.IsNullOrWhiteSpace(userMessage))
            sentences.Add($"Recent request: {Summarize(userMessage)}");

        return Summarize(string.Join(' ', sentences));
    }

    private static string DescribeRequestedArtifact(string? userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return string.Empty;

        var normalized = userMessage.Trim();
        var wantsDownload = Regex.IsMatch(normalized, @"\b(download|downloadable|export)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var format = Regex.Match(normalized, @"\b(csv|xlsx|excel|json|xml|pdf|docx)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!wantsDownload && !format.Success)
            return string.Empty;

        var normalizedFormat = format.Success
            ? format.Groups[1].Value.ToLowerInvariant() switch
            {
                "excel" => "xlsx",
                var value => value
            }
            : string.Empty;

        if (wantsDownload)
            return string.IsNullOrWhiteSpace(normalizedFormat) ? "downloadable file export" : $"downloadable {normalizedFormat} export";

        return normalizedFormat;
    }

    private static bool IsToolFailure(string? resultText)
        => !string.IsNullOrWhiteSpace(resultText)
            && (resultText.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
                || resultText.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)
                || resultText.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase));

    private static string SummarizeLegacyConversationContext(string? conversationContext)
    {
        if (string.IsNullOrWhiteSpace(conversationContext))
            return string.Empty;

        var normalized = Regex.Replace(conversationContext, @"\s+", " ").Trim();
        return normalized.Length <= 400 && normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 80
            ? normalized
            : string.Empty;
    }

    private static string Summarize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = Regex.Replace(value, @"\s+", " ").Trim();
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 80)
            normalized = string.Join(' ', words.Take(80));

        return normalized.Length <= 500 ? normalized : normalized[..500].TrimEnd();
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
