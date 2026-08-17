namespace JiwaMcpServer.ToolMetadata;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class BusinessToolAttribute : Attribute
{
    public string EntityType { get; init; } = string.Empty;

    public string ActionType { get; init; } = string.Empty;

    public string[] Aliases { get; init; } = [];

    public string[] Tags { get; init; } = [];

    /// <summary>
    /// Comma-separated list of related business entities.
    /// E.g., "PurchaseOrder,Supplier" for resolver tools.
    /// </summary>
    public string? RelatedEntities { get; init; }

    /// <summary>
    /// Comma-separated list of required companion tool names.
    /// E.g., "OpenForm,ListForms" for form navigation tools.
    /// </summary>
    public string? RequiredCompanionTools { get; init; }

    /// <summary>
    /// Comma-separated list of excluded tool names that conflict with this tool.
    /// E.g., "CreateCreditorPurchase" for PurchaseOrder creation tools.
    /// </summary>
    public string? ExcludedTools { get; init; }
}
