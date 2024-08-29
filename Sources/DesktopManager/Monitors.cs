using System;
using System.Collections.Generic;

namespace DesktopManager {
    public class Monitors {
        private readonly MonitorService _monitorService;

        public Monitors() {
            IDesktopManager desktopManager = (IDesktopManager)new DesktopManagerWrapper(); // Explicit cast
            _monitorService = new MonitorService(desktopManager);
        }

        public List<Monitor> GetMonitors() {
            return _monitorService.GetMonitors();
        }

        //public uint GetAvailableMonitorPaths() {
        //    return _monitorService.GetAvailableMonitorPaths();
        //}

        public List<Monitor> GetMonitorsConnected() {
            return _monitorService.GetMonitorsConnected();
        }

        public void SetWallpaper(string monitorId, string wallpaperPath) {
            _monitorService.SetWallpaper(monitorId, wallpaperPath);
        }

        public void SetWallpaper(int index, string wallpaperPath) {
            _monitorService.SetWallpaper(index, wallpaperPath);
        }

        public void SetWallpaper(string wallpaperPath) {
            _monitorService.SetWallpaper(wallpaperPath);
        }

        public string GetWallpaper(string monitorId) {
            return _monitorService.GetWallpaper(monitorId);
        }

        public string GetWallpaper(int index) {
            return _monitorService.GetWallpaper(index);
        }

        public string GetMonitorDevicePathAt(uint index) {
            return _monitorService.GetMonitorDevicePathAt(index);
        }

        public DesktopWallpaperPosition GetWallpaperPosition() {
            return _monitorService.GetWallpaperPosition();
        }

        public void SetWallpaperPosition(DesktopWallpaperPosition position) {
            _monitorService.SetWallpaperPosition(position);
        }

        public RECT GetMonitorRECT(string monitorId) {
            return _monitorService.GetMonitorBounds(monitorId);
        }

        public MonitorPosition GetMonitorPosition(string deviceId) {
            return _monitorService.GetMonitorPosition(deviceId);
        }

        public void SetMonitorPosition(string deviceId, MonitorPosition position) {
            _monitorService.SetMonitorPosition(deviceId, position);
        }

        public void SetMonitorPosition(string deviceId, int left, int top, int right, int bottom) {
            _monitorService.SetMonitorPosition(deviceId, left, top, right, bottom);
        }

        public List<DISPLAY_DEVICE> DisplayDevicesAll() {
            return _monitorService.DisplayDevicesAll();
        }

        public List<DISPLAY_DEVICE> DisplayDevicesConnected() {
            return _monitorService.DisplayDevicesConnected();
        }
    }
}
