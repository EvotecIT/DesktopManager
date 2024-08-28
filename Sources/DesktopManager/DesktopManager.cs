using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DesktopManager {
    public class Monitor {
        public int Index { get; internal set; }
        public string DeviceId { get; internal set; }
        public string Wallpaper { get; internal set; }
        public DesktopWallpaperPosition WallpaperPosition { get; internal set; }
        public DesktopManager.Rect Rect { get; internal set; }
    }

    public class Monitors {
        private IDesktopManager _desktop;

        public Monitors() {
            _desktop = (new DesktopManagerWrapper()) as IDesktopManager;
        }

        /// <summary>
        /// Get all available monitor connections with details
        /// </summary>
        /// <returns></returns>
        public List<Monitor> GetMonitors() {
            List<Monitor> list = new List<Monitor>();

            for (uint i = 0; i < _desktop.GetMonitorDevicePathCount(); i++) {
                var monitor = new Monitor();
                monitor.Index = (int)i;
                monitor.DeviceId = _desktop.GetMonitorDevicePathAt(i);
                if (monitor.DeviceId != "") {
                    monitor.WallpaperPosition = _desktop.GetPosition();
                    monitor.Wallpaper = _desktop.GetWallpaper(monitor.DeviceId);
                    monitor.Rect = _desktop.GetMonitorRECT(monitor.DeviceId);
                }
                list.Add(monitor);
            }

            return list;
        }

        /// <summary>
        /// Get all available monitor paths
        /// </summary>
        /// <returns></returns>
        public uint GetAvailableMonitorPaths() {
            return _desktop.GetMonitorDevicePathCount();
        }

        /// <summary>
        /// Get connected monitors
        /// </summary>
        /// <returns></returns>
        public List<string> GetConnectedMonitors() {
            var count = GetAvailableMonitorPaths();
            List<string> devices = new List<string>();
            for (uint i = 0; i < count; i++) {
                var monitorId = _desktop.GetMonitorDevicePathAt(i);
                if (monitorId != "") {
                    devices.Add(monitorId);
                }
            }
            return devices;
        }

        /// <summary>
        /// Set wallpaper to monitorId, 
        /// </summary>
        /// <param name="monitorId"></param>
        /// <param name="wallpaperPath"></param>
        public void SetWallpaper(string monitorId, string wallpaperPath) {
            _desktop.SetWallpaper(monitorId, wallpaperPath);
        }

        /// <summary>
        /// Set wallpaper using index of connected monitor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="wallpaperPath"></param>
        public void SetWallpaper(int index, string wallpaperPath) {
            var monitorId = _desktop.GetMonitorDevicePathAt((uint)index);
            _desktop.SetWallpaper(monitorId, wallpaperPath);
        }

        /// <summary>
        /// Set wallpaper to all connected monitors
        /// </summary>
        /// <param name="wallpaperPath"></param>
        public void SetWallpaper(string wallpaperPath) {
            var devicePathCount = GetConnectedMonitors();
            foreach (var devicePath in devicePathCount) {
                _desktop.SetWallpaper(devicePath, wallpaperPath);
            }
        }

        /// <summary>
        /// Get wallpaper from monitorId
        /// </summary>
        /// <param name="monitorId"></param>
        /// <returns></returns>
        public string GetWallpaper(string monitorId) {
            return _desktop.GetWallpaper(monitorId);
        }

        /// <summary>
        /// Get wallpaper from monitor index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetWallpaper(int index) {
            return _desktop.GetWallpaper(_desktop.GetMonitorDevicePathAt((uint)index));
        }

        /// <summary>
        /// Get monitorId from monitor count (index)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetMonitorDevicePathAt(uint index) {
            return _desktop.GetMonitorDevicePathAt(index);
        }

        /// <summary>
        /// Get Wallpaper position
        /// </summary>
        /// <returns></returns>
        public DesktopWallpaperPosition GetWallpaperPosition() {
            return _desktop.GetPosition();
        }

        /// <summary>
        /// Set Wallpaper Position to one of values: Tile, Center, Stretch, Fit, Fill, Span
        /// </summary>
        /// <param name="position"></param>
        public void SetWallpaperPosition(DesktopWallpaperPosition position) {
            _desktop.SetPosition(position);
        }

        public DesktopManager.Rect GetMonitorRECT(string monitorId) {
            return _desktop.GetMonitorRECT(monitorId);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int MONITOR_DEFAULTTONEAREST = 2;
        private const uint CDS_UPDATEREGISTRY = 0x00000001;
        private const uint CDS_NORESET = 0x10000000;
        private const uint DISP_CHANGE_SUCCESSFUL = 0;

        public (int Left, int Top, int Right, int Bottom) GetMonitorPosition(string deviceId) {
            var monitors = GetMonitors();
            foreach (var monitor in monitors) {
                if (monitor.DeviceId == deviceId) {
                    return (monitor.Rect.Left, monitor.Rect.Top, monitor.Rect.Right, monitor.Rect.Bottom);
                }
            }
            throw new ArgumentException("Monitor not found");
        }

        public void SetMonitorPosition(string deviceId, int left, int top, int right, int bottom) {
            var monitorRect = GetMonitorRECT(deviceId);

            // Enumerate through all display devices and match by RECT
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);
            int deviceNum = 0;

            while (EnumDisplayDevices(null, (uint)deviceNum, ref d, 0)) {
                if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                    DEVMODE devMode = new DEVMODE();
                    devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                    if (EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
                        // Compare RECTs
                        if (monitorRect.Left == devMode.dmPositionX &&
                            monitorRect.Top == devMode.dmPositionY &&
                            (monitorRect.Right - monitorRect.Left) == devMode.dmPelsWidth &&
                            (monitorRect.Bottom - monitorRect.Top) == devMode.dmPelsHeight) {
                            // Found a match, now set the position
                            devMode.dmFields = 0x00000020; // DM_POSITION
                            devMode.dmPositionX = left;
                            devMode.dmPositionY = top;

                            // Apply the changes directly
                            int result = ChangeDisplaySettingsEx(d.DeviceName, ref devMode, IntPtr.Zero, 0, IntPtr.Zero);
                            if (result != DISP_CHANGE_SUCCESSFUL) {
                                Console.WriteLine($"ChangeDisplaySettingsEx failed with error code: {result}");
                                throw new InvalidOperationException("Unable to set monitor position");
                            }

                            Console.WriteLine($"Monitor position set successfully for {d.DeviceName}");
                            return;
                        }
                    }
                }

                deviceNum++;
            }

            throw new ArgumentException("Corresponding display device not found for the given Monitor ID.");
        }


        private const int ENUM_CURRENT_SETTINGS = -1;


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [Flags]
        public enum DisplayDeviceStateFlags : int {
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            PrimaryDevice = 0x4,
            MirroringDriver = 0x8,
            VGACompatible = 0x10,
            Removable = 0x20,
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        public void ListDisplayDevices() {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);

            int deviceNum = 0;
            while (EnumDisplayDevices(null, (uint)deviceNum, ref d, 0)) {
                Console.WriteLine($"Device Name: {d.DeviceName}");
                Console.WriteLine($"Device String: {d.DeviceString}");
                Console.WriteLine($"State Flags: {d.StateFlags}");
                Console.WriteLine($"Device ID: {d.DeviceID}");
                Console.WriteLine($"Device Key: {d.DeviceKey}");
                Console.WriteLine();
                deviceNum++;
            }
        }

        public List<DISPLAY_DEVICE> GetDisplayDevices() {
            List<DISPLAY_DEVICE> devices = new List<DISPLAY_DEVICE>();
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(device);

            uint deviceNum = 0;
            while (EnumDisplayDevices(null, deviceNum, ref device, 0)) {
                if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                    devices.Add(device);
                }
                deviceNum++;
            }

            return devices;
        }

    }
}
