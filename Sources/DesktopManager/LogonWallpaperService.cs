using System;
using System.IO;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>The native Windows boundary for lock screen access and machine policy storage.</summary>
internal interface ILogonWallpaperBackend {
    void SetImage(string path);
    string GetImage();
    void SetPolicy(string path);
    string GetPolicy();
}

internal sealed class WindowsLogonWallpaperBackend : ILogonWallpaperBackend {
    private const string PolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
    private const string PolicyValue = "LockScreenImage";

    public void SetImage(string path) => LockScreenNative.SetImage(path);
    public string GetImage() => LockScreenNative.GetImage();

    public void SetPolicy(string path) {
        PrivilegeChecker.EnsureElevated();
        using RegistryKey? key = Registry.LocalMachine.CreateSubKey(PolicyPath);
        if (key == null) {
            throw new InvalidOperationException("The machine Personalization policy key could not be opened.");
        }
        key.SetValue(PolicyValue, path, RegistryValueKind.String);
    }

    public string GetPolicy() {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(PolicyPath);
        return key?.GetValue(PolicyValue) as string ?? string.Empty;
    }
}

/// <summary>Owns validation and fallback decisions independently of the Windows boundary.</summary>
internal sealed class LogonWallpaperService {
    private readonly ILogonWallpaperBackend _backend;

    internal LogonWallpaperService(ILogonWallpaperBackend backend) {
        _backend = backend;
    }

    internal LogonWallpaperResult Set(string imagePath, bool allowPolicyFallback) {
        if (string.IsNullOrWhiteSpace(imagePath)) {
            throw new ArgumentNullException(nameof(imagePath));
        }
        if (!File.Exists(imagePath)) {
            throw new FileNotFoundException("The logon wallpaper file was not found.", imagePath);
        }
        string path = Path.GetFullPath(imagePath);
        string winRtError;
        try {
            _backend.SetImage(path);
            return new LogonWallpaperResult(true, LogonWallpaperMechanism.WindowsRuntime, null, null);
        } catch (Exception ex) {
            winRtError = ex.Message;
        }

        if (!allowPolicyFallback) {
            return new LogonWallpaperResult(false, LogonWallpaperMechanism.None, winRtError, winRtError);
        }
        try {
            _backend.SetPolicy(path);
            return new LogonWallpaperResult(true, LogonWallpaperMechanism.PersonalizationPolicy, null, winRtError);
        } catch (Exception ex) {
            return new LogonWallpaperResult(false, LogonWallpaperMechanism.None, ex.Message, winRtError);
        }
    }

    internal string Get() {
        try {
            return _backend.GetImage();
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"GetLogonWallpaper WinRT failed: {ex.Message}");
        }
        try {
            return _backend.GetPolicy();
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"GetLogonWallpaper policy failed: {ex.Message}");
            return string.Empty;
        }
    }
}
