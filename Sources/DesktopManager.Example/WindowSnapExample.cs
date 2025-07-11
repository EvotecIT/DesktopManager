using System;
using System.Runtime.InteropServices;

namespace DesktopManager.Example;

/// <summary>
/// Demonstrates automatic snapping when moving windows near monitor edges.
/// </summary>
internal static class WindowSnapExample {
    /// <summary>Runs the edge snapping example.</summary>
    public static void Run() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("Window snapping requires Windows.");
            return;
        }

        var manager = new WindowManager(new WindowSnapOptions { SnapThreshold = 30 });
        manager.StartAutoSnap();
        Console.WriteLine("Move a window near the edge to snap. Press Enter to stop...");
        Console.ReadLine();
        manager.StopAutoSnap();
    }
}
