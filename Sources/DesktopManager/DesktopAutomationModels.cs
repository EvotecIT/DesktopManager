using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace DesktopManager;

/// <summary>
/// Options for launching a desktop process.
/// </summary>
public sealed class DesktopProcessStartOptions {
    /// <summary>
    /// Gets or sets the executable path or shell command.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional argument string.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the optional working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the optional input-idle wait timeout in milliseconds.
    /// </summary>
    public int? WaitForInputIdleMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the optional time to wait for a launched window in milliseconds.
    /// </summary>
    public int? WaitForWindowMilliseconds { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the polling interval used while waiting for a launched window.
    /// </summary>
    public int? WaitForWindowIntervalMilliseconds { get; set; } = 200;

    /// <summary>
    /// Gets or sets an optional launched-window title filter. Supports wildcards.
    /// </summary>
    public string? WindowTitlePattern { get; set; }

    /// <summary>
    /// Gets or sets an optional launched-window class filter. Supports wildcards.
    /// </summary>
    public string? WindowClassNamePattern { get; set; }

    /// <summary>
    /// Gets or sets whether a launched window is required.
    /// </summary>
    public bool RequireWindow { get; set; }
}

/// <summary>
/// Represents a launched desktop process and its preferred main window.
/// </summary>
public sealed class DesktopProcessLaunchInfo {
    /// <summary>
    /// Gets or sets the process file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional argument string.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the optional working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the process identifier.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the resolved main-window process identifier when it differs from the launcher process.
    /// </summary>
    public int? ResolvedProcessId { get; set; }

    /// <summary>
    /// Gets or sets whether the process has already exited.
    /// </summary>
    public bool HasExited { get; set; }

    /// <summary>
    /// Gets or sets the preferred main window when one can be resolved.
    /// </summary>
    public WindowInfo? MainWindow { get; set; }
}

/// <summary>
/// Options for launching a desktop process and then waiting for a correlated window.
/// </summary>
public sealed class DesktopProcessLaunchAndWaitOptions {
    /// <summary>
    /// Gets or sets the executable path or shell command.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional argument string.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the optional working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the optional input-idle wait timeout in milliseconds.
    /// </summary>
    public int? WaitForInputIdleMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the optional launch-time window correlation wait in milliseconds.
    /// </summary>
    public int? LaunchWaitForWindowMilliseconds { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the polling interval used while correlating the launched window.
    /// </summary>
    public int? LaunchWaitForWindowIntervalMilliseconds { get; set; } = 200;

    /// <summary>
    /// Gets or sets an optional launch-time window title filter.
    /// </summary>
    public string? LaunchWindowTitlePattern { get; set; }

    /// <summary>
    /// Gets or sets an optional launch-time window class filter.
    /// </summary>
    public string? LaunchWindowClassNamePattern { get; set; }

    /// <summary>
    /// Gets or sets an optional final window title filter.
    /// </summary>
    public string? WindowTitlePattern { get; set; }

    /// <summary>
    /// Gets or sets an optional final window class filter.
    /// </summary>
    public string? WindowClassNamePattern { get; set; }

    /// <summary>
    /// Gets or sets whether hidden windows should be included during the final wait.
    /// </summary>
    public bool IncludeHidden { get; set; }

    /// <summary>
    /// Gets or sets whether windows with empty titles should be included during the final wait.
    /// </summary>
    public bool IncludeEmptyTitles { get; set; }

    /// <summary>
    /// Gets or sets whether all matching windows should be returned instead of the first match.
    /// </summary>
    public bool All { get; set; }

    /// <summary>
    /// Gets or sets whether the final wait may follow the launched app's same-name process family.
    /// </summary>
    public bool FollowProcessFamily { get; set; }

    /// <summary>
    /// Gets or sets the maximum final wait time in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the polling interval used during the final wait.
    /// </summary>
    public int IntervalMilliseconds { get; set; } = 200;
}

/// <summary>
/// Describes how a launch-and-wait workflow bound its final wait selector.
/// </summary>
public sealed class DesktopLaunchWaitBindingPlan {
    /// <summary>
    /// Gets or sets the final window query used for waiting.
    /// </summary>
    public WindowQueryOptions Criteria { get; set; } = new();

