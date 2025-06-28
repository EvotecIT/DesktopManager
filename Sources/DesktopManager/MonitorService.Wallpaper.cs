using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DesktopManager;

public partial class MonitorService {
    /// <summary>
    /// Sets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string monitorId, string wallpaperPath) {
        try {
            Execute(() => _desktopManager.SetWallpaper(monitorId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }
        WallpaperHistory.AddEntry(wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor from a data stream.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(string monitorId, Stream imageStream) {
        string? temp = null;
        try {
            temp = WriteStreamToTempFile(imageStream);
            SetWallpaper(monitorId, temp);
        } finally {
            if (temp is not null) {
                DeleteTempFile(temp);
            }
        }
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor from a URL.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="url">URL pointing to the image.</param>
    public void SetWallpaperFromUrl(string monitorId, string url) {
        SetWallpaperFromUrlAsync(monitorId, url).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously sets the wallpaper for a specific monitor using an image from a URL.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="url">URL pointing to the image.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetWallpaperFromUrlAsync(string monitorId, string url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
            throw new NotSupportedException($"Invalid wallpaper URL '{url}'. Only HTTP and HTTPS schemes are supported.");
        }

        using HttpClient client = new();
        using Stream stream = await client.GetStreamAsync(uri);
        SetWallpaper(monitorId, stream);
        WallpaperHistory.AddEntry(url);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(int index, string wallpaperPath) {
        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            Execute(() => _desktopManager.SetWallpaper(monitorId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }
        WallpaperHistory.AddEntry(wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by index from a data stream.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(int index, Stream imageStream) {
        var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
        SetWallpaper(monitorId, imageStream);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by index from a URL.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    public void SetWallpaperFromUrl(int index, string url) {
        SetWallpaperFromUrlAsync(index, url).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously sets the wallpaper for a monitor by index using an image from a URL.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetWallpaperFromUrlAsync(int index, string url) {
        var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
        await SetWallpaperFromUrlAsync(monitorId, url);
        WallpaperHistory.AddEntry(url);
    }

    /// <summary>
    /// Sets the wallpaper for all connected monitors.
    /// </summary>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        try {
            var devicePathCount = GetMonitorsConnected();
            foreach (var device in devicePathCount) {
                Execute(() => _desktopManager.SetWallpaper(device.DeviceId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
            }
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }
        WallpaperHistory.AddEntry(wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for all monitors using image data stream.
    /// </summary>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(Stream imageStream) {
        string? temp = null;
        try {
            temp = WriteStreamToTempFile(imageStream);
            SetWallpaper(temp);
        } finally {
            if (temp is not null) {
                DeleteTempFile(temp);
            }
        }
    }

    /// <summary>
    /// Sets the wallpaper for all monitors using an image from a URL.
    /// </summary>
    /// <param name="url">URL pointing to the image.</param>
    public void SetWallpaperFromUrl(string url) {
        SetWallpaperFromUrlAsync(url).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously sets the wallpaper for all monitors using an image from a URL.
    /// </summary>
    /// <param name="url">URL pointing to the image.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetWallpaperFromUrlAsync(string url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
            throw new NotSupportedException($"Invalid wallpaper URL '{url}'. Only HTTP and HTTPS schemes are supported.");
        }

        using HttpClient client = new();
        using Stream stream = await client.GetStreamAsync(uri);
        SetWallpaper(stream);
        WallpaperHistory.AddEntry(url);
    }

    /// <summary>
    /// Gets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(string monitorId) {
        try {
            return Execute(() => _desktopManager.GetWallpaper(monitorId), nameof(IDesktopManager.GetWallpaper));
        } catch (DesktopManagerException) {
            return GetSystemWallpaper();
        } catch (COMException) {
            return GetSystemWallpaper();
        }
    }

    /// <summary>
    /// Gets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(int index) {
        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            return Execute(() => _desktopManager.GetWallpaper(monitorId), nameof(IDesktopManager.GetWallpaper));
        } catch (DesktopManagerException) {
            return GetSystemWallpaper();
        } catch (COMException) {
            return GetSystemWallpaper();
        }
    }

    /// <summary>
    /// Gets the device path of a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The device path of the monitor.</returns>
    public string GetMonitorDevicePathAt(uint index) {
        return Execute(() => _desktopManager.GetMonitorDevicePathAt(index), nameof(IDesktopManager.GetMonitorDevicePathAt));
    }

}
