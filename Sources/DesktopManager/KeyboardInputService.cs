using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides methods for simulating keyboard input.
/// </summary>
[SupportedOSPlatform("windows")]
public static class KeyboardInputService {
    /// <summary>
    /// Presses a single key using SendInput.
    /// </summary>
    /// <param name="key">Key to press.</param>
    public static void PressKey(VirtualKey key) {
        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];
        inputs[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        inputs[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = 0,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        inputs[1].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        inputs[1].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = MonitorNativeMethods.KEYEVENTF_KEYUP,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Presses a shortcut combination of keys.
    /// </summary>
    /// <param name="keys">Keys to press in order.</param>
    public static void PressShortcut(params VirtualKey[] keys) {
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[keys.Length * 2];
        int index = 0;
        foreach (VirtualKey key in keys) {
            inputs[index].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[index].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = (ushort)key,
                Scan = 0,
                Flags = 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };
            index++;
        }

        for (int i = keys.Length - 1; i >= 0; i--) {
            inputs[index].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[index].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = (ushort)keys[i],
                Scan = 0,
                Flags = MonitorNativeMethods.KEYEVENTF_KEYUP,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };
            index++;
        }

        MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }
}
