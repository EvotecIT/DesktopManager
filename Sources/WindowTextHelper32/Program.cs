using System;
using System.Runtime.InteropServices;

namespace WindowTextHelper32;

internal static class NativeMethods {
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    public const uint WM_GETTEXT = 0x000D;
    public const uint SMTO_ABORTIFHUNG = 0x0002;
}

internal class Program {
    private static int Main(string[] args) {
        if (args.Length == 0 || !long.TryParse(args[0], out var handleValue)) {
            Console.WriteLine(string.Empty);
            return 1;
        }

        IntPtr handle = new IntPtr(handleValue);
        const int capacity = 1024;
        IntPtr buffer = Marshal.AllocHGlobal(capacity);
        try {
            IntPtr result;
            IntPtr res = NativeMethods.SendMessageTimeout(
                handle,
                NativeMethods.WM_GETTEXT,
                new IntPtr(capacity / 2),
                buffer,
                NativeMethods.SMTO_ABORTIFHUNG,
                1000,
                out result);

            if (res != IntPtr.Zero) {
                string text = Marshal.PtrToStringUni(buffer) ?? string.Empty;
                Console.WriteLine(text);
                return 0;
            }

            Console.WriteLine(string.Empty);
            return 1;
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
