namespace JiwaMcpServer.ToolMetadata;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessToolAttribute : Attribute
{
    public string EntityType { get; init; } = string.Empty;

    public string ActionType { get; init; } = string.Empty;

    public string[] Aliases { get; init; } = [];

    public string[] Tags { get; init; } = [];
}
