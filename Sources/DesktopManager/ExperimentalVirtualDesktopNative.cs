using System.Runtime.InteropServices;

namespace DesktopManager;

// Narrow shell ABI declarations. Interface identities and ordering are documented by
// MScholtes/VirtualDesktop, VirtualDesktop11-24H2.cs (MIT). No external executable is required.
// HSTRINGs are handled explicitly so the same binding works on .NET Framework and modern .NET.
internal static class ExperimentalVirtualDesktopNative {
    internal static readonly Guid ShellClass = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    internal static readonly Guid ManagerService = new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
    internal static readonly Guid PinsService = new("B5A399E7-1C87-46B8-88E9-FC5747B171BD");

    [DllImport("combase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern int WindowsCreateString(string source, int length, out IntPtr value);
    [DllImport("combase.dll", ExactSpelling = true)]
    internal static extern IntPtr WindowsGetStringRawBuffer(IntPtr value, out uint length);
    [DllImport("combase.dll", ExactSpelling = true)]
    internal static extern int WindowsDeleteString(IntPtr value);

    internal static string ReadString(IntPtr value) {
        try {
            IntPtr buffer = WindowsGetStringRawBuffer(value, out uint length);
            return Marshal.PtrToStringUni(buffer, checked((int)length)) ?? string.Empty;
        } finally {
            WindowsDeleteString(value);
        }
    }

    internal static void Release(object? value) {
        if (value != null && Marshal.IsComObject(value)) { Marshal.ReleaseComObject(value); }
    }
}

[ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspaceShellServices {
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object QueryService(ref Guid service, ref Guid iid);
}

[ComImport, Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspaceObjectArray {
    void GetCount(out int count);
    void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object value);
}

[ComImport, Guid("3F07F4BE-B107-441A-AF0F-39D82529072C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspaceDesktop {
    [return: MarshalAs(UnmanagedType.Bool)] bool IsViewVisible(IntPtr view);
    Guid GetId();
    void GetName(out IntPtr name);
    void GetWallpaperPath(out IntPtr path);
}

[ComImport, Guid("53F5CA0B-158F-4124-900C-057158060B27"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspaceDesktopManager {
    int GetCount();
    void MoveViewToDesktop(IntPtr view, IWorkspaceDesktop desktop);
    [return: MarshalAs(UnmanagedType.Bool)] bool CanViewMoveDesktops(IntPtr view);
    IWorkspaceDesktop GetCurrentDesktop();
    void GetDesktops(out IWorkspaceObjectArray desktops);
    [PreserveSig] int GetAdjacentDesktop(IWorkspaceDesktop from, int direction, out IWorkspaceDesktop desktop);
    void SwitchDesktop(IWorkspaceDesktop desktop);
    void SwitchDesktopAndMoveForegroundView(IWorkspaceDesktop desktop);
    IWorkspaceDesktop CreateDesktop();
    void MoveDesktop(IWorkspaceDesktop desktop, int index);
    void RemoveDesktop(IWorkspaceDesktop desktop, IWorkspaceDesktop fallback);
    IWorkspaceDesktop FindDesktop(ref Guid id);
    void GetDesktopSwitchIncludeExcludeViews(IWorkspaceDesktop desktop, out IWorkspaceObjectArray include, out IWorkspaceObjectArray exclude);
    void SetDesktopName(IWorkspaceDesktop desktop, IntPtr name);
    void SetDesktopWallpaper(IWorkspaceDesktop desktop, IntPtr path);
}

[ComImport, Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspaceApplicationViews {
    void GetViews(out IWorkspaceObjectArray views);
    void GetViewsByZOrder(out IWorkspaceObjectArray views);
    void GetViewsByAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] string id, out IWorkspaceObjectArray views);
    void GetViewForHwnd(IntPtr handle, out IntPtr view);
}

[ComImport, Guid("4CE81583-1E4C-4632-A621-07A53543148F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspacePinnedApps {
    [return: MarshalAs(UnmanagedType.Bool)] bool IsAppIdPinned([MarshalAs(UnmanagedType.LPWStr)] string id);
    void PinAppId([MarshalAs(UnmanagedType.LPWStr)] string id);
    void UnpinAppId([MarshalAs(UnmanagedType.LPWStr)] string id);
    [return: MarshalAs(UnmanagedType.Bool)] bool IsViewPinned(IntPtr view);
    void PinView(IntPtr view);
    void UnpinView(IntPtr view);
}

// Explicit IInspectable prefix avoids the removed built-in WinRT marshaller on modern .NET.
[ComImport, Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWorkspaceApplicationIdentity {
    void GetIids(out uint count, out IntPtr ids);
    void GetRuntimeClassName(out IntPtr name);
    void GetTrustLevel(out int level);
    void SetFocus();
    void SwitchTo();
    void TryInvokeBack(IntPtr callback);
    void GetThumbnailWindow(out IntPtr handle);
    void GetMonitor(out IntPtr monitor);
    void GetVisibility(out int visibility);
    void SetCloak(int cloakType, int unknown);
    void GetPosition(ref Guid id, out IntPtr position);
    void SetPosition(ref IntPtr position);
    void InsertAfterWindow(IntPtr handle);
    void GetExtendedFramePosition(out RECT rectangle);
    void GetAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] out string id);
}
