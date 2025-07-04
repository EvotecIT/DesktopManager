using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager.Example;

/// <summary>
/// Demonstrates using <see cref="WindowKeepAlive"/> to keep windows awake.
/// </summary>
internal static class WindowKeepAliveExample {
    /// <summary>Runs the keep-alive examples.</summary>
    public static void Run() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("Window keep-alive examples require Windows.");
            return;
        }

        Simple();
        Multiple();
        QueryAndStop();
    }

    private static void Simple() {
        var manager = new WindowManager();
        var window = manager.GetWindows("*Notepad*").FirstOrDefault();
        if (window == null) {
            Console.WriteLine("Notepad not found.");
            return;
        }

        WindowKeepAlive.Instance.Start(window, TimeSpan.FromSeconds(30));
        Console.WriteLine("Keeping Notepad alive for 5 seconds...");
        Thread.Sleep(TimeSpan.FromSeconds(5));
        WindowKeepAlive.Instance.Stop(window.Handle);
    }

    private static void Multiple() {
        var manager = new WindowManager();
        var windows = manager.GetWindows("*Chrome*");
        foreach (var w in windows) {
            WindowKeepAlive.Instance.Start(w, TimeSpan.FromSeconds(15));
        }
    }

    private static void QueryAndStop() {
        foreach (var handle in WindowKeepAlive.Instance.ActiveHandles.ToList()) {
            Console.WriteLine($"Active keep-alive: {handle}");
        }

        WindowKeepAlive.Instance.StopAll();
    }
}
