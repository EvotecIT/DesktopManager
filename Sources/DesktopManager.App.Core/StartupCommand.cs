namespace DesktopManager.App.Core;

/// <summary>Builds the app's logon command while preserving its selected workspace directory.</summary>
public static class StartupCommand {
    /// <summary>Creates a Windows command line for the executable and optional workspace profile.</summary>
    public static string Build(string executable, bool startMinimized, string? workspaceDirectory = null) {
        string command = QuotePath(executable);
        if (startMinimized) { command += " --minimized"; }
        if (!string.IsNullOrWhiteSpace(workspaceDirectory)) {
            command += " --workspace-directory " + QuotePath(Path.GetFullPath(workspaceDirectory));
        }
        return command;
    }

    private static string QuotePath(string path) {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || path.IndexOf('"') >= 0) {
            throw new ArgumentException("Startup paths must be absolute paths without quotation marks.", nameof(path));
        }
        // Windows argument parsing consumes pairs of backslashes before a closing quote.
        int trailingSlashes = path.Length - path.TrimEnd('\\').Length;
        return "\"" + path + new string('\\', trailingSlashes) + "\"";
    }
}
