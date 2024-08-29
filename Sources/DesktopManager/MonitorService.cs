using System;
using System.Collections.Generic;

namespace DesktopManager {
    public class MonitorService {
        private const int ENUM_CURRENT_SETTINGS = -1;
        private readonly IDesktopManager _desktopManager;

        public MonitorService(IDesktopManager desktopManager) {
            _desktopManager = desktopManager;
        }

        public List<Monitor> GetMonitors() {
            List<Monitor> list = new List<Monitor>();

            for (uint i = 0; i < _desktopManager.GetMonitorDevicePathCount(); i++) {
                var monitor = new Monitor();
                monitor.Index = (int)i;
                monitor.DeviceId = _desktopManager.GetMonitorDevicePathAt(i);
                if (monitor.DeviceId != "") {
                    monitor.WallpaperPosition = _desktopManager.GetPosition();
                    monitor.Wallpaper = _desktopManager.GetWallpaper(monitor.DeviceId);
                    monitor.Rect = _desktopManager.GetMonitorRECT(monitor.DeviceId);
                }
                list.Add(monitor);
            }

            return list;
        }

        public uint GetAvailableMonitorPaths() {
            return _desktopManager.GetMonitorDevicePathCount();
        }

        public List<string> GetConnectedMonitors() {
            var count = GetAvailableMonitorPaths();
            List<string> devices = new List<string>();
            for (uint i = 0; i < count; i++) {
                var monitorId = _desktopManager.GetMonitorDevicePathAt(i);
                if (monitorId != "") {
                    devices.Add(monitorId);
                }
            }
            return devices;
        }

        public void SetWallpaper(string monitorId, string wallpaperPath) {
            _desktopManager.SetWallpaper(monitorId, wallpaperPath);
        }

        public void SetWallpaper(int index, string wallpaperPath) {
            var monitorId = _desktopManager.GetMonitorDevicePathAt((uint)index);
            _desktopManager.SetWallpaper(monitorId, wallpaperPath);
        }

        public void SetWallpaper(string wallpaperPath) {
            var devicePathCount = GetConnectedMonitors();
            foreach (var devicePath in devicePathCount) {
                _desktopManager.SetWallpaper(devicePath, wallpaperPath);
            }
        }

        public string GetWallpaper(string monitorId) {
            return _desktopManager.GetWallpaper(monitorId);
        }

        public string GetWallpaper(int index) {
            return _desktopManager.GetWallpaper(_desktopManager.GetMonitorDevicePathAt((uint)index));
        }

        public string GetMonitorDevicePathAt(uint index) {
            return _desktopManager.GetMonitorDevicePathAt(index);
        }

        public DesktopWallpaperPosition GetWallpaperPosition() {
            return _desktopManager.GetPosition();
        }

        public void SetWallpaperPosition(DesktopWallpaperPosition position) {
            _desktopManager.SetPosition(position);
        }

        public Rect GetMonitorRECT(string monitorId) {
            return _desktopManager.GetMonitorRECT(monitorId);
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
