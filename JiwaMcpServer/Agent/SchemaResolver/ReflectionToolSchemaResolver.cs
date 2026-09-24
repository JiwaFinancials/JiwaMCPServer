using JiwaMcpServer.Agent.LlmClient;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace JiwaMcpServer.Agent.SchemaResolver;

/// <summary>
/// Generates JSON parameter schemas from MCP tool method signatures
/// using .NET 10's <see cref="JsonSchemaExporter"/>.
/// Schemas are cached on first access.
/// </summary>
public sealed class ReflectionToolSchemaResolver : IToolSchemaResolver
{
    private static readonly JsonSerializerOptions SchemaOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly ToolRegistry _registry;
    private readonly ILogger<ReflectionToolSchemaResolver> _logger;
    private readonly ConcurrentDictionary<string, LlmToolDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);

    public int TotalToolCount => _registry.Count;
    public IReadOnlyList<string> RegisteredToolNames => _registry.Tools.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();

    public ReflectionToolSchemaResolver(ToolRegistry registry, ILogger<ReflectionToolSchemaResolver> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public Task<IReadOnlyDictionary<string, LlmToolDefinition>> ResolveAsync(
        IEnumerable<string> toolNames,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, LlmToolDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in toolNames)
        {
            if (!_registry.TryGetTool(name, out var entry))
            {
                _logger.LogWarning("Tool '{Name}' was selected by the domain router but is not registered", name);
                continue;
            }

            var definition = _cache.GetOrAdd(entry.Name, _ => BuildDefinition(entry));
            result[entry.Name] = definition;
        }

        return Task.FromResult<IReadOnlyDictionary<string, LlmToolDefinition>>(result);
    }

    public Task<IReadOnlyDictionary<string, LlmToolDefinition>> ResolveAllAsync(CancellationToken ct = default)
        => ResolveAsync(_registry.Tools.Keys, ct);

    private LlmToolDefinition BuildDefinition(ToolEntry entry) => new()
    {
        Name = entry.Name,
        Description = entry.Description,
        ParametersSchema = BuildParametersSchema(entry)
    };

    private JsonNode BuildParametersSchema(ToolEntry entry)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var param in entry.Method.GetParameters())
        {
            if (param.ParameterType == typeof(CancellationToken))
                continue;

            var typeSchema = GenerateTypeSchema(entry, param.ParameterType, param.Name ?? "parameter");

            var desc = param.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(desc) && typeSchema is JsonObject obj)
                obj["description"] = desc;

            properties[param.Name!] = typeSchema;

            if (!param.HasDefaultValue && !IsNullableType(param.ParameterType))
                required.Add(JsonValue.Create(param.Name));
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
            schema["required"] = required;

        return schema;
    }

    private JsonNode GenerateTypeSchema(ToolEntry entry, Type type, string contextName)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(bool)) return JsonNode.Parse("""{"type":"boolean"}""")!;
        if (underlying == typeof(int)) return JsonNode.Parse("""{"type":"integer","format":"int32"}""")!;
        if (underlying == typeof(long)) return JsonNode.Parse("""{"type":"integer","format":"int64"}""")!;
        if (underlying == typeof(float)) return JsonNode.Parse("""{"type":"number","format":"float"}""")!;
        if (underlying == typeof(double)) return JsonNode.Parse("""{"type":"number","format":"double"}""")!;
        if (underlying == typeof(decimal)) return JsonNode.Parse("""{"type":"number"}""")!;
        if (underlying == typeof(string)) return JsonNode.Parse("""{"type":"string"}""")!;
        if (underlying == typeof(Guid)) return JsonNode.Parse("""{"type":"string","format":"uuid"}""")!;
        if (underlying == typeof(DateTime)) return JsonNode.Parse("""{"type":"string","format":"date-time"}""")!;
        if (underlying == typeof(DateTimeOffset)) return JsonNode.Parse("""{"type":"string","format":"date-time"}""")!;
        if (underlying == typeof(DateOnly)) return JsonNode.Parse("""{"type":"string","format":"date"}""")!;

        if (underlying.IsEnum)
        {
            var enumValues = new JsonArray();
            foreach (var name in Enum.GetNames(underlying))
                enumValues.Add(name);

            return new JsonObject { ["type"] = "string", ["enum"] = enumValues };
        }

        var elementType = GetCollectionElementType(underlying);
        if (elementType != null)
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = GenerateTypeSchema(entry, elementType, contextName + "[]")
            };
        }

        try
        {
            var exporterOptions = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };
            return JsonSchemaExporter.GetJsonSchemaAsNode(SchemaOptions, underlying, exporterOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to export JSON schema for tool '{Tool}' parameter '{Parameter}' type '{Type}'. Falling back to a generic object schema.",
                entry.Name,
                contextName,
                underlying.FullName);

            return new JsonObject { ["type"] = "object" };
        }
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

    private static bool IsNullableType(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}
