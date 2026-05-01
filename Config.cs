using JiwaFinancials.Jiwa.JiwaServiceModel;
using ServiceStack.Html;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

    public static class Config
    {
        // JiwaAPIURL is the URL of the remote Jiwa API.
        public static string? JiwaAPIURL { get; set; }
        // JiwaAPIKey is the API Key to use to perform some requests (such as getting the list of debtor contacts for a given email address to disambiguate identities at login time)
        // The key should be attached to a user with minimal permisssions, and does not need an interactive Jiwa licence.
        public static string? JiwaAPIKey { get; set; }
    }