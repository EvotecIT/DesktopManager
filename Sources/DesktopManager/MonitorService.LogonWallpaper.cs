using System;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>Provides methods to manage logon (lock screen) wallpaper.</summary>
public partial class MonitorService {
    /// <summary>
    /// Sets the current account's lock screen, falling back to the machine Personalization policy.
    /// Use <see cref="SetLogonWallpaperWithResult"/> to observe failures and the mechanism used.
    /// </summary>
    /// <param name="imagePath">Path to an existing image file.</param>
    /// <remarks>Only the policy fallback requires elevation. A policy write does not prove Windows applied it.</remarks>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public void SetLogonWallpaper(string imagePath) {
        LogonWallpaperResult result = SetLogonWallpaperWithResult(imagePath, allowPolicyFallback: true);
        if (!result.Succeeded) {
            DesktopManagerDiagnostics.Report($"SetLogonWallpaper failed: {result.ErrorMessage}");
        }
    }

    /// <summary>Sets the current account's lock screen and reports the completed mechanism or failure.</summary>
    /// <param name="imagePath">Path to an existing image file. Relative paths are resolved before calling Windows.</param>
    /// <param name="allowPolicyFallback">Whether to write the machine LockScreenImage policy if WinRT fails. Requires elevation.</param>
    /// <returns>The result, including the WinRT failure when policy fallback was attempted.</returns>
    /// <remarks>
    /// The default attempts only the per-user API. File/argument errors throw; Windows API and policy errors are returned.
    /// A successful policy write is effective only on supported editions and does not confirm a visible change.
    /// </remarks>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public LogonWallpaperResult SetLogonWallpaperWithResult(string imagePath, bool allowPolicyFallback = false) {
        bool comInitialized = InitializeCom();
        try {
            return new LogonWallpaperService(new WindowsLogonWallpaperBackend()).Set(imagePath, allowPolicyFallback);
        } finally {
            if (comInitialized) {
                UninitializeCom();
            }
        }
    }

    /// <summary>Copies the current lock screen image to a temporary file, or reads the machine policy path on failure.</summary>
    /// <returns>A caller-owned temporary image path, a policy path, or an empty string if neither is available.</returns>
    /// <remarks>Delete a temporary copy when finished. Do not delete an existing image returned by policy fallback.</remarks>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public string GetLogonWallpaper() {
        bool comInitialized = InitializeCom();
        try {
            return new LogonWallpaperService(new WindowsLogonWallpaperBackend()).Get();
        } finally {
            if (comInitialized) {
                UninitializeCom();
            }
        }
    }
}
