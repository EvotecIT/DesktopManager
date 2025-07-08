using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace DesktopManager;

/// <summary>
/// Provides methods to manage logon (lock screen) wallpaper.
/// </summary>
public partial class MonitorService {
    private const string RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
    private const string RegistryValue = "LockScreenImage";

    /// <summary>
    /// Sets the logon wallpaper image path.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    [SupportedOSPlatform("windows")]
    public void SetLogonWallpaper(string imagePath) {
        try {
            Type? lockScreenType = Type.GetType("Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime");
            Type? storageFileType = Type.GetType("Windows.Storage.StorageFile, Windows, ContentType=WindowsRuntime");
            if (lockScreenType != null && storageFileType != null) {
                var getFileMethod = storageFileType.GetMethod("GetFileFromPathAsync");
                var fileOp = getFileMethod?.Invoke(null, new object[] { imagePath });
                var asTask = fileOp?.GetType().GetMethod("AsTask");
                var fileTask = asTask?.Invoke(fileOp, null) as System.Threading.Tasks.Task;
                fileTask?.Wait();
                var file = fileTask?.GetType().GetProperty("Result")?.GetValue(fileTask);
                var setMethod = lockScreenType.GetMethod("SetImageFileAsync");
                var setOp = setMethod?.Invoke(null, new object[] { file });
                var opTask = setOp?.GetType().GetMethod("AsTask")?.Invoke(setOp, null) as System.Threading.Tasks.Task;
                opTask?.Wait();
                return;
            }
        } catch {
            // ignore and use fallback
        }
        SetLogonWallpaperFallback(imagePath);
    }

    private void SetLogonWallpaperFallback(string imagePath) {
        try {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(RegistryPath);
            key?.SetValue(RegistryValue, imagePath, RegistryValueKind.String);
        } catch (Exception ex) {
            _logger.LogError(ex, "SetLogonWallpaperFallback failed");
        }
    }

    /// <summary>
    /// Gets the current logon wallpaper path if available.
    /// </summary>
    /// <returns>Path to the logon wallpaper or empty string.</returns>
    [SupportedOSPlatform("windows")]
    public string GetLogonWallpaper() {
        try {
            Type? lockScreenType = Type.GetType("Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime");
            if (lockScreenType != null) {
                var getMethod = lockScreenType.GetMethod("GetImageStream");
                var streamObj = getMethod?.Invoke(null, null);
                if (streamObj != null) {
                    var asStreamForRead = streamObj.GetType().GetMethod("AsStreamForRead");
                    using var stream = asStreamForRead?.Invoke(streamObj, null) as Stream;
                    if (stream != null) {
                        string temp = Path.GetTempFileName();
                        using FileStream fs = new FileStream(temp, FileMode.Create, FileAccess.Write);
                        stream.CopyTo(fs);
                        return temp;
                    }
                }
            }
        } catch {
            // ignore and use fallback
        }
        return GetLogonWallpaperFallback();
    }

    private string GetLogonWallpaperFallback() {
        try {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            if (key != null && key.GetValue(RegistryValue) is string value) {
                return value;
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "GetLogonWallpaperFallback failed");
        }
        return string.Empty;
    }
}
