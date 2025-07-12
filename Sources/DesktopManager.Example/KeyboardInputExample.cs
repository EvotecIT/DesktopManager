using System;
using System.Runtime.InteropServices;

namespace DesktopManager.Example;

/// <summary>
/// Demonstrates basic keyboard input using <see cref="KeyboardInputService"/>.
/// </summary>
internal static class KeyboardInputExample {
    /// <summary>Runs the keyboard input example.</summary>
    public static void Run() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("Keyboard input examples require Windows.");
            return;
        }

        Console.WriteLine("Pressing WIN+R to open Run dialog...");
        KeyboardInputService.PressShortcut(0, VirtualKey.VK_LWIN, VirtualKey.VK_R);

        Console.WriteLine("Pressing and releasing F24 using KeyDown/KeyUp...");
        KeyboardInputService.KeyDown(VirtualKey.VK_F24);
        KeyboardInputService.KeyUp(VirtualKey.VK_F24);
    }
}
