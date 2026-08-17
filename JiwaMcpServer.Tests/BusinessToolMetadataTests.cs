using JiwaMcpServer.ToolMetadata;
using JiwaMcpServer.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace JiwaMcpServer.Tests;

public class BusinessToolMetadataTests
{
    [Fact]
    public void EntityLookup_ResolvesByAlias()
    {
        var registry = new BusinessEntityRegistry();

        var found = registry.TryGetByNameOrAlias("creditor", out var entity);

        Assert.True(found);
        Assert.NotNull(entity);
        Assert.Equal("Supplier", entity!.Name);
    }

    [Fact]
    public void AliasInheritance_IncludesEntityAliasesAndTags()
    {
        var builder = new BusinessToolMetadataBuilder(new BusinessEntityRegistry());

        var metadata = builder.Build(
            toolName: "CreateSupplier",
            description: "Creates a supplier account.",
            businessTool: new BusinessToolAttribute { EntityType = "Supplier", ActionType = "Create" });

        Assert.Contains("supplier", metadata.Aliases);
        Assert.Contains("creditor", metadata.Aliases);
        Assert.Contains("vendor", metadata.Aliases);
        Assert.Contains("payee", metadata.Aliases);
        Assert.Contains("accounts payable", metadata.Tags);
        Assert.Contains("ap", metadata.Tags);
    }

    [Fact]
    public void SearchTextGeneration_CombinesNameDescriptionAliasesAndTags()
    {
        var builder = new BusinessToolMetadataBuilder(new BusinessEntityRegistry());

        var metadata = builder.Build(
            toolName: "CreateSupplier",
            description: "Creates a supplier account.",
            businessTool: new BusinessToolAttribute { EntityType = "Supplier", ActionType = "Create" });

        Assert.Contains("create", metadata.SearchText);
        Assert.Contains("supplier", metadata.SearchText);
        Assert.Contains("creditor", metadata.SearchText);
        Assert.Contains("vendor", metadata.SearchText);
        Assert.Contains("accounts payable", metadata.SearchText);
        Assert.Contains("account", metadata.SearchText);
        Assert.Contains("create supplier", metadata.IntentPhrases);
        Assert.Contains("new supplier", metadata.IntentPhrases);
    }

