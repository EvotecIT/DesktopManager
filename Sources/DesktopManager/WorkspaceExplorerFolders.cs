using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal static class WorkspaceExplorerFolders {
    // Shell.Application exposes filesystem folders and stable window handles without title guessing.
    internal static Dictionary<IntPtr, string> Read() {
        Type type = Type.GetTypeFromProgID("Shell.Application", true)!;
        object shell = Activator.CreateInstance(type)!;
        object? windows = null;
        try {
            windows = Invoke(shell, "Windows", BindingFlags.InvokeMethod);
            return windows is IEnumerable items ? ReadWindows(items) : new Dictionary<IntPtr, string>();
        } finally {
            ExperimentalVirtualDesktopNative.Release(windows);
            ExperimentalVirtualDesktopNative.Release(shell);
        }
    }

    internal static Dictionary<IntPtr, string> ReadWindows(IEnumerable windows) {
        var result = new Dictionary<IntPtr, string>();
        foreach (object? window in windows) {
            if (window == null) { continue; }
            var owned = new List<object>();
            try {
                if (Invoke(window, "FullName", BindingFlags.GetProperty) is not string executable ||
                        !string.Equals(Path.GetFileName(executable), "explorer.exe", StringComparison.OrdinalIgnoreCase)) { continue; }
                object? rawHandle = Invoke(window, "HWND", BindingFlags.GetProperty);
                if (rawHandle == null) { continue; }
                long handle = Convert.ToInt64(rawHandle);
                // Explorer exposes incomplete views while a newly opened folder is loading.
                object? document = Own(owned, Invoke(window, "Document", BindingFlags.GetProperty));
                if (document == null) { continue; }
                object? folder = Own(owned, Invoke(document, "Folder", BindingFlags.GetProperty));
                if (folder == null) { continue; }
                object? item = Own(owned, Invoke(folder, "Self", BindingFlags.GetProperty));
                if (item == null) { continue; }
                if (Invoke(item, "Path", BindingFlags.GetProperty) is string path && Path.IsPathRooted(path) && Directory.Exists(path)) {
                    result[new IntPtr(handle)] = path;
                }
            } catch (Exception ex) when (ex is COMException || ex is TargetInvocationException || ex is InvalidCastException) {
                // A shell window can disappear or navigate while it is being read.
            } finally {
                for (int i = owned.Count - 1; i >= 0; i--) { ExperimentalVirtualDesktopNative.Release(owned[i]); }
                ExperimentalVirtualDesktopNative.Release(window);
            }
        }
        return result;
    }

    internal static void Open(string path) {
        if (!Path.IsPathRooted(path) || !Directory.Exists(path) || path.IndexOf('"') >= 0) {
            throw new DirectoryNotFoundException("The saved Explorer folder is no longer an existing absolute directory.");
        }
        using Process? process = Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
    }

    private static object? Own(List<object> owned, object? value) {
        if (value != null) { owned.Add(value); }
        return value;
    }
    private static object? Invoke(object value, string member, BindingFlags flags) =>
        value.GetType().InvokeMember(member, flags, null, value, null, System.Globalization.CultureInfo.InvariantCulture);
}
