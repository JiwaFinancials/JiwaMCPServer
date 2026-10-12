using JiwaMcpServer.Agent.Models;
using JiwaMcpServer.Agent.Routing;
using JiwaMcpServer.Agent.SchemaResolver;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Agent.Catalog;

public sealed class ToolShortlistService : IToolShortlistService
{
    private readonly IToolCatalog _toolCatalog;
    private readonly IDomainRouter _domainRouter;
    private readonly IToolSchemaResolver _schemaResolver;
    private readonly IIntentExtractor _intentExtractor;
    private readonly IToolRoutingContextService _contextService;
    private readonly IReadOnlyList<IToolSelectionOverride> _selectionOverrides;
    private readonly ILogger<ToolShortlistService> _logger;

    public ToolShortlistService(
        IToolCatalog toolCatalog,
        IDomainRouter domainRouter,
        IToolSchemaResolver schemaResolver,
        IIntentExtractor intentExtractor,
        IToolRoutingContextService contextService,
        IEnumerable<IToolSelectionOverride> selectionOverrides,
        ILogger<ToolShortlistService> logger)
    {
        _toolCatalog = toolCatalog;
        _domainRouter = domainRouter;
        _schemaResolver = schemaResolver;
        _intentExtractor = intentExtractor;
        _contextService = contextService;
        _selectionOverrides = selectionOverrides?.ToList() ?? [];
        _logger = logger;
    }

    public ToolRouteResult Shortlist(ToolRouteRequest request, CancellationToken ct = default)
    {
        var safeRequest = request ?? new ToolRouteRequest();
        var prompt = safeRequest.Prompt ?? string.Empty;
        var maxTools = safeRequest.MaxTools <= 0 ? 20 : safeRequest.MaxTools;
        var workingContext = _contextService.ResolveForRouting(safeRequest);
        var intent = _intentExtractor.Extract(prompt, workingContext);
        var resolvedDomains = ResolveDomains(prompt, intent, workingContext);
        var allTools = _toolCatalog.GetAllTools();
        var isConfirmationPrompt = RoutingVocabulary.IsConfirmationPrompt(prompt);
        var hasRecoverableConfirmationContext = HasRecoverableConfirmationContext(workingContext);

        var domainTools = resolvedDomains.Count > 0
            ? _toolCatalog.GetToolsForDomains(resolvedDomains)
            : isConfirmationPrompt && !hasRecoverableConfirmationContext
                ? []
                : allTools;

        if (domainTools.Count == 0)
        {
            if (isConfirmationPrompt && !hasRecoverableConfirmationContext)
            {
                _logger.LogInformation("Confirmation prompt '{Prompt}' arrived without recoverable context. Returning no tools.", prompt);
            }
            else
            {
                _logger.LogWarning("No tools matched the resolved domains for prompt '{Prompt}'. Falling back to the full catalog.", prompt);
                domainTools = allTools;
            }
        }

        var candidateTools = isConfirmationPrompt
            ? FilterConfirmationCandidateTools(domainTools, allTools, workingContext)
            : FilterCandidateTools(domainTools, intent, workingContext);
        var rankingInput = BuildRankingInput(prompt, intent, workingContext, resolvedDomains);

        var ranked = candidateTools
            .Select(tool => new ToolSelectionCandidate
            {
                Tool = tool,
                RelevanceScore = ScoreTool(tool, rankingInput)
            })
            .Where(item => item.RelevanceScore > 0 || candidateTools.Count <= maxTools)
            .OrderByDescending(item => item.RelevanceScore)
            .ThenBy(item => item.Tool.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxTools))
            .ToList();

        if (isConfirmationPrompt)
            PromotePreferredConfirmationTool(ranked, workingContext);

        var overrideContext = new ToolSelectionContext(
            safeRequest.ConversationId,
            prompt,
            safeRequest.ConversationSummary,
            safeRequest.ConversationContext,
            intent,
            workingContext,
            resolvedDomains,
            candidateTools,
            allTools,
            ranked,
            maxTools);

        if (isConfirmationPrompt && !hasRecoverableConfirmationContext)
        {
            overrideContext.AddAdjustment("Confirmation prompt had no recoverable context, so no tool schemas were selected.");
        }

        foreach (var selectionOverride in _selectionOverrides)
            selectionOverride.Apply(overrideContext);

