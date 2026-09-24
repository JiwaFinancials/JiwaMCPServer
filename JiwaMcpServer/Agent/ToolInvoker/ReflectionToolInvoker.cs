using JiwaMcpServer.Agent.Models;
using JiwaMcpServer.Agent.SchemaResolver;
using ModelContextProtocol.Protocol;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace JiwaMcpServer.Agent.ToolInvoker;

/// <summary>
/// Invokes MCP tool methods directly via reflection, bypassing the MCP HTTP transport.
/// Tool classes are resolved from the DI container for each invocation.
/// </summary>
public sealed class ReflectionToolInvoker : IToolInvoker
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ToolRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReflectionToolInvoker> _logger;

    public ReflectionToolInvoker(
        ToolRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<ReflectionToolInvoker> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ToolInvocationResult> InvokeAsync(string toolName, string argumentsJson, CancellationToken ct = default)
    {
        if (!_registry.TryGetTool(toolName, out var entry))
            throw new InvalidOperationException($"Tool '{toolName}' is not registered.");

        _logger.LogDebug("Invoking tool '{Tool}' with args: {Args}", toolName, argumentsJson);

        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, entry.ClassType);
        var args = BuildArguments(entry.Method, argumentsJson, ct);
        var returnValue = entry.Method.Invoke(instance, args);

        return await ExtractResultAsync(returnValue).ConfigureAwait(false);
    }

    private static object?[] BuildArguments(MethodInfo method, string argumentsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argumentsJson);
        var root = doc.RootElement;

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];

            if (param.ParameterType == typeof(CancellationToken))
            {
                args[i] = ct;
                continue;
            }

            if (TryGetProperty(root, param.Name!, out var element) ||
                TryGetProperty(root, ToCamelCase(param.Name!), out element))
            {
                args[i] = element.Deserialize(param.ParameterType, JsonOpts);
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                args[i] = param.ParameterType.IsValueType
                    ? Activator.CreateInstance(param.ParameterType)
                    : null;
            }
        }

        return args;
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement element)
    {
        if (root.TryGetProperty(name, out element))
            return true;

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                element = prop.Value;
                return true;
            }
        }

        element = default;
        return false;
    }

    private static string ToCamelCase(string name)
        => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static async Task<ToolInvocationResult> ExtractResultAsync(object? returnValue)
    {
        var result = returnValue switch
        {
            Task<string> taskStr => await taskStr.ConfigureAwait(false),
            Task task when task.GetType().IsGenericType => await AwaitGenericTaskAsync(task).ConfigureAwait(false),
            Task task => await AwaitNonGenericTaskAsync(task).ConfigureAwait(false),
            _ => returnValue
        };

        return ConvertResult(result);
    }

    private static async Task<object?> AwaitGenericTaskAsync(Task task)
    {
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static async Task<object?> AwaitNonGenericTaskAsync(Task task)
    {
        await task.ConfigureAwait(false);
        return null;
    }

    private static ToolInvocationResult ConvertResult(object? result)
    {
        return result switch
        {
            null => new ToolInvocationResult(),
            string s => new ToolInvocationResult { Text = s },
            ContentBlock block => ConvertBlocks([block]),
            IEnumerable<ContentBlock> blocks => ConvertBlocks(blocks),
            _ => new ToolInvocationResult { Text = JsonSerializer.Serialize(result, JsonOpts) }
        };
    }

    private static ToolInvocationResult ConvertBlocks(IEnumerable<ContentBlock> blocks)
    {
        var text = new StringBuilder();
        var images = new List<AgentChatImage>();
        var files = new List<AgentChatFile>();

        foreach (var block in blocks)
        {
            switch (block)
            {
                case TextContentBlock textBlock:
                    if (text.Length > 0)
                        text.AppendLine();
                    text.Append(textBlock.Text);
                    break;

                case ImageContentBlock imageBlock:
                    images.Add(new AgentChatImage
                    {
                        Base64Data = Encoding.UTF8.GetString(imageBlock.Data.ToArray()),
                        MimeType = string.IsNullOrWhiteSpace(imageBlock.MimeType) ? "image/png" : imageBlock.MimeType
                    });
                    break;

                case ResourceLinkBlock resourceLink:
                    var uri = resourceLink.Uri?.ToString() ?? string.Empty;
                    files.Add(new AgentChatFile
                    {
                        Uri = uri,
                        FileName = FirstNonEmpty(resourceLink.Name ?? string.Empty, resourceLink.Title ?? string.Empty, ExtractFileNameFromUri(uri), "file"),
                        MimeType = string.IsNullOrWhiteSpace(resourceLink.MimeType) ? "application/octet-stream" : resourceLink.MimeType,
                        Size = resourceLink.Size,
                        Description = resourceLink.Description ?? string.Empty
                    });
                    break;
            }
        }

        return new ToolInvocationResult
        {
            Text = text.ToString(),
            Images = images,
            Files = files
        };
    }

    private static string ExtractFileNameFromUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return string.Empty;

        try
        {
            var parsed = new Uri(uri);
            var lastSegment = parsed.Segments.LastOrDefault();
            return string.IsNullOrWhiteSpace(lastSegment)
                ? string.Empty
                : Uri.UnescapeDataString(lastSegment.Trim('/'));
        }
        catch
        {
            return uri;
        }
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
