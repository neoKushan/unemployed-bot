using System.Reflection; // Add this using statement

public static class AppVersionInfo
{
    public static string GetInformationalVersion()
    {
        // Get the entry assembly (your main application DLL)
        var entryAssembly = Assembly.GetEntryAssembly();

        // Get the custom attribute
        var infoVersionAttribute = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        // Return the value, or a default if not found
        return infoVersionAttribute?.InformationalVersion ?? "N/A";
    }

    // Optional: Helper to extract just the commit hash if it's appended
    public static string GetCommitHash()
    {
        string fullVersion = GetInformationalVersion();
        // Default .NET SDK behavior appends '+commit_hash' if Version is set
        // Or it might just be the commit hash if Version is not explicitly set in csproj
        int plusIndex = fullVersion.LastIndexOf('+'); 
        if (plusIndex != -1 && plusIndex < fullVersion.Length - 1)
        {
            return fullVersion.Substring(plusIndex + 1);
        }
        
        // If SourceRevisionId was the *only* thing set, the whole string might be the hash
        // Add more robust parsing if needed based on your exact version format
        if (!fullVersion.Contains('.') && fullVersion != "N/A") // Basic check if it looks like just a hash
        {
             return fullVersion;
        }

        return "unknown"; // Fallback
    }
}

// --- Example Usage (e.g., in Program.MainAsync or logging setup) ---
// var informationalVersion = AppVersionInfo.GetInformationalVersion();
// var commitHash = AppVersionInfo.GetCommitHash();
// _logger.LogInformation("Starting application version: {Version} (Commit: {Commit})", informationalVersion, commitHash); 
