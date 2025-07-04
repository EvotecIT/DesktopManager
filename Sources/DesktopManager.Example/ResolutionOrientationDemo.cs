using System.Linq;

namespace DesktopManager.Example;

/// <summary>
/// Example class ResolutionOrientationDemo.
/// </summary>
internal static class ResolutionOrientationDemo {
    /// <summary>
    /// Demonstrates changing resolution and orientation on the first monitor.
    /// </summary>
    public static void Run() {
        Monitors monitors = new Monitors();
        var first = monitors.GetMonitorsConnected().FirstOrDefault();
        if (first == null) {
            return;
        }

        var position = first.GetMonitorPosition();
        int width = position.Right - position.Left;
        int height = position.Bottom - position.Top;

        monitors.SetMonitorResolution(first.DeviceId, width, height);
        monitors.SetMonitorOrientation(first.DeviceId, DisplayOrientation.Default);
    }
}

