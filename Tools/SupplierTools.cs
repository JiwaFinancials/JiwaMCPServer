using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.Creditors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class SupplierTools : JiwaToolBase
{
    [McpServerTool(ReadOnly = true), Description("Search for suppliers by field. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages.")]
    public Task<string> SearchSuppliers(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_Jiwa_CreditorSummaryQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<v_Jiwa_CreditorSummary>>();
        });

    [McpServerTool, Description("Get full details for a supplier. Suppliers are also known as creditors. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs.")]
    public Task<string> GetSupplier(CreditorGETRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.GetAsync(requestDTO, ct);
            return result.ToJson<Creditor>();
        });

    [McpServerTool(ReadOnly = true), Description("Retrieves a list of creditor classifications. Creditors are also known as suppliers. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. When the user requests all or a complete list, page using skip and take until fewer rows than take are returned or zero rows are returned, then aggregate all pages.")]
    public Task<string> SearchCreditorClassifications(JiwaFinancials.Jiwa.JiwaServiceModel.Tables.CR_ClassificationQuery requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var response = await JiwaApiClient.GetAsync(requestDTO, ct);
            return response.Results.ToJson<List<CR_Classification>>();
        });
}
