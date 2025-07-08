using System;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Provides access to Windows theme settings for monitors.
/// </summary>
public partial class MonitorService {
    /// <summary>
    /// Gets the current Windows color theme.
    /// </summary>
    /// <returns>The current <see cref="SystemTheme"/>.</returns>
    public SystemTheme GetSystemTheme() {
        using RegistryKey? key = RegistryUtil.OpenSubKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "GetSystemTheme");
        if (key != null) {
            object? value = RegistryUtil.GetValue(key, "SystemUsesLightTheme");
            if (value is int dword) {
                return dword == 0 ? SystemTheme.Dark : SystemTheme.Light;
            }
        }
        return SystemTheme.Light;
    }

    /// <summary>
    /// Sets the Windows color theme.
    /// </summary>
    /// <param name="theme">Desired theme.</param>
    public void SetSystemTheme(SystemTheme theme) {
        using RegistryKey? key = RegistryUtil.CreateSubKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SetSystemTheme");
        if (key != null) {
            int value = theme == SystemTheme.Dark ? 0 : 1;
            RegistryUtil.SetValue(key, "SystemUsesLightTheme", value, RegistryValueKind.DWord);
            RegistryUtil.SetValue(key, "AppsUseLightTheme", value, RegistryValueKind.DWord);
        }
        RefreshTheme();
    }

    private static void RefreshTheme() {
        try {
            MonitorNativeMethods.SendMessage(MonitorNativeMethods.HWND_BROADCAST, MonitorNativeMethods.WM_SETTINGCHANGE, 0, 0);
        } catch (Exception ex) {
            Console.WriteLine($"RefreshTheme failed: {ex.Message}");
        }
    }
}
