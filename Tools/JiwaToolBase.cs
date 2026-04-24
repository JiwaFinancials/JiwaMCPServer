using JiwaMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Metadata;
using System.Reflection;

namespace JiwaMcpServer.Tools;

/// <summary>
/// Convenience base that resolves JiwaApiClient from the DI container.
/// MCP tool classes are instantiated per-call, so we resolve via IServiceProvider.
/// </summary>
public abstract class JiwaToolBase
{
    private static readonly Assembly JiwaDtosAssembly = typeof(JiwaFinancials.Jiwa.JiwaServiceModel.DebtorGETRequest).Assembly;
    private const string JiwaDtoNamespacePrefix = "JiwaFinancials.Jiwa.JiwaServiceModel";

    protected static string CreateDtoSchema(Type dtoType)
    {
        JsonMetadataHandler jsonMetadataHandler = new JsonMetadataHandler();
        return jsonMetadataHandler.CreateResponse(dtoType);
    }

    protected static Type ResolveJiwaDtoType(string dtoTypeName)
    {
        if (string.IsNullOrWhiteSpace(dtoTypeName))
            throw new ArgumentException("dtoTypeName is required.", nameof(dtoTypeName));

        var matches = JiwaDtosAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                t.Namespace != null &&
                t.Namespace.StartsWith(JiwaDtoNamespacePrefix, StringComparison.Ordinal) &&
                (
                    string.Equals(t.FullName, dtoTypeName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Name, dtoTypeName, StringComparison.OrdinalIgnoreCase)
                ))
            .ToList();

        if (matches.Count == 0)
            throw new ArgumentException($"DTO type '{dtoTypeName}' was not found in Jiwa DTOs.", nameof(dtoTypeName));

        if (matches.Count > 1)
            throw new ArgumentException($"Multiple DTO types named '{dtoTypeName}' were found. Use fully qualified type name.", nameof(dtoTypeName));

        return matches[0];
    }

    protected static string GetJiwaDtoSchema(string dtoTypeName)
    {
        var dtoType = ResolveJiwaDtoType(dtoTypeName);
        return CreateDtoSchema(dtoType);
    }
}
