using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal static class WorkspaceExplorerFolders {
    // Shell.Application exposes filesystem folders and stable window handles without title guessing.
    internal static Dictionary<IntPtr, string> Read() {
        var result = new Dictionary<IntPtr, string>();
        Type type = Type.GetTypeFromProgID("Shell.Application", true)!;
        object shell = Activator.CreateInstance(type)!;
        object? windows = null;
        try {
            windows = Invoke(shell, "Windows", BindingFlags.InvokeMethod);
            foreach (object window in (IEnumerable)windows!) {
                var owned = new List<object>();
                try {
                    string executable = (string)Invoke(window, "FullName", BindingFlags.GetProperty)!;
                    if (!string.Equals(Path.GetFileName(executable), "explorer.exe", StringComparison.OrdinalIgnoreCase)) { continue; }
                    long handle = Convert.ToInt64(Invoke(window, "HWND", BindingFlags.GetProperty));
                    object document = Own(owned, Invoke(window, "Document", BindingFlags.GetProperty)!);
                    object folder = Own(owned, Invoke(document, "Folder", BindingFlags.GetProperty)!);
                    object item = Own(owned, Invoke(folder, "Self", BindingFlags.GetProperty)!);
                    string path = (string)Invoke(item, "Path", BindingFlags.GetProperty)!;
                    if (Path.IsPathRooted(path) && Directory.Exists(path)) { result[new IntPtr(handle)] = path; }
                } catch (Exception ex) when (ex is COMException || ex is TargetInvocationException || ex is InvalidCastException) {
                    // A shell window can disappear or navigate while it is being read.
                } finally {
                    for (int i = owned.Count - 1; i >= 0; i--) { ExperimentalVirtualDesktopNative.Release(owned[i]); }
                    ExperimentalVirtualDesktopNative.Release(window);
                }
            }
            return result;
        } finally {
            ExperimentalVirtualDesktopNative.Release(windows);
            ExperimentalVirtualDesktopNative.Release(shell);
        }
    }

    internal static void Open(string path) {
        if (!Path.IsPathRooted(path) || !Directory.Exists(path) || path.IndexOf('"') >= 0) {
            throw new DirectoryNotFoundException("The saved Explorer folder is no longer an existing absolute directory.");
        }
        using Process? process = Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
    }

    private static object Own(List<object> owned, object value) { owned.Add(value); return value; }
    private static object? Invoke(object value, string member, BindingFlags flags) =>
        value.GetType().InvokeMember(member, flags, null, value, null, System.Globalization.CultureInfo.InvariantCulture);
}