        var finalSelection = overrideContext.BuildSelection();
        var finalDomains = ResolveSelectedDomains(finalSelection, resolvedDomains);
        var selectedNames = finalSelection.Select(item => item.Tool.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var schemaMap = _schemaResolver.ResolveAsync(selectedNames, ct).GetAwaiter().GetResult();

        var selectedTools = finalSelection
            .Select(item => new ToolRouteItem
            {
                Name = item.Tool.Name,
                Description = item.Tool.Description,
                Domain = item.Tool.Domain,
                EntityType = item.Tool.Metadata.EntityType,
                SupportedActions = item.Tool.Metadata.SupportedActions,
                RelevanceScore = item.RelevanceScore,
                Schema = schemaMap.TryGetValue(item.Tool.Name, out var schema)
                    ? schema.ParametersSchema?.ToJsonString() ?? "{}"
                    : "{}"
            })
            .ToList();

        var scores = finalSelection
            .Select(item => new ToolRouteScore
            {
                Name = item.Tool.Name,
                Domain = item.Tool.Domain,
                EntityType = item.Tool.Metadata.EntityType,
                Score = item.RelevanceScore
            })
            .ToList();

        var effectiveContext = new WorkingContext
        {
            FocusedEntity = CloneEntity(workingContext.FocusedEntity),
            CurrentDomain = finalDomains.FirstOrDefault() ?? workingContext.CurrentDomain,
            ConversationSummary = workingContext.ConversationSummary,
            LastIntent = DescribeIntent(intent),
            LastToolName = workingContext.LastToolName
        };

        _logger.LogInformation(
            "Resolved domains {Domains}, candidateTools={CandidateCount}, selectedTools={ToolCount}, selectionAdjustments={AdjustmentCount} for prompt '{Prompt}'.",
            string.Join(", ", finalDomains.EmptyIfNull()),
            candidateTools.Count,
            selectedTools.Count,
            overrideContext.Adjustments.Count,
            prompt);

        _logger.LogInformation(
            "Selected tools: {SelectedTools}",
            string.Join(", ", selectedTools.Select(tool => tool.Name)));

        return new ToolRouteResult
        {
            Domains = finalDomains,
            Intent = intent,
            WorkingContext = effectiveContext,
            RankingInput = rankingInput,
            Tools = selectedTools,
            Scores = scores,
            SelectionAdjustments = overrideContext.Adjustments
        };
    }

    private IReadOnlyList<string> ResolveDomains(string prompt, IntentContext intent, WorkingContext workingContext)
    {
        var currentDomain = RoutingVocabulary.ResolveCanonicalDomain(workingContext.CurrentDomain);
        var domains = _domainRouter.ResolveDomains(prompt, intent, workingContext)
            .Select(RoutingVocabulary.ResolveCanonicalDomain)
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shouldPrioritizeCurrentDomain = !string.IsNullOrWhiteSpace(currentDomain)
            && (RoutingVocabulary.ContainsReferencePronoun(prompt)
                || string.IsNullOrWhiteSpace(RoutingVocabulary.TryResolveEntityTypeFromText(prompt)));

        if (shouldPrioritizeCurrentDomain)
        {
            domains.RemoveAll(domain => domain.Equals(currentDomain, StringComparison.OrdinalIgnoreCase));
            domains.Insert(0, currentDomain);
        }

        return domains;
    }

