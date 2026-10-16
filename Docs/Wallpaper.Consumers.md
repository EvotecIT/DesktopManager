# Wallpaper consumer APIs

DesktopManager owns monitor discovery, lock screen access and Windows session identity for wallpaper
applications such as [PowerBGInfo](https://github.com/EvotecIT/PowerBGInfo). Call these APIs from the
account and session whose desktop you intend to update. Rendering an image to a file needs none of the
wallpaper setters.

## Physical monitor geometry and scale

```csharp
using DesktopManager;

var monitors = new Monitors();
foreach (MonitorSnapshot display in monitors.GetMonitorSnapshots()) {
    Console.WriteLine($"{display.Index}: {display.DeviceName}");
    Console.WriteLine($"Primary: {display.IsPrimary}");
    Console.WriteLine($"Bounds: {display.Left}, {display.Top}, " +
        $"{display.Width} x {display.Height}");
    Console.WriteLine($"DPI: {display.DpiX} x {display.DpiY}; scale: {display.Scale}");
}
```

`MonitorService.GetMonitorSnapshots()` provides the same API when an application already owns a
service. Each call returns a new read-only collection with detached, getter-only snapshots:

| Property | Meaning |
| --- | --- |
| `Index` | Zero-based wallpaper monitor index when the wallpaper COM service is available; otherwise the native monitor enumeration index. |
| `DeviceId` | Wallpaper device path; empty when no path is available or the snapshot comes from native geometry fallback. |
| `DeviceName` | Display source name, for example `\\.\DISPLAY1`; empty if it cannot be matched. |
| `IsPrimary` | Whether Windows identifies the display as primary. |
| `Bounds`, `Left`, `Top`, `Right`, `Bottom`, `Width`, `Height` | Physical pixels in virtual desktop coordinates. Origins can be negative. |
| `DpiX`, `DpiY` | Effective per-monitor DPI, including Windows display scaling; null when unavailable. |
| `Scale` | `DpiX / 96.0`, for example `1.5` for 150 percent; null when unavailable. |

The query requires Windows 10 version 1607 / Windows Server 2016 or later. It temporarily selects a
per-monitor-aware context on the calling thread for geometry and DPI reads, then restores that
thread's previous context in `finally`. It does not change process DPI awareness, resolution, wallpaper,
or any system setting. It throws if Windows cannot establish or restore the context, rather than
reporting virtualized coordinates as physical pixels. Individual DPI failures leave the monitor in
the result with null DPI and scale.

Indexes remain useful while topology and the enumeration source are unchanged. They are not permanent
hardware identifiers. Reacquire snapshots after docking, display changes or a remote reconnect; do not
save an index as a lasting monitor identity. Native fallback indexes do not guarantee an independently
addressable wallpaper target when the wallpaper COM service is unavailable.

On RDS and similar session hosts, a monitor can lack a wallpaper device path. Snapshots retain that
entry and its available geometry. The existing index-based wallpaper setter uses
`SystemParametersInfo` for a valid COM monitor slot with a missing path. That operation affects the
whole session: applying several images this way leaves the last image on every display. An invalid
or out-of-range wallpaper index remains a no-op.

## The signed-in session user

```csharp
var sessions = new DesktopSessionService();
DesktopSessionUser? current = sessions.GetSessionUser();
Console.WriteLine(current?.AccountName ?? "Unknown");

DesktopSessionUser? target = sessions.GetSessionUser(sessionId: 3);
if (target is DesktopSessionUser user) {
    Console.WriteLine($"{user.SessionId}: {user.AccountName}");
}
```

This read-only API uses `WTSQuerySessionInformation`, returning `SessionId`, `UserName`, `DomainName`,
`AccountName` and `IsConsoleFallback`. `AccountName` is `DOMAIN\user`, or just the user name when WTS
has no domain. The current-session query requires no elevation. Access to another session depends on
Windows session permissions.

With no argument it reads the current process session. Only a process in session 0 selects the active
console session instead. An explicitly selected session, including 0, never falls back to somebody
else's console session. Missing users or unavailable session information return null; negative target
IDs throw `ArgumentOutOfRangeException`. There is no WMI dependency and no built-in `Unknown` string.
The application decides whether to try another source or show a placeholder.

The console is often empty on a session host. A service in session 0 cannot infer which remote user's
wallpaper is being generated: supply the intended session ID or run the renderer in that user's session.

## Per-user lock screen with a reported result

```csharp
var monitors = new Monitors();
LogonWallpaperResult result = monitors.SetLogonWallpaperWithResult(
    @"C:\Wallpapers\Machine.png");

if (!result.Succeeded) {
    Console.Error.WriteLine(result.ErrorMessage);
} else {
    Console.WriteLine(result.Mechanism);
}
```

`MonitorService` exposes the same method. It first calls the current account's Windows Runtime
`LockScreen.SetImageStreamAsync` API and waits for its completion, including its native error result.
This works through the existing `net472`, `netstandard2.0`, .NET 8 and .NET 10 package assets. The
per-user attempt needs no administrative check. An elevated process running as a different account
still targets that account, not the user whose desktop happens to be visible.

`LogonWallpaperResult` contains:

| Property | Meaning |
| --- | --- |
| `Succeeded` | The WinRT action completed or the policy value was written. |
| `Mechanism` | `WindowsRuntime`, `PersonalizationPolicy`, or `None` on failure. |
| `ErrorMessage` | The final failure message, or null on success. |
| `WindowsRuntimeError` | Why WinRT failed, including when a later policy write succeeded. |

Argument and missing-file errors throw before any native write. Native API failures are returned.
Relative image paths are converted to full paths before Windows receives them. Policy restrictions,
Windows configuration and the availability of a user profile can still prevent a visible change.

The default never writes a machine policy. A management application can opt in:

```csharp
LogonWallpaperResult result = monitors.SetLogonWallpaperWithResult(
    @"C:\Wallpapers\Machine.png", allowPolicyFallback: true);
```

If WinRT fails, this writes `HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization\LockScreenImage`.
Only this fallback requires elevation. A denied write returns a failed result. A successful policy
write is not proof of application: the policy is supported on Enterprise, Education and Server,
including relevant IoT Enterprise editions; ordinary Pro/Home installations do not acquire that
capability merely because an administrator wrote the value.

The existing void `SetLogonWallpaper(string)` remains available and opts into the policy fallback for
compatibility. It reports native failures through DesktopManager diagnostics. Applications needing
reliable user-facing warnings should use the result API and inspect the actual mechanism rather than
probing managed WinRT types or predicting the route from the PowerShell version.

`GetLogonWallpaper()` exports the per-user image to a temporary file, falling back to the configured
machine policy path when the API cannot read the image. This legacy return value does not distinguish
an export from a policy path. Do not blindly delete the returned file; only delete a copy whose
ownership you have established.

### Runtime and deployment choice

The implementation calls `RoGetActivationFactory` and the documented WinRT interface ABI directly,
using Windows stream adapters. It initializes and balances WinRT on an MTA worker, so synchronous calls
from an STA PowerShell host do not depend on that host pumping WinRT completion callbacks.

This route adds no production dependency, projection assembly or package target. It preserves the
library's Windows 10 lock screen contract and the existing .NET 8/.NET 10 target framework minimums.
A CsWinRT projection would add runtime package content and would not help a caller that still selects
the netstandard asset. The native route avoids reflection over WinRT types and generated projections;
this choice alone does not establish that the entire DesktopManager library is trim- or NativeAOT-safe.
In particular, the default `Monitors` facade still constructs the existing wallpaper COM wrapper,
which depends on runtime COM activation and is not supported by the NativeAOT probe. Read-only native
lock screen, session and geometry calls have been exercised in a trimmed .NET 8 NativeAOT application
through `MonitorService` with a supplied `IDesktopManager`; that proof does not cover applying an image
or the default wallpaper COM wrapper.

## Wallpaper fit and all-user deployment

`IDesktopWallpaper.SetPosition` is global within the current user's desktop. A wallpaper image can be
selected per monitor, but `Center`, `Fit`, `Fill`, `Stretch`, `Tile` and `Span` are shared. Calling a
position setter while applying an image to one monitor changes the position for the other monitors too.
Render each monitor's desired composition into its image and select one common Windows fit mode.

`SetWallpaperForAllUsers` writes wallpaper settings to discovered user profile hives, optionally
including the default profile. It does not enter other signed-in sessions or refresh their shells.
Run the normal per-user wallpaper operation in each intended session at sign-in, unlock or remote
reconnect. A session-0 broadcast cannot substitute for applying the setting in those sessions.
Profiles in detached FSLogix containers may not be discoverable through the local profile list.

## Platform references

- [LockScreen.SetImageStreamAsync](https://learn.microsoft.com/uwp/api/windows.system.userprofile.lockscreen.setimagestreamasync)
- [RoGetActivationFactory](https://learn.microsoft.com/windows/win32/api/roapi/nf-roapi-rogetactivationfactory)
- [CreateRandomAccessStreamOverStream](https://learn.microsoft.com/windows/win32/api/shcore/nf-shcore-createrandomaccessstreamoverstream)
- [SetThreadDpiAwarenessContext](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setthreaddpiawarenesscontext)
- [GetDpiForMonitor](https://learn.microsoft.com/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor)
- [WTSQuerySessionInformation](https://learn.microsoft.com/windows/win32/api/wtsapi32/nf-wtsapi32-wtsquerysessioninformationw)
- [IDesktopWallpaper.SetPosition](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-idesktopwallpaper-setposition)
- [Lock screen policy](https://learn.microsoft.com/windows/client-management/mdm/policy-csp-admx-controlpaneldisplay)
