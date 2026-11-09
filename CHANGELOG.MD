## 4.9.1 - 2026.11.09

- Bound cursor-fence startup to five seconds, request stop on timeout, and allow up to two further seconds for hook-thread cleanup before throwing `TimeoutException`.
- Use bounded kernel-event completion waits for wallpaper and audio COM calls, hotkey registration, clipboard writes, UI Automation dispatch and synchronous calls that wait for asynchronous work. UI Automation mutations targeting a window owned by the calling thread still run inline, and timed-out reads return their existing fallback, such as an empty control list. A timeout cancels work that has not started; other in-flight operations report an unknown outcome and clean up late owned results. This avoids unlimited STA completion waits after a worker has already completed.
- Allow shared wallpaper and hotkey services to retry a failed startup without caching its exception for the process lifetime.
- Use the same bounded startup handshake for window events, clipboard listeners, hotkey services, monitor notification windows, session-typing input monitoring, keep-awake leases, and audio and wallpaper COM threads. Late startup observes the stop request and releases native resources.

## 4.9.0 - 2026.11.07

- Resolve media source application IDs to their registered display names or executable file descriptions, while keeping the raw ID in `SourceAppId`. Retry unavailable names and avoid reusing names from a different executable with the same basename.
- Read a single audio endpoint directly instead of enumerating every device. Disabled, unplugged and not-present endpoints skip volume activation and return unavailable volume and mute values without expected activation exceptions.
- Cache default audio endpoint IDs with synchronous notification invalidation and a 30-second maximum age. Explicit default-device changes invalidate the cache immediately.
- Cache the process session ID and successful sign-in-time reads, and prevent a concurrent failed read from clearing a valid sign-in time.
- Reuse the effective power mode while its plan, registry stamp and power source stay unchanged. Failed overlay reads retry on the next call.

## 4.8.0 - 2026.11.05

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
- Add `ClipboardWatcher`, which reports new text or image content, skips copies apps mark as private, retries busy reads, and can put text or an image back without reporting it.
- Add `WifiConnectionService.GetCurrent` for the connected network, signal and link rates of a Wi-Fi interface. Windows 11 24H2 and later answer only apps that have location access.
- Add `SystemPowerService.GetActivePowerPlan` for the active plan and the Windows 11 power mode.
- Read clipboard text within the size of its memory block, and allocate before emptying the clipboard so a failed allocation in `SetText` no longer clears it.
- Add `SessionTypingService`, `Send-DesktopSessionText` and `window type-session` for typing into RDP, Citrix and VNC sessions and paste-blocked fields: scan codes for the remote keyboard layout, profile pacing, waiting for the target to come forward, Caps Lock and held-key checks, and stopping when focus moves or you type. See `Docs/DesktopManager.SessionTyping.md`.
- **Changed:** text the library pastes now requests exclusion from clipboard history, cloud synchronization and cooperating clipboard managers (`WindowInputOptions.PrivateClipboard`, or `Set-DesktopWindowText -PrivateClipboard $false` to opt out). `PreserveClipboard` restores every supported format, refuses to paste if preservation fails, and leaves a newer copy untouched.
- Fix clipboard text writes and private paste from callers without an owner window. Private paste and snapshot restoration write existing exclusion markers before payloads and clear failed partial transfers before releasing the clipboard.
- Hosted-session typing presses letters at US key positions on any local layout; on AZERTY or QWERTZ machines it used to send the wrong letters. Typing options are copied rather than changed on the caller's object.
- `window type` accepts `--text-stdin` and `--text-env` so text stays off the command line.
- **Changed:** window positions are read and set in physical pixels whatever the caller's DPI awareness, so values from `GetWindows` can differ on scaled displays and now match what the move methods accept. Client geometry and window-relative pointer input use the same coordinates. Re-save older layouts captured in logical pixels on scaled displays; their stored numbers are retained without an assumed conversion.
- Activating a window no longer removes its always-on-top state, and setting topmost no longer activates it.
- Auto-snap snaps on the monitor the window was dropped on and works from any thread. `SnapWindow` uses the work area, restores a maximised or minimised window first and allows for the invisible border; snap positions gain top and bottom halves, thirds and centre.
- Moving or resizing a minimised window changes where it restores to and leaves it minimised.
- **Changed:** `Set-DesktopWindow` applies negative coordinates, `-TopMost:$false` clears topmost, and it skips hidden and cloaked windows unless `-IncludeHidden` or `-IncludeCloaked` is given. `MinimizeAll` and `RestoreAll` leave cloaked windows (including those on other virtual desktops) alone by default.
- Window errors include the Windows error and say when the target runs elevated. Moving another app's window to a virtual desktop explains the access-denied case, and `Move-DesktopWindowToVirtualDesktop -Experimental` can do it.
- Saved layouts match a window whose title changed by process, class and application, and `Restore-DesktopWindowLayout` warns about windows it couldn't find. Stronger identities are reserved before weaker matches.
- **Changed:** a layout with a captured application ID requires the same verified live ID; unavailable identities and ambiguous targets are skipped rather than matched to another app. Window identity reads use the public Windows property store and packaged-process identity APIs.
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
