using System;
using System.Runtime.InteropServices;

namespace DesktopManager.Example;

/// <summary>
/// Demonstrates mouse drag using <see cref="MouseInputService"/>.
/// </summary>
internal static class MouseInputExample {
    /// <summary>Runs the mouse input example.</summary>
    public static void Run() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("Mouse input examples require Windows.");
            return;
        }

        Console.WriteLine("Dragging mouse from (0,0) to (100,100)...");
        MouseInputService.MouseDrag(MouseButton.Left, 0, 0, 100, 100, 5);
    }
}
