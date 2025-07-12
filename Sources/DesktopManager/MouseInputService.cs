using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides methods for simulating mouse input.
/// </summary>
[SupportedOSPlatform("windows")]
public static class MouseInputService {
    /// <summary>
    /// Moves the mouse cursor to the specified screen coordinates.
    /// </summary>
    /// <param name="x">X coordinate in pixels.</param>
    /// <param name="y">Y coordinate in pixels.</param>
    public static void MoveCursor(int x, int y) {
        int width = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CXVIRTUALSCREEN);
        int height = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CYVIRTUALSCREEN);
        int left = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_XVIRTUALSCREEN);
        int top = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_YVIRTUALSCREEN);

        int normalizedX = (int)Math.Round(((double)(x - left) * 65535) / width);
        int normalizedY = (int)Math.Round(((double)(y - top) * 65535) / height);

        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[1];
        inputs[0].Type = MonitorNativeMethods.INPUT_MOUSE;
        inputs[0].Data.Mouse = new MonitorNativeMethods.MOUSEINPUT {
            Dx = normalizedX,
            Dy = normalizedY,
            MouseData = 0,
            DwFlags = MonitorNativeMethods.MOUSEEVENTF_MOVE | MonitorNativeMethods.MOUSEEVENTF_ABSOLUTE,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Performs a mouse click using the specified button.
    /// </summary>
    /// <param name="button">Button to click.</param>
    public static void Click(MouseButton button) {
        uint down = button == MouseButton.Left ? MonitorNativeMethods.MOUSEEVENTF_LEFTDOWN : MonitorNativeMethods.MOUSEEVENTF_RIGHTDOWN;
        uint up = button == MouseButton.Left ? MonitorNativeMethods.MOUSEEVENTF_LEFTUP : MonitorNativeMethods.MOUSEEVENTF_RIGHTUP;

        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];
        inputs[0].Type = MonitorNativeMethods.INPUT_MOUSE;
        inputs[0].Data.Mouse = new MonitorNativeMethods.MOUSEINPUT {
            Dx = 0,
            Dy = 0,
            MouseData = 0,
            DwFlags = down,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        inputs[1].Type = MonitorNativeMethods.INPUT_MOUSE;
        inputs[1].Data.Mouse = new MonitorNativeMethods.MOUSEINPUT {
            Dx = 0,
            Dy = 0,
            MouseData = 0,
            DwFlags = up,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Scrolls the mouse wheel vertically.
    /// </summary>
    /// <param name="delta">Scroll amount. Positive values scroll up.</param>
    public static void Scroll(int delta) {
        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[1];
        inputs[0].Type = MonitorNativeMethods.INPUT_MOUSE;
        inputs[0].Data.Mouse = new MonitorNativeMethods.MOUSEINPUT {
            Dx = 0,
            Dy = 0,
            MouseData = (uint)delta,
            DwFlags = MonitorNativeMethods.MOUSEEVENTF_WHEEL,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Drags the mouse from a start position to an end position.
    /// </summary>
    /// <param name="button">Button to hold during the drag.</param>
    /// <param name="startX">Starting X coordinate.</param>
    /// <param name="startY">Starting Y coordinate.</param>
    /// <param name="endX">Ending X coordinate.</param>
    /// <param name="endY">Ending Y coordinate.</param>
    /// <param name="stepDelay">Delay in milliseconds between steps.</param>
    public static void MouseDrag(MouseButton button, int startX, int startY, int endX, int endY, int stepDelay) {
        uint down = button == MouseButton.Left ? MonitorNativeMethods.MOUSEEVENTF_LEFTDOWN : MonitorNativeMethods.MOUSEEVENTF_RIGHTDOWN;
        uint up = button == MouseButton.Left ? MonitorNativeMethods.MOUSEEVENTF_LEFTUP : MonitorNativeMethods.MOUSEEVENTF_RIGHTUP;

        MoveCursor(startX, startY);

        MonitorNativeMethods.INPUT[] input = new MonitorNativeMethods.INPUT[1];
        input[0].Type = MonitorNativeMethods.INPUT_MOUSE;
        input[0].Data.Mouse = new MonitorNativeMethods.MOUSEINPUT {
            Dx = 0,
            Dy = 0,
            MouseData = 0,
            DwFlags = down,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput(1, input, Marshal.SizeOf<MonitorNativeMethods.INPUT>());

        const int steps = 20;
        for (int i = 1; i <= steps; i++) {
            int x = startX + (endX - startX) * i / steps;
            int y = startY + (endY - startY) * i / steps;
            MoveCursor(x, y);
            if (stepDelay > 0) {
                System.Threading.Thread.Sleep(stepDelay);
            }
        }

        input[0].Data.Mouse = new MonitorNativeMethods.MOUSEINPUT {
            Dx = 0,
            Dy = 0,
            MouseData = 0,
            DwFlags = up,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput(1, input, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }
}
