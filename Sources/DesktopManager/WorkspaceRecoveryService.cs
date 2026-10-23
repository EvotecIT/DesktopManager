using System.IO;
using System.Linq;
using System.Text.Json;

namespace DesktopManager;

/// <summary>Captures and restores desktop-aware window layouts through the existing layout planner.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed partial class WorkspaceRecoveryService {
    private readonly IWorkspaceWindowApi _windows;
    private readonly Func<IWorkspaceDesktopApi> _desktops;

    /// <summary>Creates the Windows workspace recovery service. Experimental shell connections are opened only when explicitly requested.</summary>
    public WorkspaceRecoveryService() : this(new WorkspaceWindowApi(), () => new ExperimentalVirtualDesktopService()) { }

    internal WorkspaceRecoveryService(IWorkspaceWindowApi windows, Func<IWorkspaceDesktopApi> desktops) {
        _windows = windows;
        _desktops = desktops;
    }

    /// <summary>Captures visible top-level windows, including other virtual desktops. Optional shell and Explorer data are opt-in.</summary>
    public WindowLayout Capture(WorkspaceCaptureOptions? options = null) {
        options ??= new WorkspaceCaptureOptions();
        var layout = new WindowLayout { CapturedAt = DateTimeOffset.UtcNow };
        layout.Windows.AddRange(_windows.Capture(options.IncludeExplorerFolders));
        if (options.IncludeVirtualDesktops) {
            try {
                using IWorkspaceDesktopApi desktops = _desktops();
                layout.Desktops.AddRange(desktops.GetDesktops());
                foreach (WindowPosition window in layout.Windows.ToArray()) {
                    try { desktops.CaptureWindow(window); }
                    catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x8002802B)) {
                        // Shell infrastructure such as the input panel is not an application view.
                        layout.Windows.Remove(window);
                    }
                    catch (Exception ex) {
                        window.VirtualDesktopId = null; window.PinMode = null; window.AppUserModelId = string.Empty;
                        layout.CaptureWarnings.Add($"{window.Title}: desktop capture failed: {ex.Message}");
                    }
                }
            } catch (Exception ex) { layout.CaptureWarnings.Add($"Virtual desktop capture unavailable: {ex.Message}"); }
        }
        return layout;
    }

    /// <summary>Saves a snapshot atomically. Explicit capture replaces the selected snapshot even when fewer windows are open.</summary>
    public void Save(string path, WorkspaceCaptureOptions? options = null) => Write(path, Capture(options));

    /// <summary>Writes an existing layout atomically, without capturing or changing the desktop.</summary>
    public static void Write(string path, WindowLayout layout) {
        Validate(layout);
        AtomicFileWriter.WriteAllText(path, JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Reads a layout without changing windows. Legacy layouts need no migration.</summary>
    public static WindowLayout Read(string path, bool validate = false) {
        WindowLayout? layout;
        try { layout = JsonSerializer.Deserialize<WindowLayout>(File.ReadAllText(path)); }
        catch (JsonException ex) { throw new InvalidDataException("The workspace file is not a valid window layout.", ex); }
        if (layout == null) { throw new InvalidDataException("The workspace file contains no layout."); }
        Validate(layout);
        if (validate) { WindowManager.ValidateLayout(layout); }
        return layout;
    }

    /// <summary>Previews one-to-one window matches without launching applications or changing windows.</summary>
    public WindowLayoutPlan Plan(WindowLayout layout, bool inspectVirtualDesktops = false) {
        return PlanCore(layout, inspectVirtualDesktops, null);
    }

    private WindowLayoutPlan PlanCore(WindowLayout layout, bool inspectVirtualDesktops, IReadOnlyList<WindowPosition>? reserved) {
        Validate(layout);
        IReadOnlyList<WindowPosition> live = _windows.Capture(layout.Windows.Any(window => !string.IsNullOrEmpty(window.ExplorerFolderPath)));
        // Keep a window assigned across the whole startup operation, not merely within one attempt.
        // Process and thread identity prevent a recycled HWND in another process/thread being excluded.
        if (reserved != null) {
            live = live.Where(window => !reserved.Any(previous => previous.Handle == window.Handle &&
                previous.ProcessId == window.ProcessId && previous.ThreadId == window.ThreadId)).ToArray();
        }
        WindowLayoutPlan plan = WindowManager.BuildLayoutPlan(layout, live);
        plan.Warnings.AddRange(layout.CaptureWarnings);
        if (inspectVirtualDesktops) {
            try {
                using IWorkspaceDesktopApi desktops = _desktops();
                var available = new HashSet<Guid>(desktops.GetDesktops().Select(desktop => desktop.Id));
                foreach (Guid id in layout.Windows.Where(window => window.VirtualDesktopId.HasValue).Select(window => window.VirtualDesktopId!.Value)
                        .Concat(layout.Desktops.Select(desktop => desktop.Id)).Distinct()) {
                    if (!available.Contains(id)) { plan.Warnings.Add($"Saved virtual desktop {id} no longer exists; no desktop-number fallback is used."); }
                }
            } catch (Exception ex) { plan.Warnings.Add($"Virtual desktop inspection unavailable: {ex.Message}"); }
        }
        return plan;
    }

    /// <summary>Restores uniquely matched windows and explicitly selected optional state. Missing or ambiguous targets are reported and skipped.</summary>
    public WorkspaceRestoreResult Restore(WindowLayout layout, WorkspaceRestoreOptions? options = null) {
        return RestoreCore(layout, options, null);
    }

    private WorkspaceRestoreResult RestoreCore(WindowLayout layout, WorkspaceRestoreOptions? options, IReadOnlyList<WindowPosition>? reserved) {
        Validate(layout);
        options ??= new WorkspaceRestoreOptions();
        if (options.RestoreApplicationPins && !options.RestoreVirtualDesktops) {
            throw new ArgumentException("Application-pin recovery also requires virtual-desktop recovery.", nameof(options));
        }
        var result = new WorkspaceRestoreResult();
        result.Warnings.AddRange(layout.CaptureWarnings);
        if (options.RestoreWallpapers && layout.Desktops.Count == 0) { result.Warnings.Add("This layout contains no saved virtual desktop wallpapers."); }
        if (options.ReopenExplorerFolders) { ReopenFolders(layout, result); }
        WindowLayoutPlan plan = PlanCore(layout, false, reserved);
        IWorkspaceDesktopApi? desktops = null;
        string? desktopError = null;
        IReadOnlyList<VirtualDesktopInfo> liveDesktops = Array.Empty<VirtualDesktopInfo>();
        try {
            if (options.RestoreVirtualDesktops || options.RestoreWallpapers) {
                try { desktops = _desktops(); liveDesktops = desktops.GetDesktops(); }
                catch (Exception ex) { desktopError = ex.Message; }
            }
            foreach (WindowLayoutMatch match in plan.Windows) {
                var item = new WorkspaceWindowResult { Title = match.SavedWindow.Title, MatchStatus = match.Status, ResolvedWindow = match.Window };
                result.Windows.Add(item);
                if (match.Window == null) { continue; }
                // Revalidate the live HWND before any desktop, pin or geometry mutation.
                try { _windows.VerifyIdentity(match.Window); }
                catch (Exception ex) { item.Error = ex.Message; continue; }
                if (options.RestoreVirtualDesktops) {
                    item.VirtualDesktopApplied = false;
                    try {
                        if (desktopError != null) { throw new InvalidOperationException(desktopError); }
                        if (match.SavedWindow.PinMode == null && !match.SavedWindow.VirtualDesktopId.HasValue) {
                            throw new InvalidOperationException("This layout has no saved virtual desktop state for this window.");
                        }
                        if (match.SavedWindow.PinMode != VirtualDesktopPinMode.Window && match.SavedWindow.PinMode != VirtualDesktopPinMode.Application &&
                                !liveDesktops.Any(desktop => desktop.Id == match.SavedWindow.VirtualDesktopId)) {
                            throw new InvalidOperationException("The saved virtual desktop no longer exists. No ordinal fallback was used.");
                        }
                        desktops!.RestoreWindow(match.SavedWindow, match.Window.Handle, options.RestoreApplicationPins);
                        item.VirtualDesktopApplied = true;
                    } catch (Exception ex) { item.Error = ex.Message; }
                }
                WindowLayoutApplyResult placement = _windows.Apply(match);
                item.PositionApplied = placement.Applied;
                if (placement.Error != null) { item.Error = item.Error == null ? placement.Error : item.Error + " " + placement.Error; }
            }
            if (options.RestoreWallpapers) {
                foreach (VirtualDesktopInfo saved in layout.Desktops) {
                    var wallpaper = new VirtualDesktopWallpaperResult { DesktopId = saved.Id };
                    result.Wallpapers.Add(wallpaper);
                    try {
                        if (desktopError != null) { throw new InvalidOperationException(desktopError); }
                        if (!liveDesktops.Any(desktop => desktop.Id == saved.Id)) { throw new InvalidOperationException("The saved virtual desktop no longer exists."); }
                        desktops!.SetWallpaper(saved.Id, saved.WallpaperPath);
                        wallpaper.Applied = true;
                    } catch (Exception ex) { wallpaper.Error = ex.Message; }
                }
            }
        } finally { desktops?.Dispose(); }
        return result;
    }

    private void ReopenFolders(WindowLayout layout, WorkspaceRestoreResult result) {
        var open = new HashSet<string>(_windows.Capture(true).Select(window => window.ExplorerFolderPath), StringComparer.OrdinalIgnoreCase);
        foreach (string path in layout.Windows.Select(window => window.ExplorerFolderPath).Where(path => !string.IsNullOrEmpty(path)).Distinct(StringComparer.OrdinalIgnoreCase)) {
            if (!open.Add(path)) { continue; }
            try { _windows.OpenExplorer(path); }
            catch (Exception ex) { result.Warnings.Add($"Explorer folder '{path}': {ex.Message}"); }
        }
        // The caller's bounded startup retries handle windows that have not opened yet; no blocking sleep.
    }

    internal static void Validate(WindowLayout layout) {
        if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
        if (layout.Windows == null || layout.Windows.Any(window => window == null) || layout.Desktops == null || layout.CaptureWarnings == null) {
            throw new InvalidDataException("The workspace must contain non-null window, desktop and warning collections.");
        }
        if (layout.Desktops.Any(desktop => desktop == null || desktop.Id == Guid.Empty) || layout.Desktops.Select(desktop => desktop.Id).Distinct().Count() != layout.Desktops.Count) {
            throw new InvalidDataException("The workspace contains an empty or duplicate desktop identity.");
        }
        if (layout.Windows.Any(window => window.PinMode.HasValue && !Enum.IsDefined(typeof(VirtualDesktopPinMode), window.PinMode.Value))) {
            throw new InvalidDataException("The workspace contains an invalid pin mode.");
        }
    }
}

internal interface IWorkspaceWindowApi {
    IReadOnlyList<WindowPosition> Capture(bool includeExplorerFolders);
    void VerifyIdentity(WindowPosition window);
    WindowLayoutApplyResult Apply(WindowLayoutMatch match);
    void OpenExplorer(string path);
}

internal sealed class WorkspaceWindowApi : IWorkspaceWindowApi {
    private readonly WindowManager _manager = new();

    public IReadOnlyList<WindowPosition> Capture(bool includeExplorerFolders) {
        Dictionary<IntPtr, string> folders = includeExplorerFolders ? WorkspaceExplorerFolders.Read() : new();
        var result = new List<WindowPosition>();
        foreach (WindowInfo window in _manager.GetWindows(new WindowQueryOptions { IncludeCloaked = true, IncludeOwned = false, IncludeEmptyTitles = false })) {
            try {
                WindowPosition position = _manager.GetWindowPosition(window);
                position.ThreadId = window.ThreadId;
                WorkspaceWindowPlacement.Capture(position);
                if (folders.TryGetValue(window.Handle, out string? folder)) { position.ExplorerFolderPath = folder; }
                result.Add(position);
            } catch (InvalidOperationException) { /* Window closed during capture. */ }
        }
        return result;
    }

    public void VerifyIdentity(WindowPosition window) {
        WindowPosition live = _manager.GetWindowPosition(window);
        if (live.ProcessId != window.ProcessId || live.ClassName != window.ClassName ||
                !string.Equals(live.ProcessName, window.ProcessName, StringComparison.OrdinalIgnoreCase) ||
                WindowTextHelper.GetWindowText(window.Handle) != window.Title) {
            throw new InvalidOperationException("The target window identity changed after workspace planning.");
        }
        if (!string.IsNullOrEmpty(window.ExplorerFolderPath) &&
                (!WorkspaceExplorerFolders.Read().TryGetValue(window.Handle, out string? path) ||
                 !string.Equals(path, window.ExplorerFolderPath, StringComparison.OrdinalIgnoreCase))) {
            throw new InvalidOperationException("The target Explorer window navigated after workspace planning.");
        }
    }

    public WindowLayoutApplyResult Apply(WindowLayoutMatch match) {
        try { VerifyIdentity(match.Window!); }
        catch (Exception ex) { return new WindowLayoutApplyResult(match, false, ex.Message); }
        return WorkspaceWindowPlacement.Apply(_manager, match);
    }
    public void OpenExplorer(string path) => WorkspaceExplorerFolders.Open(path);
}
