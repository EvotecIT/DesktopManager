#if !NETSTANDARD2_0 && !NETSTANDARD2_1
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Monitors display change notifications using <c>WM_DISPLAYCHANGE</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MonitorWatcher : IDisposable {
    /// <summary>
    /// Raised when display settings change.
    /// </summary>
    public event EventHandler DisplaySettingsChanged;

    /// <summary>
    /// Raised when monitor orientation changes.
    /// </summary>
    public event EventHandler OrientationChanged;

    /// <summary>
    /// Raised when monitor resolution changes.
    /// </summary>
    public event EventHandler ResolutionChanged;

    private const int ENUM_CURRENT_SETTINGS = -1;

    private Dictionary<string, MonitorState> _state = new();

    private struct MonitorState {
        public int Width;
        public int Height;
        public int Orientation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorWatcher"/> class.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows.</exception>
    public MonitorWatcher() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("MonitorWatcher is supported only on Windows.");
        }

        _state = GetCurrentStates();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object sender, EventArgs e) {
        var current = GetCurrentStates();
        bool orientationChanged = false;
        bool resolutionChanged = false;

        foreach (var item in current) {
            if (_state.TryGetValue(item.Key, out var prev)) {
                if (prev.Orientation != item.Value.Orientation) {
                    orientationChanged = true;
                }
                if (prev.Width != item.Value.Width || prev.Height != item.Value.Height) {
                    resolutionChanged = true;
                }
            } else {
                orientationChanged = true;
                resolutionChanged = true;
            }
        }

        _state = current;

        DisplaySettingsChanged?.Invoke(this, EventArgs.Empty);
        if (orientationChanged) {
            OrientationChanged?.Invoke(this, EventArgs.Empty);
        }
        if (resolutionChanged) {
            ResolutionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Dictionary<string, MonitorState> GetCurrentStates() {
        Dictionary<string, MonitorState> states = new();
        DISPLAY_DEVICE device = new();
        device.cb = Marshal.SizeOf(device);
        uint index = 0;
        while (MonitorNativeMethods.EnumDisplayDevices(null, index, ref device, 0)) {
            if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                DEVMODE mode = new();
                mode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
                if (MonitorNativeMethods.EnumDisplaySettings(device.DeviceName, ENUM_CURRENT_SETTINGS, ref mode)) {
                    states[device.DeviceName] = new MonitorState {
                        Width = mode.dmPelsWidth,
                        Height = mode.dmPelsHeight,
                        Orientation = mode.dmDisplayOrientation
                    };
                }
            }
            index++;
        }
        return states;
    }

    /// <summary>
    /// Unsubscribes from system events.
    /// </summary>
    public void Dispose() {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        GC.SuppressFinalize(this);
    }
}
#endif
