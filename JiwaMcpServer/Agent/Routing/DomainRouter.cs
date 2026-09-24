using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public sealed class DomainRouter : IDomainRouter
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DomainKeywords = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["CustomerTools"] =
        [
            "customer",
            "customers",
            "debtor",
            "debtors",
            "account",
            "account number",
            "customer number",
            "customer code"
        ],
        ["InventoryTools"] =
        [
            "inventory",
            "product",
            "products",
            "part",
            "parts",
            "part no",
            "part number",
            "sku",
            "item code",
            "rrp",
            "retail price",
            "sell price",
            "price",
            "stock",
            "warehouse"
        ],
        ["CreditorPurchaseTools"] =
        [
            "supplier",
            "suppliers",
            "creditor",
            "creditors",
            "purchase",
            "purchases",
            "purchase order",
            "purchase orders",
            "supplier invoice",
            "invoice",
            "invoices",
            "accounts receivable",
            "accounts payable",
            "payments"
        ]
    };

    private static readonly IReadOnlyList<string> DocumentSupportKeywords =
    [
        "document",
        "file",
        "import",
        "export",
        "upload",
        "conversion",
        "convert",
        "csv",
        "xlsx",
        "excel",
        "pdf",
        "docx",
        "xml",
        "json"
    ];

    public IReadOnlyCollection<string> ResolveDomains(string userPrompt)
        => ResolveDomains(userPrompt, intentContext: null, workingContext: null);

    public IReadOnlyCollection<string> ResolveDomains(string userPrompt, string? conversationContext)
        => ResolveDomains(
            userPrompt,
            intentContext: null,
            workingContext: string.IsNullOrWhiteSpace(conversationContext)
                ? null
                : new WorkingContext { ConversationSummary = conversationContext });

    public IReadOnlyCollection<string> ResolveDomains(string userPrompt, IntentContext? intentContext, WorkingContext? workingContext)
    {
        if (string.IsNullOrWhiteSpace(userPrompt)
            && string.IsNullOrWhiteSpace(intentContext?.EntityType)
            && string.IsNullOrWhiteSpace(workingContext?.CurrentDomain)
            && string.IsNullOrWhiteSpace(workingContext?.FocusedEntity?.EntityType)
            && string.IsNullOrWhiteSpace(workingContext?.ConversationSummary))
        {
            return [];
        }

        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddDomainsFromPrompt(resolved, userPrompt);
        AddDomainsFromConversationSummary(resolved, workingContext?.ConversationSummary);
        AddDomainForEntity(resolved, intentContext?.EntityType);

        if (RoutingVocabulary.ContainsReferencePronoun(userPrompt)
            || string.IsNullOrWhiteSpace(RoutingVocabulary.TryResolveEntityTypeFromText(userPrompt)))
        {
            AddDomain(resolved, workingContext?.CurrentDomain);
            AddDomainForEntity(resolved, workingContext?.FocusedEntity?.EntityType);
        }

        if (resolved.Count == 0)
        {
            AddDomain(resolved, workingContext?.CurrentDomain);
            AddDomainForEntity(resolved, workingContext?.FocusedEntity?.EntityType);
        }

        return resolved
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddDomainsFromPrompt(HashSet<string> resolved, string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return;

        foreach (var pair in DomainKeywords)
        {
            if (pair.Value.Any(keyword => RoutingVocabulary.ContainsKeyword(userPrompt, keyword)))
                resolved.Add(pair.Key);
        }

        if (DocumentSupportKeywords.Any(keyword => RoutingVocabulary.ContainsKeyword(userPrompt, keyword)))
            resolved.Add("DocumentTools");
    }

    private static void AddDomainsFromConversationSummary(HashSet<string> resolved, string? conversationSummary)
    {
        if (string.IsNullOrWhiteSpace(conversationSummary))
            return;

        if (DocumentSupportKeywords.Any(keyword => RoutingVocabulary.ContainsKeyword(conversationSummary, keyword)))
            resolved.Add("DocumentTools");
    }

    private static void AddDomainForEntity(HashSet<string> resolved, string? entityType)
        => AddDomain(resolved, RoutingVocabulary.ResolveDomainForEntityType(entityType));

    private static void AddDomain(HashSet<string> resolved, string? domain)
    {
        var canonicalDomain = RoutingVocabulary.ResolveCanonicalDomain(domain);
        if (!string.IsNullOrWhiteSpace(canonicalDomain))
            resolved.Add(canonicalDomain);
    }
}
