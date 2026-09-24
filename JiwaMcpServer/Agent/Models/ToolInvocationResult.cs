namespace JiwaMcpServer.Agent.Models;

/// <summary>Structured result returned by a tool invocation.</summary>
public sealed class ToolInvocationResult
{
    /// <summary>Plain-text content returned by the tool.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Inline images produced by the tool.</summary>
    public IReadOnlyList<AgentChatImage> Images { get; init; } = [];

    /// <summary>Downloadable file resources produced by the tool.</summary>
    public IReadOnlyList<AgentChatFile> Files { get; init; } = [];
}

/// <summary>Inline image content returned by a tool.</summary>
public sealed class AgentChatImage
{
    public string Base64Data { get; init; } = string.Empty;
    public string MimeType { get; init; } = "image/png";
}

/// <summary>File resource returned by a tool.</summary>
public sealed class AgentChatFile
{
    public string Uri { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = "application/octet-stream";
    public long? Size { get; init; }
    public string Description { get; init; } = string.Empty;
}
