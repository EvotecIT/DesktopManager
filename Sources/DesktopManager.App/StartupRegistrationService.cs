using Microsoft.Win32;

namespace DesktopManager.App;

internal static class StartupRegistrationService {
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopManager";
    private const string MinimizedArgument = "--minimized";

    public static bool IsEnabled() {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is not string value) {
            return false;
        }

        if (string.Equals(value, GetCommand(startMinimized: true), StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (string.Equals(value, GetCommand(startMinimized: false), StringComparison.OrdinalIgnoreCase)) {
            key.SetValue(ValueName, GetCommand(startMinimized: true), RegistryValueKind.String);
            return true;
        }

        return false;
    }

    public static void SetEnabled(bool enabled) {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled) {
            key.SetValue(ValueName, GetCommand(startMinimized: true), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string GetCommand(bool startMinimized) {
        string executable = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        return startMinimized
            ? $"\"{executable}\" {MinimizedArgument}"
            : $"\"{executable}\"";
    }
}
