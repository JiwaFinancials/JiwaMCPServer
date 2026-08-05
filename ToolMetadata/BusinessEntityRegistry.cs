namespace JiwaMcpServer.ToolMetadata;

public sealed class BusinessEntityRegistry
{
    private readonly IReadOnlyDictionary<string, BusinessEntityDefinition> _entitiesByName;
    private readonly IReadOnlyDictionary<string, BusinessEntityDefinition> _entitiesByAlias;

    public BusinessEntityRegistry()
        : this(JiwaBusinessEntities.All)
    {
    }

    public BusinessEntityRegistry(IEnumerable<BusinessEntityDefinition> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var byName = new Dictionary<string, BusinessEntityDefinition>(StringComparer.OrdinalIgnoreCase);
        var byAlias = new Dictionary<string, BusinessEntityDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                continue;
            }

            byName[entity.Name.Trim()] = entity;

            foreach (var alias in entity.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                var normalizedAlias = alias.Trim();
                if (!byAlias.ContainsKey(normalizedAlias))
                {
                    byAlias[normalizedAlias] = entity;
                }
            }
        }

        _entitiesByName = byName;
        _entitiesByAlias = byAlias;
    }

    public bool TryGetByName(string entityName, out BusinessEntityDefinition? entity)
    {
        entity = null;
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return false;
        }

        return _entitiesByName.TryGetValue(entityName.Trim(), out entity);
    }

    public bool TryGetByNameOrAlias(string value, out BusinessEntityDefinition? entity)
    {
        entity = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var key = value.Trim();
        if (_entitiesByName.TryGetValue(key, out entity))
        {
            return true;
        }

        return _entitiesByAlias.TryGetValue(key, out entity);
    }

    public IReadOnlyCollection<BusinessEntityDefinition> GetAll()
        => _entitiesByName.Values.ToArray();
}
