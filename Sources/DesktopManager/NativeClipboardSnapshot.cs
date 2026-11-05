using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Owns copied Win32 clipboard handles without retaining an OLE data object.</summary>
internal static class NativeClipboardSnapshot {
    private const ulong MaximumBytes = 64 * 1024 * 1024;

    internal static IntPtr Duplicate(uint format, IntPtr source, ref ulong bytes) {
        if (source == IntPtr.Zero) { throw new InvalidOperationException("A clipboard format could not be rendered for preservation."); }
        IntPtr copy;
        if (format == 2 || format == 0x82) {
            copy = CopyImage(source, 0, 0, 0, 0x2000); // IMAGE_BITMAP, LR_CREATEDIBSECTION; never LR_COPYRETURNORG.
        } else if (format == 14 || format == 0x8e) {
            copy = CopyEnhMetaFile(source, null);
        } else if (format == 9) {
            ushort count = 0;
            if (GetObject(source, sizeof(ushort), ref count) == 0 || count == 0) { throw new InvalidOperationException("Unable to read clipboard palette."); }
            byte[] palette = new byte[4 + count * 4];
            palette[0] = 0; palette[1] = 3;
            palette[2] = (byte)count; palette[3] = (byte)(count >> 8);
            var pin = GCHandle.Alloc(palette, GCHandleType.Pinned);
            try {
                if (GetPaletteEntries(source, 0, count, IntPtr.Add(pin.AddrOfPinnedObject(), 4)) != count) { throw new InvalidOperationException("Unable to copy clipboard palette."); }
                copy = CreatePalette(pin.AddrOfPinnedObject());
            } finally { pin.Free(); }
        } else {
            // Private/GDI-object formats have application-defined ownership and cannot be copied as HGLOBAL.
            if ((format >= 0x200 && format <= 0x3ff) || format == 0x80) {
                throw new InvalidOperationException("The clipboard contains an application-owned format that cannot be preserved safely.");
            }
            ulong size = GlobalSize(source).ToUInt64();
            if (((format == 3 || format == 0x83) && size < (ulong)Marshal.SizeOf<MetafilePicture>()) || size == 0 || size > MaximumBytes - bytes || size > int.MaxValue) { throw new InvalidOperationException("Clipboard data exceeds the preservation limit or has an unsupported handle."); }
            bytes += size;
            copy = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (UIntPtr)size);
            if (copy == IntPtr.Zero) { throw new InvalidOperationException("Unable to allocate clipboard copy."); }
            IntPtr input = MonitorNativeMethods.GlobalLock(source);
            IntPtr output = MonitorNativeMethods.GlobalLock(copy);
            try {
                if (input == IntPtr.Zero || output == IntPtr.Zero) { throw new InvalidOperationException("Unable to lock clipboard data."); }
                byte[] data = new byte[(int)size];
                Marshal.Copy(input, data, 0, data.Length);
                Marshal.Copy(data, 0, output, data.Length);
                if (format == 3 || format == 0x83) {
                    var picture = Marshal.PtrToStructure<MetafilePicture>(input);
                    picture.Metafile = CopyMetaFile(picture.Metafile, null);
                    if (picture.Metafile == IntPtr.Zero) { throw new InvalidOperationException("Unable to copy clipboard metafile."); }
                    Marshal.StructureToPtr(picture, output, false);
                }
            } catch {
                if (output != IntPtr.Zero) { MonitorNativeMethods.GlobalUnlock(copy); output = IntPtr.Zero; }
                MonitorNativeMethods.GlobalFree(copy);
                throw;
            } finally {
                if (input != IntPtr.Zero) { MonitorNativeMethods.GlobalUnlock(source); }
                if (output != IntPtr.Zero) { MonitorNativeMethods.GlobalUnlock(copy); }
            }
        }
        if (copy == IntPtr.Zero) { throw new InvalidOperationException("Unable to duplicate clipboard data."); }
        return copy;
    }

    internal static void Free(uint format, IntPtr handle) {
        if (handle == IntPtr.Zero) { return; }
        if (format == 2 || format == 9 || format == 0x82) { DeleteObject(handle); }
        else if (format == 14 || format == 0x8e) { DeleteEnhMetaFile(handle); }
        else {
            if (format == 3 || format == 0x83) {
                IntPtr pointer = MonitorNativeMethods.GlobalLock(handle);
                if (pointer != IntPtr.Zero) {
                    try { DeleteMetaFile(Marshal.PtrToStructure<MetafilePicture>(pointer).Metafile); }
                    finally { MonitorNativeMethods.GlobalUnlock(handle); }
                }
            }
            MonitorNativeMethods.GlobalFree(handle);
        }
    }

    internal static IntPtr CreateOwner() {
        IntPtr owner = CreateWindowEx(0, "STATIC", null, 0, 0, 0, 0, 0, new IntPtr(-3), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (owner == IntPtr.Zero) { throw new InvalidOperationException("Unable to create a clipboard owner window."); }
        return owner;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string? name, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool DestroyWindow(IntPtr window);

    [StructLayout(LayoutKind.Sequential)]
    private struct MetafilePicture { internal int MappingMode; internal int Width; internal int Height; internal IntPtr Metafile; }
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint EnumClipboardFormats(uint format);
    [DllImport("kernel32.dll")] internal static extern void SetLastError(uint error);
    [DllImport("kernel32.dll")] private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("user32.dll")] private static extern IntPtr CopyImage(IntPtr image, uint type, int width, int height, uint flags);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CopyEnhMetaFile(IntPtr source, string? file);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CopyMetaFile(IntPtr source, string? file);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteEnhMetaFile(IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteMetaFile(IntPtr value);
    [DllImport("gdi32.dll", EntryPoint = "GetObjectW")] private static extern int GetObject(IntPtr value, int size, ref ushort count);
    [DllImport("gdi32.dll")] private static extern uint GetPaletteEntries(IntPtr palette, uint start, uint count, IntPtr entries);
    [DllImport("gdi32.dll")] private static extern IntPtr CreatePalette(IntPtr palette);
}
