using JiwaMcpServer.ToolMetadata;

namespace JiwaMcpServer.ToolRouting;

/// <summary>
/// Semantic tool routing policy that uses BusinessToolMetadata to determine required companions
/// and excluded tools instead of hardcoded keyword matching.
/// </summary>
public sealed class BusinessToolRoutingPolicy
{
    private static readonly string[] CreateIntentPrefixes = ["create", "new", "add"];

    private readonly BusinessTerminologyRegistry _terminologyRegistry;
    private readonly BusinessToolMetadataCatalog _metadataCatalog;

    public BusinessToolRoutingPolicy(
        BusinessTerminologyRegistry terminologyRegistry,
        BusinessToolMetadataCatalog metadataCatalog)
    {
        _terminologyRegistry = terminologyRegistry ?? throw new ArgumentNullException(nameof(terminologyRegistry));
        _metadataCatalog = metadataCatalog ?? throw new ArgumentNullException(nameof(metadataCatalog));
    }

    /// <summary>
    /// Determines which required companion tools must be included based on:
    /// 1. Tools that are already selected and their companion requirements
    /// 2. Explicit create/new entity requests present in the user prompt
    /// </summary>
    public IReadOnlyList<string> GetRequiredAdditionalTools(
        string userPrompt,
        IReadOnlySet<string> selectedToolNames,
        IReadOnlySet<string> availableToolNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);
        ArgumentNullException.ThrowIfNull(selectedToolNames);
        ArgumentNullException.ThrowIfNull(availableToolNames);

        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add companions required by already-selected tools
        foreach (var toolName in selectedToolNames)
        {
            if (_metadataCatalog.TryGetByToolName(toolName, out var descriptor) && descriptor?.Metadata != null)
            {
                foreach (var companion in descriptor.Metadata.RequiredCompanionTools)
                {
                    if (availableToolNames.Contains(companion, StringComparer.OrdinalIgnoreCase) &&
                        !selectedToolNames.Contains(companion, StringComparer.OrdinalIgnoreCase))
                    {
                        required.Add(companion);
                    }
                }
            }
        }

        var normalizedPrompt = Normalize(userPrompt);
        foreach (var descriptor in _metadataCatalog.Descriptors)
        {
            if (!availableToolNames.Contains(descriptor.ToolName, StringComparer.OrdinalIgnoreCase)
                || selectedToolNames.Contains(descriptor.ToolName, StringComparer.OrdinalIgnoreCase)
                || !string.Equals(descriptor.Metadata.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (PromptExplicitlyRequestsCreateTool(normalizedPrompt, descriptor.Metadata))
            {
                required.Add(descriptor.ToolName);
            }
        }

        return required.ToList();
    }

    /// <summary>
    /// Determines which tools should be excluded based on:
    /// 1. Conflict rules in tool metadata (ExcludedTools)
    /// 2. User prompt context resolution
    /// </summary>
    public IReadOnlyList<string> GetExcludedTools(string userPrompt, IReadOnlySet<string> availableToolNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);
        ArgumentNullException.ThrowIfNull(availableToolNames);

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check each available tool for exclusion rules
        foreach (var toolName in availableToolNames)
        {
            if (_metadataCatalog.TryGetByToolName(toolName, out var descriptor) && descriptor?.Metadata != null)
            {
                foreach (var excludedTool in descriptor.Metadata.ExcludedTools)
                {
                    if (availableToolNames.Contains(excludedTool, StringComparer.OrdinalIgnoreCase))
                    {
                        excluded.Add(excludedTool);
                    }
                }
            }
        }

        return excluded.ToList();
    }

    private bool PromptExplicitlyRequestsCreateTool(string normalizedPrompt, BusinessToolMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(normalizedPrompt))
        {
            return false;
        }

        foreach (var phrase in metadata.IntentPhrases)
        {
            var normalizedPhrase = Normalize(phrase);
            if (normalizedPhrase.Length == 0 || !ContainsPhrase(normalizedPrompt, normalizedPhrase))
            {
                continue;
            }

            var targetTerm = StripCreateIntentPrefix(normalizedPhrase);
            if (targetTerm.Length == 0 || !IsShadowedByLongerEntityIntent(normalizedPrompt, metadata.EntityType, targetTerm))
            {
                return true;
            }
        }

        foreach (var entityTerm in GetEntityTerms(metadata.EntityType, metadata.Aliases))
        {
            if (!ContainsCreateIntent(normalizedPrompt, entityTerm))
            {
                continue;
            }

            if (!IsShadowedByLongerEntityIntent(normalizedPrompt, metadata.EntityType, entityTerm))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<string> GetEntityTerms(string? entityType, IReadOnlyList<string> aliases)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in aliases)
        {
            var normalizedAlias = Normalize(alias);
            if (normalizedAlias.Length > 0)
            {
                terms.Add(normalizedAlias);
            }
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            terms.Add(Normalize(entityType));
            foreach (var synonym in _terminologyRegistry.GetSynonyms(entityType))
            {
                var normalizedSynonym = Normalize(synonym);
                if (normalizedSynonym.Length > 0)
                {
                    terms.Add(normalizedSynonym);
                }
            }
        }

        return terms;
    }

    private bool IsShadowedByLongerEntityIntent(string normalizedPrompt, string? entityType, string targetTerm)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(targetTerm))
        {
            return false;
        }

        foreach (var otherEntityType in _terminologyRegistry.GetEntityTypes())
        {
            if (string.Equals(otherEntityType, entityType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var otherTerm in GetEntityTerms(otherEntityType, []))
            {
                if (otherTerm.Length <= targetTerm.Length
                    || !otherTerm.StartsWith(targetTerm + ' ', StringComparison.Ordinal)
                    || !ContainsCreateIntent(normalizedPrompt, otherTerm))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private static bool ContainsCreateIntent(string normalizedPrompt, string targetTerm)
        => CreateIntentPrefixes.Any(prefix => ContainsPhrase(normalizedPrompt, $"{prefix} {targetTerm}"));

    private static string StripCreateIntentPrefix(string normalizedPhrase)
    {
        foreach (var prefix in CreateIntentPrefixes)
        {
            var prefixWithSeparator = prefix + ' ';
            if (normalizedPhrase.StartsWith(prefixWithSeparator, StringComparison.Ordinal))
            {
                return normalizedPhrase[prefixWithSeparator.Length..].Trim();
            }
        }

        return string.Empty;
    }

    private static bool ContainsPhrase(string normalizedText, string phrase)
    {
        var normalizedPhrase = Normalize(phrase);
        return normalizedPhrase.Length > 0
            && (normalizedText == normalizedPhrase
                || normalizedText.StartsWith(normalizedPhrase + ' ', StringComparison.Ordinal)
                || normalizedText.EndsWith(' ' + normalizedPhrase, StringComparison.Ordinal)
                || normalizedText.Contains(' ' + normalizedPhrase + ' ', StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        return string.Join(' ', value
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '-', '_', '/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
