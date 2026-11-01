using System.Runtime.InteropServices;

namespace DesktopManager.AotSmoke;

internal static class SmokeChecks {
    internal static void Run(string area) {
        switch (area) {
            case "monitors":
                var monitors = new Monitors().GetMonitorSnapshots();
                if (monitors.Count == 0) { throw new UnavailableException("No attached display geometry in this session."); }
                Require(monitors.All(m => m.Width > 0 && m.Height > 0), "Attached displays must have positive bounds.");
                Require(monitors.Select(m => m.Index).Distinct().Count() == monitors.Count, "Monitor indices must be unique.");
                Console.WriteLine($"Observed {monitors.Count} monitors; {monitors.Count(m => m.DpiX.HasValue)} expose DPI.");
                break;
            case "wallpaper":
                var state = new PortableDesktopService().GetWallpaper();
                CheckResult(state);
                Require(state.Position.HasValue && Enum.IsDefined(state.Position.Value), "Wallpaper fit must be a defined value.");
                Require(state.Background != null, "Successful wallpaper reads must expose background metadata.");
                Console.WriteLine($"Wallpaper read: fit={state.Position}, image assigned={state.FilePath != null}; paths omitted.");
                break;
            case "slideshow":
                NativeFixtures.ReadShellItemArray();
                var slideshow = new Monitors().GetWallpaperSlideshow();
                Require(slideshow.ImagePaths.All(path => !string.IsNullOrWhiteSpace(path)), "Slideshow items must have paths.");
                if (slideshow.IsRunning) { Require(slideshow.ImagePaths.Count > 0, "Running slideshow must expose items."); }
                Console.WriteLine($"Slideshow: state={slideshow.State}, options={slideshow.Options}, tick={slideshow.SlideshowTick}, items={slideshow.ImagePaths.Count}.");
                if (slideshow.ImagePaths.Count == 0) { Console.WriteLine("Live slideshow has no items; Shell item conversion is qualified separately by the owned fixture."); }
                break;
            case "checkpoint":
                using (var checkpoint = new PortableDesktopService().CaptureWallpaperState()) {
                    CheckResult(checkpoint);
                    Require(checkpoint.BackendId == "windows", "Checkpoint must belong to the Windows backend.");
                    Console.WriteLine("Wallpaper checkpoint captured; Dispose only, no restore.");
                }
                break;
            case "clipboard":
                NativeFixtures.DuplicateClipboardMemory();
                // Friend access exercises the actual preservation boundary without sending input or restoring data.
                uint sequence = GetClipboardSequenceNumber();
                int formats = CountClipboardFormats();
                using (ClipboardHelper.CaptureSnapshot()) { }
                uint after = GetClipboardSequenceNumber();
                Console.WriteLine("Clipboard snapshot captured and disposed; no contents logged, no restore.");
                if (sequence != after) { throw new UnavailableException("Clipboard sequence changed during capture (external update or delayed rendering); unchanged-state observation is inconclusive."); }
                Console.WriteLine($"Observed {formats} clipboard formats with an unchanged sequence number.");
                break;
            case "explorer":
                var folders = WorkspaceExplorerFolders.Read();
                Require(folders.All(pair => pair.Key != IntPtr.Zero && Path.IsPathRooted(pair.Value) && Directory.Exists(pair.Value)), "Explorer results must be live filesystem folders with window handles.");
                Console.WriteLine($"Explorer capture: {folders.Count} filesystem folder windows; paths omitted.");
                if (folders.Count == 0) { throw new UnavailableException("Explorer capture returned no filesystem folder results; this does not establish that no folders are open. Live browser/view conversion remains unqualified. No Explorer windows were opened."); }
                break;
            case "explorer-fixture":
                ExplorerNativeFixture.Run();
                break;
            case "virtual-desktops":
                try {
                    using var desktops = new ExperimentalVirtualDesktopService();
                    var items = desktops.GetDesktops();
                    Require(items.Count > 0 && items.All(d => d.Id != Guid.Empty), "Live shell must expose nonempty desktop IDs.");
                    Require(items.Select(d => d.Id).Distinct().Count() == items.Count, "Desktop IDs must be unique.");
                    Console.WriteLine($"Read {items.Count} virtual desktop IDs/names/wallpapers; values omitted. No desktop changes.");
                } catch (PlatformNotSupportedException ex) { throw new UnavailableException(ex.Message); }
                break;
            case "audio":
                var endpoints = new AudioService().GetEndpoints();
                Require(endpoints.All(e => !string.IsNullOrWhiteSpace(e.Id) && !string.IsNullOrWhiteSpace(e.Name)), "Audio endpoints must have stable IDs and names.");
                Require(endpoints.Select(e => e.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == endpoints.Count, "Audio endpoint IDs must be unique.");
                Require(endpoints.All(e => e.VolumePercent == null || (float.IsFinite(e.VolumePercent.Value) && e.VolumePercent >= 0 && e.VolumePercent <= 100)), "Available audio volume must be finite and in range.");
                foreach (var flow in new[] { AudioDataFlow.Render, AudioDataFlow.Capture }) {
                    foreach (AudioRole role in Enum.GetValues<AudioRole>()) {
                        Require(endpoints.Count(e => e.DataFlow == flow && e.DefaultRoles.Contains(role)) <= 1, "A flow/role may have at most one default endpoint.");
                    }
                }
                Console.WriteLine($"Read {endpoints.Count} endpoints, {endpoints.Count(e => e.IsDefault)} defaults, {endpoints.Count(e => e.VolumePercent.HasValue)} volumes; no writes.");
                using (new AudioEndpointWatcher()) { }
                Console.WriteLine("Core Audio accepted generated callback registration and disposal; no endpoint changes.");
                if (endpoints.Count == 0 || !endpoints.Any(e => e.IsDefault)) { throw new UnavailableException("Audio enumeration completed; no default endpoint available to qualify default-role reads."); }
                break;
            case "uia":
                using (var harness = new NativeWindowHarness()) {
                    var controls = new DesktopAutomationService().GetControls(
                        new WindowQueryOptions { Handle = harness.Handle, ProcessId = Environment.ProcessId, IncludeHidden = true },
                        new WindowControlQueryOptions { UseUiAutomation = true, IncludeUiAutomation = false, EnsureForegroundWindow = false, AllowForegroundInputFallback = false });
                    var edits = controls.Where(c => c.Control.Source == WindowControlSource.UiAutomation && c.Control.Handle == harness.EditHandle).ToArray();
                    Require(edits.Length == 1, "UIA must independently discover the owned edit control, without Win32 fallback.");
                    var edit = edits[0].Control;
                    Require(edit.Value == NativeWindowHarness.EditText, "UIA ValuePattern must read the known edit value.");
                    Require(!string.IsNullOrEmpty(edit.RuntimeId) && !string.IsNullOrEmpty(edit.ControlType), "UIA runtime identity and control type must survive native marshalling.");
                    Require(controls.Any(c => c.Control.Source == WindowControlSource.UiAutomation && c.Control.Handle == harness.ButtonHandle && c.Control.Text == NativeWindowHarness.ButtonText), "UIA must read the owned button name.");
                    using var changed = new System.Threading.ManualResetEvent(false);
                    using (var subscription = new UiAutomationControlService().TrySubscribeToChanges(harness.Handle, () => changed.Set())) {
                        Require(subscription != null, "UIA must register generated event callbacks for the owned native controls.");
                        harness.SetOwnedEditText("Owned event callback proof");
                        Require(UiAutomationControlService.WaitForSignalWithCurrentUiMessagePump(changed, 5000), "UIA must deliver an event after the owned edit value changes.");
                    }
                    Console.WriteLine($"UIA read {controls.Count} owned controls and delivered a generated callback after changing only its owned edit; subscription disposed, no activation/input.");
                }
                break;
            default: throw new ArgumentOutOfRangeException(nameof(area));
        }
    }

    private static void CheckResult(DesktopOperationResult result) {
        if (result.Status is DesktopOperationStatus.Unavailable or DesktopOperationStatus.Unsupported) { throw new UnavailableException($"{result.Status}: {result.Message}"); }
        Require(result.Succeeded, result.Message);
    }

    private static void Require(bool condition, string message) {
        if (!condition) { throw new InvalidOperationException(message); }
    }

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")]
    private static extern int CountClipboardFormats();
}