    private static IReadOnlyList<ToolDefinition> FilterCandidateTools(
        IReadOnlyList<ToolDefinition> domainTools,
        IntentContext intent,
        WorkingContext workingContext)
    {
        if (domainTools.Count == 0)
            return domainTools;

        var retainedDocumentExportTool = ShouldRetainDocumentExportTool(workingContext)
            ? domainTools.FirstOrDefault(tool => tool.Name.Equals("CreateDataExport", StringComparison.OrdinalIgnoreCase))
            : null;

        var entityType = FirstNonEmpty(intent.EntityType, workingContext.FocusedEntity?.EntityType);
        if (RequiresProductIdentifierLookup(intent, entityType, workingContext))
        {
            var lookupTool = domainTools.FirstOrDefault(tool => tool.Name.Equals("QueryInventory", StringComparison.OrdinalIgnoreCase));
            if (lookupTool is not null)
                return IncludeTool(domainTools, retainedDocumentExportTool ?? lookupTool);
        }

        if (RequiresCustomerIdentifierLookup(intent, entityType, workingContext))
        {
            var lookupTool = domainTools.FirstOrDefault(tool => tool.Name.Equals("QueryCustomers", StringComparison.OrdinalIgnoreCase));
            if (lookupTool is not null)
                return IncludeTool(domainTools, retainedDocumentExportTool ?? lookupTool);
        }

        var exactMatches = domainTools
            .Where(tool => MatchesEntityType(tool, entityType) && MatchesAction(tool, intent.Action))
            .ToList();
        if (exactMatches.Count > 0)
            return IncludeTool(exactMatches, retainedDocumentExportTool);

        var actionMatches = domainTools
            .Where(tool => MatchesAction(tool, intent.Action))
            .ToList();
        if (actionMatches.Count > 0)
            return IncludeTool(actionMatches, retainedDocumentExportTool);

        var entityMatches = domainTools
            .Where(tool => MatchesEntityType(tool, entityType))
            .ToList();
        if (entityMatches.Count > 0)
            return IncludeTool(entityMatches, retainedDocumentExportTool);

        return IncludeTool(domainTools, retainedDocumentExportTool);
    }

    private static ToolRankingInput BuildRankingInput(
        string prompt,
        IntentContext intent,
        WorkingContext workingContext,
        IReadOnlyList<string> domains)
        => new()
        {
            UserMessage = prompt,
            Intent = new IntentContext
            {
                Action = intent.Action,
                EntityType = intent.EntityType,
                Parameters = new Dictionary<string, string>(intent.Parameters, StringComparer.OrdinalIgnoreCase)
            },
            FocusedEntity = CloneEntity(workingContext.FocusedEntity),
            CurrentDomain = domains.FirstOrDefault() ?? workingContext.CurrentDomain,
            ConversationSummary = workingContext.ConversationSummary
        };

    private static bool MatchesAction(ToolDefinition tool, string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return false;

        var supportedActions = tool.Metadata.SupportedActions.Length > 0
            ? tool.Metadata.SupportedActions
            : string.IsNullOrWhiteSpace(tool.Action)
                ? []
                : [tool.Action];

        return supportedActions.Any(candidate => RoutingVocabulary.AreActionsCompatible(action, candidate));
    }

    private static bool MatchesEntityType(ToolDefinition tool, string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return false;

        if (RoutingVocabulary.AreEntityTypesCompatible(tool.Metadata.EntityType, entityType))
            return true;

        if (RoutingVocabulary.AreEntityTypesCompatible(tool.Resource, entityType))
            return true;

        return tool.Keywords.Any(keyword => RoutingVocabulary.AreEntityTypesCompatible(keyword, entityType));
    }

