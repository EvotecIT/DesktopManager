namespace DesktopManager;

/// <summary>
/// Extended window style flags.
/// </summary>
[System.Flags]
public enum WindowExStyleFlags {
    /// <summary>Marks the window as topmost.</summary>
    TopMost = 0x00000008,
    /// <summary>Enables layered window attributes.</summary>
    Layered = 0x00080000
}

