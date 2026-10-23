using Microsoft.Win32;

namespace DesktopManager;

/// <summary>The Windows Explorer selected background mode can differ from retained COM image/slideshow paths.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class WindowsWallpaperSelection {
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Wallpapers";

    internal virtual int? Read() {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue("BackgroundType") is int value ? value : (int?)null;
    }

    internal virtual void EnsureWritable() {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        if (key == null) { throw new InvalidOperationException("The selected Windows background mode is unavailable."); }
    }

    internal virtual void Set(int mode) {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        if (key == null) { throw new InvalidOperationException("The selected Windows background mode is unavailable."); }
        key.SetValue("BackgroundType", mode, RegistryValueKind.DWord);
    }
}
