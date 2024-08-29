using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DesktopManager {
    public class Monitors {
        private const int ENUM_CURRENT_SETTINGS = -1;
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

        public MonitorPosition GetMonitorPosition(string deviceId) {
            var monitors = GetMonitors();
            foreach (var monitor in monitors) {
                if (monitor.DeviceId == deviceId) {
                    return new MonitorPosition(monitor.Rect.Left, monitor.Rect.Top, monitor.Rect.Right, monitor.Rect.Bottom);
                }
            }
            throw new ArgumentException("Monitor not found");
        }

        public void SetMonitorPosition(string deviceId, MonitorPosition position) {
            SetMonitorPosition(deviceId, position.Left, position.Top, position.Right, position.Bottom);
        }

        public void SetMonitorPosition(string deviceId, int left, int top, int right, int bottom) {
            var monitorRect = GetMonitorRECT(deviceId);

            // Enumerate through all display devices and match by RECT
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);
            int deviceNum = 0;

            while (MonitorNativeMethods.EnumDisplayDevices(null, (uint)deviceNum, ref d, 0)) {
                if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                    DEVMODE devMode = new DEVMODE();
                    devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                    if (MonitorNativeMethods.EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
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
                            DisplayChangeConfirmation result = MonitorNativeMethods.ChangeDisplaySettingsEx(d.DeviceName, ref devMode, IntPtr.Zero, 0, IntPtr.Zero);
                            if (result != DisplayChangeConfirmation.Successful) {
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

        public void ListDisplayDevices() {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);

            int deviceNum = 0;
            while (MonitorNativeMethods.EnumDisplayDevices(null, (uint)deviceNum, ref d, 0)) {
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
            while (MonitorNativeMethods.EnumDisplayDevices(null, deviceNum, ref device, 0)) {
                if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                    devices.Add(device);
                }
                deviceNum++;
            }

            return devices;
        }

    }
}
