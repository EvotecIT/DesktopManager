# DesktopManager - C# Library and PowerShell Module

DesktopManager is available as NuGet from the Nuget Gallery.

[![nuget downloads](https://img.shields.io/nuget/dt/DesktopManager?label=nuget%20downloads)](https://www.nuget.org/packages/DesktopManager)
[![nuget version](https://img.shields.io/nuget/v/DesktopManager)](https://www.nuget.org/packages/DesktopManager)

You can also download it from PowerShell Gallery

[![PowerShell Gallery Version](https://img.shields.io/powershellgallery/v/DesktopManager.svg?style=flat-square)](https://www.powershellgallery.com/packages/DesktopManager)
[![PowerShell Gallery Preview Version](https://img.shields.io/powershellgallery/vpre/DesktopManager.svg?label=powershell%20gallery%20preview&colorB=yellow&style=flat-square)](https://www.powershellgallery.com/packages/DesktopManager)
[![PowerShell Gallery Downloads](https://img.shields.io/powershellgallery/dt/DesktopManager.svg?style=flat-square)](https://www.powershellgallery.com/packages/DesktopManager)

[![PowerShell Gallery Platform](https://img.shields.io/powershellgallery/p/DesktopManager.svg?style=flat-square)](https://www.powershellgallery.com/packages/DesktopManager)
[![GitHub top language](https://img.shields.io/github/languages/top/evotecit/DesktopManager.svg?style=flat-square)](https://github.com/EvotecIT/DesktopManager)
[![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/evotecit/DesktopManager.svg?style=flat-square)](https://github.com/EvotecIT/DesktopManager)
[![GitHub license](https://img.shields.io/github/license/EvotecIT/DesktopManager.svg?style=flat-square)](https://github.com/EvotecIT/DesktopManager)
[![codecov](https://img.shields.io/codecov/c/github/EvotecIT/DesktopManager?style=flat-square)](https://codecov.io/gh/EvotecIT/DesktopManager)

If you would like to contact me you can do so via Twitter or LinkedIn.

[![Twitter Follow](https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=social)](https://twitter.com/PrzemyslawKlys)
[![Blog evotec.xyz](https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg)](https://evotec.xyz/hub)
[![LinkedIn pklys](https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn)](https://www.linkedin.com/in/pklys)
[![Threads @PrzemyslawKlys](https://img.shields.io/badge/Threads-@PrzemyslawKlys-000000.svg?logo=Threads&logoColor=White)](https://www.threads.net/@przemyslaw.klys)


## What it's all about

<img height="256" align=right src="https://raw.githubusercontent.com/EvotecIT/DesktopManager/refs/heads/master/Assets/Images/DesktopManager.jpg">

**DesktopManager** is a C# library and PowerShell module that allows to play with desktop settings. It allows to get information about monitors, display devices, wallpapers and set wallpapers. There are 2 ways to use:
- **C# Library** - use it in your projects
- **PowerShell Module** - use it in your scripts

It has following features:
- Get information about monitors
- Get information about display devices
- Get information about wallpapers
- Set wallpapers
- Get/Set desktop background color
- Get/Set monitor position
- Get/Set window position
- Get/Set window state (minimize, maximize, restore)
 - Capture desktop screenshots from all monitors, a single monitor or a custom region

### Installation

For using in PowerShell you can install it from PowerShell Gallery

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

#### Example in C# - Getting/Setting Window Position

```csharp
var manager = new WindowManager();
manager.GetWindows
manager.SetWindowPosition
manager.CloseWindow
manager.MinimizeWindow
manager.MaximizeWindow
manager.RestoreWindow
```

### Platform notes

When retrieving a window's style the library uses a helper method that calls
`GetWindowLong` on 32-bit processes and `GetWindowLongPtr` on 64-bit ones. The
method returns an `IntPtr`, so callers should convert the value to the
appropriate numeric type.

#### Example in PowerShell - Getting Monitor Information

```powershell
Get-DesktopMonitor | Format-Table

Get-DesktopWallpaper -Index 0

Set-DesktopWallpaper -Index 1 -WallpaperPath "C:\Support\GitHub\ImagePlayground\Sources\ImagePlayground.Examples\bin\Debug\net7.0\Images\KulekWSluchawkach.jpg" -Position Fit
Set-DesktopWallpaper -Index 0 -WallpaperPath "C:\Users\przemyslaw.klys\Downloads\IMG_4820.jpg"
Set-DesktopBackgroundColor -Color 0x0000FF
Get-DesktopBackgroundColor
```

#### Example in PowerShell - Wallpaper Slideshow

```powershell
Start-DesktopSlideshow -ImagePath 'C:\Wallpapers\img1.jpg','C:\Wallpapers\img2.jpg'
Advance-DesktopSlideshow -Direction Forward
Stop-DesktopSlideshow
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

#### Example in PowerShell - Setting Monitor Position

```powershell
$Desktop2 = Get-DesktopMonitor -ConnectedOnly
$Desktop2 | Format-Table

Set-DesktopPosition -Index 0 -Left -3840 -Top 0 -Right 0 -Bottom 1660 -WhatIf
Set-DesktopPosition -Index 1 -Left 0 -Top 0 -Right 3840 -Bottom 2160 -WhatIf


### Example in PowerShell - Getting/Setting Window Position

```powershell
Get-DesktopWindow | Format-Table *
```

![image](https://github.com/user-attachments/assets/e4d026f7-2035-4a45-9779-a85423acdb21)


```powershell
Set-DesktopWindow -Name '*Zadanie - Notepad' -Height 800 -Width 1200 -Left 100
Set-DesktopWindow -Name '*Zadanie - Notepad' -State Maximize
```

### Example in PowerShell - Activating and Setting Window Top-Most

```powershell
Set-DesktopWindow -Name '*Notepad*' -TopMost -Activate
```

#### Example in C# - Activating and Setting Window Top-Most

```csharp
var manager = new WindowManager();
var window = manager.GetWindows().First();
manager.SetWindowTopMost(window, true);
manager.ActivateWindow(window);
#### Example in PowerShell - Monitoring Display Changes

Use `Register-DesktopMonitorEvent` to react when monitors are plugged in or the display configuration changes.

```powershell
Register-DesktopMonitorEvent -Duration 30 -Action { Write-Host 'Display settings changed' }
```

Use `Register-DesktopOrientationEvent` or `Register-DesktopResolutionEvent` to handle orientation or resolution changes individually.

```powershell
Register-DesktopResolutionEvent -Duration 30 -Action { Write-Host 'Resolution changed' }
Register-DesktopOrientationEvent -Duration 30 -Action { Write-Host 'Orientation changed' }
```

#### Example in C# - Monitoring Display Changes

Applications can subscribe to the `MonitorWatcher.DisplaySettingsChanged` event.

```csharp
MonitorWatcherExample.Run(TimeSpan.FromSeconds(30));
```

#### Example in PowerShell - Saving and Restoring Window Layout

```powershell
Save-DesktopWindowLayout -Path './layout.json'
# ... move windows around ...
Restore-DesktopWindowLayout -Path './layout.json' -Validate
```

#### Example in C# - Saving and Restoring Window Layout

```csharp
var manager = new WindowManager();
manager.SaveLayout("layout.json");
// ... move windows around ...
manager.LoadLayout("layout.json", validate: true);
```
