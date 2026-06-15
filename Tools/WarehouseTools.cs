using JiwaFinancials.Jiwa.JiwaServiceModel;
using JiwaFinancials.Jiwa.JiwaServiceModel.PurchaseOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tables;
using JiwaMcpServer.Services;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;
using System.Diagnostics;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
public class WarehouseTools : JiwaToolBase
{
    [McpServerTool(ReadOnly = true), Description(@"Search for warehouses by field. A logical warehouse represents a sub-division of a physical warehouse used for inventory management. 
                                                   A physical warehouse contains one or more logical warehouses. 
                                                   Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters.
                                                   A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.
                                                   Use for listing or finding warehouses. Do not use this tool to determine the current warehouse; use GetCurrentLogicalWarehouse instead.")]
    public Task<string> SearchWarehouses(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.v_WarehouseSelectionQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            return allResults.ToJson<List<v_WarehouseSelection>>();
        });

    [McpServerTool, Description(@"Get the current logical warehouse. 
                                  A logical warehouse represents a sub-division of a physical warehouse used for inventory management. 
                                  A physical warehouse contains one or more logical warehouses. The current physical warehouse can be inferred from the current logical warehouse.
                                  Get the current logical warehouse for the user/session.
                                  Use this tool whenever the user asks for the current warehouse, current logical warehouse, active warehouse, selected warehouse, or which warehouse they are in. Do not use warehouse search tools for that purpose.")]
    public Task<string> GetCurrentLogicalWarehouse(CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var requestDTO = new JiwaFinancials.Jiwa.JiwaServiceModel.LogicalWarehousesCurrentGETRequest();
            IN_Logical response = await JiwaApiClient.GetAsync(requestDTO, ct);

            //Resolve physical
            var v_WarehouseSelectionResponse = await JiwaApiClient.GetAsync(new v_WarehouseSelectionQuery { IN_LogicalID = response.IN_LogicalID }, ct);

            return v_WarehouseSelectionResponse?.ToJson() ?? "No logical warehouse found.";
        });

    [McpServerTool, Description(@"Change the current logical warehouse. 
                                  A logical warehouse represents a sub-division of a physical warehouse used for inventory management. 
                                  A physical warehouse contains one or more logical warehouses. The current physical warehouse can be inferred from the current logical warehouse.
                                  Change the current logical warehouse for the user/session.
                                  Use this tool whenever the user asks to change the current warehouse, current logical warehouse, active warehouse, selected warehouse, or which warehouse they are in. Do not use warehouse search tools for that purpose.")]
    public Task<string> ChangeCurrentLogicalWarehouse(JiwaFinancials.Jiwa.JiwaServiceModel.LogicalWarehousesCurrentPATCHRequest requestDTO, CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var result = await JiwaApiClient.PatchAsync(requestDTO, ct);

            // Construct response with action directive for the Jiwa client's Manager.CurrentLogicalWarehouse
            var response = new
            {
                success = true,
                newWarehouseId = result.IN_LogicalID,
                newWarehouseDetails = result?.ToJson() ?? "Warehouse updated",
            };

            return response.ToJson();
        });
}
