namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessToolMetadata
{
    public string? EntityType { get; init; }

    public string? ActionType { get; init; }

    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> IntentPhrases { get; init; } = Array.Empty<string>();

    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    /// Related business entities that commonly trigger this tool selection.
    /// Used for semantic workflow routing instead of keyword matching.
    /// </summary>
    public IReadOnlyList<string> RelatedEntities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Required companion tools that must be included when this tool is selected.
    /// Eliminates hardcoded workflow logic.
    /// </summary>
    public IReadOnlyList<string> RequiredCompanionTools { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Excluded tools that should not appear together with this tool.
    /// Prevents conflicting tool combinations.
    /// </summary>
    public IReadOnlyList<string> ExcludedTools { get; init; } = Array.Empty<string>();
}
