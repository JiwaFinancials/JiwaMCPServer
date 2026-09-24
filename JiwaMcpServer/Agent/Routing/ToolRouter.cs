using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public sealed class ToolRouter : IToolRouter
{
    private readonly IToolShortlistService _shortlistService;
    private readonly IToolRoutingContextService _contextService;
    private readonly ILogger<ToolRouter> _logger;

    public ToolRouter(
        IToolShortlistService shortlistService,
        IToolRoutingContextService contextService,
        ILogger<ToolRouter> logger)
    {
        _shortlistService = shortlistService;
        _contextService = contextService;
        _logger = logger;
    }

    public Task<ToolRouteResult> ResolveAsync(ToolRouteRequest request, CancellationToken ct = default)
    {
        var safeRequest = request ?? new ToolRouteRequest();
        var result = _shortlistService.Shortlist(safeRequest, ct);
        _logger.LogInformation(
            "Routing request: conversationId={ConversationId}; promptLength={PromptLength}; summaryLength={SummaryLength}; domains={DomainCount}; shortlistedTools={ToolCount}",
            safeRequest.ConversationId,
            (safeRequest.Prompt ?? string.Empty).Length,
            result.WorkingContext.ConversationSummary.Length,
            result.Domains.Count,
            result.Tools.Count);

        return Task.FromResult(result);
    }

    public Task<WorkingContext> UpdateContextAsync(ToolExecutionContextUpdateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var updated = _contextService.UpdateAfterToolExecution(request);
        _logger.LogInformation(
            "Updated routing context: conversationId={ConversationId}; tool={ToolName}; focusedEntityType={EntityType}; focusedEntityId={EntityId}; domain={Domain}",
            request.ConversationId,
            request.ToolName,
            updated.FocusedEntity?.EntityType,
            updated.FocusedEntity?.EntityId,
            updated.CurrentDomain);

        return Task.FromResult(updated);
    }
}
