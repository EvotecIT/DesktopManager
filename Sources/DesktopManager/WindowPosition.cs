namespace DesktopManager;

/// <summary>
/// Represents the position and size information of a window.
/// </summary>
public class WindowPosition : WindowInfo {
    /// <summary>Gets or sets the process name used to match windows after a process restart.</summary>
    public string ProcessName { get; set; } = string.Empty;
    /// <summary>Gets or sets the native window class used to constrain layout matching.</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Gets or sets the saved virtual desktop, when desktop capture was requested.</summary>
    public Guid? VirtualDesktopId { get; set; }

    /// <summary>Gets or sets the explicitly captured window or application pin state.</summary>
    public VirtualDesktopPinMode? PinMode { get; set; }

    /// <summary>Gets or sets the application identity required to restore an application pin.</summary>
    public string AppUserModelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Explorer filesystem folder used to distinguish otherwise identical windows.</summary>
    public string ExplorerFolderPath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a captured minimized window should restore maximized.</summary>
    public bool RestoreToMaximized { get; set; }
}
