using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
#if NET8_0_OR_GREATER && WINDOWS && WINDOWS10_0_19041_0
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.System.UserProfile;
#endif

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
    [SupportedOSPlatform("windows10.0.10240.0")]
    public void SetLogonWallpaper(string imagePath) {
        bool comInitialized = InitializeCom();
        
        try {
            if (string.IsNullOrWhiteSpace(imagePath)) {
                throw new ArgumentNullException(nameof(imagePath));
            }
            if (!File.Exists(imagePath)) {
                throw new FileNotFoundException("The logon wallpaper file was not found.", imagePath);
            }
            PrivilegeChecker.EnsureElevated();
        
            try {
#if NET8_0_OR_GREATER && WINDOWS && WINDOWS10_0_19041_0
                try {
                    var file = StorageFile.GetFileFromPathAsync(imagePath).AsTask().GetAwaiter().GetResult();
                    LockScreen.SetImageFileAsync(file).AsTask().GetAwaiter().GetResult();
                    return;
                } catch {
                    // ignore and use fallback
                }
#else
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    throw new PlatformNotSupportedException("Logon wallpaper is only supported on Windows.");
                }

                Type? lockScreenType = Type.GetType(
                    "Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime");
                Type? storageFileType = Type.GetType(
                    "Windows.Storage.StorageFile, Windows, ContentType=WindowsRuntime");

                if (lockScreenType != null && storageFileType != null) {
                    var getFileMethod = storageFileType.GetMethod("GetFileFromPathAsync")
                        ?? throw new InvalidOperationException("GetFileFromPathAsync method not found");
                    var fileOp = getFileMethod.Invoke(null, new object[] { imagePath });
                    if (fileOp == null) {
                        throw new InvalidOperationException("GetFileFromPathAsync returned null");
                    }
                    var asTaskMethod = fileOp.GetType().GetMethod("AsTask")
                        ?? throw new InvalidOperationException("AsTask method not found on file operation");
                    var fileTaskObject = asTaskMethod.Invoke(fileOp, null);
                    if (fileTaskObject is not System.Threading.Tasks.Task fileTask) {
                        throw new InvalidOperationException("AsTask did not return a Task for file operation");
                    }
                    fileTask.Wait();
                    var fileProp = fileTask.GetType().GetProperty("Result")
                        ?? throw new InvalidOperationException("Result property missing on task");
                    var file = fileProp.GetValue(fileTask);
                    if (file == null) {
                        throw new InvalidOperationException("GetFileFromPathAsync returned a null result");
                    }
                    var setMethod = lockScreenType.GetMethod("SetImageFileAsync")
                        ?? throw new InvalidOperationException("SetImageFileAsync method not found");
                    var setOp = setMethod.Invoke(null, new object[] { file });
                    if (setOp == null) {
                        throw new InvalidOperationException("SetImageFileAsync returned null");
                    }
                    var opAsTaskMethod = setOp.GetType().GetMethod("AsTask")
                        ?? throw new InvalidOperationException("AsTask method not found on set operation");
                    var opTaskObject = opAsTaskMethod.Invoke(setOp, null);
                    if (opTaskObject is not System.Threading.Tasks.Task opTask) {
                        throw new InvalidOperationException("AsTask did not return a Task for set operation");
                    }
                    opTask.Wait();
                    return;
                }
#endif
            } catch {
                // ignore and use fallback
            }

            SetLogonWallpaperFallback(imagePath);
        } finally {
            if (comInitialized) {
                UninitializeCom();
            }
        }
    }

    private static void SetLogonWallpaperFallback(string imagePath) {
        PrivilegeChecker.EnsureElevated();
        try {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(RegistryPath);
            key?.SetValue(RegistryValue, imagePath, RegistryValueKind.String);
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"SetLogonWallpaperFallback failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current logon wallpaper path if available.
    /// </summary>
    /// <returns>Path to the logon wallpaper or empty string.</returns>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public string GetLogonWallpaper() {
        bool comInitialized = InitializeCom();
        try {
#if NET8_0_OR_GREATER && WINDOWS && WINDOWS10_0_19041_0
            try {
                using var inputStream = LockScreen.GetImageStream();
                using var stream = inputStream.AsStreamForRead();
                string temp = Path.GetTempFileName();
                using FileStream fs = new FileStream(temp, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fs);
                return temp;
            } catch {
                // ignore and use fallback
            }
#else
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                throw new PlatformNotSupportedException("Logon wallpaper is only supported on Windows.");
            }

            Type? lockScreenType = Type.GetType(
                "Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime");

            if (lockScreenType != null) {
                var getMethod = lockScreenType.GetMethod("GetImageStream")
                    ?? throw new InvalidOperationException("GetImageStream method not found");
                var streamObj = getMethod.Invoke(null, null);
                if (streamObj == null) {
                    throw new InvalidOperationException("GetImageStream returned null");
                }
                var asStreamForRead = streamObj.GetType().GetMethod("AsStreamForRead")
                    ?? throw new InvalidOperationException("AsStreamForRead method not found");
                var streamObjResult = asStreamForRead.Invoke(streamObj, null);
                if (streamObjResult is not Stream stream) {
                    throw new InvalidOperationException("AsStreamForRead did not return a Stream");
                }
                using var streamReader = stream;
                string temp = Path.GetTempFileName();
                using FileStream fs = new FileStream(temp, FileMode.Create, FileAccess.Write);
                streamReader.CopyTo(fs);
                return temp;
            }
#endif
        } catch {
            // ignore and use fallback
        }
        finally {
            if (comInitialized) {
                UninitializeCom();
            }
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
            DesktopManagerDiagnostics.Report($"GetLogonWallpaperFallback failed: {ex.Message}");
        }
        return string.Empty;
    }
}

