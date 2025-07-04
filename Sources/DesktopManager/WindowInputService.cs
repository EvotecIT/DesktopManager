using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides methods for pasting or typing text into windows.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowInputService {
    /// <summary>
    /// Pastes the specified text into the window using the clipboard.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to paste.</param>
    public static void PasteText(WindowInfo window, string text) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        ClipboardHelper.SetText(text);
        MonitorNativeMethods.SetForegroundWindow(window.Handle);
        MonitorNativeMethods.SendMessage(window.Handle, MonitorNativeMethods.WM_PASTE, 0, 0);
    }

    /// <summary>
    /// Types the specified text into the window using simulated keyboard input.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="delay">Optional delay in milliseconds between characters.</param>
    public static void TypeText(WindowInfo window, string text, int delay = 0) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        MonitorNativeMethods.SetForegroundWindow(window.Handle);

        foreach (char c in text) {
            MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];

            inputs[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = 0,
                Scan = c,
                Flags = MonitorNativeMethods.KEYEVENTF_UNICODE,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            inputs[1].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[1].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = 0,
                Scan = c,
                Flags = MonitorNativeMethods.KEYEVENTF_UNICODE | MonitorNativeMethods.KEYEVENTF_KEYUP,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());

            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }
    }
}

