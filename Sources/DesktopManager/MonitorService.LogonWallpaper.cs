using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

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
        PrivilegeChecker.EnsureElevated();
        try {
            Type lockScreenType = Type.GetType(
                "Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime")
                    ?? throw new InvalidOperationException("LockScreen type not found");
            Type storageFileType = Type.GetType(
                "Windows.Storage.StorageFile, Windows, ContentType=WindowsRuntime")
                    ?? throw new InvalidOperationException("StorageFile type not found");

            var getFileMethod = storageFileType.GetMethod("GetFileFromPathAsync")
                ?? throw new InvalidOperationException("GetFileFromPathAsync method not found");
            var fileOp = getFileMethod.Invoke(null, new object[] { imagePath });
            var asTaskMethod = fileOp.GetType().GetMethod("AsTask")
                ?? throw new InvalidOperationException("AsTask method not found on file operation");
            var fileTask = (System.Threading.Tasks.Task)asTaskMethod.Invoke(fileOp, null);
            fileTask.Wait();
            var fileProp = fileTask.GetType().GetProperty("Result")
                ?? throw new InvalidOperationException("Result property missing on task");
            var file = fileProp.GetValue(fileTask);
            var setMethod = lockScreenType.GetMethod("SetImageFileAsync")
                ?? throw new InvalidOperationException("SetImageFileAsync method not found");
            var setOp = setMethod.Invoke(null, new object[] { file });
            var opAsTaskMethod = setOp.GetType().GetMethod("AsTask")
                ?? throw new InvalidOperationException("AsTask method not found on set operation");
            var opTask = (System.Threading.Tasks.Task)opAsTaskMethod.Invoke(setOp, null);
            opTask.Wait();
            return;
        } catch (InvalidOperationException) {
            throw;
        } catch {
            // ignore and use fallback
        }

        SetLogonWallpaperFallback(imagePath);
    }

    private static void SetLogonWallpaperFallback(string imagePath) {
        PrivilegeChecker.EnsureElevated();
        try {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(RegistryPath);
            key?.SetValue(RegistryValue, imagePath, RegistryValueKind.String);
        } catch (Exception ex) {
            Console.WriteLine($"SetLogonWallpaperFallback failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current logon wallpaper path if available.
    /// </summary>
    /// <returns>Path to the logon wallpaper or empty string.</returns>
    [SupportedOSPlatform("windows")]
    public string GetLogonWallpaper() {
        try {
            Type lockScreenType = Type.GetType(
                "Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime")
                    ?? throw new InvalidOperationException("LockScreen type not found");

            var getMethod = lockScreenType.GetMethod("GetImageStream")
                ?? throw new InvalidOperationException("GetImageStream method not found");
            var streamObj = getMethod.Invoke(null, null);
            var asStreamForRead = streamObj.GetType().GetMethod("AsStreamForRead")
                ?? throw new InvalidOperationException("AsStreamForRead method not found");
            using var stream = (Stream)asStreamForRead.Invoke(streamObj, null);
            string temp = Path.GetTempFileName();
            using FileStream fs = new FileStream(temp, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);
            return temp;
        } catch (InvalidOperationException) {
            throw;
        } catch {
            // ignore and use fallback
        }

        return GetLogonWallpaperFallback();
    }

    private static string GetLogonWallpaperFallback() {
        try {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            if (key != null && key.GetValue(RegistryValue) is string value) {
                return value;
            }
        } catch (Exception ex) {
            Console.WriteLine($"GetLogonWallpaperFallback failed: {ex.Message}");
        }
        return string.Empty;
    }
}
