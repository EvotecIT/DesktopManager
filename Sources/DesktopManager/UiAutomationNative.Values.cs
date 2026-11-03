#if NET8_0_OR_GREATER
using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

// VARIANT is 16 bytes on x86 and 24 bytes on x64/ARM64 (the union contains BRECORD).
[StructLayout(LayoutKind.Sequential)]
internal struct UiaVariant {
    internal ushort Type;
    private ushort _reserved1, _reserved2, _reserved3;
    internal UiaVariantData Data;
}

[StructLayout(LayoutKind.Explicit)]
internal struct UiaVariantData {
    [FieldOffset(0)] internal int Integer;
    [FieldOffset(0)] internal double Double;
    [FieldOffset(0)] internal IntPtr Pointer;
    [FieldOffset(0)] private UiaRecord _record;
}

[StructLayout(LayoutKind.Sequential)]
internal struct UiaRecord {
    internal IntPtr Record, RecordInfo;
}

internal static class UiaValues {
    internal static object? Read(ref UiaVariant value) {
        try {
            return value.Type switch {
                3 => value.Data.Integer,
                5 => value.Data.Double,
                8 => value.Data.Pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(value.Data.Pointer),
                11 => (short)value.Data.Integer != 0,
                0x2005 => ReadDoubles(value.Data.Pointer),
                _ => null // Includes UIA's reserved NotSupported object: never invent a privacy-sensitive default.
            };
        } finally {
            VariantClear(ref value);
        }
    }

    internal static int[] ReadRuntimeId(IntPtr array) {
        if (array == IntPtr.Zero) { return Array.Empty<int>(); }
        try {
            GetBounds(array, out int lower, out int count);
            var values = new int[count];
            for (int i = 0; i < count; i++) {
                int index = lower + i;
                Marshal.ThrowExceptionForHR(SafeArrayGetElement(array, ref index, out values[i]));
            }
            return values;
        } finally { SafeArrayDestroy(array); }
    }

    private static double[] ReadDoubles(IntPtr array) {
        if (array == IntPtr.Zero) { return Array.Empty<double>(); }
        GetBounds(array, out int lower, out int count);
        var values = new double[count];
        for (int i = 0; i < count; i++) {
            int index = lower + i;
            Marshal.ThrowExceptionForHR(SafeArrayGetDouble(array, ref index, out values[i]));
        }
        return values;
    }

    private static void GetBounds(IntPtr array, out int lower, out int count) {
        if (SafeArrayGetDim(array) != 1) { throw new InvalidOperationException("UIA returned a multidimensional array."); }
        Marshal.ThrowExceptionForHR(SafeArrayGetLBound(array, 1, out lower));
        Marshal.ThrowExceptionForHR(SafeArrayGetUBound(array, 1, out int upper));
        count = checked(upper - lower + 1);
    }

    [DllImport("oleaut32.dll")] internal static extern int VariantClear(ref UiaVariant value);
    [DllImport("oleaut32.dll")] private static extern uint SafeArrayGetDim(IntPtr array);
    [DllImport("oleaut32.dll")] private static extern int SafeArrayGetLBound(IntPtr array, uint dimension, out int lower);
    [DllImport("oleaut32.dll")] private static extern int SafeArrayGetUBound(IntPtr array, uint dimension, out int upper);
    [DllImport("oleaut32.dll")] private static extern int SafeArrayGetElement(IntPtr array, ref int index, out int value);
    [DllImport("oleaut32.dll", EntryPoint = "SafeArrayGetElement")] private static extern int SafeArrayGetDouble(IntPtr array, ref int index, out double value);
    [DllImport("oleaut32.dll")] private static extern int SafeArrayDestroy(IntPtr array);
}
#endif
