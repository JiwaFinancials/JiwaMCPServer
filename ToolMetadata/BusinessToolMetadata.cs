namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessToolMetadata
{
    public string? EntityType { get; init; }

    public string? ActionType { get; init; }

    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> IntentPhrases { get; init; } = Array.Empty<string>();

    public string SearchText { get; init; } = string.Empty;
}