    /// <summary>
    /// Gets or sets the binding strategy identifier.
    /// </summary>
    public string WaitBinding { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bound process identifier when the wait is pinned to a specific process.
    /// </summary>
    public int? BoundProcessId { get; set; }

    /// <summary>
    /// Gets or sets the bound process-name family when the wait follows same-name processes.
    /// </summary>
    public string? BoundProcessName { get; set; }
}

/// <summary>
/// Represents a launch-and-wait workflow executed by the desktop automation core.
/// </summary>
public sealed class DesktopProcessLaunchAndWaitResult {
    /// <summary>
    /// Gets or sets whether the workflow resolved at least one matching window.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the total elapsed time in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the launch metadata.
    /// </summary>
    public DesktopProcessLaunchInfo Launch { get; set; } = null!;

    /// <summary>
    /// Gets or sets the final wait binding plan.
    /// </summary>
    public DesktopLaunchWaitBindingPlan WaitPlan { get; set; } = null!;

    /// <summary>
    /// Gets or sets the final wait result.
    /// </summary>
    public DesktopWindowWaitResult WindowWait { get; set; } = new();
}

/// <summary>
/// Represents terminating the process that owns a specific window.
/// </summary>
public sealed class DesktopProcessTerminationResult {
    /// <summary>
    /// Gets or sets the terminated process identifier.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the terminated process name when it could be resolved.
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the originating window title.
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the process exited within the requested wait time.
    /// </summary>
    public bool HasExited { get; set; }

    /// <summary>
    /// Gets or sets whether the termination request targeted the full process tree.
    /// </summary>
    public bool EntireProcessTree { get; set; }

    /// <summary>
    /// Gets or sets the exit wait timeout used after termination.
    /// </summary>
    public int WaitForExitMilliseconds { get; set; }
}

/// <summary>
/// Represents a wait operation for one or more windows.
/// </summary>
public sealed class DesktopWindowWaitResult {
    /// <summary>
    /// Gets or sets the elapsed wait time in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> Windows { get; set; } = Array.Empty<WindowInfo>();
}

/// <summary>
/// Represents a wait operation for one or more controls.
/// </summary>
public sealed class DesktopControlWaitResult {
    /// <summary>
    /// Gets or sets the elapsed wait time in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> Controls { get; set; } = Array.Empty<WindowControlTargetInfo>();
}

/// <summary>
/// Represents diagnostics for control discovery against a single window.
/// </summary>
public sealed class DesktopControlDiscoveryDiagnostics {
    /// <summary>
    /// Gets or sets the window being inspected.
    /// </summary>
    public WindowInfo Window { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the query requires UI Automation.
    /// </summary>
    public bool RequiresUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets whether the query explicitly requests UI Automation only.
    /// </summary>
    public bool UseUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets whether the query combines Win32 and UI Automation discovery.
    /// </summary>
    public bool IncludeUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets whether the query requested foreground preparation.
    /// </summary>
    public bool EnsureForegroundWindow { get; set; }

    /// <summary>
    /// Gets or sets whether UI Automation assemblies were available.
    /// </summary>
    public bool UiAutomationAvailable { get; set; }

    /// <summary>
    /// Gets or sets the total elapsed time for this diagnostic pass in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets whether shared window preparation was attempted before UI Automation discovery.
    /// </summary>
    public bool PreparationAttempted { get; set; }

    /// <summary>
    /// Gets or sets whether shared window preparation succeeded.
    /// </summary>
    public bool PreparationSucceeded { get; set; }

    /// <summary>
    /// Gets or sets the number of child handles considered as UI Automation fallback roots.
    /// </summary>
    public int UiAutomationFallbackRootCount { get; set; }

    /// <summary>
    /// Gets or sets whether child-handle fallback roots were used for UI Automation discovery.
    /// </summary>
    public bool UsedUiAutomationFallbackRoots { get; set; }

    /// <summary>
    /// Gets or sets whether cached UI Automation controls were reused during discovery.
    /// </summary>
    public bool UsedCachedUiAutomationControls { get; set; }

    /// <summary>
    /// Gets or sets whether a previously learned preferred UI Automation root was reused.
    /// </summary>
    public bool UsedPreferredUiAutomationRoot { get; set; }

    /// <summary>
    /// Gets or sets the preferred UI Automation root handle when one is known.
    /// </summary>
    public IntPtr PreferredUiAutomationRootHandle { get; set; }

    /// <summary>
    /// Gets or sets the effective discovery mode used for the final control set.
    /// </summary>
    public string EffectiveSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of Win32 controls discovered before filtering.
    /// </summary>
    public int Win32ControlCount { get; set; }

    /// <summary>
    /// Gets or sets the number of UI Automation controls discovered before filtering.
    /// </summary>
    public int UiAutomationControlCount { get; set; }

    /// <summary>
    /// Gets or sets the number of controls in the effective discovery set before filtering.
    /// </summary>
    public int EffectiveControlCount { get; set; }

    /// <summary>
    /// Gets or sets the number of controls that matched the supplied control filter.
    /// </summary>
    public int MatchedControlCount { get; set; }

    /// <summary>
    /// Gets or sets a sample of discovered controls from the effective discovery set.
    /// </summary>
    public IReadOnlyList<WindowControlInfo> SampleControls { get; set; } = Array.Empty<WindowControlInfo>();

    /// <summary>
    /// Gets or sets per-root UI Automation probe results when UI Automation was requested.
    /// </summary>
    public IReadOnlyList<DesktopUiAutomationRootDiagnostic> UiAutomationRoots { get; set; } = Array.Empty<DesktopUiAutomationRootDiagnostic>();

    /// <summary>
    /// Gets or sets optional read-only action-resolution diagnostics for the first matched UI Automation control.
    /// </summary>
    public DesktopUiAutomationActionDiagnostic? UiAutomationActionProbe { get; set; }
}

/// <summary>
/// Represents UI Automation probe details for a single root handle.
/// </summary>
public sealed class DesktopUiAutomationRootDiagnostic {
    /// <summary>
    /// Gets or sets the probe order, starting with the primary window root.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the root handle that was probed.
    /// </summary>
    public IntPtr Handle { get; set; }

