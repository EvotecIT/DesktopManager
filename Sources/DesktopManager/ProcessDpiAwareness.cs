namespace DesktopManager;

/// <summary>
/// Specifies the DPI awareness of a process.
/// </summary>
public enum ProcessDpiAwareness {
    /// <summary>The process is not DPI aware.</summary>
    Process_DPI_Unaware = 0,
    /// <summary>The process is system DPI aware.</summary>
    Process_System_DPI_Aware = 1,
    /// <summary>The process is per-monitor DPI aware.</summary>
    Process_Per_Monitor_DPI_Aware = 2
}
