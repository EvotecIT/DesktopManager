# Portable desktop integration

`PortableDesktopService` gives image generators one .NET entry point for display discovery,
current-user session diagnostics, wallpaper readback, and assigning a local wallpaper file.
It selects Windows, macOS, GNOME, or KDE Plasma from the current operating system and user session.
Discovery never changes desktop settings.

```csharp
using DesktopManager;

var desktop = new PortableDesktopService();
DesktopCapabilities capabilities = desktop.GetCapabilities();
DesktopUserSession session = desktop.GetSession();
Console.WriteLine($"{capabilities.BackendId}: {capabilities.Message}");
Console.WriteLine($"Process account: {session.AccountName}; graphical session: {session.HasGraphicalSession}");

foreach (DesktopDisplayInfo display in desktop.GetDisplays()) {
    Console.WriteLine($"{display.Id}: {display.Width} x {display.Height} pixels");
}

DesktopWallpaperState previous = desktop.GetWallpaper();
if (previous.Succeeded) {
    Console.WriteLine(previous.FilePath ?? "The desktop has no readable local image.");
}
```

Assigning an image is a separate explicit operation:

```csharp
DesktopOperationResult result = desktop.ApplyWallpaper(new DesktopWallpaperRequest {
    FilePath = imagePath,
    Position = DesktopWallpaperPosition.Fill
});
if (!result.Succeeded) {
    Console.Error.WriteLine($"{result.Status}: {result.Message}");
}
```

The caller owns image generation, refresh scheduling, image retention, and deciding when restoration
is appropriate. A successful result means that the native API or desktop command completed. It does
not prove that a management policy allowed the visible change. Readback reports a path; it does not
promise that an image decoder supports that file's format. For example, macOS may return a HEIC image.
GNOME reports a successful read with no image path when `picture-options` is `none`, even if an image
URI remains configured. KDE image-package directories return `Unsupported` with a diagnostic;
the adapter does not resolve the effective image inside a package. Ordinary KDE image files remain readable.

For composition, inspect `previous.Background` and `previous.ImageOpacity` as well as the image and fit.
`Background` describes a solid color or a horizontal/vertical gradient. Its primary color is the solid,
left or top color; its secondary color is the right or bottom color of a gradient. Windows and macOS
return `#RRGGBB`; GNOME returns the configured CSS color string. Unsupported color syntax needs a
diagnostic in the consuming renderer. A null background or opacity means unknown; it is not a default
black color or a promise of full opacity. Disabled retained images are not exposed as `FilePath`.

## Targets and preserving fit

Wallpaper targets and physical pixel geometry are separate capabilities. `GetWallpaperTargets()`
discovers a GNOME desktop-wide target and the current KDE activity's `screen:<number>` targets even
when `GetDisplays()` cannot provide qualified image dimensions. A target's `Display` is null in that
case: the caller must supply dimensions. `DisplayId == null` means desktop-wide assignment.

`SupportedWallpaperPositions` reports the exact accepted modes for the current backend. To preserve
the existing fit, read each target's state and use its known `Position`; do not silently replace an
unknown or unsupported mode with Fill. Windows fit is global, even for a targeted image assignment.
KDE targets also carry a `ScopeId`. Pass it as `ExpectedScopeId` to reject an activity change before
the first assignment rather than painting a newly selected activity.

```csharp
foreach (DesktopWallpaperTarget target in desktop.GetWallpaperTargets()) {
    DesktopWallpaperState current = desktop.GetWallpaper(target.DisplayId);
    Console.WriteLine($"{target.Name}: {current.Status}; fit: {current.Position}");
    // Generate an image only after deciding how to handle missing source, color or geometry.
    // An eventual assignment uses target.DisplayId, target.ScopeId and current.Position.
}
```

## Bounded restoration

For a temporary operation, capture native state before the first write and restore it in `finally`:

```csharp
using DesktopWallpaperSnapshot checkpoint = desktop.CaptureWallpaperState();
if (!checkpoint.Succeeded) {
    throw new InvalidOperationException(checkpoint.Message);
}
try {
    // Perform the explicitly requested temporary assignment and inspect its result.
} finally {
    DesktopOperationResult restored = desktop.RestoreWallpaperState(checkpoint);
    if (!restored.Succeeded) {
        Console.Error.WriteLine($"{restored.Status}: {restored.Message}");
    }
}
```

The checkpoint is opaque, in memory and disposable. Restore it through the same service instance
while its original graphical session and targets are available. It is not a persisted backup or
a reboot recovery format. A failed preflight avoids writes; a native failure after writes begin
is reported as incomplete restoration while the adapter attempts the remaining settings.

Windows captures native per-monitor images (including an empty image), shared fit/color, slideshow
items/options and enabled state. Its selected Explorer background mode is captured separately from
retained COM paths. A solid-color desktop with retained picture assignments returns `Unsupported`
for checkpoint capture: restoring those paths alone can display a hidden photo, while clearing them
can discard its retained configuration. Spotlight and absent/unknown selected modes are also
unsupported checkpoints. A successful wallpaper read can still describe a solid background even
when its checkpoint cannot be qualified. Explicit image application synchronizes Explorer's selected
image mode after the native assignment. This Windows mode integration requires native qualification
on the target Windows version; registry values alone do not prove visible acceptance.

GNOME captures the effective light/dark URI and picture-options
values, including `none`; it restores effective values, not the distinction between schema defaults
and explicit user overrides. KDE captures the current activity's plugins and static image/fit keys,
including key absence. macOS captures active screen image URLs and full native AppKit options;
it does not enumerate all Spaces or identify a switch to another Space. Do not switch Spaces while
using a checkpoint. Missing original files, changed topology or a changed session can prevent restoration.

