using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager.AotSmoke;

/// <summary>Qualifies native conversions using resources owned exclusively by this worker.</summary>
internal static class NativeFixtures {
    internal const string DirectoryVariable = "DESKTOPMANAGER_AOTSMOKE_FIXTURE_DIRECTORY";
    private const string FileName = "shell-item-ą.txt";

    internal static void ReadShellItemArray() => WallpaperComApartment.Invoke(() => {
        ReadShellItemArrayCore();
        return true;
    });

    private static void ReadShellItemArrayCore() {
        string directory = Environment.GetEnvironmentVariable(DirectoryVariable)
            ?? throw new InvalidOperationException("Run the smoke runner so it owns fixture cleanup after a worker timeout.");
        string file = Path.Combine(directory, FileName);
        IntPtr pidl = IntPtr.Zero, arrayPointer = IntPtr.Zero, agilePointer = IntPtr.Zero;
        MonitorNativeMethods.IShellItemArray? array = null;
        IAgileWallpaperReference? reference = null;
        Directory.CreateDirectory(directory);
        try {
            File.WriteAllText(file, "Owned Native AOT Shell fixture.");
            Marshal.ThrowExceptionForHR(MonitorNativeMethods.SHParseDisplayName(file, IntPtr.Zero, out pidl, 0, out _));
            Require(pidl != IntPtr.Zero, "Shell parsing returned no fixture PIDL.");
            Marshal.ThrowExceptionForHR(MonitorNativeMethods.SHCreateShellItemArrayFromIDLists(1, [pidl], out arrayPointer));
            Require(arrayPointer != IntPtr.Zero, "Shell array creation returned no fixture array.");
            array = NativeComActivation.FromPointer<MonitorNativeMethods.IShellItemArray>(arrayPointer);
            VerifyShellItem(array, file);
            Guid arrayId = typeof(MonitorNativeMethods.IShellItemArray).GUID;
            Marshal.ThrowExceptionForHR(RoGetAgileReference(0, ref arrayId, arrayPointer, out agilePointer));
            Require(agilePointer != IntPtr.Zero, "Shell retention returned no agile reference.");
            reference = NativeComActivation.FromPointer<IAgileWallpaperReference>(agilePointer);
            Marshal.Release(agilePointer);
            agilePointer = IntPtr.Zero;

            // Release capture references before resolving; the agile reference must retain the array.
            NativeComActivation.Release(array);
            array = null;
            Marshal.Release(arrayPointer);
            arrayPointer = IntPtr.Zero;
            IAgileWallpaperReference retained = reference;
            Task.Run(() => VerifyRetainedShellItem(retained, file)).GetAwaiter().GetResult();
            Console.WriteLine("Synthetic Shell array: Unicode path verified before and after agile retention/Resolve on another initialized MTA thread; no wallpaper changes or restoration.");
        } finally {
            NativeComActivation.Release(reference);
            NativeComActivation.Release(array);
            if (agilePointer != IntPtr.Zero) { Marshal.Release(agilePointer); }
            if (arrayPointer != IntPtr.Zero) { Marshal.Release(arrayPointer); }
            if (pidl != IntPtr.Zero) { Marshal.FreeCoTaskMem(pidl); }
            // Delete only the known owned file, then its empty directory; never recursively clean temp.
            CleanupDirectory(directory);
        }
    }

    private static void VerifyRetainedShellItem(IAgileWallpaperReference reference, string expectedPath) {
        int initialized = MonitorNativeMethods.CoInitializeEx(IntPtr.Zero, 0); // COINIT_MULTITHREADED
        Marshal.ThrowExceptionForHR(initialized);
        try {
            Guid arrayId = typeof(MonitorNativeMethods.IShellItemArray).GUID;
            IntPtr pointer = IntPtr.Zero;
            MonitorNativeMethods.IShellItemArray? array = null;
            try {
                reference.Resolve(ref arrayId, out pointer);
                Require(pointer != IntPtr.Zero, "Agile Resolve returned no Shell item array.");
                array = NativeComActivation.FromPointer<MonitorNativeMethods.IShellItemArray>(pointer);
                VerifyShellItem(array, expectedPath);
            } finally {
                NativeComActivation.Release(array);
                if (pointer != IntPtr.Zero) { Marshal.Release(pointer); }
            }
        } finally { MonitorNativeMethods.CoUninitialize(); }
    }

