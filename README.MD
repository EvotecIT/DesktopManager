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
- Manage monitor brightness
- Start/Stop/Advance wallpaper slideshows
- Track wallpaper history
- Adjust monitor resolution, orientation and DPI scaling
- Move monitors around the virtual desktop
- Save and restore window layouts
- Snap or move windows between monitors
- Subscribe to resolution, orientation or display changes
- Keep inactive windows awake using periodic input
- Manage keep-alive sessions for windows


### Available PowerShell Cmdlets

| Cmdlet | Description |
| ------ | ----------- |
| Get-DesktopMonitor | Retrieve monitor information with filtering options |
| Get-DesktopWallpaper | Get current wallpaper path for monitors |
| Set-DesktopWallpaper | Apply wallpaper from path, URL or stream |
| Get-DesktopWallpaperHistory | List stored wallpaper history entries |
| Set-DesktopWallpaperHistory | Update or clear wallpaper history file |
| Start-DesktopSlideshow | Begin wallpaper slideshow across monitors |
| Stop-DesktopSlideshow | Stop currently running slideshow |
| Advance-DesktopSlideshow | Move slideshow forward or backward |
| Get-DesktopBackgroundColor | Read current desktop background color |
| Set-DesktopBackgroundColor | Change desktop background color |
| Get-DesktopBrightness | Read monitor brightness level |
| Set-DesktopBrightness | Set monitor brightness level |
| Set-DesktopPosition | Configure monitor coordinates |
| Set-DesktopResolution | Change monitor resolution or orientation |
| Set-DesktopDpiScaling | Adjust DPI scaling percentage |
| Set-DefaultAudioDevice | Set the default audio playback device |
| Get-LogonWallpaper | Get the lock screen wallpaper path |
| Set-LogonWallpaper | Set the lock screen wallpaper |
| Set-TaskbarPosition | Move or hide the taskbar |
| Invoke-DesktopMouseMove | Move the mouse cursor |
| Invoke-DesktopMouseClick | Simulate a mouse click |
| Invoke-DesktopMouseScroll | Scroll the mouse wheel |
| Invoke-DesktopScreenshot | Capture monitor or region screenshots |
| Get-DesktopWindow | Enumerate visible windows |
| Set-DesktopWindow | Move, resize or control windows |
| Set-DesktopWindowSnap | Snap window to common positions |
| Set-DesktopWindowText | Paste or type text into a window |
| Start-DesktopWindowKeepAlive | Send periodic input to keep a window awake |
| Stop-DesktopWindowKeepAlive | Stop sending keep-alive input |
| Get-DesktopWindowKeepAlive | List windows with active keep-alive |
| Save-DesktopWindowLayout | Save current window layout to file |
| Restore-DesktopWindowLayout | Restore saved window layout |
| Register-DesktopMonitorEvent | Subscribe to display configuration changes |
| Register-DesktopOrientationEvent | Subscribe to orientation changes |
| Register-DesktopResolutionEvent | Subscribe to resolution changes |

### Cmdlet to C# method map

The table below shows the most relevant API methods behind each PowerShell cmdlet.

