public static class Config
{
    // JiwaAPIURL is the URL of the remote Jiwa API.
    public static string? JiwaAPIURL { get; set; }

    // JiwaAPIKey is the API Key to use to perform some requests (such as getting the list of debtor contacts for a given email address to disambiguate identities at login time)
    // The key should be attached to a user with minimal permisssions, and does not need an interactive Jiwa licence.
    public static string? JiwaAPIKey { get; set; }

    // PageSize is the default page size for paginated queries
    public static int PageSize { get; set; } = 100;

    // LocalFileSystemAllowedRoots is the list of absolute paths (including UNC roots) that local file tools may read from.
    public static string[] LocalFileSystemAllowedRoots { get; set; } = Array.Empty<string>();

    // LocalFileSystemMaxReadBytes is the maximum number of bytes that can be returned from a single local file read.
    public static int LocalFileSystemMaxReadBytes { get; set; } = 256 * 1024;
}
