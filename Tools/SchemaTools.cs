using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
[BusinessTool(EntityType = "Schema", ActionType = "Get", Aliases = ["dto schema", "request schema", "response schema"], Tags = ["contracts", "field list", "model structure"]) ]
public class SchemaTools : JiwaToolBase
{
    [McpServerTool, Description("Get the schema for a Jiwa DTO type.")]
    public Task<string> GetDtoSchema([Description("Jiwa DTO type name or fully qualified type name.")] string dtoTypeName, CancellationToken ct = default)
        => InvokeToolAsync(() => Task.FromResult(GetJiwaDtoSchema(dtoTypeName)));
}
