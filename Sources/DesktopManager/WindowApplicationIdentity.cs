using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

/// <summary>Reads public Windows application identity without opening experimental shell interfaces.</summary>
internal static class WindowApplicationIdentity {
    private static readonly Guid PropertyStoreId = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
    private static readonly PropertyKey ApplicationId = new() {
        FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), PropertyId = 5
    };

    /// <summary>Prefers an explicit window identity, then the packaged process identity; unavailable metadata is empty.</summary>
    internal static string Read(IntPtr window, uint processId) {
        string identity = ReadWindowIdentity(window);
        return identity.Length > 0 ? identity : ReadProcessIdentity(processId);
    }

    /// <summary>A recorded identity must remain available and equal; an absent recorded identity adds no constraint.</summary>
    internal static bool Matches(string? recorded, string? current) =>
        string.IsNullOrEmpty(recorded) || string.Equals(recorded, current, StringComparison.Ordinal);

    private static string ReadWindowIdentity(IntPtr window) {
        int initialized = MonitorNativeMethods.CoInitializeEx(IntPtr.Zero, 0);
        // An existing STA is usable too; only successful initialization calls need balancing.
        if (initialized < 0 && initialized != unchecked((int)0x80010106)) { return string.Empty; }
        try {
            if (MonitorNativeMethods.SHGetPropertyStoreForWindow(window, in PropertyStoreId, out IntPtr pointer) != 0 || pointer == IntPtr.Zero) {
                return string.Empty;
            }
            IPropertyStore store;
            try { store = NativeComActivation.FromPointer<IPropertyStore>(pointer); }
            finally { Marshal.Release(pointer); }
            try {
                PropertyKey key = ApplicationId;
                int result = store.GetValue(ref key, out PropVariant value);
                using (value) { return result == 0 ? value.GetString() ?? string.Empty : string.Empty; }
            } finally { NativeComActivation.Release(store); }
        } catch (Exception exception) when (exception is COMException || exception is UnauthorizedAccessException || exception is InvalidCastException) {
            return string.Empty;
        } finally {
            if (initialized >= 0) { MonitorNativeMethods.CoUninitialize(); }
        }
    }

    private static string ReadProcessIdentity(uint processId) {
        IntPtr process = MonitorNativeMethods.OpenProcess(MonitorNativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (process == IntPtr.Zero) { return string.Empty; }
        try {
            uint length = 0;
            if (GetApplicationUserModelId(process, ref length, null) != 122 || length == 0 || length > int.MaxValue) {
                return string.Empty;
            }
            var identity = new StringBuilder((int)length);
            return GetApplicationUserModelId(process, ref length, identity) == 0 ? identity.ToString() : string.Empty;
        } catch (EntryPointNotFoundException) {
            return string.Empty;
        } finally { MonitorNativeMethods.CloseHandle(process); }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetApplicationUserModelId(IntPtr process, ref uint length, StringBuilder? identity);
}
