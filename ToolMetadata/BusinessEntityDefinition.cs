namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessEntityDefinition
{
    public required string Name { get; init; }

    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