    [Fact]
    public void ReflectionDiscovery_ExtractsBusinessMetadataFromAttributes()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));

        var descriptors = extractor.ExtractFromAssemblies([typeof(TestMcpTools).Assembly]);
        var descriptor = descriptors.Single(x => x.ToolName == "CreateSupplierTest");

        Assert.Equal("Supplier", descriptor.Metadata.EntityType);
        Assert.Equal("Create", descriptor.Metadata.ActionType);
        Assert.Contains("creditor", descriptor.Metadata.Aliases);
    }

    [Fact]
    public void BackwardCompatibility_WithoutBusinessAttributeStillBuildsMetadata()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));

        var descriptors = extractor.ExtractFromAssemblies([typeof(TestMcpTools).Assembly]);
        var descriptor = descriptors.Single(x => x.ToolName == "PlainTool");

        Assert.NotNull(descriptor.Metadata);
        Assert.NotEqual(string.Empty, descriptor.Metadata.SearchText);
    }

    [Fact]
    public void MissingEntityDefinitions_DoesNotThrowAndUsesProvidedEntityType()
    {
        var builder = new BusinessToolMetadataBuilder(new BusinessEntityRegistry());

        var metadata = builder.Build(
            toolName: "CreateMysteryEntity",
            description: "Creates a mystery ERP entity.",
            businessTool: new BusinessToolAttribute
            {
                EntityType = "MysteryEntity",
                ActionType = "Create",
                Aliases = ["mystery"],
                Tags = ["custom"]
            });

        Assert.Equal("MysteryEntity", metadata.EntityType);
        Assert.Contains("mystery", metadata.Aliases);
        Assert.Contains("custom", metadata.Tags);
    }

    [Fact]
    public void PurchaseOrderWorkflowTool_HasStrongCreateIntentMetadata()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var descriptors = extractor.ExtractFromAssemblies([typeof(PurchaseOrderTools).Assembly]);

        var descriptor = descriptors.Single(x => x.ToolName == "CreatePurchaseOrderWithLines");

        Assert.Equal("PurchaseOrder", descriptor.Metadata.EntityType);
        Assert.Equal("Create", descriptor.Metadata.ActionType);
        Assert.Contains("create po for creditor with parts", descriptor.Metadata.Aliases);
        Assert.Contains("create purchase order", descriptor.Metadata.SearchText);
        Assert.Contains("create purchase order", descriptor.Metadata.IntentPhrases);
    }

    [Fact]
    public void DiscoveryMetadata_IsAddedToToolMeta()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var catalog = new BusinessToolMetadataCatalog(extractor.ExtractFromAssemblies([typeof(TestMcpTools).Assembly]));

        var result = new ListToolsResult
        {
            Tools =
            [
                new Tool
                {
                    Name = "CreateSupplierTest",
                    Description = "Creates a supplier account.",
                    InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
                }
            ]
        };

        var descriptionComposer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());
        result.ApplyBusinessToolMetadata(catalog, descriptionComposer);

        var enrichedDescription = result.Tools.Single().Description ?? string.Empty;
        Assert.Equal("Creates a supplier account.", enrichedDescription);

        var toolMeta = result.Tools.Single().Meta;
        Assert.NotNull(toolMeta);
        Assert.Equal("Supplier", toolMeta!["entityType"]!.GetValue<string>());
        Assert.Equal("Create", toolMeta["actionType"]!.GetValue<string>());
        Assert.Contains("supplier", toolMeta["searchText"]!.GetValue<string>());

        var intentPhrases = toolMeta["intentPhrases"]!.Deserialize<string[]>();
        Assert.NotNull(intentPhrases);
        Assert.Contains("create supplier", intentPhrases!);
    }

    [Fact]
    public void ClassLevelBusinessTool_IsUsedWhenMethodAttributeMissing()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));

        var descriptors = extractor.ExtractFromAssemblies([typeof(TestMcpTools).Assembly]);
        var descriptor = descriptors.Single(x => x.ToolName == "ClassLevelListSuppliers");

        Assert.Equal("Supplier", descriptor.Metadata.EntityType);
        Assert.Equal("List", descriptor.Metadata.ActionType);
        Assert.Contains("supplier", descriptor.Metadata.Aliases);
    }

    [Fact]
    public void AttributeMerge_PreservesCompanionAndExclusionMetadata()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));

        var descriptors = extractor.ExtractFromAssemblies([typeof(TestMcpTools).Assembly]);
        var descriptor = descriptors.Single(x => x.ToolName == "MergedMetadataCreateSupplier");

        Assert.Contains("Customer", descriptor.Metadata.RelatedEntities);
        Assert.Contains("Document", descriptor.Metadata.RelatedEntities);
        Assert.Contains("ListSuppliers", descriptor.Metadata.RequiredCompanionTools);
        Assert.Contains("GetSupplierDetails", descriptor.Metadata.RequiredCompanionTools);
        Assert.Contains("DeleteSupplier", descriptor.Metadata.ExcludedTools);
        Assert.Contains("UpdateSupplier", descriptor.Metadata.ExcludedTools);
    }

    [Fact]
    public void ApplyBusinessToolMetadata_RemovesAliasToolsFromList()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var catalog = new BusinessToolMetadataCatalog(extractor.ExtractFromAssemblies([typeof(PurchaseOrderTools).Assembly]));
        var composer = new ToolDescriptionComposer(new BusinessTerminologyRegistry());
        var result = new ListToolsResult
        {
            Tools =
            [
                new Tool
                {
                    Name = "CreatePurchaseOrder",
                    Description = "Create a purchase order.",
                    InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
                },
                new Tool
                {
                    Name = "CreatePO",
                    Description = "Alias for CreatePurchaseOrder.",
                    InputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement.Clone()
                }
            ]
        };

        result.ApplyBusinessToolMetadata(catalog, composer);

        Assert.Collection(
            result.Tools,
            tool => Assert.Equal("CreatePurchaseOrder", tool.Name));
    }

    [Fact]
    public void AliasRegistry_CoversAllAliasToolDescriptions()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var descriptors = extractor.ExtractFromAssemblies([typeof(SupplierTools).Assembly]);
        var aliasDescriptors = descriptors
            .Where(x => x.Description.StartsWith("Alias for ", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(aliasDescriptors);
        Assert.All(aliasDescriptors, descriptor =>
        {
            Assert.True(ToolAliasRegistry.TryGetCanonicalName(descriptor.ToolName, out var canonicalToolName));
            Assert.Equal($"Alias for {canonicalToolName}.", descriptor.Description);
        });
    }

    [Fact]
    public void ToolDescriptions_AreSingleSentenceAndTrimmed()
    {
        var extractor = new ToolMetadataExtractor(new BusinessToolMetadataBuilder(new BusinessEntityRegistry()));
        var descriptors = extractor.ExtractFromAssemblies([typeof(SupplierTools).Assembly]);

        Assert.All(descriptors, descriptor =>
        {
            Assert.DoesNotContain("GetDtoSchema", descriptor.Description, StringComparison.Ordinal);
            Assert.DoesNotContain("Supports pagination", descriptor.Description, StringComparison.Ordinal);
            Assert.DoesNotContain("For large result sets", descriptor.Description, StringComparison.Ordinal);
            Assert.DoesNotContain("Use this tool when", descriptor.Description, StringComparison.Ordinal);
            Assert.DoesNotContain(". ", descriptor.Description);
        });
    }

    [McpServerToolType]
    private class TestMcpTools
    {
        [BusinessTool(EntityType = "Supplier", ActionType = "Create")]
        [McpServerTool(Name = "CreateSupplierTest")]
        [Description("Creates a supplier account.")]
        public string CreateSupplierTest() => "ok";

        [McpServerTool(Name = "PlainTool")]
        [Description("Creates creditor records")]
        public string PlainTool() => "ok";
    }

    [McpServerToolType]
    [BusinessTool(EntityType = "Supplier", ActionType = "List", Tags = ["ap", "creditors"])]
    private class ClassLevelBusinessToolTestTools
    {
        [McpServerTool(Name = "ClassLevelListSuppliers")]
        [Description("List suppliers for matching.")]
        public string ListSuppliers() => "ok";
    }

    [McpServerToolType]
    [BusinessTool(EntityType = "Supplier", ActionType = "Create", RelatedEntities = "Customer", RequiredCompanionTools = "ListSuppliers", ExcludedTools = "DeleteSupplier")]
    private class MergedMetadataTestTools
    {
        [BusinessTool(RelatedEntities = "Document", RequiredCompanionTools = "GetSupplierDetails", ExcludedTools = "UpdateSupplier")]
        [McpServerTool(Name = "MergedMetadataCreateSupplier")]
        [Description("Create supplier with merged metadata.")]
        public string CreateSupplier() => "ok";
    }
}
