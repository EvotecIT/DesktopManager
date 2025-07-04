using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DesktopManager.Example;

/// <summary>
/// Example class MonitorWatcherExample.
/// </summary>
internal static class MonitorWatcherExample {
    /// <summary>
    /// Runs the monitor watcher example for the specified duration.
    /// </summary>
    /// <param name="duration">Amount of time to listen for events.</param>
    public static async Task RunAsync(TimeSpan duration) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("MonitorWatcher works only on Windows.");
            return;
        }

        using var watcher = new MonitorWatcher();
        watcher.DisplaySettingsChanged += (_, _) =>
            Helpers.AddLine("MonitorWatcher", "Display settings changed");
        Console.WriteLine($"Monitoring display changes for {duration.TotalSeconds} seconds...");
        await Task.Delay(duration);
    }
}