| Cmdlet | Main C# methods |
| ------ | --------------- |
| Get-DesktopMonitor | `Monitors.GetMonitors` |
| Get-DesktopWallpaper | `Monitors.GetWallpaper` or `Monitor.GetWallpaper` |
| Set-DesktopWallpaper | `Monitors.SetWallpaper`, `Monitors.SetWallpaperFromUrl` |
| Get-DesktopWallpaperHistory | `WallpaperHistory.GetHistory` |
| Set-DesktopWallpaperHistory | `WallpaperHistory.SetHistory` |
| Start-DesktopSlideshow | `Monitors.StartWallpaperSlideshow` |
| Stop-DesktopSlideshow | `Monitors.StopWallpaperSlideshow` |
| Advance-DesktopSlideshow | `Monitors.AdvanceWallpaperSlide` |
| Get-DesktopBackgroundColor | `Monitors.GetBackgroundColor` |
| Set-DesktopBackgroundColor | `Monitors.SetBackgroundColor` |
| Get-DesktopBrightness | `Monitors.GetMonitorBrightness` |
| Set-DesktopBrightness | `Monitors.SetMonitorBrightness` |
| Set-DesktopPosition | `Monitor.SetMonitorPosition` or `Monitors.SetMonitorPosition` |
| Set-DesktopResolution | `Monitors.SetMonitorResolution`, `Monitors.SetMonitorOrientation` |
| Set-DesktopDpiScaling | `Monitors.SetMonitorDpiScaling` |
| Set-DefaultAudioDevice | `AudioService.SetDefaultAudioDevice` |
| Get-LogonWallpaper | `Monitors.GetLogonWallpaper` |
| Set-LogonWallpaper | `Monitors.SetLogonWallpaper` |
| Set-TaskbarPosition | `TaskbarService.SetTaskbarPosition` / `SetTaskbarVisibility` |
| Invoke-DesktopMouseMove | `WindowManager.MoveMouse` |
| Invoke-DesktopMouseClick | `WindowManager.ClickMouse` |
| Invoke-DesktopMouseScroll | `WindowManager.ScrollMouse` |
| Invoke-DesktopScreenshot | `ScreenshotService.CaptureScreen`, `ScreenshotService.CaptureRegion` |
| Get-DesktopWindow | `WindowManager.GetWindows` |
| Set-DesktopWindow | `WindowManager.SetWindowPosition`, `MoveWindowToMonitor`, etc. |
| Set-DesktopWindowSnap | `WindowManager.SnapWindow` |
| Set-DesktopWindowText | `WindowInputService` |
| Start-DesktopWindowKeepAlive | `WindowKeepAlive.Start` |
| Stop-DesktopWindowKeepAlive | `WindowKeepAlive.Stop` or `StopAll` |
| Get-DesktopWindowKeepAlive | `WindowKeepAlive.ActiveHandles` |
| Save-DesktopWindowLayout | `WindowManager.SaveLayout` |
| Restore-DesktopWindowLayout | `WindowManager.LoadLayout` |
| Register-DesktopMonitorEvent | `MonitorWatcher.DisplaySettingsChanged` |
| Register-DesktopOrientationEvent | `MonitorWatcher.OrientationChanged` |
| Register-DesktopResolutionEvent | `MonitorWatcher.ResolutionChanged` |

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

```powershell
Set-DesktopWindowText -Name '*Notepad*' -Text 'Hello world'
```

```powershell
# Send Ctrl+S to Notepad
$notepad = Get-DesktopWindow -Name '*Notepad*'
Invoke-DesktopKeyPress -Window $notepad -Keys @(
    [DesktopManager.VirtualKey]::VK_CONTROL,
    [DesktopManager.VirtualKey]::VK_S
)
# See Examples/KeyboardActions.ps1
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

#### Examples in PowerShell - Window Keep-Alive Cmdlets

```powershell
# Keep Notepad alive for one minute
Start-DesktopWindowKeepAlive -Name '*Notepad*' -Interval 00:01:00

# List active sessions
Get-DesktopWindowKeepAlive

# Stop the session
Stop-DesktopWindowKeepAlive -Name '*Notepad*'
```

```powershell
# Keep Notepad and Calculator alive
Start-DesktopWindowKeepAlive -Name '*Notepad*'
Start-DesktopWindowKeepAlive -Name '*Calculator*' -Interval 00:00:30
Start-Sleep -Seconds 5
Get-DesktopWindowKeepAlive | Format-Table Title, Handle
Stop-DesktopWindowKeepAlive -All
```

```powershell
# Monitor RDP windows
Start-DesktopWindowKeepAlive -Name '*RDP*' -Interval 00:00:30
1..3 | ForEach-Object {
    Start-Sleep -Seconds 10
    Get-DesktopWindowKeepAlive | ForEach-Object { "Active: $($_.Title)" }
}
Stop-DesktopWindowKeepAlive -Name '*RDP*'
```

#### Examples in C# - Window Keep-Alive

```csharp
var manager = new WindowManager();
var notepad = manager.GetWindows("*Notepad*").FirstOrDefault();
if (notepad != null) {
    WindowKeepAlive.Instance.Start(notepad, TimeSpan.FromMinutes(1));
}
```

```csharp
foreach (var window in new WindowManager().GetWindows("*Chrome*")) {
    WindowKeepAlive.Instance.Start(window, TimeSpan.FromSeconds(30));
}
```

```csharp
foreach (var handle in WindowKeepAlive.Instance.ActiveHandles) {
    Console.WriteLine($"Keeping {handle} alive");
}
WindowKeepAlive.Instance.StopAll();
```

### C# API Highlights

DesktopManager ships as a .NET library targeting `net472`, `netstandard2.0` and `net8.0`. The main entry points are:

- `Monitors` for monitor enumeration, wallpaper management, brightness, resolution, orientation and slideshow control.
- `WindowManager` for window enumeration, positioning, resizing and layout persistence.
- `MonitorWatcher` to receive events when display settings change.
- `ScreenshotService` to capture the desktop or custom regions.
