namespace DesktopManager;

/// <summary>
/// Represents the position and size information of a window.
/// </summary>
public class WindowPosition : WindowInfo {
    /// <summary>Gets or sets the process name used to match windows after a process restart.</summary>
    public string ProcessName { get; set; } = string.Empty;
    /// <summary>Gets or sets the native window class used to constrain layout matching.</summary>
    public string ClassName { get; set; } = string.Empty;
}
