using ModelContextProtocol.Server;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SchemaTools : JiwaToolBase
{
    [McpServerTool, Description("Returns the DTO schema for any Jiwa DTO type by name (e.g. DebtorGETRequest, Debtor, v_Jiwa_Debtor_ListQuery).")]
    public async Task<string> GetDtoSchema([Description("Jiwa DTO type name or fully qualified type name.")] string dtoTypeName, CancellationToken ct = default)
    {
        return GetJiwaDtoSchema(dtoTypeName);
    }
}
