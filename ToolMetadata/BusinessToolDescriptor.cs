using System.Reflection;

namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessToolDescriptor
{
    public required string ToolName { get; init; }

    public required string Description { get; init; }

    public required Type ToolType { get; init; }

    public required MethodInfo Method { get; init; }

    public required BusinessToolMetadata Metadata { get; init; }
}
