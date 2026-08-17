namespace JiwaMcpServer.ToolMetadata;

/// <summary>
/// Registry for mapping user-friendly form names/abbreviations to actual Jiwa form ClassNames.
/// Enables "open po" to resolve to the actual Purchase Order form class.
/// </summary>
public sealed class FormNameRegistry
{
    private readonly IReadOnlyDictionary<string, string> _mappings;

    public FormNameRegistry(IReadOnlyDictionary<string, string>? mappings = null)
    {
        _mappings = mappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to resolve a user-friendly form name to its actual ClassName.
    /// </summary>
    /// <param name="userProvidedName">The name or abbreviation provided by the user (e.g., "po", "purchase order")</param>
    /// <param name="className">The resolved ClassName if found</param>
    /// <returns>True if a mapping exists for the provided name</returns>
    public bool TryResolveFormName(string? userProvidedName, out string className)
    {
        className = string.Empty;
        if (string.IsNullOrWhiteSpace(userProvidedName))
            return false;

        return _mappings.TryGetValue(userProvidedName.Trim(), out className!);
    }

    /// <summary>
    /// Gets all configured form name mappings.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAllMappings() => _mappings;
}
