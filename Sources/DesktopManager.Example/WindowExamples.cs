using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Example {
    /// <summary>
    /// Demonstrates WindowManager features such as setting a window top-most and activating it.
    /// </summary>
    internal static class WindowExamples {
        public static void Run() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Console.WriteLine("Window management examples require Windows.");
                return;
            }

            var manager = new WindowManager();
            var window = manager.GetWindows().FirstOrDefault();
            if (window == null) {
                Console.WriteLine("No windows found to manipulate.");
                return;
            }

            // Make the window top-most
            manager.SetWindowTopMost(window, true);
            Console.WriteLine($"Set '{window.Title}' as top-most.");

            // Bring the window to the foreground
            manager.ActivateWindow(window);
            Console.WriteLine($"Activated '{window.Title}'.");

            // Reset top-most state
            manager.SetWindowTopMost(window, false);
        }
    }
}
