# Workspace recovery

DesktopManager saves and restores window layouts with optional virtual desktop
assignments, window and application pins, Explorer folder paths and per-desktop
wallpapers. Restoration reports missing, ambiguous and failed targets instead of
guessing which window or desktop to use.

## Capture, preview and restore

```powershell
Import-Module DesktopManager

Save-DesktopWindowLayout -Path "$HOME\work-layout.json" -IncludeVirtualDesktops -IncludeExplorerFolders
Restore-DesktopWindowLayout -Path "$HOME\work-layout.json" -Preview -RestoreVirtualDesktops
Restore-DesktopWindowLayout -Path "$HOME\work-layout.json" -RestoreVirtualDesktops -PassThru
```

Virtual desktop capture and restoration use an experimental, undocumented Windows
shell contract. The adapter checks Windows build families 26100, 26200 and 26300
and the exact required COM interface IDs. An unsupported build or unavailable
interface produces a capture warning or a reported restore failure. The stable
`VirtualDesktopService` remains limited to Microsoft's public window operations.

Capture includes application windows on inactive virtual desktops. Layouts store
desktop GUIDs; deleting or recreating a desktop does not redirect its windows to
another desktop at the same index. Ordinary layout files remain readable, and
ordinary save/restore calls continue to restore geometry without shell recovery.

Only uniquely identified windows are restored. Process and class names constrain
matches after a process restarts. Explorer filesystem paths distinguish folder
windows with otherwise identical titles. Each live window is used once and its
identity is checked again before mutation. Applications must already be running;
DesktopManager does not relaunch arbitrary executables or restore their documents.

The workspace engine preserves a minimized or maximized window's normal bounds,
including taskbar workspace-coordinate conversion. It does not request a switch
of the active virtual desktop.

## Explicit optional actions

```powershell
# List live desktop identities, names and wallpaper paths.
Get-DesktopVirtualDesktop -List -Experimental

# Permit changes to pins that affect every window of an application.
Restore-DesktopWindowLayout -Path "$HOME\work-layout.json" -RestoreVirtualDesktops -RestoreApplicationPins -PassThru

# Reopen missing filesystem folders in Explorer; other applications are not started.
Restore-DesktopWindowLayout -Path "$HOME\work-layout.json" -ReopenExplorerFolders -PassThru

# Restore only explicitly saved per-desktop wallpaper choices alongside window placement.
Restore-DesktopWindowLayout -Path "$HOME\work-layout.json" -RestoreWallpapers -PassThru
```

Application pin restoration requires virtual desktop restoration as well as the
separate application-pin option. The saved application identity must match.
Missing Explorer folders are reported. Newly opened folders may initially appear
as missing; app startup recovery retries while their windows are opening.

Wallpaper restoration requires existing absolute image paths and surviving desktop
GUIDs. It applies through the live shell and checks the reported result. Duplicate
images are valid choices. Recovery does not disable slideshow, change its registry
configuration, infer user intent or automatically "heal" wallpaper changes.

## Desktop app

Open **Workspace** to capture a snapshot, preview matches, inspect desktops and
restore selected state. Recovery settings are off by default. Capture a complete
snapshot before enabling periodic capture. Failed or empty manual capture preserves
the previous app snapshot.

**Restore when DesktopManager starts** retries only missing windows, up to twelve
attempts separated by two seconds. Already restored and ambiguous windows are not
reapplied. Closing the app cancels pending retries. Enable the existing **Startup**
switch separately to launch the app at Windows sign-in; saving recovery settings
does not create another scheduled task or startup entry.

Periodic capture runs every five minutes after a successful recovery or manual
capture. It pauses for locked sessions, incomplete recovery, capture warnings and
snapshots with fewer windows. It does not capture during app shutdown. Wallpapers
are restored only by a manually selected restore action, never by startup recovery.

The default settings and snapshot live under
`%LOCALAPPDATA%\Evotec\DesktopManager\Workspace`. An explicit alternate workspace
keeps recovery files and a separate hotkey profile together:

```text
DesktopManager.App.exe --workspace-directory "C:\Workspaces\Coding"
```

## CLI and MCP

```text
desktopmanager workspace capture --path "C:\Workspaces\coding.json" --virtual-desktops --explorer-folders
desktopmanager workspace preview --path "C:\Workspaces\coding.json" --virtual-desktops
desktopmanager workspace restore --path "C:\Workspaces\coding.json" --virtual-desktops
desktopmanager workspace desktops
```

CLI output is structured JSON. Exit code 2 indicates incomplete capture, preview
or restoration. `--application-pins`, `--reopen-explorer` and `--wallpapers` enable
their corresponding optional restore actions.

MCP exposes `capture_workspace`, `preview_workspace`, `restore_workspace` and
`get_workspace_desktops`. This tool group requires `--allow-experimental`.
Capture also requires `--allow-mutations`; restoration additionally requires
`--allow-system-settings`. Workspace restoration cannot be constrained by process
allow/deny filters, so it is blocked in a process-filtered session. `--dry-run`
previews an allowed mutation without writing files or changing the desktop.

## C#

```csharp
var recovery = new WorkspaceRecoveryService();
WindowLayout layout = recovery.Capture(new WorkspaceCaptureOptions {
    IncludeVirtualDesktops = true,
    IncludeExplorerFolders = true
});
WorkspaceRecoveryService.Write("coding.json", layout);
WindowLayoutPlan plan = recovery.Plan(layout, inspectVirtualDesktops: true);
WorkspaceRestoreResult result = await recovery.RestoreAsync(layout,
    new WorkspaceRestoreOptions { RestoreVirtualDesktops = true },
    cancellationToken: cancellationToken);
```

`ExperimentalVirtualDesktopService` instances are thread-affine and disposable.
The recovery service opens and closes a shell connection within each synchronous
attempt, so asynchronous startup retries do not retain a COM object across awaits.
