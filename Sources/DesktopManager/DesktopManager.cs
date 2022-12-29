using System.Collections.Generic;
using System.Drawing;

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

        public Rect GetMonitorRECT(string monitorId) {
            return _desktop.GetMonitorRECT(monitorId);
        }
    }
}
