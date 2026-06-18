using System;

namespace DesktopManager;

/// <summary>
/// Describes a reliable window-placement operation that can be reused by apps, command-line tools, and PowerShell cmdlets.
/// </summary>
public sealed class WindowPlacementRequest {
    /// <summary>
    /// Gets or sets the requested target window handle. When zero, the current foreground window is used.
    /// Child handles are normalized to their root top-level window before execution.
    /// </summary>
    public IntPtr TargetWindowHandle { get; set; }

    /// <summary>Gets or sets the monitor target selector.</summary>
    public WindowMonitorTargetKind MonitorTarget { get; set; } = WindowMonitorTargetKind.Current;

    /// <summary>Gets or sets an explicit DesktopManager monitor index. This takes precedence over <see cref="MonitorTarget"/>.</summary>
    public int? MonitorIndex { get; set; }

    /// <summary>Gets or sets the placement to apply.</summary>
    public WindowPlacementKind Placement { get; set; } = WindowPlacementKind.Maximize;

    /// <summary>Gets or sets the exact left coordinate for <see cref="WindowPlacementKind.ExactRectangle"/>.</summary>
    public int? ExactLeft { get; set; }

    /// <summary>Gets or sets the exact top coordinate for <see cref="WindowPlacementKind.ExactRectangle"/>.</summary>
    public int? ExactTop { get; set; }

    /// <summary>Gets or sets the exact width for <see cref="WindowPlacementKind.ExactRectangle"/>.</summary>
    public int? ExactWidth { get; set; }

    /// <summary>Gets or sets the exact height for <see cref="WindowPlacementKind.ExactRectangle"/>.</summary>
    public int? ExactHeight { get; set; }

    /// <summary>Gets or sets whether the observed geometry should be verified after the operation.</summary>
    public bool VerifyAfterAction { get; set; } = true;

    /// <summary>Gets or sets how many times the operation should be retried when verification fails.</summary>
    public int MaxAttempts { get; set; } = 2;

    /// <summary>Gets or sets the total verification wait per attempt, in milliseconds.</summary>
    public int VerificationTimeoutMilliseconds { get; set; } = 700;

    /// <summary>Gets or sets the verification polling interval, in milliseconds.</summary>
    public int VerificationIntervalMilliseconds { get; set; } = 50;

    /// <summary>Gets or sets the accepted geometry verification tolerance, in pixels.</summary>
    public int GeometryTolerancePixels { get; set; } = 48;

    /// <summary>Gets whether this request contains a complete exact rectangle.</summary>
    public bool HasExactRectangle =>
        ExactLeft.HasValue &&
        ExactTop.HasValue &&
        ExactWidth.HasValue &&
        ExactHeight.HasValue;
}
