#if !NETSTANDARD2_0 && !NETSTANDARD2_1
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Management;

namespace DesktopManager;

/// <summary>
/// Monitors display change notifications using <c>WM_DISPLAYCHANGE</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public class MonitorWatcher : IDisposable {
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

    /// <summary>
    /// Raised when monitor is connected.
    /// </summary>
    public event EventHandler MonitorConnected;

    /// <summary>
    /// Raised when monitor is disconnected.
    /// </summary>
    public event EventHandler MonitorDisconnected;

    private const int ENUM_CURRENT_SETTINGS = -1;

    private Dictionary<string, MonitorState> _state = new();
    private bool _disposed;
    private ManagementEventWatcher? _connectWatcher;
    private ManagementEventWatcher? _disconnectWatcher;

    public struct MonitorState {
        /// <summary>Current width of the monitor.</summary>
        public int Width;

        /// <summary>Current height of the monitor.</summary>
        public int Height;

        /// <summary>Current orientation of the monitor.</summary>
        public int Orientation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorWatcher"/> class.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows.</exception>
    public MonitorWatcher() : this(true) { }

    protected MonitorWatcher(bool enableDeviceWatchers) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("MonitorWatcher is supported only on Windows.");
        }

        _state = GetCurrentStates();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        if (enableDeviceWatchers) {
            InitializeDeviceWatchers();
        }
    }

    private void OnDisplaySettingsChanged(object sender, EventArgs e) {
        CheckForChanges();
    }

    protected virtual Dictionary<string, MonitorState> GetCurrentStates() {
        Dictionary<string, MonitorState> states = new();
        DISPLAY_DEVICE device = new();
        device.cb = Marshal.SizeOf(device);
        uint index = 0;
        while (MonitorNativeMethods.EnumDisplayDevices(null, index, ref device, (uint)EnumDisplayDevicesFlags.EDD_GET_DEVICE_INTERFACE_NAME)) {
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

    protected void CheckForChanges() {
        var current = GetCurrentStates();
        bool orientationChanged = false;
        bool resolutionChanged = false;
        bool connected = false;
        bool disconnected = false;

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
                connected = true;
            }
        }

        foreach (var item in _state.Keys) {
            if (!current.ContainsKey(item)) {
                disconnected = true;
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
        if (connected) {
            MonitorConnected?.Invoke(this, EventArgs.Empty);
        }
        if (disconnected) {
            MonitorDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void InitializeDeviceWatchers() {
        try {
            var connectQuery = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1), "TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.PNPClass = 'Monitor'");
            _connectWatcher = new ManagementEventWatcher(connectQuery);
            _connectWatcher.EventArrived += (_, _) => CheckForChanges();
            _connectWatcher.Start();

            var disconnectQuery = new WqlEventQuery("__InstanceDeletionEvent", new TimeSpan(0, 0, 1), "TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.PNPClass = 'Monitor'");
            _disconnectWatcher = new ManagementEventWatcher(disconnectQuery);
            _disconnectWatcher.EventArrived += (_, _) => CheckForChanges();
            _disconnectWatcher.Start();
        } catch (ManagementException) {
            // WMI might not be available
        }
    }


    /// <summary>
    /// Unsubscribes from system events.
    /// </summary>
    public void Dispose() {
        Dispose(true);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="MonitorWatcher"/> class.
    /// </summary>
    ~MonitorWatcher() {
        Dispose(false);
    }

    private void Dispose(bool disposing) {
        if (_disposed) {
            return;
        }

        if (disposing) {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _connectWatcher?.Stop();
            _connectWatcher?.Dispose();
            _disconnectWatcher?.Stop();
            _disconnectWatcher?.Dispose();
            GC.SuppressFinalize(this);
        }

        _disposed = true;
    }
}
#endif
