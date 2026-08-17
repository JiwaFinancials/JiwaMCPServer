using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.ToolRouting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
[BusinessTool(EntityType = "Form", Tags = ["navigation", "open form", "module"]) ]
public class FormTools(
    BusinessTerminologyRegistry terminologyRegistry,
    FormNameRegistry formNameRegistry,
    ILogger<FormTools> logger,
    IToolRoutingContextAccessor? routingContextAccessor = null,
    IHttpContextAccessor? httpContextAccessor = null) : JiwaToolBase
{
    private static readonly HashSet<string> SearchStopWords =
    [
        "open", "form", "forms", "plugin", "plugins", "class", "classes", "screen", "screens",
        "find", "show", "list", "jiwa", "launch", "window", "module", "modules"
    ];

    private enum FormTypes
    {
        Form = 1,
        Plugin = 2
    }


    [BusinessTool(EntityType = "Form", ActionType = "Search", Aliases = ["find form", "find plugin", "po form class", "purchase order form class", "jiwa screen class"], Tags = ["navigation", "screen lookup", "ui"])]
    [McpServerTool(Name = "ListForms", ReadOnly = true), Description("Search for forms and plugins by description, class name, or other fields.")]
    public Task<string> SearchForms(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.SY_FormsQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var userPrompt = GetCurrentUserPrompt();
            var queryAnalysis = BuildQueryAnalysisForRanking(requestDTO, userPrompt);
            logger.LogInformation(
                "Form search analysis. OriginalQuery: {OriginalQuery}; SignificantTerms: {@SignificantTerms}; ExpandedTerms: {@ExpandedTerms}; CanonicalEntity: {CanonicalEntity}; PromptUsedForRanking: {PromptUsedForRanking}",
                queryAnalysis.OriginalQuery,
                queryAnalysis.SignificantTerms,
                queryAnalysis.ExpandedTerms,
                queryAnalysis.CanonicalEntity ?? "<none>",
                !string.IsNullOrWhiteSpace(userPrompt));

            var shouldUseExpandedSearch = queryAnalysis.ExpandedTerms.Count > 0 || !string.IsNullOrWhiteSpace(queryAnalysis.CanonicalEntity);
            if (!shouldUseExpandedSearch)
            {
                var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
                logger.LogInformation(
                    "Form candidates before ranking for query '{OriginalQuery}': {@Candidates}",
                    queryAnalysis.OriginalQuery,
                    allResults.Select(f => new { f.ClassName, f.Description }).ToArray());

                var rankedCandidatesNoExpansion = RankCandidates(allResults, queryAnalysis, terminologyRegistry).ToList();
                rankedCandidatesNoExpansion = await ApplyPromptAwareRerankingAsync(rankedCandidatesNoExpansion, userPrompt, ct);
                rankedCandidatesNoExpansion = ApplyFormNameMappingPrecedence(rankedCandidatesNoExpansion, userPrompt);

                logger.LogInformation(
                    "Final ranked form candidates for query '{OriginalQuery}': {@RankedCandidates}",
                    queryAnalysis.OriginalQuery,
                    rankedCandidatesNoExpansion.Select(c => new { c.Form.ClassName, c.Form.Description, c.Score }).ToArray());

                var responseWithoutExpansion = rankedCandidatesNoExpansion.Select(candidate => new
                {
                    candidate.Form.Description,
                    FormTypeText = candidate.Form.FormType.HasValue && Enum.IsDefined(typeof(FormTypes), candidate.Form.FormType.Value)
                        ? ((FormTypes)candidate.Form.FormType.Value).ToString()
                        : candidate.Form.FormType?.ToString(),
                    candidate.Form.ClassName,
                    candidate.Form.ChangeTrackingEnabled,
                    candidate.Form.ChangeTrackingRetentionDays
                }).ToList();

                return CreateSearchResponseJson(responseWithoutExpansion, Config.PageSize);
            }

            var candidateForms = new Dictionary<string, SY_Forms>(StringComparer.OrdinalIgnoreCase);
            await AddFormCandidatesAsync(requestDTO, candidateForms, ct);

            foreach (var term in queryAnalysis.RetrievalTerms)
            {
                var termQuery = CreateTermExpansionQuery(requestDTO, term);
                await AddFormCandidatesAsync(termQuery, candidateForms, ct);
            }

            var preRankingCandidates = candidateForms.Values.ToList();
            logger.LogInformation(
                "Form candidates before ranking for query '{OriginalQuery}': {@Candidates}",
                queryAnalysis.OriginalQuery,
                preRankingCandidates.Select(f => new { f.ClassName, f.Description }).ToArray());

            var rankedCandidates = RankCandidates(preRankingCandidates, queryAnalysis, terminologyRegistry).ToList();
            rankedCandidates = await ApplyPromptAwareRerankingAsync(rankedCandidates, userPrompt, ct);
            rankedCandidates = ApplyFormNameMappingPrecedence(rankedCandidates, userPrompt);
            logger.LogInformation(
                "Final ranked form candidates for query '{OriginalQuery}': {@RankedCandidates}",
                queryAnalysis.OriginalQuery,
                rankedCandidates.Select(c => new { c.Form.ClassName, c.Form.Description, c.Score }).ToArray());
            var response = rankedCandidates.Select(candidate => new
            {
                candidate.Form.Description,
                FormTypeText = candidate.Form.FormType.HasValue && Enum.IsDefined(typeof(FormTypes), candidate.Form.FormType.Value)
                    ? ((FormTypes)candidate.Form.FormType.Value).ToString()
                    : candidate.Form.FormType?.ToString(),
                candidate.Form.ClassName,
                candidate.Form.ChangeTrackingEnabled,
                candidate.Form.ChangeTrackingRetentionDays
            }).ToList();

            return CreateSearchResponseJson(response, Config.PageSize);
        });


    private FormSearchAnalysis BuildQueryAnalysisForRanking(SY_FormsQuery requestDTO, string? userPrompt)
    {
        var promptQuery = string.IsNullOrWhiteSpace(userPrompt)
            ? null
            : new SY_FormsQuery
            {
                DescriptionContains = userPrompt
            };

        var promptAnalysis = promptQuery is null
            ? null
            : AnalyzeQuery(promptQuery, terminologyRegistry);

        if (promptAnalysis is not null &&
            (!string.IsNullOrWhiteSpace(promptAnalysis.CanonicalEntity) || promptAnalysis.RetrievalTerms.Count > 0))
        {
            return promptAnalysis;
        }

        return AnalyzeQuery(requestDTO, terminologyRegistry);
    }

    private Task<List<RankedFormCandidate>> ApplyPromptAwareRerankingAsync(
        IReadOnlyList<RankedFormCandidate> rankedCandidates,
        string? userPrompt,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userPrompt) || rankedCandidates.Count <= 1)
            return Task.FromResult(rankedCandidates.ToList());

        const int maxCandidatesForReranking = 20;
        var shortlist = rankedCandidates.Take(maxCandidatesForReranking).ToList();
        var normalizedPrompt = NormalizeForMatching(userPrompt);
        var rerankedShortlist = shortlist
            .Select(candidate => new
            {
                Candidate = candidate,
                Score = candidate.Score + CalculatePromptAlignmentBoost(candidate, normalizedPrompt)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Candidate.Score)
            .Select(item => item.Candidate)
            .ToList();

        var shortlistClassNames = shortlist
            .Select(candidate => candidate.Form.ClassName)
            .Where(className => !string.IsNullOrWhiteSpace(className))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        rerankedShortlist.AddRange(rankedCandidates
            .Where(candidate => !shortlistClassNames.Contains(candidate.Form.ClassName ?? string.Empty)));

        return Task.FromResult(rerankedShortlist);
    }

    private double CalculatePromptAlignmentBoost(RankedFormCandidate candidate, string normalizedPrompt)
    {
        if (string.IsNullOrWhiteSpace(normalizedPrompt))
            return 0;

        var boost = 0d;
        var className = NormalizeForMatching(candidate.Form.ClassName ?? string.Empty);
        var description = NormalizeForMatching(candidate.Form.Description ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(className) && ContainsWholePhrase(normalizedPrompt, className))
        {
            boost += 4;
        }

        if (!string.IsNullOrWhiteSpace(description) && ContainsWholePhrase(normalizedPrompt, description))
        {
            boost += 3;
        }

        foreach (var term in normalizedPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (term.Length <= 1 || SearchStopWords.Contains(term))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(className) && ContainsWholePhrase(className, term))
            {
                boost += 0.75;
            }

            if (!string.IsNullOrWhiteSpace(description) && ContainsWholePhrase(description, term))
            {
                boost += 0.5;
            }
        }

        return boost;
    }

    private List<RankedFormCandidate> ApplyFormNameMappingPrecedence(
        IReadOnlyList<RankedFormCandidate> rankedCandidates,
        string? userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt) || rankedCandidates.Count <= 1)
            return rankedCandidates.ToList();

        var mappedClassNames = ResolveMappedClassNamesFromPrompt(userPrompt);

        var precedenceOrder = mappedClassNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((className, index) => (className, index))
            .ToDictionary(item => item.className, item => item.index, StringComparer.OrdinalIgnoreCase);

        if (precedenceOrder.Count == 0)
            return rankedCandidates.ToList();

        var prioritized = rankedCandidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Form.ClassName) && precedenceOrder.ContainsKey(candidate.Form.ClassName))
            .OrderBy(candidate => precedenceOrder[candidate.Form.ClassName!])
            .ThenByDescending(candidate => candidate.Score)
            .ToList();

        if (prioritized.Count == 0)
            return rankedCandidates.ToList();

        var prioritizedClassNames = prioritized
            .Select(candidate => candidate.Form.ClassName)
            .Where(className => !string.IsNullOrWhiteSpace(className))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var remainder = rankedCandidates
            .Where(candidate => string.IsNullOrWhiteSpace(candidate.Form.ClassName) || !prioritizedClassNames.Contains(candidate.Form.ClassName))
            .ToList();

        prioritized.AddRange(remainder);
        return prioritized;
    }

    private IReadOnlyList<string> ResolveMappedClassNamesFromPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return [];

        var mappedClassNames = new List<string>();

        if (formNameRegistry.TryResolveFormName(prompt, out var directMappedClassName) && !string.IsNullOrWhiteSpace(directMappedClassName))
        {
            mappedClassNames.Add(directMappedClassName.Trim());
        }

        foreach (var tokenCandidate in BuildPromptTokenCandidates(prompt))
        {
            if (formNameRegistry.TryResolveFormName(tokenCandidate, out var mappedClassName) && !string.IsNullOrWhiteSpace(mappedClassName))
            {
                mappedClassNames.Add(mappedClassName.Trim());
            }
        }

        var normalizedPrompt = NormalizeForMatching(prompt);
        mappedClassNames.AddRange(formNameRegistry
            .GetAllMappings()
            .Where(mapping => ContainsWholePhrase(normalizedPrompt, mapping.Key))
            .OrderByDescending(mapping => NormalizeForMatching(mapping.Key).Length)
            .Select(mapping => mapping.Value)
            .Where(mappedClassName => !string.IsNullOrWhiteSpace(mappedClassName))
            .Select(mappedClassName => mappedClassName.Trim()));

        return mappedClassNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> BuildPromptTokenCandidates(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return [];

        var normalizedPrompt = NormalizeForMatching(prompt);
        var tokens = normalizedPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return [];

        var candidates = new List<string>();
        for (var phraseLength = tokens.Length; phraseLength >= 1; phraseLength--)
        {
            for (var start = 0; start + phraseLength <= tokens.Length; start++)
            {
                var candidate = string.Join(' ', tokens.Skip(start).Take(phraseLength));
                if (candidate.Length >= 2)
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [BusinessTool(EntityType = "Form", ActionType = "Open", Aliases = ["open jiwa form", "open jiwa window", "launch form"], Tags = ["navigation", "workspace", "ui"], RequiredCompanionTools = "ListForms")]
    [McpServerTool(Name = "OpenForm"), Description("Open a form or plugin in the Jiwa client with an optional document number.")]
    public Task<string> OpenForm(
        string UserPrompt,
        string? DocumentNumber = null,
        string? RequestedRecordReference = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(UserPrompt))
            {
                return new
                {
                    success = false,
                    message = "UserPrompt is required."
                }.ToJson();
            }

            var effectiveClassName = await ResolveClassNameFromUserPromptAsync(UserPrompt, ct);
            var effectiveDocumentNumber = string.IsNullOrWhiteSpace(DocumentNumber) ? null : DocumentNumber.Trim();
            var effectiveRequestedReference = string.IsNullOrWhiteSpace(RequestedRecordReference)
                ? null
                : RequestedRecordReference.Trim();

            if (!string.IsNullOrWhiteSpace(effectiveClassName))
            {
                logger.LogInformation("Inferred ClassName '{ClassName}' from current user prompt context.", effectiveClassName);
            }

            if (string.IsNullOrWhiteSpace(effectiveClassName))
            {
                return new
                {
                    success = false,
                    message = "Unable to determine a form from your request. Use a clearer form name in your prompt (for example: open purchase order)."
                }.ToJson();
            }

            var formQuery = new SY_FormsQuery
            {
                ClassName = effectiveClassName
            };

            var allResults = await GetAllQueryResultsAsync(formQuery, Config.PageSize, ct);
            var result = allResults.FirstOrDefault();
            if (result is null)
            {
                return new
                {
                    success = false,
                    message = $"No form or plugin found with ClassName '{effectiveClassName}'."
                }.ToJson();
            }

            if (result.FormType != (int)FormTypes.Form && result.FormType != (int)FormTypes.Plugin)
            {
                return new
                {
                    success = false,
                    message = $"The specified class '{effectiveClassName}' is not a form or plugin."
                }.ToJson();
            }

            var effectiveRecordReference = !string.IsNullOrWhiteSpace(effectiveDocumentNumber)
                ? effectiveDocumentNumber
                : effectiveRequestedReference;
            var hasRequestedRecordReference = !string.IsNullOrWhiteSpace(effectiveRecordReference);

            var effectiveDrillDownId = (string?)null;
            if (hasRequestedRecordReference)
            {
                var resolution = await TryResolveDrillDownIdFromDocumentNumberAsync(
                    result.ClassName,
                    UserPrompt,
                    effectiveRecordReference!,
                    ct);

                effectiveDrillDownId = resolution.DrillDownId;

                if (string.IsNullOrWhiteSpace(effectiveDrillDownId))
                {
                    var noResolverMessage = resolution.AttemptedResolvers.Count == 0
                        ? $"I'm sorry, I can't load a specific record for class '{result.ClassName}' from document number '{effectiveRecordReference}' because no document-number resolvers are available in this MCP server. I can still open the form without loading a specific record."
                        : resolution.IsAmbiguous
                            ? $"I'm sorry, document number '{effectiveRecordReference}' matched multiple resolver strategies and did not produce a unique DrillDownID for class '{result.ClassName}'. I can still open the form without loading a specific record."
                            : $"I'm sorry, I couldn't resolve document number '{effectiveRecordReference}' to a unique DrillDownID for class '{result.ClassName}' using the available resolver strategies. I can still open the form without loading a specific record.";

                    return new
                    {
                        success = false,
                        className = result.ClassName,
                        documentNumber = effectiveRecordReference,
                        requestedRecordReference = effectiveRecordReference,
                        resolver = resolution.ResolverToolName,
                        attemptedResolvers = resolution.AttemptedResolvers,
                        canOpenWithoutRecord = true,
                        message = noResolverMessage
                    }.ToJson();
                }
            }

            return new
            {
                success = true,
                className = result.ClassName,
                documentNumber = effectiveRecordReference,
                drillDownID = effectiveDrillDownId,
                recordLoaded = !string.IsNullOrWhiteSpace(effectiveDrillDownId)
            }.ToJson();
        });

    private sealed record DocumentResolutionResult(
        string? DrillDownId,
        string? ResolverToolName,
        IReadOnlyList<string> AttemptedResolvers,
        bool IsAmbiguous = false);

    private sealed record DocumentNumberResolver(
        string ToolName,
        IReadOnlyList<string> PriorityTerms,
        Func<string, CancellationToken, Task<string?>> ResolveAsync);

    private async Task<DocumentResolutionResult> TryResolveDrillDownIdFromDocumentNumberAsync(
        string? className,
        string userPrompt,
        string documentNumber,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return new DocumentResolutionResult(null, null, []);

        var orderedResolvers = BuildOrderedDocumentResolvers(className, userPrompt);
        if (orderedResolvers.Count == 0)
            return new DocumentResolutionResult(null, null, []);

        var attemptedResolvers = new List<string>();
        var resolvedMatches = new List<(string ToolName, string DrillDownId)>();

        foreach (var resolver in orderedResolvers)
        {
            attemptedResolvers.Add(resolver.ToolName);

            string? resolvedId;
            try
            {
                resolvedId = await resolver.ResolveAsync(documentNumber, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Document resolver {ResolverToolName} failed for document number {DocumentNumber}.", resolver.ToolName, documentNumber);
                resolvedId = null;
            }

            if (!string.IsNullOrWhiteSpace(resolvedId))
                resolvedMatches.Add((resolver.ToolName, resolvedId));
        }

        var distinctIds = resolvedMatches
            .Select(match => match.DrillDownId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctIds.Count > 1)
            return new DocumentResolutionResult(null, null, attemptedResolvers, IsAmbiguous: true);

        if (distinctIds.Count == 1)
        {
            var selected = resolvedMatches.First(match => string.Equals(match.DrillDownId, distinctIds[0], StringComparison.OrdinalIgnoreCase));
            return new DocumentResolutionResult(selected.DrillDownId, selected.ToolName, attemptedResolvers);
        }

        return new DocumentResolutionResult(null, null, attemptedResolvers);
    }

    private IReadOnlyList<DocumentNumberResolver> BuildOrderedDocumentResolvers(string? className, string? userPrompt)
    {
        var resolvers = new List<DocumentNumberResolver>
        {
            new(
                ToolName: "ResolvePurchaseOrderId",
                PriorityTerms: ["purchase order", "purchase orders", "po"],
                ResolveAsync: TryResolvePurchaseOrderIdFromDocumentNumberAsync),
            new(
                ToolName: "ResolveSalesOrderId",
                PriorityTerms: ["sales order", "sales orders", "invoice", "so", "order entry"],
                ResolveAsync: TryResolveSalesOrderIdFromDocumentNumberAsync),
            new(
                ToolName: "ResolveCreditorPurchaseBatchId",
                PriorityTerms: ["creditor purchase", "supplier invoice", "vendor bill", "accounts payable", "ap invoice", "creditor invoice"],
                ResolveAsync: TryResolveCreditorPurchaseBatchIdFromDocumentNumberAsync)
        };

        var normalizedClassName = NormalizeForMatching(className ?? string.Empty);
        var normalizedPrompt = NormalizeForMatching(userPrompt ?? string.Empty);

        return resolvers
            .Select(resolver => new { Resolver = resolver, Score = ScoreResolver(resolver, normalizedClassName, normalizedPrompt) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Resolver.ToolName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Resolver)
            .ToList();
    }

    private static int ScoreResolver(DocumentNumberResolver resolver, string normalizedClassName, string normalizedPrompt)
    {
        var score = 0;
        foreach (var term in resolver.PriorityTerms)
        {
            if (ContainsWholePhrase(normalizedClassName, term))
                score += 3;

            if (ContainsWholePhrase(normalizedPrompt, term))
                score += 2;
        }

        return score;
    }

    private static async Task<string?> TryResolvePurchaseOrderIdFromDocumentNumberAsync(string documentNumber, CancellationToken ct)
    {
        var candidates = BuildDocumentNumberCandidates(documentNumber, ["PO"]);
        foreach (var candidate in candidates)
        {
            var response = await JiwaApiClient.GetAsync(new v_Jiwa_PurchaseOrdersQuery
            {
                OrderNo = candidate,
                Take = 2,
                Skip = 0
            }, ct);

            var matches = response.Results?
                .Select(x => x.OrderID?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matches is { Count: 1 })
                return matches[0];
        }

        return null;
    }

    private static async Task<string?> TryResolveSalesOrderIdFromDocumentNumberAsync(string documentNumber, CancellationToken ct)
    {
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in BuildDocumentNumberCandidates(documentNumber, ["SO", "INV", "INVOICE"]))
        {
            var invoiceResponse = await JiwaApiClient.GetAsync(new v_Jiwa_SalesOrdersQuery
            {
                InvoiceNo = candidate,
                Take = 2,
                Skip = 0
            }, ct);

            AddDistinctMatches(matches, invoiceResponse.Results?.Select(row => row.InvoiceID));

            var orderResponse = await JiwaApiClient.GetAsync(new v_Jiwa_SalesOrdersQuery
            {
                OrderNo = candidate,
                Take = 2,
                Skip = 0
            }, ct);

            AddDistinctMatches(matches, orderResponse.Results?.Select(row => row.InvoiceID));

            if (matches.Count > 1)
                return null;
        }

        return matches.Count == 1 ? matches.First() : null;
    }

    private static async Task<string?> TryResolveCreditorPurchaseBatchIdFromDocumentNumberAsync(string documentNumber, CancellationToken ct)
    {
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in BuildDocumentNumberCandidates(documentNumber, ["BATCH", "RECEIPT", "CP"]))
        {
            var receiptResponse = await JiwaApiClient.GetAsync(new v_Jiwa_CreditorPurchasesQuery
            {
                ReceiptID = candidate,
                Take = 2,
                Skip = 0
            }, ct);

            AddDistinctMatches(matches, receiptResponse.Results?.Select(row => row.ReceiptID));

            var batchResponse = await JiwaApiClient.GetAsync(new v_Jiwa_CreditorPurchasesQuery
            {
                BatchNum = candidate,
                Take = 2,
                Skip = 0
            }, ct);

            AddDistinctMatches(matches, batchResponse.Results?.Select(row => row.ReceiptID));

            if (matches.Count > 1)
                return null;
        }

        return matches.Count == 1 ? matches.First() : null;
    }

    private static IReadOnlyList<string> BuildDocumentNumberCandidates(string rawValue, IReadOnlyList<string> prefixes)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trimmed = rawValue.Trim();
        AddCandidate(trimmed);

        if (prefixes.Count > 0)
        {
            var prefixPattern = string.Join("|", prefixes.Select(Regex.Escape));
            var withoutPrefix = Regex.Replace(trimmed, $"^({prefixPattern})[\\s-]*", string.Empty, RegexOptions.IgnoreCase).Trim();
            AddCandidate(withoutPrefix);
        }

        return candidates;

        void AddCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (seen.Add(value))
                candidates.Add(value);
        }
    }

    private static void AddDistinctMatches(ISet<string> matches, IEnumerable<string?>? values)
    {
        foreach (var value in values ?? [])
        {
            var trimmed = value?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                matches.Add(trimmed);
        }
    }

    private async Task<string?> ResolveClassNameFromUserPromptAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return null;

        var trimmedPrompt = prompt.Trim();

        var mappedClassNameFromPrompt = ResolveMappedClassNamesFromPrompt(trimmedPrompt).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(mappedClassNameFromPrompt))
            return mappedClassNameFromPrompt;

        var promptQuery = new SY_FormsQuery
        {
            DescriptionContains = trimmedPrompt
        };

        var searchJson = await SearchForms(promptQuery, ct: ct);
        if (TryExtractTopClassNameFromSearchResponse(searchJson, out var topClassName))
            return topClassName;

        var queryAnalysis = AnalyzeQuery(promptQuery, terminologyRegistry);
        var candidateForms = new Dictionary<string, SY_Forms>(StringComparer.OrdinalIgnoreCase);

        foreach (var term in queryAnalysis.RetrievalTerms)
        {
            var termQuery = CreateTermExpansionQuery(promptQuery, term);
            await AddFormCandidatesAsync(termQuery, candidateForms, ct);
        }

        if (candidateForms.Count == 0 && !string.IsNullOrWhiteSpace(queryAnalysis.CanonicalEntity))
        {
            await AddFormCandidatesAsync(new SY_FormsQuery
            {
                DescriptionContains = queryAnalysis.CanonicalEntity
            }, candidateForms, ct);
        }

        var bestCandidate = RankCandidates(candidateForms.Values, queryAnalysis, terminologyRegistry)
            .FirstOrDefault(candidate => candidate.Score > 0);

        return bestCandidate?.Form.ClassName;
    }

    private static bool TryExtractTopClassNameFromSearchResponse(string? searchResponseJson, out string? className)
    {
        className = null;

        if (string.IsNullOrWhiteSpace(searchResponseJson))
            return false;

        try
        {
            using var json = JsonDocument.Parse(searchResponseJson);
            if (!json.RootElement.TryGetProperty("results", out var resultsElement) ||
                resultsElement.ValueKind != JsonValueKind.Array ||
                resultsElement.GetArrayLength() == 0)
            {
                return false;
            }

            var firstResult = resultsElement[0];
            if (TryGetPropertyIgnoreCase(firstResult, "ClassName", out var classNameElement) &&
                classNameElement.ValueKind == JsonValueKind.String)
            {
                var extractedClassName = classNameElement.GetString();
                if (!string.IsNullOrWhiteSpace(extractedClassName))
                {
                    className = extractedClassName.Trim();
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private string? GetCurrentUserPrompt()
    {
        var routingPrompt = routingContextAccessor?.Current?.Prompt;
        if (!string.IsNullOrWhiteSpace(routingPrompt))
            return routingPrompt;

        var headers = httpContextAccessor?.HttpContext?.Request?.Headers;
        var headerPrompt = headers?["X-User-Prompt"].FirstOrDefault()
            ?? headers?["X-Tool-Query"].FirstOrDefault();

        return string.IsNullOrWhiteSpace(headerPrompt)
            ? null
            : headerPrompt;
    }

    private static async Task AddFormCandidatesAsync(SY_FormsQuery query, IDictionary<string, SY_Forms> candidates, CancellationToken ct)
    {
        var response = await JiwaApiClient.GetAsync(query, ct);
        foreach (var form in response.Results ?? [])
        {
            if (form is null || string.IsNullOrWhiteSpace(form.ClassName))
            {
                continue;
            }

            if (form.FormType != (int)FormTypes.Form && form.FormType != (int)FormTypes.Plugin)
            {
                continue;
            }

            candidates[form.ClassName] = form;
        }
    }

    private static SY_FormsQuery CreateTermExpansionQuery(SY_FormsQuery requestDTO, string term)
        => new()
        {
            ClassNameContains = term,
            DescriptionContains = term,
            FormType = requestDTO.FormType
        };

    private static FormSearchAnalysis AnalyzeQuery(SY_FormsQuery requestDTO, BusinessTerminologyRegistry registry)
    {
        var originalQuery = BuildOriginalQuery(requestDTO);
        var significantTerms = ExtractSignificantTerms(originalQuery);
        var matchedEntities = ResolveEntities(originalQuery, significantTerms, registry);
        var canonicalEntity = ResolveCanonicalEntity(originalQuery, matchedEntities, registry, out var hasOpenEntityPattern);

        var expandedTerms = new List<string>();
        foreach (var entityType in matchedEntities)
        {
            foreach (var synonym in registry.GetSynonyms(entityType))
            {
                if (!expandedTerms.Contains(synonym, StringComparer.OrdinalIgnoreCase))
                {
                    expandedTerms.Add(synonym);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(canonicalEntity))
        {
            foreach (var synonym in registry.GetSynonyms(canonicalEntity))
            {
                if (!expandedTerms.Contains(synonym, StringComparer.OrdinalIgnoreCase))
                {
                    expandedTerms.Add(synonym);
                }
            }
        }

        var retrievalTerms = significantTerms
            .Concat(expandedTerms)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Select(term => term.Trim())
            .Where(term => term.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FormSearchAnalysis(
            originalQuery,
            significantTerms,
            expandedTerms,
            retrievalTerms,
            canonicalEntity,
            hasOpenEntityPattern);
    }

    private static string BuildOriginalQuery(SY_FormsQuery requestDTO)
    {
        var parts = new[]
        {
            requestDTO.ClassName,
            requestDTO.ClassNameStartsWith,
            requestDTO.ClassNameEndsWith,
            requestDTO.ClassNameContains,
            requestDTO.ClassNameLike,
            requestDTO.Description,
            requestDTO.DescriptionStartsWith,
            requestDTO.DescriptionEndsWith,
            requestDTO.DescriptionContains,
            requestDTO.DescriptionLike,
            requestDTO.HelpPageName,
            requestDTO.HelpPageNameStartsWith,
            requestDTO.HelpPageNameEndsWith,
            requestDTO.HelpPageNameContains,
            requestDTO.HelpPageNameLike
        };

        return string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static IReadOnlyList<string> ExtractSignificantTerms(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var normalized = NormalizeForMatching(query);
        var terms = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => !SearchStopWords.Contains(term))
            .Where(term => term.Length >= 2)
            .Where(term => !Regex.IsMatch(term, "^\\d+$"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return terms;
    }

    private static IReadOnlyList<string> ResolveEntities(string originalQuery, IReadOnlyList<string> significantTerms, BusinessTerminologyRegistry registry)
    {
        var resolvedEntities = new List<string>();

        foreach (var term in significantTerms)
        {
            if (registry.TryResolveEntity(term, out var entityType) && !resolvedEntities.Contains(entityType, StringComparer.OrdinalIgnoreCase))
            {
                resolvedEntities.Add(entityType);
            }
        }

        var normalizedQuery = NormalizeForMatching(originalQuery);
        foreach (var entityType in registry.GetEntityTypes())
        {
            var terms = registry.GetSynonyms(entityType).Append(SplitPascalCase(entityType));
            if (terms.Any(term => ContainsWholePhrase(normalizedQuery, term)) &&
                !resolvedEntities.Contains(entityType, StringComparer.OrdinalIgnoreCase))
            {
                resolvedEntities.Add(entityType);
            }
        }

        return resolvedEntities;
    }

    private static string? ResolveCanonicalEntity(
        string originalQuery,
        IReadOnlyList<string> matchedEntities,
        BusinessTerminologyRegistry registry,
        out bool hasOpenEntityPattern)
    {
        hasOpenEntityPattern = false;
        if (string.IsNullOrWhiteSpace(originalQuery))
            return matchedEntities.FirstOrDefault();

        var openEntityMatch = Regex.Match(
            originalQuery,
            @"\bopen\s+(?<entity>[\p{L}\p{Nd}\s\-/]+?)\s+(?<identifier>[\p{L}\p{Nd}_\-/]+)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (openEntityMatch.Success)
        {
            var entityText = openEntityMatch.Groups["entity"].Value.Trim();
            if (registry.TryResolveEntity(entityText, out var resolvedEntity))
            {
                hasOpenEntityPattern = true;
                return resolvedEntity;
            }

            var tokens = entityText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = tokens.Length; i >= 1; i--)
            {
                var candidate = string.Join(' ', tokens.Take(i));
                if (registry.TryResolveEntity(candidate, out resolvedEntity))
                {
                    hasOpenEntityPattern = true;
                    return resolvedEntity;
                }
            }
        }

        return matchedEntities.FirstOrDefault();
    }

    private static IEnumerable<RankedFormCandidate> RankCandidates(
        IEnumerable<SY_Forms> candidates,
        FormSearchAnalysis analysis,
        BusinessTerminologyRegistry registry)
    {
        var canonicalEntityTerms = string.IsNullOrWhiteSpace(analysis.CanonicalEntity)
            ? []
            : registry.GetSynonyms(analysis.CanonicalEntity)
                .Append(SplitPascalCase(analysis.CanonicalEntity))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return candidates
            .Select(form =>
            {
                var normalizedClassName = NormalizeForMatching(form.ClassName ?? string.Empty);
                var normalizedDescription = NormalizeForMatching(form.Description ?? string.Empty);
                var searchableText = string.Join(' ', normalizedClassName, normalizedDescription).Trim();

                var score = 0;

                foreach (var term in analysis.SignificantTerms)
                {
                    if (ContainsWholePhrase(searchableText, term))
                        score += 50;
                }

                foreach (var term in analysis.ExpandedTerms)
                {
                    if (ContainsWholePhrase(searchableText, term))
                        score += 90;
                }

                foreach (var entityTerm in canonicalEntityTerms)
                {
                    if (!ContainsWholePhrase(searchableText, entityTerm))
                        continue;

                    score += analysis.HasOpenEntityPattern ? 220 : 140;
                }

                return new RankedFormCandidate(form, score);
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Form.Description, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Form.ClassName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsWholePhrase(string normalizedSource, string term)
    {
        var normalizedTerm = NormalizeForMatching(term);
        if (string.IsNullOrWhiteSpace(normalizedSource) || string.IsNullOrWhiteSpace(normalizedTerm))
            return false;

        return Regex.IsMatch(
            normalizedSource,
            $@"\b{Regex.Escape(normalizedTerm)}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string NormalizeForMatching(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var pascalExpanded = SplitPascalCase(value);
        var normalized = Regex.Replace(pascalExpanded.ToLowerInvariant(), @"[^\p{L}\p{Nd}]+", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    private static string SplitPascalCase(string value)
        => Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");

    private sealed record FormSearchAnalysis(
        string OriginalQuery,
        IReadOnlyList<string> SignificantTerms,
        IReadOnlyList<string> ExpandedTerms,
        IReadOnlyList<string> RetrievalTerms,
        string? CanonicalEntity,
        bool HasOpenEntityPattern);

    private sealed record RankedFormCandidate(SY_Forms Form, int Score);
}

