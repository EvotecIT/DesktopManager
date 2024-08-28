using Monitors = DesktopManager.Monitors;

namespace DesktopManagerSample {
    class Program {
        static void Main(string[] args) {

            Monitors monitor = new Monitors();
            var test = monitor.GetMonitors();

            Console.WriteLine("Number of monitors: " + test.Count);
            foreach (var item in test) {
                Console.WriteLine("Monitor: " + item);
            }


            Console.WriteLine("Available monitor connections: " + monitor.GetAvailableMonitorPaths());
            Console.WriteLine("Connected monitors: " + monitor.GetConnectedMonitors().Count);

            monitor.ListDisplayDevices();

            Console.WriteLine("1======");

            List<Monitors.DISPLAY_DEVICE> devices = monitor.GetDisplayDevices();

            foreach (var device in devices) {
                Console.WriteLine($"Device Name: {device.DeviceName}");
                Console.WriteLine($"Device String: {device.DeviceString}");
                Console.WriteLine($"State Flags: {device.StateFlags}");
                Console.WriteLine($"Device ID: {device.DeviceID}");
                Console.WriteLine($"Device Key: {device.DeviceKey}");
                Console.WriteLine();
            }

            Console.WriteLine("2======");

            foreach (var deviceId in monitor.GetConnectedMonitors()) {
                Console.WriteLine("3==================================");
                Console.WriteLine("MonitorID: " + deviceId);
                Console.WriteLine("Wallpaper Path: " + monitor.GetWallpaper(deviceId));
                Console.WriteLine("Wallpaper Position (only first monitor): " + monitor.GetWallpaperPosition());
                var rect = monitor.GetMonitorRECT(deviceId);
                Console.WriteLine("RECT: {0} {1} {2} {3}", rect.Left, rect.Top, rect.Right, rect.Bottom);


                // Get and display monitor position
                var position = monitor.GetMonitorPosition(deviceId);
                Console.WriteLine("Position: Left={0}, Top={1}, Right={2}, Bottom={3}", position.Left, position.Top, position.Right, position.Bottom);

                position = monitor.GetMonitorPosition(deviceId);
                Console.WriteLine("Position: Left={0}, Top={1}, Right={2}, Bottom={3}", position.Left, position.Top, position.Right, position.Bottom);


            }

            monitor.SetMonitorPosition(@"\\?\DISPLAY#GSM5BBF#5&22b00b5d&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}", -3840, 0, 0, 2160);


            //   monitor.SetWallpaper(1, @"C:\Users\przemyslaw.klys\Downloads\IMG_4644.jpeg");

        }
    }
}