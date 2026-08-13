using System.Linq;

namespace JiwaMcpServer.Services;

public static class LocalFilePathResolver
{
    public static bool TryResolveAllowedPath(string requestedPath, out string fullPath, out string error)
    {
        fullPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            error = "path is required";
            return false;
        }

        string[] allowedRoots = Config.LocalFileSystemAllowedRoots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedRoots.Length == 0)
        {
            error = "Local file access is disabled. Configure LocalFileSystem:AllowedRoots in appsettings.json";
            return false;
        }

        var normalizedRequestedPath = NormalizeRequestedPath(requestedPath);

        try
        {
            fullPath = Path.GetFullPath(normalizedRequestedPath);
        }
        catch (Exception ex)
        {
            error = $"Invalid path '{normalizedRequestedPath}': {ex.Message}";
            return false;
        }

        var resolvedPath = fullPath;
        var isAllowed = allowedRoots
            .Select(TryNormalizeRoot)
            .Where(normalizedRoot => !string.IsNullOrEmpty(normalizedRoot))
            .Any(normalizedRoot => IsPathWithinRoot(resolvedPath, normalizedRoot!));

        if (!isAllowed)
        {
            error = $"Path '{fullPath}' is outside allowed roots";
            return false;
        }

        return true;
    }

    private static string NormalizeRequestedPath(string requestedPath)
    {
        var normalizedPath = requestedPath.Trim();

        if (normalizedPath.Length >= 2)
        {
            var firstChar = normalizedPath[0];
            var lastChar = normalizedPath[^1];
            if ((firstChar == '"' && lastChar == '"') || (firstChar == '\'' && lastChar == '\''))
            {
                normalizedPath = normalizedPath[1..^1].Trim();
            }
        }

        return normalizedPath;
    }

    private static string? TryNormalizeRoot(string root)
    {
        try
        {
            return Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPathWithinRoot(string fullPath, string normalizedRoot)
    {
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
