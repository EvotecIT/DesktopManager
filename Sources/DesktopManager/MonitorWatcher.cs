#if !NETSTANDARD2_0 && !NETSTANDARD2_1
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading;

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

    /// <summary>
    /// Raised when monitor power is turned off.
    /// </summary>
    public event EventHandler MonitorPoweredOff;

    /// <summary>
    /// Raised when monitor power is turned on.
    /// </summary>
    public event EventHandler MonitorPoweredOn;

    private const int ENUM_CURRENT_SETTINGS = -1;

    private Dictionary<string, MonitorState> _state = new();
    private bool _disposed;
    private PowerBroadcastWindow? _powerWindow;

    private static readonly Guid GUID_MONITOR_POWER_ON = new("02731015-4510-4526-99E6-E5A17EBD1AEA");

    private struct MonitorState {
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
    public MonitorWatcher() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("MonitorWatcher is supported only on Windows.");
        }

        _state = GetCurrentStates();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _powerWindow = new PowerBroadcastWindow(this);
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

    internal void ProcessPowerBroadcast(int state) {
        if (state == 0) {
            MonitorPoweredOff?.Invoke(this, EventArgs.Empty);
        } else {
            MonitorPoweredOn?.Invoke(this, EventArgs.Empty);
        }
    }

    private Dictionary<string, MonitorState> GetCurrentStates() {
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
            _powerWindow?.Dispose();
            GC.SuppressFinalize(this);
        }

        _disposed = true;
    }

    private sealed class PowerBroadcastWindow : IDisposable {
        private readonly MonitorWatcher _parent;
        private IntPtr _hwnd;
        private IntPtr _notificationHandle;
        private Thread _thread;
        private MonitorNativeMethods.WndProc? _wndProc;
        private readonly ManualResetEventSlim _ready = new(false);

        public PowerBroadcastWindow(MonitorWatcher parent) {
            _parent = parent;
            _thread = new Thread(MessageLoop) { IsBackground = true };
            _thread.Start();
            _ready.Wait();
        }

        private void MessageLoop() {
            _wndProc = WndProc;
            _hwnd = MonitorNativeMethods.CreateWindowExW(0, "Static", string.Empty, 0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            MonitorNativeMethods.SetWindowLongPtr(_hwnd, MonitorNativeMethods.GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProc));
            var guid = GUID_MONITOR_POWER_ON;
            _notificationHandle = MonitorNativeMethods.RegisterPowerSettingNotification(
                _hwnd,
                ref guid,
                MonitorNativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            _ready.Set();

            MonitorNativeMethods.MSG msg;
            while (MonitorNativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0) != 0) {
                MonitorNativeMethods.TranslateMessage(ref msg);
                MonitorNativeMethods.DispatchMessage(ref msg);
            }

            if (_notificationHandle != IntPtr.Zero) {
                MonitorNativeMethods.UnregisterPowerSettingNotification(_notificationHandle);
                _notificationHandle = IntPtr.Zero;
            }
            if (_hwnd != IntPtr.Zero) {
                MonitorNativeMethods.DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            if (msg == (uint)WindowMessage.WM_POWERBROADCAST && wParam.ToInt32() == MonitorNativeMethods.PBT_POWERSETTINGCHANGE) {
                var setting = Marshal.PtrToStructure<MonitorNativeMethods.POWERBROADCAST_SETTING>(lParam);
                if (setting.PowerSetting == GUID_MONITOR_POWER_ON) {
                    _parent.ProcessPowerBroadcast(setting.Data);
                }
            }
            return MonitorNativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        public void Dispose() {
            if (_hwnd != IntPtr.Zero) {
                MonitorNativeMethods.PostMessage(_hwnd, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                _thread.Join();
            }
            _ready.Dispose();
        }
    }
}


#endif
