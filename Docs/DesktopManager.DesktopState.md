# Desktop State and Controls

DesktopManager keeps desktop-state behavior in the shared C# library. PowerShell, CLI, MCP, and the desktop app call those services instead of maintaining separate implementations.

## Capability map

| Area | Shared API | What it covers |
| ---- | ---------- | -------------- |
| Workstation profiles | `WorkstationProfileService`, `WorkstationProfileStore` | Monitor modes and placement, wallpaper, supported brightness/HDR, active audio, personalization, taskbars, matching, staged display application, and rollback |
| Audio | `AudioService`, `AudioEndpointWatcher` | Render/capture endpoint inventory, active/unavailable state, volume, mute, independent default roles, and endpoint change events |
| Power and session | `SystemPowerService`, `DesktopSessionService`, `DesktopSessionWatcher` | AC/battery status, bounded keep-awake leases, lock, sleep/hibernate, sign-out, session identity, connection, remote/console, lock, and meaningful state changes |
| Personalization and taskbar | `PersonalizationService`, `PersonalizationStateStore`, `TaskbarService` | Reversible personalization snapshots, monitor wallpapers, theme and Start/taskbar settings, taskbar monitor mapping, visibility, position, bounds, and auto-hide |
| Radios | `RadioService` | Supported Windows radio inventory, explicit On/Off requests, access results, and state change events |
| Experimental airplane mode | `ExperimentalAirplaneModeService` | Explicit global Enabled/Disabled state through an undocumented Windows shell COM contract, followed by verification |
| Virtual desktops | `VirtualDesktopService` | Current-desktop check, desktop ID lookup, and moving a top-level window to a known desktop ID |

## Workstation profiles

Capture a current profile and store it by name:

```csharp
var profiles = new WorkstationProfileService();
WorkstationProfile captured = profiles.SaveProfile("office");
```

Applying a profile is strict about missing saved monitors by default. Display changes are staged and committed together. If a required step fails, the service attempts to restore the selected sections from a pre-apply capture.

```csharp
WorkstationProfileApplyResult result = profiles.ApplyProfile(
    "office",
    new WorkstationProfileApplyOptions {
        RequireAllMonitors = true,
        ApplyMachinePolicies = false,
        RollbackOnFailure = true
    });

if (!result.Succeeded) {
    Console.Error.WriteLine(result.Error);
}
```

Brightness and HDR depend on monitor and driver support. Failures in those optional capabilities are returned as warnings rather than making an otherwise usable profile impossible to apply. Machine-wide lock-screen and Spotlight policies are captured but are not restored by workstation profiles unless `ApplyMachinePolicies` is set; restoring them normally requires elevation.

## Audio

Audio endpoints have stable Windows IDs. Use the ID for mutations and the friendly name for display only.

```csharp
var audio = new AudioService();
AudioEndpointInfo speakers = audio
    .GetEndpoints(AudioDataFlow.Render, AudioEndpointState.Active)
    .First(endpoint => endpoint.IsDefault);

audio.SetEndpointVolume(speakers.Id, 45);
audio.SetEndpointMute(speakers.Id, false);
audio.SetDefaultAudioDevice(speakers.Id, AudioRole.Multimedia);
```

Use `AudioEndpointWatcher` when a long-running process needs device arrival, removal, state, property, or default-role changes.

## Power and session state

`KeepAwakeLease` owns a Windows execution-state request on a dedicated thread. Dispose the lease to release the request.

```csharp
var power = new SystemPowerService();
SystemPowerStatus status = power.GetStatus();

using (power.CreateKeepAwakeLease(KeepAwakeOptions.System | KeepAwakeOptions.Display)) {
    RunBoundedWork();
}
```

Lock, sleep/hibernate, and sign-out are explicit methods. CLI and PowerShell add confirmation for the disruptive actions. `DesktopSessionWatcher` ignores idle-time-only movement and raises events for identity, connection, transport, remote, or lock-state changes.

## Supported radios and experimental airplane mode

Use `RadioService` for normal radio work:

```csharp
using var radios = new RadioService();
IReadOnlyList<DesktopRadioInfo> current = await radios.GetRadiosAsync();
IReadOnlyList<DesktopRadioSetResult> results = await radios.SetRadioStateAsync(
    DesktopRadioKind.WiFi,
    DesktopRadioState.On);
```

Windows can deny state changes because of user consent, policy, hardware switches, or device state. The result reports the Windows access status and the effective state after the request. The API requires an explicit `On` or `Off`; it does not offer a race-prone toggle operation.

Global airplane mode is separate:

```csharp
var airplaneMode = new ExperimentalAirplaneModeService();
AirplaneModeState observed = airplaneMode.SetState(AirplaneModeState.Enabled);
```

That contract is undocumented and may change between Windows releases. The CLI requires `--experimental`, PowerShell requires `-Experimental`, the app requires an acknowledgement, and MCP requires `--allow-experimental`.

## Supported virtual-desktop boundary

The public Windows `IVirtualDesktopManager` contract provides only three window operations, and DesktopManager exposes exactly those operations:

```csharp
var desktops = new VirtualDesktopService();
Guid desktopId = desktops.GetWindowDesktopId(windowHandle);
bool isCurrent = desktops.IsWindowOnCurrentDesktop(windowHandle);
desktops.MoveWindowToDesktop(otherWindowHandle, desktopId);
```

Desktop enumeration, creation, renaming, switching, reordering, and removal are not part of this supported surface. If Windows later publishes those capabilities, they can be added through the same service without binding the public API to private shell interfaces.

## MCP gates

MCP remains read-only by default:

```text
desktopmanager mcp serve
```

Use both flags for global state changes:

```text
desktopmanager mcp serve --allow-mutations --allow-system-settings
```

Add `--allow-experimental` only when the session explicitly accepts the undocumented global airplane-mode contract. Process allow/deny filters block global desktop mutations because audio, display, personalization, taskbar, power/session, and radio changes cannot be constrained to one process.
