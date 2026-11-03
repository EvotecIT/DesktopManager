using System;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

// Unused automation methods retain their native signatures to preserve the inherited vtable.
// VARIANT is 16 bytes on x86 and 24 on x64/ARM64 (the union includes a two-pointer record).
[StructLayout(LayoutKind.Sequential)]
internal struct ExplorerVariantIndex {
    internal ushort Type;
    private ushort _reserved1, _reserved2, _reserved3;
    internal int Value;
    private IntPtr _recordPointer;
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
internal partial interface IExplorerShellWindows {
    [PreserveSig] int GetTypeInfoCount(out uint count);
    [PreserveSig] int GetTypeInfo(uint index, uint locale, out IntPtr info);
    [PreserveSig] int GetIdsOfNames(in Guid id, IntPtr names, uint count, uint locale, IntPtr ids);
    [PreserveSig] int Invoke(int member, in Guid id, uint locale, ushort flags, IntPtr parameters, IntPtr result, IntPtr exception, IntPtr argumentError);
    [PreserveSig] int GetCount(out int count);
    [PreserveSig] int Item(ExplorerVariantIndex index, out IntPtr browser);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("0002DF05-0000-0000-C000-000000000046")]
internal partial interface IExplorerWebBrowserApp {
    [PreserveSig] int GetTypeInfoCount(out uint count);
    [PreserveSig] int GetTypeInfo(uint index, uint locale, out IntPtr info);
    [PreserveSig] int GetIdsOfNames(in Guid id, IntPtr names, uint count, uint locale, IntPtr ids);
    [PreserveSig] int Invoke(int member, in Guid id, uint locale, ushort flags, IntPtr parameters, IntPtr result, IntPtr exception, IntPtr argumentError);
    [PreserveSig] int GoBack();
    [PreserveSig] int GoForward();
    [PreserveSig] int GoHome();
    [PreserveSig] int GoSearch();
    [PreserveSig] int Navigate([MarshalAs(UnmanagedType.BStr)] string url, IntPtr flags, IntPtr frame, IntPtr postData, IntPtr headers);
    [PreserveSig] int Refresh();
    [PreserveSig] int Refresh2(IntPtr level);
    [PreserveSig] int Stop();
    [PreserveSig] int GetApplication(out IntPtr application);
    [PreserveSig] int GetParent(out IntPtr parent);
    [PreserveSig] int GetContainer(out IntPtr container);
    [PreserveSig] int GetDocument(out IntPtr document);
    [PreserveSig] int GetTopLevelContainer(out short topLevel);
    [PreserveSig] int GetBrowserType(out IntPtr type);
    [PreserveSig] int GetLeft(out int left);
    [PreserveSig] int SetLeft(int left);
    [PreserveSig] int GetTop(out int top);
    [PreserveSig] int SetTop(int top);
    [PreserveSig] int GetWidth(out int width);
    [PreserveSig] int SetWidth(int width);
    [PreserveSig] int GetHeight(out int height);
    [PreserveSig] int SetHeight(int height);
    [PreserveSig] int GetLocationName(out IntPtr name);
    [PreserveSig] int GetLocationUrl([MarshalAs(UnmanagedType.BStr)] out string location);
    [PreserveSig] int GetBusy(out short busy);
    [PreserveSig] int Quit();
    [PreserveSig] int ClientToWindow(ref int width, ref int height);
    [PreserveSig] int PutProperty([MarshalAs(UnmanagedType.BStr)] string property, ExplorerVariantIndex value);
    [PreserveSig] int GetProperty([MarshalAs(UnmanagedType.BStr)] string property, IntPtr value);
    [PreserveSig] int GetName(out IntPtr name);
    [PreserveSig] int GetHwnd(out IntPtr handle);
    [PreserveSig] int GetFullName([MarshalAs(UnmanagedType.BStr)] out string executable);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal partial interface IExplorerServiceProvider {
    [PreserveSig] int QueryService(in Guid service, in Guid interfaceId, out IntPtr value);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("000214E2-0000-0000-C000-000000000046")]
internal partial interface IExplorerShellBrowser {
    [PreserveSig] int GetWindow(out IntPtr window);
    [PreserveSig] int ContextSensitiveHelp(int enterMode);
    [PreserveSig] int InsertMenusSB(IntPtr menu, IntPtr widths);
    [PreserveSig] int SetMenuSB(IntPtr menu, IntPtr oleMenu, IntPtr activeObject);
    [PreserveSig] int RemoveMenusSB(IntPtr menu);
    [PreserveSig] int SetStatusTextSB(IntPtr text);
    [PreserveSig] int EnableModelessSB(int enable);
    [PreserveSig] int TranslateAcceleratorSB(IntPtr message, ushort id);
    [PreserveSig] int BrowseObject(IntPtr pidl, uint flags);
    [PreserveSig] int GetViewStateStream(uint mode, out IntPtr stream);
    [PreserveSig] int GetControlWindow(uint id, out IntPtr window);
    [PreserveSig] int SendControlMsg(uint id, uint message, UIntPtr wParam, IntPtr lParam, out IntPtr result);
    [PreserveSig] int QueryActiveShellView(out IntPtr view);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE")]
internal partial interface IExplorerFolderView {
    [PreserveSig] int GetCurrentViewMode(out uint mode);
    [PreserveSig] int SetCurrentViewMode(uint mode);
    [PreserveSig] int GetFolder(in Guid interfaceId, out IntPtr folder);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("1AC3D9F0-175C-11D1-95BE-00609797EA4F")]
internal partial interface IExplorerPersistFolder2 {
    [PreserveSig] int GetClassId(out Guid classId);
    [PreserveSig] int Initialize(IntPtr pidl);
    [PreserveSig] int GetCurFolder(out IntPtr pidl);
}
