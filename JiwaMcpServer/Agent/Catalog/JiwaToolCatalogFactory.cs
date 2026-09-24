using JiwaMcpServer.Agent.Models;
using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Agent.SchemaResolver;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Agent.Catalog;

/// <summary>
/// Factory that builds the Jiwa-specific <see cref="ToolCatalog"/> from the reflected MCP tool registry.
/// Domain and tool metadata are derived from the live tool set so routing cannot advertise tools that do not exist.
/// </summary>
public static class JiwaToolCatalogFactory
{
    private static readonly IReadOnlyDictionary<string, DomainDefinition> DomainDefinitions = BuildDomains()
        .ToDictionary(domain => domain.Name, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> KeywordNoiseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "tool", "this", "that", "with", "when", "from", "into", "your", "their", "then",
        "user", "users", "using", "use", "whenever", "where", "already", "other", "through",
        "local", "server", "results", "result", "response", "returns", "return", "required"
    };

    private static readonly HashSet<string> ResourceStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "to", "from", "with", "using", "via", "for", "on", "in", "into", "through", "by", "after", "before", "attached"
    };

    private static readonly HashSet<string> ParameterNoiseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "LastSavedDateTime", "LastModifiedDateTime", "LastUpdatedDateTime", "RowHash", "ETag", "Timestamp"
    };

    public static ToolCatalog Create() => Create(new ToolRegistry([typeof(JiwaToolCatalogFactory).Assembly]));

    public static ToolCatalog Create(ToolRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var tools = BuildTools(registry);
        var activeDomains = DomainDefinitions.Values
            .Where(domain =>
                tools.Any(tool => string.Equals(tool.Domain, domain.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return new ToolCatalog(activeDomains, tools);
    }

    private static IReadOnlyList<DomainDefinition> BuildDomains() =>
    [
        new DomainDefinition
        {
            Name = "InventoryTools",
            Description = "Products, parts, stock levels, pricing, warehouses, and inventory records.",
            Capabilities =
            [
                "Query and search inventory items by keyword, part number, category, or classification",
                "Check stock on hand and available quantities",
                "View sell price and RRP",
                "Browse inventory categories and classifications",
                "View physical and logical warehouse locations",
                "Retrieve, create, modify, and delete products",
                "Get product pictures"
            ]
        },
        new DomainDefinition
        {
            Name = "CustomerTools",
            Description = "Customer accounts, debtor records, contacts, and addresses.",
            Capabilities =
            [
                "Query and search customers (debtors) by name, code, or contact",
                "View customer details, contacts, and addresses",
                "Create, modify, and delete customer records",
                "Look up customer pricing and terms"
            ]
        },
        new DomainDefinition
        {
            Name = "CreditorPurchaseTools",
            Description = "Suppliers, creditor purchases, purchase documents, and purchase transactions.",
            Capabilities =
            [
                "Query and manage creditor purchases",
                "View and modify creditor purchase lines",
                "Manage creditor purchase documents and document types",
                "Query supplier balances and invoices"
            ]
        },

        new DomainDefinition
        {
            Name = "DocumentTools",
            Description = "Uploaded business documents, document extraction, conversion, and downloadable exports.",
            Capabilities =
            [
                "Extract structured content from CSV, XLSX, JSON, XML, PDF, and DOCX uploads",
                "Extract and upload local files from allowed Windows paths",
                "Convert uploaded files between supported formats",
                "Generate downloadable files from structured data",
                "Export full structured results from prior Jiwa tool calls as downloadable files"
            ]
        }
    ];

    private static IReadOnlyList<ToolDefinition> BuildTools(ToolRegistry registry)
    {
        var tools = registry.Tools.Values
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entry =>
            {
                var domain = ResolveDomain(entry);
                var fullDescription = NormalizeDescription(entry.Description);
                var description = BuildToolDescription(entry, fullDescription);
                var action = BuildAction(entry, fullDescription);
                var resource = BuildResource(entry, fullDescription);
                var inputSummary = BuildInputSummary(entry.Method);
                var outputSummary = BuildOutputSummary(entry.Method, fullDescription);
                var prerequisites = BuildPrerequisites(entry.Method, fullDescription, inputSummary);
                var metadata = BuildToolMetadata(entry, domain, fullDescription, action, resource);

                return new ToolDefinition
                {
                    Name = entry.Name,
                    Domain = domain,
                    Description = description,
                    Action = action,
                    Resource = resource,
                    Prerequisites = prerequisites,
                    InputSummary = inputSummary,
                    OutputSummary = outputSummary,
                    Metadata = metadata,
                    Keywords = BuildKeywords(entry, domain, fullDescription, description, action, resource, prerequisites, inputSummary, outputSummary)
                };
            })
            .ToList();

        if (tools.Count == 0)
            throw new InvalidOperationException("No MCP tools were discovered for domain catalog generation.");

        return tools;
    }

    private static string ResolveDomain(ToolEntry entry)
    {
        var domain = entry.ClassType.GetCustomAttribute<PlannerDomainAttribute>()?.Name;
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new InvalidOperationException(
                $"Tool class '{entry.ClassType.FullName}' must declare [PlannerDomain] so domain routing can classify '{entry.Name}'.");
        }

        if (!DomainDefinitions.ContainsKey(domain))
            throw new InvalidOperationException($"Tool class '{entry.ClassType.FullName}' declares unknown routing domain '{domain}'.");

        return domain;
    }

    private static string BuildToolDescription(ToolEntry entry, string fullDescription)
    {
        if (string.IsNullOrWhiteSpace(fullDescription))
            return EnsureSentence(HumanizeIdentifier(entry.Name));

        var firstSentence = EnsureSentence(ExtractFirstSentence(fullDescription));
        var useWhen = ExtractUseWhenSummary(fullDescription);
        if (string.IsNullOrWhiteSpace(useWhen))
            return firstSentence;

        var normalizedUseWhen = TrimTrailingPunctuation(useWhen);
        if (string.Equals(TrimTrailingPunctuation(firstSentence), normalizedUseWhen, StringComparison.OrdinalIgnoreCase))
            return firstSentence;

        return $"{firstSentence} Use when {LowercaseFirst(normalizedUseWhen)}.";
    }

    private static string? BuildAction(ToolEntry entry, string fullDescription)
    {
        var action = ExtractLeadingWord(ExtractFirstSentence(fullDescription));
        if (string.IsNullOrWhiteSpace(action))
            action = ExtractLeadingWord(HumanizeIdentifier(entry.Name));

        return string.IsNullOrWhiteSpace(action) ? null : action.ToLowerInvariant();
    }

    private static string? BuildResource(ToolEntry entry, string fullDescription)
    {
        var resource = ExtractResourceFromSentence(ExtractFirstSentence(fullDescription));
        if (!string.IsNullOrWhiteSpace(resource))
            return resource;

        foreach (var parameter in entry.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
                continue;

            if (!IsSimpleType(parameter.ParameterType))
            {
                var typeName = HumanizeTypeName(parameter.ParameterType);
                if (!string.IsNullOrWhiteSpace(typeName))
                    return typeName.ToLowerInvariant();
            }
        }

        return null;
    }

    private static ToolMetadata BuildToolMetadata(ToolEntry entry, string domain, string fullDescription, string? action, string? resource)
    {
        var entityType = BuildEntityType(entry, domain, resource);
        var supportedActions = BuildSupportedActions(entry, action, fullDescription);

        return new ToolMetadata
        {
            ToolName = entry.Name,
            Domain = domain,
            EntityType = entityType,
            SupportedActions = supportedActions
        };
    }

    private static string BuildEntityType(ToolEntry entry, string domain, string? resource)
    {
        foreach (var candidate in new[]
        {
            ExtractEntityHintFromToolName(entry.Name),
            GetPrimaryRequestTypeName(entry.Method),
            resource,
            domain.Replace("Tools", string.Empty, StringComparison.OrdinalIgnoreCase)
        })
        {
            var entityType = RoutingVocabulary.ResolveEntityType(candidate);
            if (!string.IsNullOrWhiteSpace(entityType))
                return entityType;
        }

        return string.Empty;
    }

    private static string[] BuildSupportedActions(ToolEntry entry, string? action, string fullDescription)
    {
        var actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in new[] { action, entry.Name, fullDescription })
        {
            var resolvedAction = RoutingVocabulary.ResolveCanonicalAction(candidate);
            if (!string.IsNullOrWhiteSpace(resolvedAction))
                actions.Add(resolvedAction);
        }

        if (actions.Contains("Read"))
            actions.Add("Query");

        if (actions.Contains("Query"))
            actions.Add("Read");

        return actions.Count == 0 ? ["Read"] : actions.ToArray();
    }

    private static string? ExtractEntityHintFromToolName(string toolName)
    {
        var humanized = HumanizeIdentifier(toolName);
        foreach (var prefix in new[] { "Query", "Search", "Get", "Create", "Add", "Modify", "Delete", "Activate", "Approve", "Upload", "Import", "Export", "Convert" })
        {
            if (humanized.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                humanized = humanized[(prefix.Length + 1)..];
                break;
            }
        }

        var tokens = humanized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 0 ? null : string.Join(' ', tokens.Take(2));
    }

    private static string? GetPrimaryRequestTypeName(MethodInfo method)
    {
        var parameter = method.GetParameters().FirstOrDefault(candidate => candidate.ParameterType != typeof(CancellationToken));
        return parameter is null || IsSimpleType(parameter.ParameterType)
            ? null
            : HumanizeTypeName(parameter.ParameterType);
    }

    private static string? BuildPrerequisites(MethodInfo method, string fullDescription, string? inputSummary)
    {
        var fromDescription = ExtractPrerequisiteSummary(fullDescription);
        if (!string.IsNullOrWhiteSpace(fromDescription))
            return fromDescription;

        var prerequisites = new List<string>();
        var summary = inputSummary ?? string.Empty;

        if (summary.Contains("localPath", StringComparison.OrdinalIgnoreCase))
            prerequisites.Add("accessible local path");

        if (summary.Contains("FileId", StringComparison.OrdinalIgnoreCase))
            prerequisites.Add("uploaded FileId");

        if (Regex.IsMatch(summary, "\\b[A-Za-z0-9]*ID\\b", RegexOptions.IgnoreCase))
            prerequisites.Add("target identifiers");

        if (summary.Contains("FileBinary", StringComparison.OrdinalIgnoreCase) || summary.Contains("Data", StringComparison.OrdinalIgnoreCase))
            prerequisites.Add("payload data");

        if (summary.Contains("Question", StringComparison.OrdinalIgnoreCase))
            prerequisites.Add("user question");

        return prerequisites.Distinct(StringComparer.OrdinalIgnoreCase).Take(2) switch
        {
            var items when items.Any() => string.Join("; ", items),
            _ => null
        };
    }

    private static string? BuildInputSummary(MethodInfo method)
    {
        var inputs = new List<string>();

        foreach (var parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
                continue;

            if (IsSimpleType(parameter.ParameterType))
            {
                if (!string.IsNullOrWhiteSpace(parameter.Name))
                    inputs.Add(parameter.Name!);
                continue;
            }

            inputs.AddRange(GetSalientMemberNames(parameter.ParameterType));
        }

        var distinctInputs = inputs
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

        return distinctInputs.Count == 0 ? null : string.Join(", ", distinctInputs);
    }

    private static string? BuildOutputSummary(MethodInfo method, string fullDescription)
    {
        var fromDescription = ExtractReturnSummary(fullDescription);
        if (!string.IsNullOrWhiteSpace(fromDescription))
            return fromDescription;

        var returnType = UnwrapTaskType(method.ReturnType);
        if (returnType == typeof(void) || returnType == typeof(string))
            return null;

        if (IsContentBlockSequence(returnType))
            return "downloadable MCP file/resource";

        return HumanizeTypeName(returnType).ToLowerInvariant();
    }

    private static IReadOnlyList<string> BuildKeywords(
        ToolEntry entry,
        string domain,
        string fullDescription,
        string toolDescription,
        string? action,
        string? resource,
        string? prerequisites,
        string? inputSummary,
        string? outputSummary)
    {
        var keywords = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddKeywordVariants(keywords, seen, entry.Name);
        AddKeywordVariants(keywords, seen, action);
        AddKeywordVariants(keywords, seen, resource);

        foreach (var phrase in ExtractKeywordPhrases(fullDescription))
            AddKeywordVariants(keywords, seen, phrase);

        AddKeywordVariants(keywords, seen, domain);
        AddKeywordVariants(keywords, seen, toolDescription);
        AddKeywordVariants(keywords, seen, prerequisites);
        AddKeywordVariants(keywords, seen, inputSummary);
        AddKeywordVariants(keywords, seen, outputSummary);
        AddKeywordVariants(keywords, seen, fullDescription);

        return keywords.Take(24).ToList();
    }

    private static IEnumerable<string> ExtractKeywordPhrases(string fullDescription)
    {
        var phrases = new List<string>();
        var useWhen = ExtractUseWhenSummary(fullDescription);
        if (!string.IsNullOrWhiteSpace(useWhen))
            phrases.Add(useWhen);

        var returnSummary = ExtractReturnSummary(fullDescription);
        if (!string.IsNullOrWhiteSpace(returnSummary))
            phrases.Add(returnSummary);

        foreach (var line in fullDescription.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();
            if (!IsBulletLine(trimmed))
                continue;

            var bullet = CleanBullet(trimmed);
            if (bullet.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
                continue;

            phrases.Add(bullet);
            if (phrases.Count >= 16)
                break;
        }

        return phrases;
    }

    private static void AddKeywordVariants(List<string> keywords, HashSet<string> seen, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalizedPhrase = NormalizeKeywordPhrase(value);
        if (!string.IsNullOrWhiteSpace(normalizedPhrase) && seen.Add(normalizedPhrase))
            keywords.Add(normalizedPhrase);

        foreach (Match match in Regex.Matches(HumanizeIdentifier(value), "[A-Za-z0-9]+"))
        {
            var token = match.Value.ToLowerInvariant();
            if (token.Length < 4 || KeywordNoiseWords.Contains(token))
                continue;

            if (seen.Add(token))
                keywords.Add(token);
        }
    }

    private static string NormalizeKeywordPhrase(string value)
    {
        var normalized = NormalizeWhitespace(HumanizeIdentifier(value)).ToLowerInvariant();
        normalized = TrimTrailingPunctuation(normalized);

        if (string.IsNullOrWhiteSpace(normalized) || KeywordNoiseWords.Contains(normalized) || normalized.Length < 3)
            return string.Empty;

        return normalized;
    }

    private static string ExtractFirstSentence(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var firstParagraph = description.Replace("\r\n", "\n").Split(["\n\n"], 2, StringSplitOptions.None)[0];
        var normalized = NormalizeWhitespace(firstParagraph);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var match = Regex.Match(normalized, "^.+?[.!?](?=\\s|$)");
        return match.Success ? match.Value : normalized;
    }

    private static string? ExtractUseWhenSummary(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var lines = description.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("Use this tool whenever the user wants to:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Use this tool whenever the user says things like:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Use this tool whenever the user asks for:", StringComparison.OrdinalIgnoreCase))
            {
                var items = CollectFollowingListItems(lines, i + 1);
                if (items.Count > 0)
                    return JoinCompactPhrases(items);
            }

            if (trimmed.StartsWith("Use this tool when ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Use this tool whenever ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Always use this tool when ", StringComparison.OrdinalIgnoreCase))
            {
                return TrimTrailingPunctuation(RemoveLeadingDirective(trimmed));
            }
        }

        return null;
    }

    private static string? ExtractPrerequisiteSummary(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        foreach (var rawLine in description.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("Use this tool when other tools require", StringComparison.OrdinalIgnoreCase))
                return TrimTrailingPunctuation(RemoveLeadingDirective(line));

            if (line.StartsWith("The source file must", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("The resolved file must", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("The user uploads", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("User uploads", StringComparison.OrdinalIgnoreCase))
            {
                return TrimTrailingPunctuation(line);
            }
        }

        return null;
    }

    private static string? ExtractReturnSummary(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var match = Regex.Match(description, "\\breturns?\\s+(?<value>[^.\\n]+)", RegexOptions.IgnoreCase);
        if (match.Success)
            return TrimTrailingPunctuation(NormalizeWhitespace(match.Groups["value"].Value));

        return null;
    }

    private static List<string> CollectFollowingListItems(string[] lines, int startIndex)
    {
        var items = new List<string>();

        for (var i = startIndex; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (items.Count > 0)
                    break;

                continue;
            }

            if (IsSectionHeader(trimmed))
                break;

            if (IsBulletLine(trimmed))
            {
                items.Add(CleanBullet(trimmed));
                continue;
            }

            if (items.Count == 0)
                items.Add(trimmed);
            else
                break;
        }

        return items;
    }

    private static bool IsSectionHeader(string value)
        => value.EndsWith(':') && !IsBulletLine(value) && value.Length < 80;

    private static bool IsBulletLine(string value)
        => value.StartsWith("- ", StringComparison.Ordinal)
            || value.StartsWith("• ", StringComparison.Ordinal)
            || Regex.IsMatch(value, "^\\d+\\.\\s");

    private static string CleanBullet(string value)
        => TrimTrailingPunctuation(NormalizeWhitespace(Regex.Replace(value, "^(?:-|•|\\d+\\.)\\s*", string.Empty)));

    private static string JoinCompactPhrases(IReadOnlyList<string> items)
    {
        var phrases = items
            .Select(item => LowercaseFirst(TrimTrailingPunctuation(item)))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        return phrases.Count switch
        {
            0 => string.Empty,
            1 => phrases[0],
            _ => string.Join(" or ", phrases)
        };
    }

    private static string RemoveLeadingDirective(string value)
    {
        var cleaned = value;
        cleaned = Regex.Replace(cleaned, "^Always use this tool when\\s+", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, "^Use this tool whenever\\s+", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, "^Use this tool when\\s+", string.Empty, RegexOptions.IgnoreCase);
        return NormalizeWhitespace(cleaned);
    }

    private static string? ExtractResourceFromSentence(string sentence)
    {
        var tokens = Regex.Matches(HumanizeIdentifier(sentence), "[A-Za-z0-9]+")
            .Select(match => match.Value)
            .ToList();

        if (tokens.Count < 2)
            return null;

        var index = 1;
        while (index < tokens.Count && IsArticle(tokens[index]))
            index++;

        var resourceTokens = new List<string>();
        for (; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (ResourceStopWords.Contains(token))
                break;

            resourceTokens.Add(token.ToLowerInvariant());
            if (resourceTokens.Count >= 4)
                break;
        }

        return resourceTokens.Count == 0 ? null : string.Join(" ", resourceTokens);
    }

    private static bool IsArticle(string value)
        => value.Equals("a", StringComparison.OrdinalIgnoreCase)
            || value.Equals("an", StringComparison.OrdinalIgnoreCase)
            || value.Equals("the", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractLeadingWord(string value)
    {
        var match = Regex.Match(HumanizeIdentifier(value), "[A-Za-z][A-Za-z0-9]*");
        return match.Success ? match.Value : null;
    }

    private static IEnumerable<string> GetSalientMemberNames(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        if (IsSimpleType(effectiveType))
            return [];

        return effectiveType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod is not null)
            .Where(property => property.GetIndexParameters().Length == 0)
            .Where(property => !property.IsDefined(typeof(IgnoreDataMemberAttribute), inherit: true))
            .OrderByDescending(property => ScoreMemberName(property.Name))
            .ThenBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(property => property.Name)
            .Where(name => !ParameterNoiseNames.Contains(name))
            .Take(4)
            .ToList();
    }

    private static int ScoreMemberName(string name)
    {
        if (ParameterNoiseNames.Contains(name))
            return int.MinValue;

        var score = 0;
        if (name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)) score += 80;
        if (name.Contains("FileId", StringComparison.OrdinalIgnoreCase)) score += 90;
        if (name.Contains("Path", StringComparison.OrdinalIgnoreCase)) score += 75;
        if (name.Contains("File", StringComparison.OrdinalIgnoreCase)) score += 70;
        if (name.Contains("Format", StringComparison.OrdinalIgnoreCase)) score += 65;
        if (name.Contains("Question", StringComparison.OrdinalIgnoreCase)) score += 60;
        if (name.Contains("Data", StringComparison.OrdinalIgnoreCase)) score += 55;
        if (name.Contains("Type", StringComparison.OrdinalIgnoreCase)) score += 45;
        if (name.Contains("Name", StringComparison.OrdinalIgnoreCase)) score += 35;
        if (name.Contains("Description", StringComparison.OrdinalIgnoreCase)) score += 35;
        return score;
    }

    private static bool IsSimpleType(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        return effectiveType.IsPrimitive
            || effectiveType.IsEnum
            || effectiveType == typeof(string)
            || effectiveType == typeof(decimal)
            || effectiveType == typeof(Guid)
            || effectiveType == typeof(DateTime)
            || effectiveType == typeof(DateTimeOffset)
            || effectiveType == typeof(DateOnly)
            || effectiveType == typeof(TimeOnly)
            || effectiveType == typeof(bool)
            || effectiveType == typeof(object)
            || effectiveType == typeof(System.Text.Json.JsonElement)
            || effectiveType == typeof(System.Text.Json.Nodes.JsonNode)
            || effectiveType == typeof(System.Text.Json.Nodes.JsonObject)
            || effectiveType == typeof(System.Text.Json.Nodes.JsonArray);
    }

    private static Type UnwrapTaskType(Type type)
    {
        if (type == typeof(Task) || type == typeof(ValueTask))
            return typeof(void);

        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            return type.GetGenericArguments()[0];

        return type;
    }

    private static bool IsContentBlockSequence(Type type)
    {
        if (type.IsArray)
            return string.Equals(type.GetElementType()?.Name, "ContentBlock", StringComparison.OrdinalIgnoreCase);

        if (!type.IsGenericType)
            return false;

        return type.GetInterfaces()
            .Append(type)
            .Where(candidate => candidate.IsGenericType)
            .Any(candidate => candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                && string.Equals(candidate.GetGenericArguments()[0].Name, "ContentBlock", StringComparison.OrdinalIgnoreCase));
    }

    private static string HumanizeTypeName(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        return HumanizeIdentifier(TrimKnownSuffixes(effectiveType.Name));
    }

    private static string TrimKnownSuffixes(string value)
    {
        var trimmed = value;
        foreach (var suffix in new[] { "Request", "Response", "GETMany", "GET", "POST", "PATCH", "DELETE", "Dto", "DTO" })
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^suffix.Length];
                break;
            }
        }

        return trimmed;
    }

    private static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var spaced = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
        spaced = spaced.Replace('_', ' ').Replace('-', ' ');
        return NormalizeWhitespace(spaced);
    }

    private static string NormalizeDescription(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("\r\n", "\n").Trim();

    private static string NormalizeWhitespace(string value)
        => Regex.Replace(value, "\\s+", " ").Trim();

    private static string LowercaseFirst(string value)
        => string.IsNullOrWhiteSpace(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private static string TrimTrailingPunctuation(string value)
        => value.Trim().TrimEnd('.', '!', '?', ':', ';', ',');

    private static string EnsureSentence(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Tool summary unavailable.";

        return value.EndsWith('.') || value.EndsWith('!') || value.EndsWith('?') ? value : value + ".";
    }
}
