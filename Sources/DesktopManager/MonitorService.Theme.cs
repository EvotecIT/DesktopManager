using System;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

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
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            if (key != null) {
                object? value = key.GetValue("SystemUsesLightTheme");
                if (value is int dword) {
                    return dword == 0 ? SystemTheme.Dark : SystemTheme.Light;
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "GetSystemTheme failed");
        }
        return SystemTheme.Light;
    }

    /// <summary>
    /// Sets the Windows color theme.
    /// </summary>
    /// <param name="theme">Desired theme.</param>
    public void SetSystemTheme(SystemTheme theme) {
        try {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null) {
                int value = theme == SystemTheme.Dark ? 0 : 1;
                key.SetValue("SystemUsesLightTheme", value, RegistryValueKind.DWord);
                key.SetValue("AppsUseLightTheme", value, RegistryValueKind.DWord);
            }
            RefreshTheme();
        } catch (Exception ex) {
            _logger.LogError(ex, "SetSystemTheme failed");
        }
    }

    private void RefreshTheme() {
        try {
            MonitorNativeMethods.SendMessage(MonitorNativeMethods.HWND_BROADCAST, MonitorNativeMethods.WM_SETTINGCHANGE, 0, 0);
        } catch (Exception ex) {
            _logger.LogError(ex, "RefreshTheme failed");
        }
    }
}
