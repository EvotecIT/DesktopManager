namespace DesktopManager;

/// <summary>
/// Specifies flags for changing display settings.
/// </summary>
[Flags]
public enum ChangeDisplaySettingsFlags : uint {
    /// <summary>
    /// No flags.
    /// </summary>
    CDS_NONE = 0,
    /// <summary>
    /// Update the registry with the new settings.
    /// </summary>
    CDS_UPDATEREGISTRY = 0x00000001,
    /// <summary>
    /// Test the new settings.
    /// </summary>
    CDS_TEST = 0x00000002,
    /// <summary>
    /// The settings are for a full-screen application.
    /// </summary>
    CDS_FULLSCREEN = 0x00000004,
    /// <summary>
    /// The settings are global.
    /// </summary>
    CDS_GLOBAL = 0x00000008,
    /// <summary>
    /// Set the primary display device.
    /// </summary>
    CDS_SET_PRIMARY = 0x00000010,
    /// <summary>
    /// The settings are for video parameters.
    /// </summary>
    CDS_VIDEOPARAMETERS = 0x00000020,
    /// <summary>
    /// Enable unsafe modes.
    /// </summary>
    CDS_ENABLE_UNSAFE_MODES = 0x00000100,
    /// <summary>
    /// Disable unsafe modes.
    /// </summary>
    CDS_DISABLE_UNSAFE_MODES = 0x00000200,
    /// <summary>
    /// Reset the settings.
    /// </summary>
    CDS_RESET = 0x40000000,
    /// <summary>
    /// Reset the settings (extended).
    /// </summary>
    CDS_RESET_EX = 0x20000000,
    /// <summary>
    /// Do not reset the settings.
    /// </summary>
    CDS_NORESET = 0x10000000
}
