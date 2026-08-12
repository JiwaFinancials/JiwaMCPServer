using System.Text.RegularExpressions;

namespace JiwaMcpServer.ToolMetadata;

public sealed partial class BusinessToolMetadataBuilder
{
    private static readonly string[] KnownActionPrefixes =
    [
        "create",
        "update",
        "delete",
        "get",
        "list",
        "search",
        "add",
        "set",
        "open"
    ];

    private readonly BusinessEntityRegistry _entityRegistry;

    public BusinessToolMetadataBuilder(BusinessEntityRegistry entityRegistry)
    {
        _entityRegistry = entityRegistry;
    }

    public BusinessToolMetadata Build(string toolName, string? description, BusinessToolAttribute? businessTool)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var actionType = ResolveActionType(toolName, businessTool?.ActionType);
        var entity = ResolveEntity(toolName, description, businessTool?.EntityType);
        var aliases = MergeTerms(entity?.Aliases, businessTool?.Aliases);
        var tags = MergeTerms(entity?.Tags, businessTool?.Tags);
        var intentPhrases = BuildIntentPhrases(actionType, entity?.Name, aliases);

        var metadata = new BusinessToolMetadata
        {
            EntityType = entity?.Name ?? TrimOrNull(businessTool?.EntityType),
            ActionType = actionType,
            Aliases = aliases,
            Tags = tags,
            IntentPhrases = intentPhrases,
            SearchText = BuildSearchText(toolName, description, actionType, entity?.Name, aliases, tags, intentPhrases)
        };

        return metadata;
    }

    public string BuildSearchText(
        string toolName,
        string? description,
        string? actionType,
        string? entityType,
        IReadOnlyList<string> aliases,
        IReadOnlyList<string> tags,
        IReadOnlyList<string>? intentPhrases = null)
    {
        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddTerm(actionType, terms, seen);
        AddTerm(entityType, terms, seen);
        AddTerm(toolName, terms, seen);
        AddTerm(description, terms, seen);

        foreach (var alias in aliases)
        {
            AddTerm(alias, terms, seen);
        }

        foreach (var tag in tags)
        {
            AddTerm(tag, terms, seen);
        }

        if (intentPhrases is not null)
        {
            foreach (var phrase in intentPhrases)
            {
                AddTerm(phrase, terms, seen);
            }
        }

        return string.Join(' ', terms);
    }

    private BusinessEntityDefinition? ResolveEntity(string toolName, string? description, string? explicitEntityType)
    {
        if (!string.IsNullOrWhiteSpace(explicitEntityType) &&
            _entityRegistry.TryGetByNameOrAlias(explicitEntityType, out var entityByAttribute))
        {
            return entityByAttribute;
        }

        var tokens = Tokenize(toolName, description);
        foreach (var candidate in _entityRegistry.GetAll())
        {
            if (ContainsTerm(tokens, candidate.Name))
            {
                return candidate;
            }

            if (candidate.Aliases.Any(alias => ContainsTerm(tokens, alias)))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveActionType(string toolName, string? explicitActionType)
    {
        if (!string.IsNullOrWhiteSpace(explicitActionType))
        {
            return explicitActionType.Trim();
        }

        var words = Tokenize(toolName);
        var first = words.FirstOrDefault();
        if (first is null)
        {
            return null;
        }

        return KnownActionPrefixes.Contains(first, StringComparer.OrdinalIgnoreCase)
            ? char.ToUpperInvariant(first[0]) + first[1..]
            : null;
    }

    private static IReadOnlyList<string> MergeTerms(IReadOnlyList<string>? inheritedTerms, IReadOnlyList<string>? localTerms)
    {
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AppendTerms(inheritedTerms);
        AppendTerms(localTerms);

        return results;

        void AppendTerms(IReadOnlyList<string>? terms)
        {
            if (terms is null)
            {
                return;
            }

            foreach (var term in terms)
            {
                var normalized = NormalizeOrNull(term);
                if (normalized is null)
                {
                    continue;
                }

                if (seen.Add(normalized))
                {
                    results.Add(normalized);
                }
            }
        }
    }

    private static void AddTerm(string? rawValue, List<string> terms, HashSet<string> seen)
    {
        var normalized = NormalizeOrNull(rawValue);
        if (normalized is null)
        {
            return;
        }

        if (seen.Add(normalized))
        {
            terms.Add(normalized);
        }

        foreach (var token in Tokenize(normalized))
        {
            if (seen.Add(token))
            {
                terms.Add(token);
            }
        }
    }

    private static IReadOnlyList<string> BuildIntentPhrases(
        string? actionType,
        string? entityType,
        IReadOnlyList<string> aliases)
    {
        var phrases = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var normalizedAction = NormalizeActionPhrase(actionType);
        var normalizedEntity = NormalizeEntityPhrase(entityType);

        AddPhrasePair(normalizedAction, normalizedEntity);

        foreach (var alias in aliases)
        {
            var normalizedAlias = NormalizeEntityPhrase(alias);
            AddPhrasePair(normalizedAction, normalizedAlias);
        }

        return phrases;

        void AddPhrasePair(string? action, string? target)
        {
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(target))
                return;

            var phrase = $"{action} {target}";
            if (seen.Add(phrase))
                phrases.Add(phrase);

            if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
            {
                var newPhrase = $"new {target}";
                if (seen.Add(newPhrase))
                    phrases.Add(newPhrase);
            }
        }
    }

    private static string? NormalizeActionPhrase(string? actionType)
    {
        if (string.IsNullOrWhiteSpace(actionType))
            return null;

        return string.Join(' ', Tokenize(actionType));
    }

    private static string? NormalizeEntityPhrase(string? entity)
    {
        if (string.IsNullOrWhiteSpace(entity))
            return null;

        return string.Join(' ', Tokenize(entity));
    }

    private static bool ContainsTerm(HashSet<string> tokens, string value)
    {
        foreach (var token in Tokenize(value))
        {
            if (tokens.Contains(token))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> Tokenize(params string?[] values)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var camelSplit = SplitCamelCaseRegex.Replace(value, "$1 $2");
            foreach (Match match in TokenRegex.Matches(camelSplit))
            {
                var token = match.Value.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    tokens.Add(token);
                }
            }
        }

        return tokens;
    }

    private static string? NormalizeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant();
    }

    private static readonly Regex SplitCamelCaseRegex = new("([a-z0-9])([A-Z])", RegexOptions.Compiled);

    private static readonly Regex TokenRegex = new("[A-Za-z0-9]+", RegexOptions.Compiled);

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
