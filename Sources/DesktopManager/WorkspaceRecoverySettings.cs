using System.IO;
using System.Text.Json;

namespace DesktopManager;

/// <summary>Per-user, opt-in settings for the desktop app's workspace recovery host.</summary>
public sealed class WorkspaceRecoverySettings {
    /// <summary>Gets or sets whether the app should restore its snapshot at startup.</summary>
    public bool RestoreOnStartup { get; set; }
    /// <summary>Gets or sets whether a running app should capture periodically after successful startup recovery or explicit capture.</summary>
    public bool AutoCapture { get; set; }
    /// <summary>Gets or sets whether capture includes the experimental virtual desktop state.</summary>
    public bool IncludeVirtualDesktops { get; set; }
    /// <summary>Gets or sets whether capture includes Explorer filesystem folders.</summary>
    public bool IncludeExplorerFolders { get; set; }
    /// <summary>Gets or sets whether restoration may reopen missing Explorer folders.</summary>
    public bool ReopenExplorerFolders { get; set; }
    /// <summary>Gets or sets whether restoration may change application-wide pinning.</summary>
    public bool RestoreApplicationPins { get; set; }

    /// <summary>Gets the per-user workspace directory. An explicit root can isolate tests or alternate profiles.</summary>
    public static string GetDirectory(string? root = null) => root ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Evotec", "DesktopManager", "Workspace");

    /// <summary>Gets the default app snapshot path under the selected workspace directory.</summary>
    public static string GetSnapshotPath(string? root = null) => Path.Combine(GetDirectory(root), "layout.json");

    /// <summary>Loads settings without creating files. Invalid existing settings are reported to the caller.</summary>
    public static WorkspaceRecoverySettings Load(string? root = null) {
        string path = Path.Combine(GetDirectory(root), "settings.json");
        if (!File.Exists(path)) { return new WorkspaceRecoverySettings(); }
        return JsonSerializer.Deserialize<WorkspaceRecoverySettings>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Workspace recovery settings contain no object.");
    }

    /// <summary>Saves the explicitly selected settings atomically. Does not register startup or change desktop state.</summary>
    public void Save(string? root = null) => AtomicFileWriter.WriteAllText(Path.Combine(GetDirectory(root), "settings.json"),
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
}
