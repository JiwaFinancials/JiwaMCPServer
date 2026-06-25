using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace JiwaMcpServer.Services;

public static class PluginAssemblyLoader
{
    public static IReadOnlyList<Assembly> LoadPluginAssemblies(IConfiguration configuration, string contentRootPath, ILogger logger)
    {
        var pluginsSection = configuration.GetSection("Plugins");
        var pluginsEnabled = pluginsSection.GetValue("Enabled", true);
        if (!pluginsEnabled)
        {
            logger.LogInformation("Plugin loading is disabled via Plugins:Enabled=false.");
            return [];
        }

        var pluginFolderSetting = pluginsSection.GetValue<string>("Folder") ?? "Plugins";
        var pluginFolderPath = ResolvePath(pluginFolderSetting, contentRootPath);

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(pluginFolderPath))
        {
            foreach (var dll in Directory.EnumerateFiles(pluginFolderPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                candidates.Add(Path.GetFullPath(dll));
            }
        }
        else
        {
            logger.LogInformation("Plugin folder '{PluginFolderPath}' does not exist. No plugin tools loaded.", pluginFolderPath);
        }

        var configuredAssemblies = pluginsSection.GetSection("Assemblies").Get<string[]>() ?? [];
        foreach (var configuredAssembly in configuredAssemblies)
        {
            if (string.IsNullOrWhiteSpace(configuredAssembly))
                continue;

            candidates.Add(ResolvePath(configuredAssembly, contentRootPath));
        }

        var loadedAssemblies = new List<Assembly>();

        foreach (var assemblyPath in candidates)
        {
            if (!File.Exists(assemblyPath))
            {
                logger.LogWarning("Configured plugin assembly not found: {AssemblyPath}", assemblyPath);
                continue;
            }

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                loadedAssemblies.Add(assembly);
                logger.LogInformation("Loaded plugin assembly: {AssemblyName} from {AssemblyPath}", assembly.GetName().Name, assemblyPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load plugin assembly from {AssemblyPath}", assemblyPath);
            }
        }

        return loadedAssemblies;
    }

    private static string ResolvePath(string path, string contentRootPath)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
