## Unreleased

- Report no image when GNOME background images are disabled, and explain unsupported KDE image-package sources instead of returning a directory as an image file.
- Preserve normal restore bounds and minimized-from-maximized state in ordinary window layout saves and restores.
- Resolve current-monitor placement from Windows monitor association, with largest-intersection and nearest-monitor fallback for detached observations.

## 4.6.0 - 2026.10.23

- Add workspace capture, preview and recovery with virtual-desktop assignments, window and application pins, Explorer folder identity and explicit per-desktop wallpapers.
- Add the Workspace app tab with optional startup recovery and guarded periodic snapshots. PowerShell, CLI and MCP share the recovery engine.
- Preserve window identity across startup retries and skip missing desktops, incomplete Explorer views and windows that close during capture.
- Return physical-pixel screenshots and consistent control geometry from DPI-unaware callers.
- Native virtual-desktop extensions are qualified on Windows build family 26300. Build families 26100 and 26200 still require runtime qualification.

The Windows CLI and MCP plugin release is 0.2.2.

## 3.1.0 - 2025.07.20

Added;

- InvertWindowSelection method for toggling window selections

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
