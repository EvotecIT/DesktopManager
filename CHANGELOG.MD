## Unreleased

- Ignore microphone and camera usage records that predate both boot and the current sign-in, so a crash or power loss no longer leaves an app reported as still using the device. Active records use the recorded sign-in time rather than a moving boot estimate; ambiguous timestamps after Fast Startup or a backwards clock correction return unavailable rather than a successful empty snapshot.
- Make monitor brightness work: monitor lookup no longer gives up when the enumeration stops early on a match, `GetMonitorBrightness`/`SetMonitorBrightness` accept the device ID or the display name, and `Monitor` gains `GetBrightness()` and `SetBrightness()`.
- Keep monitor IDs consistent: the fallback enumeration reports the same device path as the normal one, and display-setting fallbacks map it back to the display name.
- Add `Monitor.Bounds` and `Monitor.WorkingArea`, and make `Monitor.Position` use the bounds already read during enumeration.
- Move windows between monitors relative to the work area, keep them inside the target, and maximise a maximised window again on the target.
- Correct interop that failed or misbehaved: `SetSuspendState` marshalling, `SetWindowLongPtr` on 32-bit processes, the ANSI window-long entry points, missing last-error capture, hive unload errors, and `MonitorWatcher` startup, shutdown and 64-bit message parameters.
- Add `CursorFence` to keep the mouse pointer from crossing a screen edge, either always or until it is pushed for a moment, while touch and pen pass through.
- Add `WindowEventWatcher` for window move, resize, show, destroy and foreground events on its own thread.
- Add `NonActivatingWindow`, `WindowEdgeGestures` and `TouchInputMapping` for touch-screen and side-panel apps, and make `DesktopSessionService.GetIdleTime()` public.
- Add `WindowThumbnail` for live DWM window previews.
- Add `AudioDefaultEndpointSession`, which follows the default device as it changes and reports a missing device as unavailable rather than throwing, and `AudioLoopbackCapture` for the samples playing on the default output.
- Add `ClipboardWatcher`, which reports each copy of text or an image and skips copies apps mark as private, and can put text or an image back without reporting it.
- Add `WifiConnectionService.GetCurrent` for the connected network, signal and link rates of a Wi-Fi interface. Windows 11 24H2 and later answer only apps that have location access.
- Add `SystemPowerService.GetActivePowerPlan` for the active plan and the Windows 11 power mode.
- Read clipboard text within the size of its memory block, and allocate before emptying the clipboard so a failed allocation in `SetText` no longer clears it.
- Stop the mouse input tests clicking and dragging on the live desktop.

## 4.7.0 - 2026.11.03

- Add shared services for per-application audio volume, mute and peaks; live endpoint levels; current media metadata, artwork and transport; and microphone/camera usage records. Modern targets retain trimming and Native AOT support, while older targets report unsupported media-session capability.

- Make Core Audio work in trimmed and Native AOT applications. On .NET 8 and later, `AudioService`, `PolicyConfigClient`, `AudioEndpointWatcher` and `VirtualDesktopService` use source-generated COM; net472 and netstandard2.0 keep built-in COM.
- Mark the .NET 8 and .NET 10 targets as AOT compatible. Persisted JSON state uses source-generated metadata and native structure marshalling no longer needs runtime code generation.
- Support wallpaper and monitor enumeration, slideshow Shell items and wallpaper checkpoints in Native AOT applications. Native wallpaper calls run in one COM apartment.
- Capture clipboard snapshots through Win32 data handles, and capture Explorer folders through typed Shell views without late-bound automation.
- Use generated COM for the gated experimental virtual desktop interfaces and the UI Automation interfaces used by control discovery, patterns, text ranges and event subscriptions. The older targets retain their existing interop paths.
- Add a Windows Native AOT smoke consumer that reads desktop state and exercises owned controls without changing wallpaper, clipboard data, audio defaults or user windows.
- Preserve connected monitor identities when a disconnected wallpaper slot cannot return its bounds; omit unavailable geometry from connected snapshots.

## 4.6.1 - 2026.10.24

- Report no image when GNOME background images are disabled, and explain unsupported KDE image-package sources instead of returning a directory as an image file.
- Preserve normal restore bounds and minimized-from-maximized state in ordinary window layout saves and restores.
- Resolve current-monitor placement from Windows monitor association, with largest-intersection and nearest-monitor fallback for detached observations.
- Discover portable wallpaper targets independently of physical display geometry and expose native fit, background color and shading metadata.
- Report GNOME Shell's effective full image opacity without trusting its ignored retained opacity setting.
- Expose optional physical dimensions for a qualified single-display X11 layout through `xrandr`; transformed, rotated, reflected, panning, multi-display and Wayland layouts still require explicit dimensions.
- Add disposable, session-bound wallpaper checkpoints with explicit rejection of sources or selected modes that cannot be faithfully restored. See `Docs/Portable.Desktop.md` for platform limits.
- Correct the Windows Shell slideshow array ABI and retain checkpoint COM references safely across caller apartments.
- Reuse the native wallpaper COM interface when another caller has already activated its managed wrapper, preserving monitor discovery in that process.

The Windows CLI and MCP plugin release is 0.2.3.

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
