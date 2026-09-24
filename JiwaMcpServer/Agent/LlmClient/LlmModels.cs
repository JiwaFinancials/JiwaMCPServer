using System.Text.Json.Nodes;

namespace JiwaMcpServer.Agent.LlmClient;

/// <summary>Schema definition for a callable tool.</summary>
public sealed class LlmToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }

    /// <summary>JSON Schema object describing the tool's input parameters.</summary>
    public required JsonNode ParametersSchema { get; init; }
}
