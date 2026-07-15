using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Provides wallpaper management functionality for monitors.
/// </summary>
public partial class MonitorService {
    private const int WallpaperCacheTtlSeconds = 1;
    private const long MaxWallpaperDownloadBytes = 100L * 1024 * 1024;
    private static readonly HttpClient WallpaperHttpClient = new();
    private static readonly TimeSpan WallpaperCacheTtl = TimeSpan.FromSeconds(WallpaperCacheTtlSeconds);
    private readonly object _wallpaperCacheLock = new();
    private readonly Dictionary<string, WallpaperCacheEntry> _wallpaperCache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly struct WallpaperCacheEntry {
        public WallpaperCacheEntry(string path, DateTimeOffset updated) {
            Path = path;
            Updated = updated;
        }

        public string Path { get; }
        public DateTimeOffset Updated { get; }
    }

    private void SetWallpaperInternal(string monitorId, string wallpaperPath, bool addToHistory) {
        if (string.IsNullOrWhiteSpace(monitorId)) {
            throw new ArgumentNullException(nameof(monitorId));
        }
        if (string.IsNullOrWhiteSpace(wallpaperPath)) {
            throw new ArgumentNullException(nameof(wallpaperPath));
        }

        EnsureDesktopWallpaperEnabled();

        try {
            Execute(() => _desktopManager.SetWallpaper(monitorId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }

        UpdateWallpaperCache(monitorId, wallpaperPath);

        if (addToHistory) {
            WallpaperHistory.AddEntry(wallpaperPath);
        }
    }

    private void SetWallpaperInternal(string wallpaperPath, bool addToHistory) {
        if (string.IsNullOrWhiteSpace(wallpaperPath)) {
            throw new ArgumentNullException(nameof(wallpaperPath));
        }

        EnsureDesktopWallpaperEnabled();

        var monitors = new List<Monitor>();
        try {
            monitors = GetMonitorsConnected();
            if (monitors.Count == 0) {
                SetSystemWallpaper(wallpaperPath);
            } else {
                foreach (var device in monitors) {
                    Execute(() => _desktopManager.SetWallpaper(device.DeviceId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
                }
            }
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }

        if (monitors.Count > 0) {
            UpdateWallpaperCache(monitors, wallpaperPath);
        }

        if (addToHistory) {
            WallpaperHistory.AddEntry(wallpaperPath);
        }
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string monitorId, string wallpaperPath) {
        SetWallpaperInternal(monitorId, wallpaperPath, true);
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor from a data stream.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(string monitorId, Stream imageStream) {
        if (imageStream == null) {
            throw new ArgumentNullException(nameof(imageStream));
        }
        string temp = WriteStreamToTempFile(imageStream);
        try {
            SetWallpaperInternal(monitorId, temp, false);
        } finally {
            DeleteTempFile(temp);
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
    public Task SetWallpaperFromUrlAsync(string monitorId, string url) {
        return SetWallpaperFromUrlAsync(monitorId, url, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously sets the wallpaper for a specific monitor using an image from a URL.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="url">URL pointing to the image.</param>
    /// <param name="cancellationToken">Token used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetWallpaperFromUrlAsync(string monitorId, string url, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(monitorId)) {
            throw new ArgumentNullException(nameof(monitorId));
        }

        Uri uri = CreateWallpaperUri(url);
        using MemoryStream stream = await DownloadWallpaperAsync(uri, cancellationToken).ConfigureAwait(false);
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
            if (string.IsNullOrWhiteSpace(monitorId)) {
                return;
            }
            SetWallpaperInternal(monitorId, wallpaperPath, true);
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
            WallpaperHistory.AddEntry(wallpaperPath);
        }
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by index from a data stream.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(int index, Stream imageStream) {
        if (imageStream == null) {
            throw new ArgumentNullException(nameof(imageStream));
        }

        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            if (string.IsNullOrWhiteSpace(monitorId)) {
                return;
            }

            SetWallpaper(monitorId, imageStream);
        } catch (DesktopManagerException) {
            SetWallpaper(imageStream);
        }
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
    public Task SetWallpaperFromUrlAsync(int index, string url) {
        return SetWallpaperFromUrlAsync(index, url, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously sets the wallpaper for a monitor by index using an image from a URL.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    /// <param name="cancellationToken">Token used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetWallpaperFromUrlAsync(int index, string url, CancellationToken cancellationToken) {
        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            if (string.IsNullOrWhiteSpace(monitorId)) {
                return;
            }

            await SetWallpaperFromUrlAsync(monitorId, url, cancellationToken).ConfigureAwait(false);
        } catch (DesktopManagerException) {
            await SetWallpaperFromUrlAsync(url, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sets the wallpaper for all connected monitors.
    /// </summary>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        SetWallpaperInternal(wallpaperPath, true);
    }

    /// <summary>
    /// Sets the wallpaper for all monitors using image data stream.
    /// </summary>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(Stream imageStream) {
        if (imageStream == null) {
            throw new ArgumentNullException(nameof(imageStream));
        }
        string temp = WriteStreamToTempFile(imageStream);
        try {
            SetWallpaperInternal(temp, false);
        } finally {
            DeleteTempFile(temp);
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
    public Task SetWallpaperFromUrlAsync(string url) {
        return SetWallpaperFromUrlAsync(url, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously sets the wallpaper for all monitors using an image from a URL.
    /// </summary>
    /// <param name="url">URL pointing to the image.</param>
    /// <param name="cancellationToken">Token used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetWallpaperFromUrlAsync(string url, CancellationToken cancellationToken) {
        Uri uri = CreateWallpaperUri(url);
        using MemoryStream stream = await DownloadWallpaperAsync(uri, cancellationToken).ConfigureAwait(false);
        SetWallpaper(stream);
        WallpaperHistory.AddEntry(url);
    }

    private static async Task<MemoryStream> DownloadWallpaperAsync(Uri uri, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await WallpaperHttpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? contentLength = response.Content.Headers.ContentLength;
        if (contentLength > MaxWallpaperDownloadBytes) {
            throw new InvalidDataException($"Wallpaper download exceeds the {MaxWallpaperDownloadBytes}-byte limit.");
        }

        int initialCapacity = contentLength.HasValue && contentLength.Value > 0
            ? checked((int)contentLength.Value)
            : 0;
        var downloaded = new MemoryStream(initialCapacity);
        try {
            using Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            byte[] buffer = new byte[81920];
            while (true) {
                int read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (read == 0) {
                    break;
                }
                if (downloaded.Length + read > MaxWallpaperDownloadBytes) {
                    throw new InvalidDataException($"Wallpaper download exceeds the {MaxWallpaperDownloadBytes}-byte limit.");
                }

                downloaded.Write(buffer, 0, read);
            }

            downloaded.Position = 0;
            return downloaded;
        } catch {
            downloaded.Dispose();
            throw;
        }
    }

    private static Uri CreateWallpaperUri(string url) {
        if (string.IsNullOrWhiteSpace(url)) {
            throw new ArgumentNullException(nameof(url));
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
            throw new NotSupportedException($"Invalid wallpaper URL '{url}'. Only HTTP and HTTPS schemes are supported.");
        }

        return uri;
    }

    /// <summary>
    /// Gets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(string monitorId) {
        if (string.IsNullOrWhiteSpace(monitorId)) {
            throw new ArgumentNullException(nameof(monitorId));
        }

        if (TryGetCachedWallpaper(monitorId, allowStale: false, out var cached)) {
            return cached;
        }

        try {
            var path = Execute(() => _desktopManager.GetWallpaper(monitorId), nameof(IDesktopManager.GetWallpaper));
            if (!string.IsNullOrWhiteSpace(path)) {
                UpdateWallpaperCache(monitorId, path);
                return path;
            }
        } catch (DesktopManagerException) {
            if (TryGetCachedWallpaper(monitorId, allowStale: true, out cached)) {
                return cached;
            }
            return GetSystemWallpaper();
        } catch (COMException) {
            if (TryGetCachedWallpaper(monitorId, allowStale: true, out cached)) {
                return cached;
            }
            return GetSystemWallpaper();
        }

        if (TryGetCachedWallpaper(monitorId, allowStale: true, out cached)) {
            return cached;
        }

        return GetSystemWallpaper();
    }

    /// <summary>
    /// Gets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(int index) {
        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            if (string.IsNullOrWhiteSpace(monitorId)) {
                return GetSystemWallpaper();
            }
            return GetWallpaper(monitorId);
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

    private void UpdateWallpaperCache(string monitorId, string wallpaperPath) {
        if (string.IsNullOrWhiteSpace(monitorId) || string.IsNullOrWhiteSpace(wallpaperPath)) {
            return;
        }

        lock (_wallpaperCacheLock) {
            _wallpaperCache[monitorId] = new WallpaperCacheEntry(wallpaperPath, DateTimeOffset.UtcNow);
        }
    }

    private void UpdateWallpaperCache(IEnumerable<Monitor> monitors, string wallpaperPath) {
        if (string.IsNullOrWhiteSpace(wallpaperPath)) {
            return;
        }

        foreach (var monitor in monitors) {
            if (monitor == null || string.IsNullOrWhiteSpace(monitor.DeviceId)) {
                continue;
            }

            UpdateWallpaperCache(monitor.DeviceId, wallpaperPath);
        }
    }

    private bool TryGetCachedWallpaper(string monitorId, bool allowStale, out string wallpaperPath) {
        wallpaperPath = string.Empty;
        if (string.IsNullOrWhiteSpace(monitorId)) {
            return false;
        }

        lock (_wallpaperCacheLock) {
            if (_wallpaperCache.TryGetValue(monitorId, out var entry)) {
                if (allowStale || DateTimeOffset.UtcNow - entry.Updated <= WallpaperCacheTtl) {
                    wallpaperPath = entry.Path;
                    return !string.IsNullOrWhiteSpace(wallpaperPath);
                }
            }
        }

        return false;
    }

}
