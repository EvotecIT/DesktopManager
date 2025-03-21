## 3.0.0 - 2025.03.21

Added;

- Get-DesktopWindow
- Set-DesktopWindow

```powershell
Get-DesktopWindow | Format-Table *
```

![image](https://github.com/user-attachments/assets/e4d026f7-2035-4a45-9779-a85423acdb21)


```powershell
Set-DesktopWindow -Name '*Zadanie - Notepad' -Height 800 -Width 1200 -Left 100
Set-DesktopWindow -Name '*Zadanie - Notepad' -State Maximize
```

In C# adds multiple methods to use in C#

```csharp
var manager = new WindowManager();
manager.GetWindows
manager.SetWindowPosition
manager.CloseWindow
manager.MinimizeWindow
manager.MaximizeWindow
manager.RestoreWindow
```

## 2.0.1 - 2025.02.15
- Added missing aliases to `Set-DesktopWallpaper`

## 2.0.0 - 2025.02.14
- Rewritten in C#
- Added `Set-DesktopPosition`
- Improved `Get-DesktopWallpaper`
- Improved `Set-DesktopWallpaper`
- Improved `Get-DesktopMonitor`