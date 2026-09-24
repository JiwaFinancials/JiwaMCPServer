using JiwaMcpServer.Agent.Models;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Agent.Routing;

public sealed class IntentExtractor : IIntentExtractor
{
    public IntentContext Extract(string userPrompt, WorkingContext? workingContext = null)
    {
        var prompt = userPrompt?.Trim() ?? string.Empty;
        var intent = new IntentContext
        {
            Action = ResolveAction(prompt),
            EntityType = ResolveEntityType(prompt, workingContext)
        };

        foreach (var pair in ExtractParameters(prompt, intent.Action))
            intent.Parameters[pair.Key] = pair.Value;

        if (intent.Parameters.Count > 0 && string.IsNullOrWhiteSpace(intent.Action))
            intent.Action = "Update";

        return intent;
    }

    private static string ResolveAction(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return string.Empty;

        foreach (var action in new[] { "Approve", "Update", "Delete", "Create", "Query", "Read" })
        {
            var canonical = RoutingVocabulary.ResolveCanonicalAction(action);
            if (canonical.Equals("Read", StringComparison.OrdinalIgnoreCase) && RoutingVocabulary.ContainsKeyword(prompt, "transactions"))
                continue;

            if (canonical.Equals("Query", StringComparison.OrdinalIgnoreCase)
                && (RoutingVocabulary.ContainsKeyword(prompt, "transactions")
                    || RoutingVocabulary.ContainsKeyword(prompt, "history")
                    || RoutingVocabulary.ContainsKeyword(prompt, "list")
                    || RoutingVocabulary.ContainsKeyword(prompt, "find")
                    || RoutingVocabulary.ContainsKeyword(prompt, "search")
                    || RoutingVocabulary.ContainsKeyword(prompt, "query")))
            {
                return canonical;
            }

            if (RoutingVocabulary.ResolveCanonicalAction(prompt).Equals(canonical, StringComparison.OrdinalIgnoreCase))
                return canonical;
        }

        return string.Empty;
    }

    private static string ResolveEntityType(string prompt, WorkingContext? workingContext)
    {
        var explicitEntityType = RoutingVocabulary.TryResolveEntityTypeFromText(prompt);
        if (!string.IsNullOrWhiteSpace(explicitEntityType))
            return explicitEntityType;

        if (workingContext?.FocusedEntity is not null
            && (RoutingVocabulary.ContainsReferencePronoun(prompt)
                || (!string.IsNullOrWhiteSpace(prompt) && !Regex.IsMatch(prompt, @"\b(customer|debtor|supplier|creditor|purchase|invoice|product|part|inventory|document|file)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))))
        {
            return workingContext.FocusedEntity.EntityType;
        }

        return string.Empty;
    }

    private static IReadOnlyDictionary<string, string> ExtractParameters(string prompt, string action)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(prompt))
            return parameters;

        var updateMatch = Regex.Match(
            prompt,
            @"\b(?:change|update|modify|edit|set|correct|amend)\b\s+(?:the\s+)?(?<field>[a-z][a-z0-9 ]{1,40}?)(?:\s+(?:to|as)\s+(?<value>.+))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (updateMatch.Success)
        {
            var field = NormalizeFieldName(updateMatch.Groups["field"].Value);
            var value = updateMatch.Groups["value"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(field))
                parameters[field] = string.IsNullOrWhiteSpace(value) ? "pending" : value;
        }

        var entityReferenceMatch = Regex.Match(
            prompt,
            @"\b(?:customer|debtor|invoice|purchase order|purchase|supplier|creditor|product|part)\b\s+(?<identifier>[A-Za-z0-9][A-Za-z0-9\-/]*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (entityReferenceMatch.Success)
            parameters.TryAdd("identifier", entityReferenceMatch.Groups["identifier"].Value.Trim());

        var productPriceReferenceMatch = Regex.Match(
            prompt,
            @"\b(?:rrp|retail price|sell price|price)\b\s+(?:of|for)?\s*(?<identifier>[A-Za-z0-9][A-Za-z0-9\-/]*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (productPriceReferenceMatch.Success)
            parameters.TryAdd("identifier", productPriceReferenceMatch.Groups["identifier"].Value.Trim());

        if (string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase)
            && !parameters.ContainsKey("status"))
        {
            parameters["status"] = "approved";
        }

        return parameters;
    }

    private static string NormalizeFieldName(string value)
    {
        var normalized = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim().ToLowerInvariant();
        return normalized switch
        {
            "phone number" => "phone",
            _ => normalized
        };
    }
}
