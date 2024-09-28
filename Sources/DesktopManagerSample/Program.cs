using DesktopManager;

namespace DesktopManagerSample {
    class Program {
        static void Main(string[] args) {

            Monitors monitor = new Monitors();
            var getMonitors = monitor.GetMonitors();

            Helpers.AddLine("Number of monitors", getMonitors.Count);
            Helpers.ShowPropertiesTable("GetMonitors() ", getMonitors);

            var getMonitorsConnected = monitor.GetMonitorsConnected();
            Helpers.AddLine("Number of monitors (connected):", getMonitorsConnected.Count);
            Helpers.ShowPropertiesTable("GetMonitorsConnected() ", getMonitorsConnected);

            var listDisplayDevices = monitor.DisplayDevicesAll();
            Console.WriteLine("Count DisplayDevicesAll: " + listDisplayDevices.Count);
            Helpers.ShowPropertiesTable("DisplayDevicesAll()", listDisplayDevices);

            Console.WriteLine("======");

            var getDisplayDevices = monitor.DisplayDevicesConnected();
            Console.WriteLine("Count DisplayDevicesConnected: " + getDisplayDevices.Count);
            Helpers.ShowPropertiesTable("DisplayDevicesConnected()", getDisplayDevices);

            Console.WriteLine("======");

            Console.WriteLine("Wallpaper Position (only first monitor): " + monitor.GetWallpaperPosition());

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

            // Set monitor position
            monitor.SetMonitorPosition(@"\\?\DISPLAY#GSM5BBF#5&22b00b5d&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}", -3840, 500, 0, 2160);

            // Get and display monitor position
            var testPosition = monitor.GetMonitorPosition(@"\\?\DISPLAY#GSM5BBF#5&22b00b5d&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}");
            Helpers.ShowPropertiesTable("Position after move", testPosition);

            Thread.Sleep(5000);

            // Set monitor position
            monitor.SetMonitorPosition(@"\\?\DISPLAY#GSM5BBF#5&22b00b5d&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}", -3840, 0, 0, 2160);

            // Get and display monitor position
            testPosition = monitor.GetMonitorPosition(@"\\?\DISPLAY#GSM5BBF#5&22b00b5d&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}");
            Helpers.ShowPropertiesTable("Position after move", testPosition);

            monitor.SetWallpaper(1, @"C:\Users\przemyslaw.klys\Downloads\CleanupMonster2.jpg");

        }
    }
}