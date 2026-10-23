namespace DesktopManager;

/// <summary>Describes whether a window or its application appears on every virtual desktop.</summary>
public enum VirtualDesktopPinMode {
    /// <summary>The window belongs to one desktop.</summary>
    None,
    /// <summary>Only this window is pinned.</summary>
    Window,
    /// <summary>The application is pinned.</summary>
    Application
}

/// <summary>Describes one Windows virtual desktop, including its live shell wallpaper.</summary>
public sealed class VirtualDesktopInfo {
    /// <summary>Gets or sets the desktop's identity. Its ordinal is never used as a restoration fallback.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the current one-based display order.</summary>
    public int Index { get; set; }
    /// <summary>Gets or sets the desktop name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the wallpaper path reported by the shell.</summary>
    public string WallpaperPath { get; set; } = string.Empty;
}

/// <summary>Controls optional workspace data captured in an ordinary window layout.</summary>
public sealed class WorkspaceCaptureOptions {
    /// <summary>Gets or sets whether to capture virtual desktop identities, wallpaper paths and pin state using the experimental shell adapter.</summary>
    public bool IncludeVirtualDesktops { get; set; }
    /// <summary>Gets or sets whether to capture filesystem paths from Explorer windows.</summary>
    public bool IncludeExplorerFolders { get; set; }
}

/// <summary>Controls explicitly requested recovery actions. Every optional mutation is disabled by default.</summary>
public sealed class WorkspaceRestoreOptions {
    /// <summary>Gets or sets whether to restore desktop assignments and captured pin states through the experimental shell adapter.</summary>
    public bool RestoreVirtualDesktops { get; set; }
    /// <summary>Gets or sets whether recovery may change application-wide pins, affecting all windows of that application.</summary>
    public bool RestoreApplicationPins { get; set; }
    /// <summary>Gets or sets whether to reopen missing Explorer filesystem folders. Other applications are not launched.</summary>
    public bool ReopenExplorerFolders { get; set; }
    /// <summary>Gets or sets whether saved per-desktop wallpapers should be explicitly restored. Slideshow settings are untouched.</summary>
    public bool RestoreWallpapers { get; set; }
}

/// <summary>Reports a saved window's recovery without hiding skipped matches or failed actions.</summary>
public sealed class WorkspaceWindowResult {
    /// <summary>Gets or sets the saved title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the matching decision.</summary>
    public WindowLayoutMatchStatus MatchStatus { get; set; }
    /// <summary>Gets or sets whether geometry and window state were restored.</summary>
    public bool PositionApplied { get; set; }
    /// <summary>Gets or sets whether the requested desktop and pin state were restored, or null when not requested.</summary>
    public bool? VirtualDesktopApplied { get; set; }
    /// <summary>Gets or sets a concrete recovery failure.</summary>
    public string? Error { get; set; }
}

/// <summary>Reports one explicit wallpaper restoration.</summary>
public sealed class VirtualDesktopWallpaperResult {
    /// <summary>Gets or sets the target desktop.</summary>
    public Guid DesktopId { get; set; }
    /// <summary>Gets or sets whether the live shell reports the requested wallpaper after application.</summary>
    public bool Applied { get; set; }
    /// <summary>Gets or sets the reason restoration failed.</summary>
    public string? Error { get; set; }
}

/// <summary>Reports all workspace actions and limitations.</summary>
public sealed class WorkspaceRestoreResult {
    /// <summary>Gets the individual window outcomes.</summary>
    public List<WorkspaceWindowResult> Windows { get; } = new();
    /// <summary>Gets the individual wallpaper outcomes.</summary>
    public List<VirtualDesktopWallpaperResult> Wallpapers { get; } = new();
    /// <summary>Gets recovery warnings, including missing Explorer folders and capture limitations.</summary>
    public List<string> Warnings { get; } = new();
}
