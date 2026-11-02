using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal static class WorkspaceExplorerFolders {
    internal static Dictionary<IntPtr, string> Read() {
        object shellWindows = NativeComActivation.CreateInstance(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"));
        try {
            var windows = (IExplorerShellWindows)shellWindows;
            Marshal.ThrowExceptionForHR(windows.GetCount(out int count));
            var result = new Dictionary<IntPtr, string>();
            // Bound a changing shell collection, retaining the initial enumeration order.
            for (int i = 0; i < Math.Min(count, 4096); i++) {
                IntPtr pointer = IntPtr.Zero;
                object? window = null;
                try {
                    var index = new ExplorerVariantIndex { Type = 3, Value = i }; // VT_I4
                    Marshal.ThrowExceptionForHR(windows.Item(index, out pointer));
                    if (pointer == IntPtr.Zero) { continue; }
                    window = NativeComActivation.FromPointer(pointer);
                    var browser = (IExplorerWebBrowserApp)window;
                    Marshal.ThrowExceptionForHR(browser.GetFullName(out string executable));
                    if (!string.Equals(Path.GetFileName(executable), "explorer.exe", StringComparison.OrdinalIgnoreCase)) { continue; }
                    Marshal.ThrowExceptionForHR(browser.GetHwnd(out IntPtr handle));
                    string? path = ReadCurrentFolder(window);
                    if (path != null && Path.IsPathRooted(path) && Directory.Exists(path)) { result[handle] = path; }
                } catch (Exception ex) when (ex is COMException || ex is InvalidCastException ||
                    ex is UnauthorizedAccessException || ex is NotImplementedException || ex is ArgumentException) {
                    // A shell window can disappear or navigate while it is being read.
                } finally {
                    NativeComActivation.Release(window);
                    if (pointer != IntPtr.Zero) { Marshal.Release(pointer); }
                }
            }
            return result;
        } finally {
            NativeComActivation.Release(shellWindows);
        }
    }

    private static string? ReadCurrentFolder(object window) {
        var owned = new List<object>();
        IntPtr pidl = IntPtr.Zero;
        IntPtr name = IntPtr.Zero;
        try {
            var provider = (IExplorerServiceProvider)window;
            Guid service = new("4C96BE40-915C-11CF-99D3-00AA004AE837"); // SID_STopLevelBrowser
            Guid browserId = new("000214E2-0000-0000-C000-000000000046");
            int result = provider.QueryService(in service, in browserId, out IntPtr browserPointer);
            var browser = (IExplorerShellBrowser)OwnResult(owned, result, browserPointer);
            result = browser.QueryActiveShellView(out IntPtr viewPointer);
            object view = OwnResult(owned, result, viewPointer);
            Guid folderId = new("1AC3D9F0-175C-11D1-95BE-00609797EA4F");
            result = ((IExplorerFolderView)view).GetFolder(in folderId, out IntPtr folderPointer);
            var folder = (IExplorerPersistFolder2)OwnResult(owned, result, folderPointer);
            Marshal.ThrowExceptionForHR(folder.GetCurFolder(out pidl));
            if (pidl == IntPtr.Zero) { return null; }
            // SIGDN_FILESYSPATH rejects virtual folders instead of treating display names as paths.
            if (SHGetNameFromIDList(pidl, 0x80058000, out name) < 0 || name == IntPtr.Zero) { return null; }
            return Marshal.PtrToStringUni(name);
        } finally {
            if (name != IntPtr.Zero) { Marshal.FreeCoTaskMem(name); }
            if (pidl != IntPtr.Zero) { Marshal.FreeCoTaskMem(pidl); }
            for (int i = owned.Count - 1; i >= 0; i--) { NativeComActivation.Release(owned[i]); }
        }
    }

    /// <summary>Consumes the native result reference and tracks its independent wrapper for deterministic release.</summary>
    private static object OwnResult(List<object> owned, int result, IntPtr pointer) {
        try {
            Marshal.ThrowExceptionForHR(result);
            if (pointer == IntPtr.Zero) { throw new COMException("The Explorer view is not ready.", unchecked((int)0x80004005)); }
            object value = NativeComActivation.FromPointer(pointer);
            owned.Add(value);
            return value;
        } finally {
            if (pointer != IntPtr.Zero) { Marshal.Release(pointer); }
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetNameFromIDList(IntPtr pidl, uint nameType, out IntPtr name);

    internal static void Open(string path) {
        if (!Path.IsPathRooted(path) || !Directory.Exists(path) || path.IndexOf('"') >= 0) {
            throw new DirectoryNotFoundException("The saved Explorer folder is no longer an existing absolute directory.");
        }
        using Process? process = Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
    }

}