    private static double ScoreTool(ToolDefinition tool, ToolRankingInput rankingInput)
    {
        if (tool is null)
            return 0d;

        var entityType = FirstNonEmpty(rankingInput.Intent.EntityType, rankingInput.FocusedEntity?.EntityType);
        var score = 0d;

        if (!string.IsNullOrWhiteSpace(rankingInput.Intent.Action) && MatchesAction(tool, rankingInput.Intent.Action))
            score += 24d;

        if (!string.IsNullOrWhiteSpace(entityType) && MatchesEntityType(tool, entityType))
            score += 30d;

        if (LooksLikePartNoResolutionPrompt(rankingInput.UserMessage))
        {
            if (tool.Name.Equals("QueryInventory", StringComparison.OrdinalIgnoreCase))
                score += 60d;

            if (tool.Name.Equals("ModifyProduct", StringComparison.OrdinalIgnoreCase))
                score -= 25d;
        }

        if (LooksLikeStockQueryPrompt(rankingInput.UserMessage, rankingInput.Intent.Action, entityType, rankingInput.FocusedEntity))
        {
            if (tool.Name.Equals("GetStockOnHand", StringComparison.OrdinalIgnoreCase))
                score += 80d;

            if (tool.Name.Equals("QueryInventory", StringComparison.OrdinalIgnoreCase))
                score -= 35d;

            if (tool.Name.Equals("GetProduct", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("ModifyProduct", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("DeleteProduct", StringComparison.OrdinalIgnoreCase))
            {
                score -= 10d;
            }
        }

        if (RequiresProductIdentifierLookup(rankingInput.Intent, entityType, new WorkingContext { FocusedEntity = rankingInput.FocusedEntity }))
        {
            if (tool.Name.Equals("QueryInventory", StringComparison.OrdinalIgnoreCase))
                score += 60d;

            if (tool.Name.Equals("GetProduct", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("ModifyProduct", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("DeleteProduct", StringComparison.OrdinalIgnoreCase))
            {
                score -= 25d;
            }
        }

        if (LooksLikeGenericProductDiscoveryPrompt(rankingInput.UserMessage, rankingInput.Intent.Action, entityType))
        {
            if (tool.Name.Equals("QueryInventory", StringComparison.OrdinalIgnoreCase))
                score += 45d;

            if (tool.Name.Equals("SearchProductClassifications", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("SearchProductCategories", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("GetStockOnHand", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("GetProductPicture", StringComparison.OrdinalIgnoreCase))
            {
                score -= 20d;
            }
        }

        if (LooksLikeGroupedInventoryCountPrompt(rankingInput.UserMessage))
        {
            if (tool.Name.Equals("AggregateInventoryCounts", StringComparison.OrdinalIgnoreCase))
                score += 90d;

            if (tool.Name.Equals("QueryInventory", StringComparison.OrdinalIgnoreCase))
                score -= 30d;

            if (tool.Name.Equals("SearchProductClassifications", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("SearchProductCategories", StringComparison.OrdinalIgnoreCase))
            {
                score -= 10d;
            }
        }

        if (RequiresCustomerIdentifierLookup(rankingInput.Intent, entityType, new WorkingContext { FocusedEntity = rankingInput.FocusedEntity }))
        {
            if (tool.Name.Equals("QueryCustomers", StringComparison.OrdinalIgnoreCase))
                score += 60d;

            if (tool.Name.Equals("GetCustomer", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("ModifyCustomer", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("DeleteCustomer", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("QueryCustomerTransactions", StringComparison.OrdinalIgnoreCase))
            {
                score -= 25d;
            }
        }

        if (!string.IsNullOrWhiteSpace(rankingInput.CurrentDomain)
            && RoutingVocabulary.ResolveCanonicalDomain(tool.Domain).Equals(RoutingVocabulary.ResolveCanonicalDomain(rankingInput.CurrentDomain), StringComparison.OrdinalIgnoreCase))
        {
            score += 18d;
        }

        if (tool.Name.Equals("CreateDataExport", StringComparison.OrdinalIgnoreCase)
            && ShouldRetainDocumentExportTool(new WorkingContext { ConversationSummary = rankingInput.ConversationSummary }))
        {
            score += 20d;
        }

        if (LooksLikeDownloadExportPrompt(rankingInput.UserMessage, rankingInput.ConversationSummary))
        {
            if (tool.Name.Equals("CreateDataExport", StringComparison.OrdinalIgnoreCase))
                score += 100d;

            if (tool.Name.Equals("ConvertDocument", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("ExtractDocumentData", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("ExtractLocalDocumentData", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("UploadLocalDocument", StringComparison.OrdinalIgnoreCase)
                || tool.Name.Equals("QueryUploadedDocument", StringComparison.OrdinalIgnoreCase))
            {
                score -= 25d;
            }
        }

        var primaryTokens = Tokenize(string.Join(' ', new[]
        {
            rankingInput.UserMessage,
            rankingInput.Intent.Action,
            rankingInput.Intent.EntityType,
            string.Join(' ', rankingInput.Intent.Parameters.Keys),
            string.Join(' ', rankingInput.Intent.Parameters.Values),
            rankingInput.FocusedEntity?.EntityType,
            rankingInput.FocusedEntity?.EntityId,
            rankingInput.FocusedEntity?.DisplayName,
            rankingInput.CurrentDomain
        }.Where(value => !string.IsNullOrWhiteSpace(value))));

        var summaryTokens = Tokenize(rankingInput.ConversationSummary);
        var haystack = string.Join(' ', new[]
        {
            tool.Name,
            tool.Description,
            tool.Domain,
            tool.Action,
            tool.Resource,
            tool.Metadata.EntityType,
            string.Join(' ', tool.Metadata.SupportedActions),
            tool.InputSummary,
            tool.OutputSummary,
            tool.Prerequisites,
            string.Join(' ', tool.Keywords ?? [])
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        foreach (var token in primaryTokens)
        {
            if (tool.Name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 8d;

            if (haystack.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 2d;
        }

        foreach (var token in summaryTokens)
        {
            if (tool.Name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 1d;

            if (haystack.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 0.25d;
        }

        var safeKeywords = tool.Keywords ?? Array.Empty<string>();
        if (safeKeywords.Any(keyword => !string.IsNullOrWhiteSpace(keyword)
            && keyword.IndexOfAny([' ', '-']) >= 0
            && rankingInput.UserMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            score += 2d;
        }

        return score;
    }

    private static bool LooksLikePartNoResolutionPrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var priceField = Regex.IsMatch(prompt, @"\b(?:rrp|retail price|sell price|price)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var identifier = Regex.IsMatch(prompt, @"\b[A-Za-z]{1,5}\d{2,10}\b|\b[A-Za-z0-9][A-Za-z0-9\-/]{2,20}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var partHint = Regex.IsMatch(prompt, @"\b(?:part\s*(?:no|number)|sku|item\s*code)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return priceField && (identifier || partHint);
    }

    private static bool LooksLikeStockQueryPrompt(string? prompt, string? action, string? entityType, ActiveEntity? focusedEntity)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        if (!RoutingVocabulary.AreActionsCompatible(action, "Read")
            && !RoutingVocabulary.AreActionsCompatible(action, "Query"))
        {
            return false;
        }

        var productContext = RoutingVocabulary.AreEntityTypesCompatible(entityType, "Product")
            || (focusedEntity is not null && RoutingVocabulary.AreEntityTypesCompatible(focusedEntity.EntityType, "Product"));
        if (!productContext)
            return false;

        return Regex.IsMatch(prompt, @"\b(?:stock|qty|quantity|available|availability|on\s+hand|inventory\s+balance|warehouse\s+qty)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool LooksLikeGenericProductDiscoveryPrompt(string? prompt, string? action, string? entityType)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        if (!RoutingVocabulary.AreEntityTypesCompatible(entityType, "Product"))
            return false;

        if (!RoutingVocabulary.AreActionsCompatible(action, "Query") && !RoutingVocabulary.AreActionsCompatible(action, "Read"))
            return false;

        if (Regex.IsMatch(prompt, @"\b(?:classification|classifications|category|categories|stock\s+on\s+hand|warehouse|image|picture|photo)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return false;

        return Regex.IsMatch(prompt, @"\b(?:part|parts|product|products|inventory|items?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool LooksLikeGroupedInventoryCountPrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var countSignal = Regex.IsMatch(prompt, @"\b(?:count|counts|total|totals|summary|summaries|group(?:ed|ing)?|per|first\s+letter|prefix|starts\s+with)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var inventorySignal = Regex.IsMatch(prompt, @"\b(?:part|parts|product|products|inventory|classification|classifications|category|categories|warehouse|stock)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return countSignal && inventorySignal;
    }

    private static bool LooksLikeDownloadExportPrompt(string? prompt, string? conversationSummary)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var wantsDownload = Regex.IsMatch(prompt, @"\b(?:download|downloadable|export|csv|xlsx|excel|json|xml|pdf|docx)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!wantsDownload)
            return false;

        var convertIntent = Regex.IsMatch(prompt, @"\b(?:convert|conversion|transform)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            || Regex.IsMatch(prompt, @"\bfile\s*id\b|\bupload(?:ed)?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (convertIntent)
            return false;

        return !string.IsNullOrWhiteSpace(conversationSummary)
            || Regex.IsMatch(prompt, @"\b(?:make|create|generate)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool RequiresProductIdentifierLookup(IntentContext intent, string? entityType, WorkingContext workingContext)
    {
        if (!RoutingVocabulary.AreEntityTypesCompatible(entityType, "Product"))
            return false;

        if (!intent.Parameters.TryGetValue("identifier", out var identifier) || string.IsNullOrWhiteSpace(identifier))
            return false;

        return intent.Action switch
        {
            "Read" or "Update" or "Delete" => true,
            _ => false
        };
    }

    private static bool RequiresCustomerIdentifierLookup(IntentContext intent, string? entityType, WorkingContext workingContext)
    {
        if (!RoutingVocabulary.AreEntityTypesCompatible(entityType, "Customer"))
            return false;

        if (!intent.Parameters.TryGetValue("identifier", out var identifier) || string.IsNullOrWhiteSpace(identifier))
            return false;

        return intent.Action switch
        {
            "Read" or "Query" or "Update" or "Delete" => true,
            _ => false
        };
    }

    private static IReadOnlyList<ToolDefinition> FilterConfirmationCandidateTools(
        IReadOnlyList<ToolDefinition> domainTools,
        IReadOnlyList<ToolDefinition> availableTools,
        WorkingContext workingContext)
    {
        if (domainTools.Count == 0)
            return domainTools;

        var retainedDocumentExportTool = ShouldRetainDocumentExportTool(workingContext)
            ? availableTools.FirstOrDefault(tool => tool.Name.Equals("CreateDataExport", StringComparison.OrdinalIgnoreCase))
            : null;

        var summary = workingContext.ConversationSummary ?? string.Empty;
        var groupedInventoryCountIntent = LooksLikeGroupedInventoryCountPrompt(summary)
            || LooksLikeGroupedInventoryCountPrompt(workingContext.LastToolName);

        if (!string.IsNullOrWhiteSpace(workingContext.LastToolName))
        {
            var priorTool = availableTools.FirstOrDefault(tool => tool.Name.Equals(workingContext.LastToolName, StringComparison.OrdinalIgnoreCase));
            if (groupedInventoryCountIntent)
            {
                var aggregateTool = availableTools.FirstOrDefault(tool => tool.Name.Equals("AggregateInventoryCounts", StringComparison.OrdinalIgnoreCase));
                var preferredTools = new List<ToolDefinition>();
                if (aggregateTool is not null)
                    preferredTools.Add(aggregateTool);
                if (priorTool is not null && !string.Equals(priorTool.Name, aggregateTool?.Name, StringComparison.OrdinalIgnoreCase))
                    preferredTools.Add(priorTool);
                if (preferredTools.Count > 0)
                    return IncludeTool(preferredTools, retainedDocumentExportTool);
            }

            if (priorTool is not null)
                return IncludeTool([priorTool], retainedDocumentExportTool);
        }

        var currentDomain = RoutingVocabulary.ResolveCanonicalDomain(workingContext.CurrentDomain);
        if (!string.IsNullOrWhiteSpace(currentDomain))
        {
            var currentDomainTools = domainTools
                .Where(tool => RoutingVocabulary.ResolveCanonicalDomain(tool.Domain).Equals(currentDomain, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (currentDomainTools.Count > 0)
            {
                var preferredCurrentDomainTool = SelectPreferredConfirmationTool(currentDomainTools, currentDomain, workingContext.ConversationSummary);
                if (preferredCurrentDomainTool is not null)
                    return IncludeTool([preferredCurrentDomainTool], retainedDocumentExportTool);

                return IncludeTool(currentDomainTools, retainedDocumentExportTool);
            }
        }

        var fallbackDomain = domainTools
            .Select(tool => RoutingVocabulary.ResolveCanonicalDomain(tool.Domain))
            .FirstOrDefault(domain => !domain.Equals("DocumentTools", StringComparison.OrdinalIgnoreCase))
            ?? domainTools.Select(tool => RoutingVocabulary.ResolveCanonicalDomain(tool.Domain)).FirstOrDefault();

        var preferredFallbackTool = SelectPreferredConfirmationTool(domainTools, fallbackDomain, workingContext.ConversationSummary);
        if (preferredFallbackTool is not null)
            return IncludeTool([preferredFallbackTool], retainedDocumentExportTool);

        return IncludeTool(domainTools, retainedDocumentExportTool);
    }

    private static ToolDefinition? SelectPreferredConfirmationTool(
        IReadOnlyList<ToolDefinition> tools,
        string? currentDomain,
        string? conversationSummary)
    {
        if (tools.Count == 0)
            return null;

        var summary = conversationSummary ?? string.Empty;
        var preferredToolNames = RoutingVocabulary.ResolveCanonicalDomain(currentDomain) switch
        {
            "InventoryTools" when LooksLikeGroupedInventoryCountPrompt(summary)
                => new[] { "AggregateInventoryCounts", "QueryInventory", "GetStockOnHand" },
            "InventoryTools" when Regex.IsMatch(summary, @"\b(stock\s+on\s+hand|warehouse|bin|quantity|qty)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                => new[] { "GetStockOnHand", "QueryInventory" },
            "InventoryTools" => new[] { "QueryInventory", "GetStockOnHand" },
            "CustomerTools" when Regex.IsMatch(summary, @"\b(transaction|transactions|history|payment|payments|balance|balances|invoice|invoices)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                => new[] { "QueryCustomerTransactions", "QueryCustomers" },
            "CustomerTools" => new[] { "QueryCustomers", "QueryCustomerTransactions" },
            "CreditorPurchaseTools" when Regex.IsMatch(summary, @"\bhistory\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                => new[] { "QuerySupplierPurchaseHistory", "QuerySupplierPurchases" },
            "CreditorPurchaseTools" => new[] { "QuerySupplierPurchases", "QuerySupplierPurchaseHistory" },
            _ => Array.Empty<string>()
        };

        foreach (var toolName in preferredToolNames)
        {
            var match = tools.FirstOrDefault(tool => tool.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return null;
    }

    private static void PromotePreferredConfirmationTool(List<ToolSelectionCandidate> rankedTools, WorkingContext workingContext)
    {
        if (rankedTools.Count <= 1)
            return;

        var currentDomain = RoutingVocabulary.ResolveCanonicalDomain(workingContext.CurrentDomain);
        if (string.IsNullOrWhiteSpace(currentDomain))
        {
            currentDomain = rankedTools
                .Select(item => RoutingVocabulary.ResolveCanonicalDomain(item.Tool.Domain))
                .FirstOrDefault(domain => !domain.Equals("DocumentTools", StringComparison.OrdinalIgnoreCase))
                ?? rankedTools.Select(item => RoutingVocabulary.ResolveCanonicalDomain(item.Tool.Domain)).FirstOrDefault()
                ?? string.Empty;
        }

        var preferredTool = SelectPreferredConfirmationTool(
            rankedTools.Select(item => item.Tool).ToList(),
            currentDomain,
            workingContext.ConversationSummary);
        if (preferredTool is null)
            return;

        var index = rankedTools.FindIndex(item => item.Tool.Name.Equals(preferredTool.Name, StringComparison.OrdinalIgnoreCase));
        if (index <= 0)
            return;

        var preferredCandidate = rankedTools[index];
        rankedTools.RemoveAt(index);
        rankedTools.Insert(0, preferredCandidate);
    }

    private static bool HasRecoverableConfirmationContext(WorkingContext workingContext)
        => !string.IsNullOrWhiteSpace(workingContext.LastToolName)
            || !string.IsNullOrWhiteSpace(workingContext.CurrentDomain)
            || workingContext.FocusedEntity is not null
            || !string.IsNullOrWhiteSpace(workingContext.ConversationSummary);

    private static IReadOnlyList<string> ResolveSelectedDomains(
        IReadOnlyList<ToolSelectionCandidate> selectedTools,
        IReadOnlyList<string> resolvedDomains)
    {
        var selectedDomains = selectedTools
            .Select(item => RoutingVocabulary.ResolveCanonicalDomain(item.Tool.Domain))
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return selectedDomains.Count > 0 ? selectedDomains : resolvedDomains;
    }

    private static bool ShouldRetainDocumentExportTool(WorkingContext workingContext)
    {
        var summary = workingContext.ConversationSummary;
        if (string.IsNullOrWhiteSpace(summary))
            return false;

        return summary.Contains("download", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("export", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("csv", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("xlsx", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("excel", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("docx", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ToolDefinition> IncludeTool(IReadOnlyList<ToolDefinition> tools, ToolDefinition? additionalTool)
    {
        if (additionalTool is null || tools.Any(tool => tool.Name.Equals(additionalTool.Name, StringComparison.OrdinalIgnoreCase)))
            return tools;

        return [.. tools, additionalTool];
    }

    private static string DescribeIntent(IntentContext intent)
    {
        var action = FirstNonEmpty(intent.Action, "Unknown");
        var entityType = FirstNonEmpty(intent.EntityType, "Entity");
        return intent.Parameters.Count == 0
            ? $"{action} {entityType}"
            : $"{action} {entityType} ({string.Join(", ", intent.Parameters.Keys.Take(2))})";
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

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(token => token.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

internal static class ToolRouteExtensions
{
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source)
        => source ?? Enumerable.Empty<T>();
}
