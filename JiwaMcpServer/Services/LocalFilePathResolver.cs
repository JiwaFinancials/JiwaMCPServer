namespace JiwaMcpServer.Services;

public sealed record ResolvedLocalFilePath(string OriginalPath, string ResolvedPath, bool InferredFromDirectory);

public static class LocalFilePathResolver
{
    public static ResolvedLocalFilePath ResolveSingleFilePath(string localPath, IEnumerable<string> allowedRoots, int maxReadBytes)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new InvalidOperationException("A local path is required.");
        }

        if (maxReadBytes <= 0)
        {
            throw new InvalidOperationException("Local file max read bytes must be greater than zero.");
        }

        var normalizedRoots = NormalizeAllowedRoots(allowedRoots);
        if (normalizedRoots.Count == 0)
        {
            throw new InvalidOperationException("Local file access is disabled because no LocalFileSystem:AllowedRoots are configured.");
        }

        var fullInputPath = Path.GetFullPath(localPath.Trim().Trim('"'));
        EnsureInsideAllowedRoots(fullInputPath, normalizedRoots);

        string resolvedFilePath;
        var inferredFromDirectory = false;

        if (File.Exists(fullInputPath))
        {
            resolvedFilePath = fullInputPath;
        }
        else if (Directory.Exists(fullInputPath))
        {
            var supportedFiles = Directory.EnumerateFiles(fullInputPath, "*", SearchOption.TopDirectoryOnly)
                .Where(IsSupportedDocumentFile)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (supportedFiles.Count == 0)
            {
                throw new InvalidOperationException($"Directory '{fullInputPath}' does not contain any supported document files.");
            }

            if (supportedFiles.Count > 1)
            {
                var sample = string.Join(", ", supportedFiles.Select(Path.GetFileName).Take(5));
                throw new InvalidOperationException($"Directory '{fullInputPath}' contains {supportedFiles.Count} supported files. Provide an explicit file path. Files: {sample}");
            }

            resolvedFilePath = supportedFiles[0];
            inferredFromDirectory = true;
        }
        else
        {
            throw new FileNotFoundException($"Local path '{localPath}' was not found.", localPath);
        }

        EnsureInsideAllowedRoots(resolvedFilePath, normalizedRoots);

        var fileInfo = new FileInfo(resolvedFilePath);
        if (fileInfo.Length > maxReadBytes)
        {
            throw new InvalidOperationException(
                $"Local file '{resolvedFilePath}' is {fileInfo.Length} bytes, which exceeds LocalFileSystem:MaxReadBytes ({maxReadBytes}).");
        }

        return new ResolvedLocalFilePath(localPath, resolvedFilePath, inferredFromDirectory);
    }

    private static List<string> NormalizeAllowedRoots(IEnumerable<string> allowedRoots)
        => allowedRoots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsSupportedDocumentFile(string path)
    {
        try
        {
            DocumentFormatMappings.DetectFormat(path);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static void EnsureInsideAllowedRoots(string candidatePath, IReadOnlyCollection<string> normalizedRoots)
    {
        var fullCandidatePath = Path.GetFullPath(candidatePath);
        foreach (var root in normalizedRoots)
        {
            if (IsPathWithinRoot(root, fullCandidatePath))
            {
                return;
            }
        }

        throw new UnauthorizedAccessException($"Local path '{candidatePath}' is outside LocalFileSystem:AllowedRoots.");
    }

    private static bool IsPathWithinRoot(string rootPath, string candidatePath)
    {
        var relative = Path.GetRelativePath(rootPath, candidatePath);
        return !relative.Equals("..", StringComparison.Ordinal)
               && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
               && !Path.IsPathRooted(relative);
    }
}
