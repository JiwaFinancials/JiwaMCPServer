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
public class FormTools : JiwaToolBase
{
    private enum FormTypes
    {
        Form = 1,
        Plugin = 2
    }

    [McpServerTool(ReadOnly = true), Description("Search for forms by field. Use GetDtoSchema in SchemaTools if you are unsure what fields are available in the request and return DTOs. Supports pagination via skip and take parameters. A single call may return only a partial result set. For large result sets, first call with confirmLargeResultSet=false to receive a confirmation token. Then call again with confirmLargeResultSet=true and that token.")]
    public Task<string> SearchForms(
        JiwaFinancials.Jiwa.JiwaServiceModel.Tables.SY_FormsQuery requestDTO,
        bool confirmLargeResultSet = false,
        string? confirmationToken = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            var confirmationMessage = await ValidateLargeResultSetConfirmationAsync(requestDTO, confirmLargeResultSet, confirmationToken, ct);
            if (!string.IsNullOrEmpty(confirmationMessage))
                return confirmationMessage;

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            var response = allResults.Select(f => new
            {
                f.Description,
                FormTypeText = f.FormType.HasValue && Enum.IsDefined(typeof(FormTypes), f.FormType.Value)
                    ? ((FormTypes)f.FormType.Value).ToString()
                    : f.FormType?.ToString(),
                f.ClassName,
                f.ChangeTrackingEnabled,
                f.ChangeTrackingRetentionDays
            }).ToList();

            return response.ToJson();
        });


    [McpServerTool, Description(@"Open a form in the Jiwa client.
                                  If a DrillDownID value is supplied then the form will be opened with that specific record loaded.
                                  If DrillDownID is omitted, the form will be opened without a specific record loaded.
                                  If a requestedRecordReference is provided without a DrillDownID, the tool returns guidance that specific-record loading is not yet available for that module.")]
    public Task<string> OpenAForm(
        string ClassName,
        string? DrillDownID = null,
        string? RequestedRecordReference = null,
        CancellationToken ct = default)
        => InvokeToolAsync(async () =>
        {
            //Ensure class exists and is a form or plugin
            JiwaFinancials.Jiwa.JiwaServiceModel.Tables.SY_FormsQuery requestDTO = new JiwaFinancials.Jiwa.JiwaServiceModel.Tables.SY_FormsQuery
            {
                ClassName = ClassName
            };

            var allResults = await GetAllQueryResultsAsync(requestDTO, Config.PageSize, ct);
            var result = allResults.FirstOrDefault();
            if (result != null)
            {
                if (result.FormType != (int)FormTypes.Form && result.FormType != (int)FormTypes.Plugin)
                {
                    return new
                    {
                        success = false,
                        message = $"The specified class '{ClassName}' is not a form or plugin."
                    }.ToJson();
                }

                var hasDrillDownId = !string.IsNullOrWhiteSpace(DrillDownID);
                var hasRequestedRecordReference = !string.IsNullOrWhiteSpace(RequestedRecordReference);

                if (!hasDrillDownId && hasRequestedRecordReference)
                {
                    return new
                    {
                        success = false,
                        className = result.ClassName,
                        requestedRecordReference = RequestedRecordReference,
                        canOpenWithoutRecord = true,
                        message = $"I'm sorry, I am unable to load particular records for class '{result.ClassName}' yet because there is no resolver available to convert '{RequestedRecordReference}' to a DrillDownID. I can still open the form without loading a specific record."
                    }.ToJson();
                }

                return new
                {
                    success = true,
                    className = result.ClassName,
                    drillDownID = hasDrillDownId ? DrillDownID : null,
                    recordLoaded = hasDrillDownId
                }.ToJson();
            }
            else
            {
                return new
                {
                    success = false,
                    message = $"No form or plugin found with ClassName '{ClassName}'."
                }.ToJson();
            }

        });
}
