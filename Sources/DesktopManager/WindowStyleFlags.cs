namespace DesktopManager;

/// <summary>
/// Standard window style flags.
/// </summary>
[System.Flags]
public enum WindowStyleFlags {
    /// <summary>Window has a maximize button.</summary>
    MaximizeBox = 0x00010000,
    /// <summary>Window has a minimize button.</summary>
    MinimizeBox = 0x00020000,
    /// <summary>Window has a thick frame that can be resized.</summary>
    ThickFrame = 0x00040000,
    /// <summary>Window has a system menu.</summary>
    SysMenu = 0x00080000
}