    /// <summary>
    /// Gets or sets the root class name when available.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this probe represents the top-level window root.
    /// </summary>
    public bool IsPrimaryRoot { get; set; }

    /// <summary>
    /// Gets or sets whether this root was the preferred probe root reused from a previous successful lookup.
    /// </summary>
    public bool IsPreferredRoot { get; set; }

    /// <summary>
    /// Gets or sets whether this root reused cached controls instead of re-enumerating UI Automation elements.
    /// </summary>
    public bool UsedCachedControls { get; set; }

    /// <summary>
    /// Gets or sets whether the root element itself was included in the enumeration scope.
    /// </summary>
    public bool IncludeRoot { get; set; }

    /// <summary>
    /// Gets or sets whether UI Automation resolved an AutomationElement for this root.
    /// </summary>
    public bool ElementResolved { get; set; }

    /// <summary>
    /// Gets or sets the number of controls discovered from this root before filtering.
    /// </summary>
    public int ControlCount { get; set; }

    /// <summary>
    /// Gets or sets a sample of controls discovered from this root.
    /// </summary>
    public IReadOnlyList<WindowControlInfo> SampleControls { get; set; } = Array.Empty<WindowControlInfo>();

    /// <summary>
    /// Gets or sets the probe error when a root could not be inspected cleanly.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Represents a read-only probe of UI Automation action resolution for a specific control.
/// </summary>
public sealed class DesktopUiAutomationActionDiagnostic {
    /// <summary>
    /// Gets or sets whether a probe was attempted.
    /// </summary>
    public bool Attempted { get; set; }

    /// <summary>
    /// Gets or sets whether the action target could be resolved.
    /// </summary>
    public bool Resolved { get; set; }

    /// <summary>
    /// Gets or sets whether a cached action match was reused.
    /// </summary>
    public bool UsedCachedActionMatch { get; set; }

    /// <summary>
    /// Gets or sets whether a preferred UI Automation root was reused.
    /// </summary>
    public bool UsedPreferredRoot { get; set; }

    /// <summary>
    /// Gets or sets the resolved root handle when one was found.
    /// </summary>
    public IntPtr RootHandle { get; set; }

    /// <summary>
    /// Gets or sets the final match score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets or sets the search mode used during resolution.
    /// </summary>
    public string SearchMode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the elapsed time for the read-only action probe in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }
}

/// <summary>
/// Describes a window and its client-area geometry.
/// </summary>
public sealed class DesktopWindowGeometry {
    /// <summary>
    /// Gets or sets the target window.
    /// </summary>
    public WindowInfo Window { get; set; } = null!;

