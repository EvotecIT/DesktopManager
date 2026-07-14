namespace DesktopManager.Example {
    /// <summary>
    /// The main class for the DesktopManager example application.
    /// </summary>
    class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static void Main(string[] args) {
            bool runMutations = args.Any(argument => string.Equals(argument, "--run-mutations", StringComparison.OrdinalIgnoreCase));
            Monitors monitor = new Monitors();

            // Get all monitors
            var getMonitors = monitor.GetMonitors();
            Helpers.AddLine("Number of monitors", getMonitors.Count);
            Helpers.ShowPropertiesTable("GetMonitors() ", getMonitors);

            // Get connected monitors
            var getMonitorsConnected = monitor.GetMonitorsConnected();
            Helpers.AddLine("Number of monitors (connected):", getMonitorsConnected.Count);
            Helpers.ShowPropertiesTable("GetMonitorsConnected() ", getMonitorsConnected);

            // Get all display devices
            var listDisplayDevices = monitor.DisplayDevicesAll();
            Console.WriteLine("Count DisplayDevicesAll: " + listDisplayDevices.Count);
            Helpers.ShowPropertiesTable("DisplayDevicesAll()", listDisplayDevices);

            Console.WriteLine("======");

            // Get connected display devices
            var getDisplayDevices = monitor.DisplayDevicesConnected();
            Console.WriteLine("Count DisplayDevicesConnected: " + getDisplayDevices.Count);
            Helpers.ShowPropertiesTable("DisplayDevicesConnected()", getDisplayDevices);

            Console.WriteLine("======");

            // Get wallpaper position for the first monitor
            Console.WriteLine("Wallpaper Position (only first monitor): " + monitor.GetWallpaperPosition());

            // Iterate through connected monitors
            foreach (var device in monitor.GetMonitorsConnected()) {
                Console.WriteLine("3==================================");
                Console.WriteLine("MonitorID: " + device.DeviceId);
                Console.WriteLine("Wallpaper Path: " + device.GetWallpaper());
                var rect1 = device.GetMonitorPosition();
                Console.WriteLine("RECT1: {0} {1} {2} {3}", rect1.Left, rect1.Top, rect1.Right, rect1.Bottom);

                // Get and display monitor position
                var position = monitor.GetMonitorPosition(device.DeviceId);
                Helpers.ShowPropertiesTable($"Position before move {device.DeviceId}", position);

                var position1 = device.GetMonitorPosition();
                Helpers.ShowPropertiesTable($"Position before move {device.DeviceId}", position1);
            }

            if (!runMutations) {
                Console.WriteLine("Inspection complete. Pass --run-mutations to run the interactive window, input, and display mutation demos.");
                return;
            }

            Console.WriteLine("Running explicitly requested mutation demos. These examples can move windows, send input, and change display settings.");

            // Demonstrate window management features
            WindowExamples.Run();

            // Demonstrate automatic window snapping
            WindowSnapExample.Run();

            // Demonstrate window keep-alive features
            WindowKeepAliveExample.Run();

            // Demonstrate keyboard input features
            KeyboardInputExample.Run();

            // Demonstrate mouse input features
            MouseInputExample.Run();

            // Run monitor watcher example for 30 seconds
            MonitorWatcherExample.RunAsync(TimeSpan.FromSeconds(30)).Wait();
          
            // Demonstrate resolution & orientation features
            ResolutionOrientationDemo.Run();
        }
    }
}
