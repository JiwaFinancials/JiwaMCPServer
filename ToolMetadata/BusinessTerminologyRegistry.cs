using System.Text.RegularExpressions;

namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessTerminologyRegistry
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _terms;
    private readonly IReadOnlyDictionary<string, string> _entityByNormalizedTerm;

    public BusinessTerminologyRegistry()
    {
        _terms = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Supplier"] = ["supplier", "creditor", "vendor", "payee"],
            ["Customer"] = ["customer", "debtor", "client", "account holder"],
            ["PurchaseOrder"] = ["purchase order", "po", "supplier order", "procurement order", "purchase orders"],
            ["SalesOrder"] = ["sales order", "so", "sales invoice", "customer invoice", "invoice", "customer order", "Sales Orders"],
            ["CreditorPurchase"] = ["creditor purchase", "creditor purchases", "supplier invoice", "supplier bill", "vendor bill", "creditor invoice", "ap invoice", "accounts payable invoice"],
            ["Inventory"] = ["inventory", "stock", "product", "item", "sku", "part"],
            ["Warehouse"] = ["warehouse", "location", "storage location"],
            ["Document"] = ["document", "invoice file", "pdf", "scan", "attachment"],
            ["File"] = ["file", "csv", "excel", "xml", "json", "local file"],
            ["Form"] = ["form", "screen", "plugin", "module"],
            ["Schema"] = ["schema", "dto", "request model", "response model"]
        };

        var entityLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in _terms)
        {
            var canonicalEntity = entry.Key;
            entityLookup[NormalizeTerm(canonicalEntity)] = canonicalEntity;
            entityLookup[NormalizeTerm(SplitPascalCase(canonicalEntity))] = canonicalEntity;

            foreach (var synonym in entry.Value)
            {
                entityLookup[NormalizeTerm(synonym)] = canonicalEntity;
            }
        }

        _entityByNormalizedTerm = entityLookup;
    }

    public IReadOnlyList<string> GetSynonyms(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return [];

        return _terms.TryGetValue(entityType.Trim(), out var synonyms)
            ? synonyms
            : [];
    }

    public IReadOnlyList<string> GetEntityTypes() => _terms.Keys.ToArray();

    public bool TryResolveEntity(string? term, out string entityType)
    {
        entityType = string.Empty;
        if (string.IsNullOrWhiteSpace(term))
            return false;

        var normalizedTerm = NormalizeTerm(term);
        if (string.IsNullOrWhiteSpace(normalizedTerm))
            return false;

        if (_entityByNormalizedTerm.TryGetValue(normalizedTerm, out var canonicalEntity))
        {
            entityType = canonicalEntity;
            return true;
        }

        return false;
    }

    public IReadOnlyList<string> ExpandTerms(IEnumerable<string> terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        var expanded = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var term in terms)
        {
            if (string.IsNullOrWhiteSpace(term))
                continue;

            var trimmed = term.Trim();
            if (seen.Add(trimmed))
            {
                expanded.Add(trimmed);
            }

            if (!TryResolveEntity(trimmed, out var canonicalEntity))
                continue;

            foreach (var synonym in GetSynonyms(canonicalEntity))
            {
                if (seen.Add(synonym))
                {
                    expanded.Add(synonym);
                }
            }
        }

        return expanded;
    }

    private static string NormalizeTerm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var pascalExpanded = SplitPascalCase(value.Trim());
        var normalized = Regex.Replace(pascalExpanded.ToLowerInvariant(), @"[^\p{L}\p{Nd}]+", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    private static string SplitPascalCase(string value)
        => Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
}
