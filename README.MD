<p align="center">
  <a href="https://dev.azure.com/evotecpl/DesktopManager/_build/results?buildId=latest"><img src="https://img.shields.io/azure-devops/build/evotecpl/39c74615-8f34-4af0-a835-68dc33f9214f/14?label=Azure%20Pipelines&style=flat-square"></a>
  <a href="https://www.powershellgallery.com/packages/DesktopManager"><img src="https://img.shields.io/powershellgallery/v/DesktopManager.svg?style=flat-square"></a>
  <a href="https://www.powershellgallery.com/packages/DesktopManager"><img src="https://img.shields.io/powershellgallery/vpre/DesktopManager.svg?label=powershell%20gallery%20preview&colorB=yellow&style=flat-square"></a>
  <a href="https://github.com/EvotecIT/DesktopManager"><img src="https://img.shields.io/github/license/EvotecIT/DesktopManager.svg?style=flat-square"></a>
</p>

<p align="center">
  <a href="https://www.powershellgallery.com/packages/DesktopManager"><img src="https://img.shields.io/powershellgallery/p/DesktopManager.svg?style=flat-square"></a>
  <a href="https://github.com/EvotecIT/DesktopManager"><img src="https://img.shields.io/github/languages/top/evotecit/DesktopManager.svg?style=flat-square"></a>
  <a href="https://github.com/EvotecIT/DesktopManager"><img src="https://img.shields.io/github/languages/code-size/evotecit/DesktopManager.svg?style=flat-square"></a>
  <a href="https://www.powershellgallery.com/packages/DesktopManager"><img src="https://img.shields.io/powershellgallery/dt/DesktopManager.svg?style=flat-square"></a>
</p>

<p align="center">
  <a href="https://twitter.com/PrzemyslawKlys"><img src="https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=flat-square&logo=twitter"></a>
  <a href="https://evotec.xyz/hub"><img src="https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg?style=flat-square"></a>
  <a href="https://www.linkedin.com/in/pklys"><img src="https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn&style=flat-square"></a>
</p>

**DesktopManager** is a C# library and PowerShell module that allows to get and set wallpapers to given monitor.

### Installation

```powershell
Install-Module DesktopManager -Force -Verbose
```

### Usage

#### Example in C#

Full exaple can be found in `DesktopManager.Example` project, as helper methods are requried to display data properly.

```csharp
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
```

#### Example in PowerShell

```powershell
Get-DesktopMonitor | Format-Table

Get-DesktopWallpaper -Index 0

Set-DesktopWallpaper -Index 1 -WallpaperPath "C:\Support\GitHub\ImagePlayground\Sources\ImagePlayground.Examples\bin\Debug\net7.0\Images\KulekWSluchawkach.jpg" -Position Fit
Set-DesktopWallpaper -Index 0 -WallpaperPath "C:\Users\przemyslaw.klys\Downloads\IMG_4820.jpg"
```

```powershell
$Desktop1 = Get-DesktopMonitor
$Desktop1 | Format-Table

$Desktop2 = Get-DesktopMonitor -ConnectedOnly
$Desktop2 | Format-Table

$Desktop3 = Get-DesktopMonitor -PrimaryOnly
$Desktop3 | Format-Table

$Desktop4 = Get-DesktopMonitor -Index 1
$Desktop4 | Format-Table

$Desktop5 = Get-DesktopMonitor -DeviceName "\\.\DISPLAY2"
$Desktop5 | Format-Table
```