    private static void VerifyShellItem(MonitorNativeMethods.IShellItemArray array, string expectedPath) {
        IntPtr name = IntPtr.Zero;
        MonitorNativeMethods.IShellItem? item = null;
        try {
            Marshal.ThrowExceptionForHR(array.GetCount(out uint count));
            Require(count == 1, "Owned Shell array must contain exactly one item.");
            Marshal.ThrowExceptionForHR(array.GetItemAt(0, out item));
            Require(item != null, "Owned Shell array returned no item.");
            Marshal.ThrowExceptionForHR(item!.GetDisplayName(MonitorNativeMethods.SIGDN.FileSystemPath, out name));
            Require(name != IntPtr.Zero, "Owned Shell item returned no filesystem path.");
            Require(string.Equals(Marshal.PtrToStringUni(name), expectedPath, StringComparison.OrdinalIgnoreCase),
                "Shell item filesystem path must match the independently created Unicode fixture path.");
        } finally {
            if (name != IntPtr.Zero) { Marshal.FreeCoTaskMem(name); }
            NativeComActivation.Release(item);
        }
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int RoGetAgileReference(uint options, ref Guid interfaceId, IntPtr instance, out IntPtr reference);

    internal static void CleanupDirectory(string directory) {
        if (!Directory.Exists(directory)) { return; }
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) {
            throw new IOException("Refusing to clean a fixture directory replaced by a reparse point.");
        }
        string file = Path.Combine(directory, FileName);
        if (File.Exists(file)) {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) {
                throw new IOException("Refusing to clean a fixture file replaced by a reparse point.");
            }
            File.Delete(file);
        }
        Directory.Delete(directory, recursive: false);
    }

    internal static void DuplicateClipboardMemory() {
        const uint unicodeText = 13;
        byte[] expected = Encoding.Unicode.GetBytes("Native AOT owned HGLOBAL — ą\0");
        IntPtr source = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (UIntPtr)expected.Length);
        if (source == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to allocate owned HGLOBAL fixture."); }
        IntPtr copy = IntPtr.Zero;
        try {
            WriteMemory(source, expected);
            ulong bytes = 0;
            copy = NativeClipboardSnapshot.Duplicate(unicodeText, source, ref bytes);
            Require(copy != IntPtr.Zero && copy != source, "Duplication must allocate an independent native handle.");
            Require(bytes >= (ulong)expected.Length, "Duplication must account for the copied allocation.");
            // Change only the owned source; the independent copy must retain its original contents.
            WriteMemory(source, new byte[expected.Length]);
            IntPtr pointer = MonitorNativeMethods.GlobalLock(copy);
            if (pointer == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to lock duplicated fixture."); }
            try {
                byte[] observed = new byte[expected.Length];
                Marshal.Copy(pointer, observed, 0, observed.Length);
                Require(observed.SequenceEqual(expected), "Duplicated HGLOBAL must retain the exact original bytes independently of its source.");
            } finally { MonitorNativeMethods.GlobalUnlock(copy); }
            Console.WriteLine("Synthetic clipboard handle: independent CF_UNICODETEXT HGLOBAL duplication and exact contents verified; no clipboard writes.");
        } finally {
            NativeClipboardSnapshot.Free(unicodeText, copy);
            NativeClipboardSnapshot.Free(unicodeText, source);
        }
    }

    private static void WriteMemory(IntPtr handle, byte[] bytes) {
        IntPtr pointer = MonitorNativeMethods.GlobalLock(handle);
        if (pointer == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to lock owned HGLOBAL fixture."); }
        try { Marshal.Copy(bytes, 0, pointer, bytes.Length); }
        finally { MonitorNativeMethods.GlobalUnlock(handle); }
    }

    private static void Require(bool condition, string message) {
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
