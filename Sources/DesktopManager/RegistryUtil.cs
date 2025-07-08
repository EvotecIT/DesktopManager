using System;
using Microsoft.Win32;

namespace DesktopManager;

internal static class RegistryUtil
{
    public static RegistryKey? OpenSubKey(RegistryKey root, string subKey, bool writable, string operation)
    {
        try {
            return root.OpenSubKey(subKey, writable);
        } catch (Exception ex) {
            Console.WriteLine($"{operation} failed: {ex.Message}");
            return null;
        }
    }

    public static RegistryKey? OpenSubKey(RegistryKey root, string subKey, string operation)
        => OpenSubKey(root, subKey, false, operation);

    public static RegistryKey? CreateSubKey(RegistryKey root, string subKey, string operation)
    {
        try {
            return root.CreateSubKey(subKey);
        } catch (Exception ex) {
            Console.WriteLine($"{operation} failed: {ex.Message}");
            return null;
        }
    }

    public static object? GetValue(RegistryKey key, string valueName)
    {
        try {
            return key.GetValue(valueName);
        } catch (Exception ex) {
            Console.WriteLine($"GetValue failed: {ex.Message}");
            return null;
        }
    }

    public static void SetValue(RegistryKey key, string name, object value, RegistryValueKind kind = RegistryValueKind.String)
    {
        try {
            key.SetValue(name, value, kind);
        } catch (Exception ex) {
            Console.WriteLine($"SetValue failed: {ex.Message}");
        }
    }
}
