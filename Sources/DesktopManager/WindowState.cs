namespace DesktopManager;

/// <summary>
/// Represents the state of a window.
/// </summary>
public enum WindowState {
    /// <summary>Normal (restored) window state</summary>
    Normal = 0,
    /// <summary>Minimized window state</summary>
    Minimize = 1,
    /// <summary>Maximized window state</summary>
    Maximize = 2,
    /// <summary>Close window</summary>
    Close = 3
}