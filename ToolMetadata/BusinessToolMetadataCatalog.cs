namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessToolMetadataCatalog
{
    private readonly IReadOnlyDictionary<string, BusinessToolDescriptor> _descriptorsByToolName;

    public BusinessToolMetadataCatalog(IEnumerable<BusinessToolDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var map = new Dictionary<string, BusinessToolDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var descriptor in descriptors)
        {
            if (string.IsNullOrWhiteSpace(descriptor.ToolName))
            {
                continue;
            }

            map[descriptor.ToolName] = descriptor;
        }

        _descriptorsByToolName = map;
        Descriptors = _descriptorsByToolName.Values
            .OrderBy(x => x.ToolName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<BusinessToolDescriptor> Descriptors { get; }

    public bool TryGetByToolName(string toolName, out BusinessToolDescriptor? descriptor)
    {
        descriptor = null;
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        return _descriptorsByToolName.TryGetValue(toolName.Trim(), out descriptor);
    }
}
