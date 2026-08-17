using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

namespace JiwaMcpServer.ToolMetadata;

public sealed class ToolMetadataExtractor
{
    private readonly BusinessToolMetadataBuilder _metadataBuilder;

    public ToolMetadataExtractor(BusinessToolMetadataBuilder metadataBuilder)
    {
        _metadataBuilder = metadataBuilder;
    }

    public IReadOnlyList<BusinessToolDescriptor> ExtractFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var descriptors = new List<BusinessToolDescriptor>();

        foreach (var assembly in assemblies.Where(a => a is not null).Distinct())
        {
            foreach (var toolType in GetToolTypes(assembly))
            {
                descriptors.AddRange(GetToolDescriptors(toolType));
            }
        }

        return descriptors
            .OrderBy(d => d.ToolName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IEnumerable<BusinessToolDescriptor> GetToolDescriptors(Type toolType)
    {
        const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        foreach (var method in toolType.GetMethods(Binding))
        {
            var mcpTool = method.GetCustomAttribute<McpServerToolAttribute>(inherit: true);
            if (mcpTool is null)
            {
                continue;
            }

            var toolName = string.IsNullOrWhiteSpace(mcpTool.Name)
                ? method.Name
                : mcpTool.Name.Trim();

            var description = method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty;
            var businessTool = method.GetCustomAttribute<BusinessToolAttribute>(inherit: true);
            var classBusinessTool = toolType.GetCustomAttribute<BusinessToolAttribute>(inherit: true);
            var mergedBusinessTool = MergeBusinessToolAttributes(classBusinessTool, businessTool);

            yield return new BusinessToolDescriptor
            {
                ToolName = toolName,
                Description = description,
                ToolType = toolType,
                Method = method,
                Metadata = _metadataBuilder.Build(toolName, description, mergedBusinessTool)
            };
        }
    }

    private static BusinessToolAttribute? MergeBusinessToolAttributes(BusinessToolAttribute? classLevel, BusinessToolAttribute? methodLevel)
    {
        if (classLevel is null)
            return methodLevel;

        if (methodLevel is null)
            return classLevel;

        return new BusinessToolAttribute
        {
            EntityType = string.IsNullOrWhiteSpace(methodLevel.EntityType) ? classLevel.EntityType : methodLevel.EntityType,
            ActionType = string.IsNullOrWhiteSpace(methodLevel.ActionType) ? classLevel.ActionType : methodLevel.ActionType,
            Aliases = [.. (classLevel.Aliases ?? []), .. (methodLevel.Aliases ?? [])],
            Tags = [.. (classLevel.Tags ?? []), .. (methodLevel.Tags ?? [])],
            RelatedEntities = MergeDelimitedValues(classLevel.RelatedEntities, methodLevel.RelatedEntities),
            RequiredCompanionTools = MergeDelimitedValues(classLevel.RequiredCompanionTools, methodLevel.RequiredCompanionTools),
            ExcludedTools = MergeDelimitedValues(classLevel.ExcludedTools, methodLevel.ExcludedTools)
        };
    }

    private static string? MergeDelimitedValues(string? classLevelValue, string? methodLevelValue)
    {
        var values = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddValues(classLevelValue);
        AddValues(methodLevelValue);

        return values.Count == 0 ? null : string.Join(',', values);

        void AddValues(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            foreach (var value in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (seen.Add(value))
                {
                    values.Add(value);
                }
            }
        }
    }

    private static IEnumerable<Type> GetToolTypes(Assembly assembly)
    {
        try
        {
            return assembly
                .GetTypes()
                .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>(inherit: true) is not null)
                .ToArray();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(t => t is not null && t.GetCustomAttribute<McpServerToolTypeAttribute>(inherit: true) is not null)!;
        }
    }
}
