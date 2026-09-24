using JiwaMcpServer.Services;

namespace JiwaMcpServer.Tests;

public class LocalFilePathResolverTests
{
    [Fact]
    public void ResolveSingleFilePath_FilePath_ReturnsDirectFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "invoice.json");
            File.WriteAllText(file, "{}");

            var resolved = LocalFilePathResolver.ResolveSingleFilePath(file, [root], maxReadBytes: 1024);

            Assert.Equal(file, resolved.ResolvedPath);
            Assert.False(resolved.InferredFromDirectory);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void ResolveSingleFilePath_DirectoryWithSingleSupportedFile_InferFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "invoice.csv");
            File.WriteAllText(file, "col\r\nvalue\r\n");

            var resolved = LocalFilePathResolver.ResolveSingleFilePath(root, [root], maxReadBytes: 1024);

            Assert.Equal(file, resolved.ResolvedPath);
            Assert.True(resolved.InferredFromDirectory);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void ResolveSingleFilePath_DirectoryWithMultipleSupportedFiles_Throws()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "a.json"), "{}");
            File.WriteAllText(Path.Combine(root, "b.xml"), "<a/>");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                LocalFilePathResolver.ResolveSingleFilePath(root, [root], maxReadBytes: 1024));

            Assert.Contains("Provide an explicit file path", ex.Message);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void ResolveSingleFilePath_OutsideAllowedRoots_Throws()
    {
        var allowedRoot = CreateTempDirectory();
        var outsideRoot = CreateTempDirectory();
        try
        {
            var outsideFile = Path.Combine(outsideRoot, "invoice.json");
            File.WriteAllText(outsideFile, "{}");

            Assert.Throws<UnauthorizedAccessException>(() =>
                LocalFilePathResolver.ResolveSingleFilePath(outsideFile, [allowedRoot], maxReadBytes: 1024));
        }
        finally
        {
            DeleteDirectory(allowedRoot);
            DeleteDirectory(outsideRoot);
        }
    }

    [Fact]
    public void ResolveSingleFilePath_FileLargerThanMaxRead_Throws()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "invoice.json");
            File.WriteAllText(file, new string('x', 2048));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                LocalFilePathResolver.ResolveSingleFilePath(file, [root], maxReadBytes: 1024));

            Assert.Contains("exceeds LocalFileSystem:MaxReadBytes", ex.Message);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JiwaMcpServerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
