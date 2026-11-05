using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native mouse-related platform invocations.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>
    /// Retrieves the current cursor position in screen coordinates.
    /// </summary>
    /// <param name="lpPoint">Receives the cursor coordinates.</param>
    /// <returns>True when the position is available.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>Reads the cursor position in physical desktop pixels, independent of caller DPI awareness.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPhysicalCursorPos(out POINT point);

    /// <summary>Moves the cursor in physical desktop pixels, independent of caller DPI awareness.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetPhysicalCursorPos(int x, int y);

    /// <summary>Waits for queued input without preventing the fence from observing a stop request.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint MsgWaitForMultipleObjectsEx(uint count, IntPtr handles, uint milliseconds, uint wakeMask, uint flags);

    /// <summary>
    /// Retrieves information about the global cursor.
    /// </summary>
    /// <param name="pci">Receives the cursor information.</param>
    /// <returns>True when cursor information is available.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorInfo(ref CURSORINFO pci);

    /// <summary>
    /// Retrieves the current state of a virtual key.
    /// </summary>
    /// <param name="vKey">The virtual key code.</param>
    /// <returns>A bitmask describing the key state.</returns>
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    /// <summary>
    /// Cursor is currently showing.
    /// </summary>
    public const int CURSOR_SHOWING = 0x00000001;

    /// <summary>
    /// Virtual key code for the left mouse button.
    /// </summary>
    public const int VK_LBUTTON = 0x01;

    /// <summary>
    /// Virtual key code for the right mouse button.
    /// </summary>
    public const int VK_RBUTTON = 0x02;

    /// <summary>
    /// Describes the global cursor state.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO {
        /// <summary>
        /// Structure size.
        /// </summary>
        public int cbSize;

        /// <summary>
        /// Cursor state flags.
        /// </summary>
        public int flags;

        /// <summary>
        /// Cursor handle.
        /// </summary>
        public IntPtr hCursor;

        /// <summary>
        /// Cursor position.
        /// </summary>
        public POINT ptScreenPos;
    }
}
