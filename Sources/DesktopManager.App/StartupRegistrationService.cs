using Microsoft.Win32;

namespace DesktopManager.App;

internal static class StartupRegistrationService {
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopManager";

    public static bool IsEnabled() {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value &&
            string.Equals(value, GetCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled) {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled) {
            key.SetValue(ValueName, GetCommand(), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string GetCommand() {
        string executable = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        return $"\"{executable}\"";
    }
}
