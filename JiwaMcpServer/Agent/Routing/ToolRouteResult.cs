using JiwaMcpServer.Agent.Models;
using System.Text.Json.Serialization;

namespace JiwaMcpServer.Agent.Routing;

public sealed class ToolRouteResult
{
    public IReadOnlyList<string> Domains { get; init; } = [];

    public IntentContext Intent { get; init; } = new();

    public WorkingContext WorkingContext { get; init; } = new();

    public ToolRankingInput RankingInput { get; init; } = new();

    public IReadOnlyList<ToolRouteItem> Tools { get; init; } = [];

    [JsonPropertyName("selectedTools")]
    public IReadOnlyList<ToolRouteItem> SelectedTools => Tools;

    public IReadOnlyList<ToolRouteScore> Scores { get; init; } = [];

    public IReadOnlyList<string> SelectionAdjustments { get; init; } = [];
}

public sealed class ToolRouteItem
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public string Schema { get; init; } = "{}";

    public required string Domain { get; init; }

    public string EntityType { get; init; } = string.Empty;

    public IReadOnlyList<string> SupportedActions { get; init; } = [];

    public double RelevanceScore { get; init; }
}

public sealed class ToolRouteScore
{
    public required string Name { get; init; }

    public required string Domain { get; init; }

    public string EntityType { get; init; } = string.Empty;

    public double Score { get; init; }
}

public sealed class ToolRankingInput
{
    public string UserMessage { get; init; } = string.Empty;

    public IntentContext Intent { get; init; } = new();

    public ActiveEntity? FocusedEntity { get; init; }

    public string CurrentDomain { get; init; } = string.Empty;

    public string ConversationSummary { get; init; } = string.Empty;
}
