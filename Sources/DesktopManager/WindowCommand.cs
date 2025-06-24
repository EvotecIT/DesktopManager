namespace DesktopManager;

/// <summary>
/// Window command constants.
/// </summary>
public enum WindowCommand {
    /// <summary>
    /// Close the window.
    /// </summary>
    SC_CLOSE = 0xF060,

    /// <summary>
    /// Show context help.
    /// </summary>
    SC_CONTEXTHELP = 0xF180,

    /// <summary>
    /// Restore to default position.
    /// </summary>
    SC_DEFAULT = 0xF160,

    /// <summary>
    /// Activate via hotkey.
    /// </summary>
    SC_HOTKEY = 0xF150,

    /// <summary>
    /// Scroll horizontally.
    /// </summary>
    SC_HSCROLL = 0xF080,

    /// <summary>
    /// Indicate secure content.
    /// </summary>
    SCF_ISSECURE = 0x00000001,

    /// <summary>
    /// Open keyboard menu.
    /// </summary>
    SC_KEYMENU = 0xF100,

    /// <summary>
    /// Maximize the window.
    /// </summary>
    SC_MAXIMIZE = 0xF030,

    /// <summary>
    /// Minimize the window.
    /// </summary>
    SC_MINIMIZE = 0xF020,

    /// <summary>
    /// Move the window.
    /// </summary>
    SC_MOVE = 0xF010,

    /// <summary>
    /// Restore the window.
    /// </summary>
    SC_RESTORE = 0xF120
}