Successful restoration describes native completion/readback as reported by the backend. Explorer
can finish its wallpaper transition after readback has settled. Validate the visible result separately;
an immediate screenshot can still show the outgoing image.

## Platform capabilities

| Backend | Displays | Wallpaper scope | Fit modes and limits |
| --- | --- | --- | --- |
| Windows | Existing DesktopManager physical-pixel snapshots | Current user's displays; optional addressable display ID | Existing Center, Tile, Stretch, Fit, Fill and Span modes; Windows shares the fit setting across monitors |
| macOS | Public AppKit screens and CoreGraphics pixel dimensions | Current user's active Spaces/screens; optional display ID | Center, Stretch, Fit and Fill; Tile, Span and enumerating all Spaces are unsupported |
| GNOME on X11 or Wayland | Physical display enumeration is not qualified; supply image dimensions | Current user background; individual display assignment is unsupported | GNOME's centered, tiled, stretched, scaled, zoom and spanned settings |
| KDE Plasma | Physical display enumeration is unavailable; supply image dimensions | Current activity; optional native `screen:<number>` target | Static image wallpaper; Center, Tile, Stretch, Fit and Fill; Span is unsupported |
| Other or headless sessions | Empty when unavailable | Explicit unavailable/unsupported result | File rendering can run independently of desktop integration |

Display IDs describe the current topology. Refresh discovery after docking, reconnecting, or changing
displays. `Left` and `Top` retain the platform's desktop coordinate system: macOS uses points and an
upward Y axis; Windows and X11 retain their native desktop coordinates. `Width` and `Height` are image
pixel dimensions. Do not assume that subtracting desktop coordinates reproduces the image dimensions
on scaled displays. Linux display geometry is not exposed: X11 transforms and scaling can make
`xrandr` desktop geometry differ from the physical image pixel size, and Wayland needs its own
qualified display protocol.

`GetDisplays` returns an empty list when enumeration is unsupported. A failed native read throws so
callers can report a diagnostic instead of inventing a monitor. `GetWallpaper` and `ApplyWallpaper`
return `Success`, `Unsupported`, `Unavailable`, or `Failed`. Missing files and invalid fit values fail
before the first desktop write. No files are downloaded and no platform tools are installed.

On Unix, `AccountName` identifies the process account. It does not establish that the user has a
graphical login; inspect `HasGraphicalSession`. Both Unix adapters reject SSH contexts. macOS also
requires the console user's account; Linux requires display/session-bus context. These checks do not authorize
cross-user operations.

## Native facilities and policy

Windows delegates to the existing monitor and session implementations. Targeted requests use the
owner's strict display operations: native failures are reported without applying the image to every
monitor or substituting another display's readback. Anonymous snapshot IDs remain useful for geometry
but cannot select an individual image. `SupportsPerDisplayWallpaper` is false when no snapshot has an
addressable wallpaper device path. The legacy Windows setters keep their existing session fallback.
Native Windows operations retain platform annotations. Portable contracts and detached values
are available through the `netstandard2.0` package asset; selecting that asset does not make the other
Windows APIs portable. .NET Framework 4.7.2 and Windows-targeted assets retain their existing role.

macOS uses the system-provided JavaScript for Automation host with direct public AppKit calls. It
does not automate Finder or System Events, modify private wallpaper databases, or install a helper.
The AppKit operation runs in its own main-thread process. Its scope is the active desktop on each
selected screen; all-Spaces behavior requires separate qualification.

GNOME uses the session's `gsettings` command and background schema. Readback selects the active
light/dark image. Assignment updates both image keys when present and checks that every affected key
is writable before changing settings. Locked settings return an unavailable result. A race or native
failure can still interrupt a multi-key update; the result must be checked.

KDE uses the existing `qdbus6` or `qdbus` platform command and Plasma's documented desktop scripting
API. Readback reports unsupported when the selected desktop uses a non-image wallpaper plugin;
assigning an image explicitly selects the static image plugin. Other activities are unchanged.
Targeted application checks the supplied current-activity scope and all selected desktops' lock state
before writes. KDE image-package resolution and automatic physical geometry remain unsupported.
Desktop administration policy can disable scripting.

Platform subprocesses run without a shell. Argument data is passed separately from scripts, their
capture is limited to 1,048,576 characters per stream, and each command has one 15-second deadline
covering both process exit and output draining. A descendant retaining an output pipe cannot extend
that deadline.

Lock screen, all-user settings and slideshow management are not portable capabilities. Applications
that need the existing Windows variants can use the established Windows APIs behind a platform
guard. Scheduling belongs to the consuming application: use an interactive user task on Windows,
a graphical-session LaunchAgent/login helper on macOS, or user-session integration on Linux.

## Validation

Run the portable .NET 8 contract and read-only native probes on Windows, macOS, or Linux:

```shell
dotnet test Sources/DesktopManager.Portable.Tests/DesktopManager.Portable.Tests.csproj -c Release --property:EnableWindowsTargeting=true
```

The suite covers desktop command boundaries, explicit unsupported results, GNOME policy locks and
light/dark selection, strict Windows target failures, inherited output pipes, and loading the real facade. On an available macOS graphical
session it reads real screens and the current wallpaper. No test changes wallpaper or schedules a
background task. Native application, visible results, restoration, multi-display behavior, and
GNOME/KDE desktop sessions require their own acceptance runs; a headless CI pass does not prove them.

Public platform contracts: [Windows wallpaper](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-idesktopwallpaper-setwallpaper),
[Apple NSWorkspace](https://developer.apple.com/documentation/appkit/nsworkspace),
[GNOME backgrounds](https://help.gnome.org/system-admin-guide/desktop-background.html),
and [KDE Plasma scripting](https://develop.kde.org/docs/plasma/scripting/api/).
