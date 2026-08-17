using JiwaMcpServer.ToolMetadata;
using ModelContextProtocol.Server;
using ServiceStack;
using System.ComponentModel;

namespace JiwaMcpServer.Tools;

[McpServerToolType]
[BusinessTool(EntityType = "Schema", ActionType = "Get", Aliases = ["business action catalog", "available business tools", "what can you do"], Tags = ["tool discovery", "tool selection", "capabilities"])]
public sealed class BusinessDiscoveryTools(
    BusinessToolMetadataCatalog catalog,
    ToolDescriptionComposer descriptionComposer) : JiwaToolBase
{
    private static readonly string[] SectionOrder =
    [
        "Customers",
        "Suppliers",
        "Supplier Invoices",
        "Purchase Orders",
        "Sales Orders",
        "Inventory",
        "Warehouses",
        "Documents",
        "Files"
    ];

    private static readonly IReadOnlyDictionary<string, string> EntityToSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = "Customers",
        ["Supplier"] = "Suppliers",
        ["CreditorPurchase"] = "Supplier Invoices",
        ["PurchaseOrder"] = "Purchase Orders",
        ["SalesOrder"] = "Sales Orders",
        ["Inventory"] = "Inventory",
        ["Warehouse"] = "Warehouses",
        ["Document"] = "Documents",
        ["File"] = "Files"
    };

    [McpServerTool(Name = "GetAvailableBusinessActions", ReadOnly = true), Description("List available business tools by area.")]
    public Task<string> GetAvailableBusinessActions(CancellationToken ct = default)
        => InvokeToolAsync(() =>
        {
            var sections = SectionOrder
                .ToDictionary(
                    section => section,
                    _ => new List<CatalogToolEntry>(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var descriptor in catalog.Descriptors)
            {
                if (ToolAliasRegistry.IsAlias(descriptor.ToolName))
                    continue;

                var entity = descriptor.Metadata.EntityType;
                if (string.IsNullOrWhiteSpace(entity) || !EntityToSection.TryGetValue(entity, out var section))
                    continue;

                sections[section].Add(new CatalogToolEntry(
                    descriptor.ToolName,
                    descriptionComposer.Compose(descriptor),
                    descriptor.Metadata.Aliases));
            }

            var response = SectionOrder.Select(section => new
            {
                area = section,
                tools = sections[section]
                    .OrderBy(x => x.ToolName, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new
                    {
                        toolName = x.ToolName,
                        businessPurpose = x.BusinessPurpose,
                        commonAliases = x.CommonAliases
                    })
                    .ToArray()
            }).ToArray();

            return Task.FromResult(new
            {
                generatedAtUtc = DateTime.UtcNow,
                sections = response
            }.ToJson());
        });

    private sealed record CatalogToolEntry(string ToolName, string BusinessPurpose, IReadOnlyList<string> CommonAliases);
}
