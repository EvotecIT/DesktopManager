using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides methods for simulating keyboard input.
/// </summary>
[SupportedOSPlatform("windows")]
public static class KeyboardInputService {
    /// <summary>
    /// Presses a single key by sending a down and up event.
    /// </summary>
    /// <param name="key">Key to press.</param>
    public static void PressKey(VirtualKey key) {
        KeyDown(key);
        KeyUp(key);
    }

    /// <summary>
    /// Sends a key down event for the specified key.
    /// </summary>
    /// <param name="key">Key to press down.</param>
    public static void KeyDown(VirtualKey key) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = 0,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput(1, [input], Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Sends a key up event for the specified key.
    /// </summary>
    /// <param name="key">Key to release.</param>
    public static void KeyUp(VirtualKey key) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = MonitorNativeMethods.KEYEVENTF_KEYUP,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput(1, [input], Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Presses a shortcut combination of keys.
    /// </summary>
    /// <param name="delay">Delay in milliseconds between each key event.</param>
    /// <param name="keys">Keys to press in order.</param>
    public static void PressShortcut(int delay, params VirtualKey[] keys) {
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        foreach (VirtualKey key in keys) {
            KeyDown(key);
            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }

        for (int i = keys.Length - 1; i >= 0; i--) {
            KeyUp(keys[i]);
            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Sends key presses directly to a background control.
    /// </summary>
    /// <param name="control">Target control.</param>
    /// <param name="keys">Keys to send.</param>
    public static void SendToControl(WindowControlInfo control, params VirtualKey[] keys) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        foreach (VirtualKey key in keys) {
            MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_KEYDOWN, (uint)key, 0);
            if ((key >= VirtualKey.VK_SPACE && key <= VirtualKey.VK_Z) || (key >= VirtualKey.VK_0 && key <= VirtualKey.VK_9)) {
                MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_CHAR, (uint)key, 0);
            }
            MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_KEYUP, (uint)key, 0);
        }
    }
}
