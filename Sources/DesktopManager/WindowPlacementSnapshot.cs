using System;

namespace DesktopManager;

/// <summary>
/// Captures the observed state of a window at a specific placement execution stage.
/// </summary>
public sealed class WindowPlacementSnapshot {
    /// <summary>Gets or sets the execution stage name.</summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>Gets or sets the window title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the window handle.</summary>
    public IntPtr Handle { get; set; }

    /// <summary>Gets or sets the owning process ID.</summary>
    public uint ProcessId { get; set; }

    /// <summary>Gets or sets the observed window state.</summary>
    public WindowState? State { get; set; }

    /// <summary>Gets or sets the observed left coordinate.</summary>
    public int Left { get; set; }

    /// <summary>Gets or sets the observed top coordinate.</summary>
    public int Top { get; set; }

    /// <summary>Gets or sets the observed width.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the observed height.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the observed monitor index.</summary>
    public int MonitorIndex { get; set; }

    /// <summary>Gets or sets the observed monitor device name.</summary>
    public string MonitorDeviceName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the window was visible.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Gets or sets whether the window was topmost.</summary>
    public bool IsTopMost { get; set; }

    /// <summary>
    /// Creates a placement snapshot from current window information.
    /// </summary>
    /// <param name="stage">Execution stage name.</param>
    /// <param name="window">Observed window information.</param>
    /// <returns>A snapshot with copied window state.</returns>
    public static WindowPlacementSnapshot FromWindow(string stage, WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        return new WindowPlacementSnapshot {
            Stage = stage,
            Title = window.Title,
            Handle = window.Handle,
            ProcessId = window.ProcessId,
            State = window.State,
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            MonitorIndex = window.MonitorIndex,
            MonitorDeviceName = window.MonitorDeviceName,
            IsVisible = window.IsVisible,
            IsTopMost = window.IsTopMost
        };
    }
}
