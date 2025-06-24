using System.Text.Json.Serialization;

namespace DesktopManager;
/// <summary>
/// Represents basic information about a window.
/// </summary>
public class WindowInfo {
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the window handle.
    /// </summary>
    [JsonIgnore]
    public IntPtr Handle { get; set; }

    /// <summary>
    /// Gets or sets the process ID of the window.
    /// </summary>
    public uint ProcessId { get; set; }

    /// <summary>
    /// Gets the width of the window.
    /// </summary>
    public int Width => Right - Left;

    /// <summary>
    /// Gets the height of the window.
    /// </summary>
    public int Height => Bottom - Top;

    /// <summary>
    /// Gets or sets the left position of the window.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the top position of the window.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the right position of the window.
    /// </summary>
    public int Right { get; set; }

    /// <summary>
    /// Gets or sets the bottom position of the window.
    /// </summary>
    public int Bottom { get; set; }

    /// <summary>
    /// Gets or sets the monitor index on which this window is primarily located.
    /// </summary>
    public int MonitorIndex { get; set; }

    /// <summary>
    /// Gets or sets the monitor device ID on which this window is primarily located.
    /// </summary>
    public string MonitorDeviceId { get; set; }

    /// <summary>
    /// Gets or sets the monitor device name on which this window is primarily located.
    /// </summary>
    public string MonitorDeviceName { get; set; }

    /// <summary>
    /// Gets or sets whether this monitor is the primary monitor.
    /// </summary>
    public bool IsOnPrimaryMonitor { get; set; }

    /// <summary>
    /// Gets or sets the current state of the window.
    /// </summary>
    public WindowState? State { get; set; }
}