    /// <summary>
    /// Gets or sets the window bounds left coordinate in screen space.
    /// </summary>
    public int WindowLeft { get; set; }

    /// <summary>
    /// Gets or sets the window bounds top coordinate in screen space.
    /// </summary>
    public int WindowTop { get; set; }

    /// <summary>
    /// Gets or sets the window width in pixels.
    /// </summary>
    public int WindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the window height in pixels.
    /// </summary>
    public int WindowHeight { get; set; }

    /// <summary>
    /// Gets or sets the client-area left coordinate in screen space.
    /// </summary>
    public int ClientLeft { get; set; }

    /// <summary>
    /// Gets or sets the client-area top coordinate in screen space.
    /// </summary>
    public int ClientTop { get; set; }

    /// <summary>
    /// Gets or sets the client-area width in pixels.
    /// </summary>
    public int ClientWidth { get; set; }

    /// <summary>
    /// Gets or sets the client-area height in pixels.
    /// </summary>
    public int ClientHeight { get; set; }

    /// <summary>
    /// Gets or sets the client-area left offset relative to the outer window.
    /// </summary>
    public int ClientOffsetLeft { get; set; }

    /// <summary>
    /// Gets or sets the client-area top offset relative to the outer window.
    /// </summary>
    public int ClientOffsetTop { get; set; }
}

/// <summary>
/// Defines a reusable window-relative target.
/// </summary>
public sealed class DesktopWindowTargetDefinition {
    /// <summary>
    /// Gets or sets an optional description for the target.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the horizontal pixel coordinate relative to the target bounds.
    /// </summary>
    public int? X { get; set; }

    /// <summary>
    /// Gets or sets the vertical pixel coordinate relative to the target bounds.
    /// </summary>
    public int? Y { get; set; }

    /// <summary>
    /// Gets or sets the horizontal normalized ratio from 0 to 1.
    /// </summary>
    public double? XRatio { get; set; }

    /// <summary>
    /// Gets or sets the vertical normalized ratio from 0 to 1.
    /// </summary>
    public double? YRatio { get; set; }

    /// <summary>
    /// Gets or sets the optional target area width in pixels.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the optional target area height in pixels.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the optional target area width ratio from 0 to 1.
    /// </summary>
    public double? WidthRatio { get; set; }

    /// <summary>
    /// Gets or sets the optional target area height ratio from 0 to 1.
    /// </summary>
    public double? HeightRatio { get; set; }

    /// <summary>
    /// Gets or sets whether the target is relative to the client area.
    /// </summary>
    public bool ClientArea { get; set; }
}

/// <summary>
/// Defines a reusable control-selection target.
/// </summary>
public sealed class DesktopControlTargetDefinition {
    /// <summary>
    /// Gets or sets an optional description for the target.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the control class name filter.
    /// </summary>
    public string ClassNamePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the control text filter.
    /// </summary>
    public string TextPattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the control value filter.
    /// </summary>
    public string ValuePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the optional control identifier.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Gets or sets the optional control handle in decimal or hexadecimal string form.
    /// </summary>
    public string? Handle { get; set; }

    /// <summary>
    /// Gets or sets the UI Automation automation identifier filter.
    /// </summary>
    public string AutomationIdPattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the UI Automation control type filter.
    /// </summary>
    public string ControlTypePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the UI Automation framework identifier filter.
    /// </summary>
    public string FrameworkIdPattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets whether matching controls must be enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether matching controls must be keyboard focusable.
    /// </summary>
    public bool? IsKeyboardFocusable { get; set; }

    /// <summary>
    /// Gets or sets whether matching controls must support background-safe click or invoke actions.
    /// </summary>
    public bool? SupportsBackgroundClick { get; set; }

    /// <summary>
    /// Gets or sets whether matching controls must support background-safe text updates.
    /// </summary>
    public bool? SupportsBackgroundText { get; set; }

    /// <summary>
    /// Gets or sets whether matching controls must support background-safe key delivery.
    /// </summary>
    public bool? SupportsBackgroundKeys { get; set; }

