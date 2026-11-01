using System.Runtime.InteropServices;

namespace DesktopManager.AotSmoke;

/// <summary>Reads a real, unregistered folder view object without creating its view HWND.</summary>
internal static class ExplorerNativeFixture {
    internal static void Run() {
        string directory = Environment.GetEnvironmentVariable(NativeFixtures.DirectoryVariable)
            ?? throw new InvalidOperationException("Run through the smoke runner for owned fixture cleanup.");
        int initialized = MonitorNativeMethods.CoInitializeEx(IntPtr.Zero, 2); // COINIT_APARTMENTTHREADED
        Marshal.ThrowExceptionForHR(initialized);
        try { ReadFolderView(directory); }
        finally { MonitorNativeMethods.CoUninitialize(); }
    }

    private static void ReadFolderView(string directory) {
        IntPtr itemPointer = IntPtr.Zero, viewPointer = IntPtr.Zero, folderPointer = IntPtr.Zero;
        IntPtr folderViewPointer = IntPtr.Zero;
        IntPtr pidl = IntPtr.Zero, name = IntPtr.Zero;
        MonitorNativeMethods.IShellItem? item = null;
        IExplorerFolderView? view = null;
        IExplorerPersistFolder2? folder = null;
        object? viewObject = null, folderObject = null;
        Directory.CreateDirectory(directory);
        try {
            Guid itemId = typeof(MonitorNativeMethods.IShellItem).GUID;
            Marshal.ThrowExceptionForHR(MonitorNativeMethods.SHCreateItemFromParsingName(directory, IntPtr.Zero, ref itemId, out itemPointer));
            item = NativeComActivation.FromPointer<MonitorNativeMethods.IShellItem>(itemPointer);
            Guid handler = new("3981E226-F559-11D3-8E3A-00C04F6837D5"); // BHID_SFViewObject
            Guid shellViewId = new("000214E3-0000-0000-C000-000000000046"); // IShellView
            Marshal.ThrowExceptionForHR(item.BindToHandler(IntPtr.Zero, ref handler, ref shellViewId, out viewPointer));
            Guid folderViewId = typeof(IExplorerFolderView).GUID;
            int queryResult = Marshal.QueryInterface(viewPointer, in folderViewId, out folderViewPointer);
            Console.WriteLine($"BHID_SFViewObject QueryInterface(IFolderView) HRESULT=0x{queryResult:X8}.");
            if (queryResult == unchecked((int)0x80004002)) {
                throw new UnavailableException("Unregistered Shell view returns native E_NOINTERFACE for IFolderView before HWND initialization; no CreateViewWindow or ShellWindows registration was attempted.");
            }
            Marshal.ThrowExceptionForHR(queryResult);
            // Keep concrete casts reachable, matching the production Explorer capture path under AOT.
            viewObject = NativeComActivation.FromPointer(folderViewPointer);
            view = (IExplorerFolderView)viewObject;
            Console.WriteLine("Concrete IFolderView wrapper acquired.");
            // Creating the COM view object is separate from IShellView.CreateViewWindow, which is never called.
            Guid folderId = typeof(IExplorerPersistFolder2).GUID;
            int folderResult = view.GetFolder(in folderId, out folderPointer);
            if (folderResult == unchecked((int)0x80004005) || folderResult == unchecked((int)0x8000FFFF)) {
                throw new UnavailableException($"Unregistered view exposes IFolderView without HWND, but GetFolder requires further initialization on this Shell (HRESULT=0x{folderResult:X8}); CreateViewWindow was not called.");
            }
            Marshal.ThrowExceptionForHR(folderResult);
            folderObject = NativeComActivation.FromPointer(folderPointer);
            folder = (IExplorerPersistFolder2)folderObject;
            Marshal.ThrowExceptionForHR(folder.GetCurFolder(out pidl));
            Require(pidl != IntPtr.Zero, "Owned folder view must return an absolute folder PIDL.");
            Marshal.ThrowExceptionForHR(SHGetNameFromIDList(pidl, 0x80058000, out name)); // SIGDN_FILESYSPATH
            Require(name != IntPtr.Zero && string.Equals(Marshal.PtrToStringUni(name), directory, StringComparison.OrdinalIgnoreCase),
                "Folder view GetFolder/GetCurFolder must resolve the independently created directory.");
            Console.WriteLine("Synthetic Explorer folder view: IFolderView.GetFolder, IPersistFolder2.GetCurFolder and PIDL filesystem path verified; no HWND or ShellWindows registration.");
            Console.WriteLine("Coverage limit: this fixture does not qualify live IServiceProvider.QueryService/IShellBrowser.QueryActiveShellView; those require live Explorer capture.");
        } finally {
            if (name != IntPtr.Zero) { Marshal.FreeCoTaskMem(name); }
            if (pidl != IntPtr.Zero) { Marshal.FreeCoTaskMem(pidl); }
            NativeComActivation.Release(folderObject);
            NativeComActivation.Release(viewObject);
            NativeComActivation.Release(item);
            if (folderPointer != IntPtr.Zero) { Marshal.Release(folderPointer); }
            if (folderViewPointer != IntPtr.Zero) { Marshal.Release(folderViewPointer); }
            if (viewPointer != IntPtr.Zero) { Marshal.Release(viewPointer); }
            if (itemPointer != IntPtr.Zero) { Marshal.Release(itemPointer); }
            NativeFixtures.CleanupDirectory(directory);
        }
    }

    private static void Require(bool condition, string message) {
        if (!condition) { throw new InvalidOperationException(message); }
    }

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHGetNameFromIDList(IntPtr pidl, uint nameType, out IntPtr name);

}
