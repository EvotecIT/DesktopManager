using Monitors = DesktopManager.Monitors;

namespace DesktopManagerSample {
    class Program {
        static void Main(string[] args) {

            Monitors monitor = new Monitors();
            var test = monitor.GetMonitors();


            Console.WriteLine("Available monitor connections: " + monitor.GetAvailableMonitorPaths());
            Console.WriteLine("Connected monitors: " + monitor.GetConnectedMonitors().Count);

            foreach (var deviceId in monitor.GetConnectedMonitors()) {
                Console.WriteLine("==================================");
                Console.WriteLine("MonitorID: " + deviceId);
                Console.WriteLine("Wallpaper Path: " + monitor.GetWallpaper(deviceId));
                Console.WriteLine("Wallpaper Position (only first monitor): " + monitor.GetWallpaperPosition());
                var rect = monitor.GetMonitorRECT(deviceId);
                Console.WriteLine("RECT: {0} {1} {2} {3}", rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            monitor.SetWallpaper(1, @"C:\Users\przemyslaw.klys\Downloads\IMG_4644.jpeg");
        }
    }
}