    /// <summary>
    /// Gets or sets whether matching controls must support explicit foreground input fallback.
    /// </summary>
    public bool? SupportsForegroundInputFallback { get; set; }

    /// <summary>
    /// Gets or sets whether UI Automation should be used while resolving the target.
    /// </summary>
    public bool UseUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets whether Win32 and UI Automation results should be combined while resolving the target.
    /// </summary>
    public bool IncludeUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets whether the target window should be prepared for UI Automation discovery before resolving the target.
    /// </summary>
    public bool EnsureForegroundWindow { get; set; }
}

/// <summary>
/// Represents a named target resolved against a live window.
/// </summary>
public sealed class DesktopResolvedWindowTarget {
    /// <summary>
    /// Gets or sets the target name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target definition.
    /// </summary>
    public DesktopWindowTargetDefinition Definition { get; set; } = new();

    /// <summary>
    /// Gets or sets the resolved window geometry.
    /// </summary>
    public DesktopWindowGeometry Geometry { get; set; } = null!;

    /// <summary>
    /// Gets or sets the resolved X coordinate relative to the selected bounds.
    /// </summary>
    public int RelativeX { get; set; }

    /// <summary>
    /// Gets or sets the resolved Y coordinate relative to the selected bounds.
    /// </summary>
    public int RelativeY { get; set; }

    /// <summary>
    /// Gets or sets the resolved width relative to the selected bounds when the target defines an area.
    /// </summary>
    public int? RelativeWidth { get; set; }

    /// <summary>
    /// Gets or sets the resolved height relative to the selected bounds when the target defines an area.
    /// </summary>
    public int? RelativeHeight { get; set; }

    /// <summary>
    /// Gets or sets the resolved screen-space X coordinate.
    /// </summary>
    public int ScreenX { get; set; }

    /// <summary>
    /// Gets or sets the resolved screen-space Y coordinate.
    /// </summary>
    public int ScreenY { get; set; }

    /// <summary>
    /// Gets or sets the resolved screen-space width when the target defines an area.
    /// </summary>
    public int? ScreenWidth { get; set; }

    /// <summary>
    /// Gets or sets the resolved screen-space height when the target defines an area.
    /// </summary>
    public int? ScreenHeight { get; set; }
}

/// <summary>
/// Represents a named control target resolved against a live window and control.
/// </summary>
public sealed class DesktopResolvedControlTarget {
    /// <summary>
    /// Gets or sets the target name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target definition.
    /// </summary>
    public DesktopControlTargetDefinition Definition { get; set; } = new();

    /// <summary>
    /// Gets or sets the resolved window.
    /// </summary>
    public WindowInfo Window { get; set; } = null!;

    /// <summary>
    /// Gets or sets the resolved control.
    /// </summary>
    public WindowControlInfo Control { get; set; } = null!;
}

/// <summary>
/// Represents a screenshot capture produced by DesktopManager.
/// </summary>
public sealed class DesktopCapture : IDisposable {
    /// <summary>
    /// Gets or sets the capture kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the captured bitmap.
    /// </summary>
    public Bitmap Bitmap { get; set; } = null!;

    /// <summary>
    /// Gets the captured bitmap width.
    /// </summary>
    public int Width => Bitmap.Width;

    /// <summary>
    /// Gets the captured bitmap height.
    /// </summary>
    public int Height => Bitmap.Height;

    /// <summary>
    /// Gets or sets the captured window when applicable.
    /// </summary>
    public WindowInfo? Window { get; set; }

    /// <summary>
    /// Gets or sets the captured control when applicable.
    /// </summary>
    public WindowControlInfo? Control { get; set; }

    /// <summary>
    /// Gets or sets the captured window geometry when applicable.
    /// </summary>
    public DesktopWindowGeometry? Geometry { get; set; }

    /// <summary>
    /// Gets or sets the captured monitor index when applicable.
    /// </summary>
    public int? MonitorIndex { get; set; }

    /// <summary>
    /// Gets or sets the captured monitor device name when applicable.
    /// </summary>
    public string? MonitorDeviceName { get; set; }

    /// <summary>
    /// Saves the captured bitmap using the output path extension.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    public void Save(string path) {
        Bitmap.Save(path);
    }

    /// <summary>
    /// Disposes the underlying bitmap.
    /// </summary>
    public void Dispose() {
        Bitmap?.Dispose();
    }
}
