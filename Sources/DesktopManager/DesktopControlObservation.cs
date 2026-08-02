using System;

namespace DesktopManager;

/// <summary>
/// Represents one provider-neutral semantic snapshot of a native or UI Automation control.
/// </summary>
public sealed class DesktopControlObservation {
    /// <summary>Gets or sets the control identity.</summary>
    public DesktopControlIdentity Identity { get; set; } = new();

    /// <summary>Gets or sets the control capabilities.</summary>
    public DesktopControlCapabilities Capabilities { get; set; } = new();

    /// <summary>Gets or sets the bounded text observation.</summary>
    public DesktopControlTextObservation Text { get; set; } = new();

    /// <summary>Gets or sets the selection state.</summary>
    public DesktopControlSelectionObservation Selection { get; set; } = new();

    /// <summary>Gets or sets numeric range state.</summary>
    public DesktopControlRangeObservation Range { get; set; } = new();

    /// <summary>Gets or sets scroll state.</summary>
    public DesktopControlScrollObservation Scroll { get; set; } = new();

    /// <summary>Gets or sets grid and table state.</summary>
    public DesktopControlGridObservation Grid { get; set; } = new();

    /// <summary>Gets or sets the provider source used to resolve the control.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC observation timestamp.</summary>
    public DateTime ObservedAtUtc { get; set; }

    /// <summary>Gets or sets whether provider-cached discovery metadata was used.</summary>
    public bool UsedCachedMetadata { get; set; }

    /// <summary>Gets or sets the wait strategy that produced this observation, when returned from a wait.</summary>
    public string WaitStrategy { get; set; } = string.Empty;

    /// <summary>Gets or sets a provider-neutral observation status.</summary>
    public string Status { get; set; } = "available";

    /// <summary>Gets or sets a bounded provider failure reason when semantic state was only partially available.</summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the control is password protected.</summary>
    public bool? IsPassword { get; set; }

    /// <summary>Gets or sets whether the control is enabled.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets whether the control is visible.</summary>
    public bool? IsVisible { get; set; }

    /// <summary>Gets or sets whether the control is off-screen.</summary>
    public bool? IsOffscreen { get; set; }

    /// <summary>Gets or sets whether the control has keyboard focus.</summary>
    public bool? IsFocused { get; set; }

    /// <summary>Gets or sets whether the control can receive keyboard focus.</summary>
    public bool? IsKeyboardFocusable { get; set; }

    /// <summary>Gets or sets whether the control is checked.</summary>
    public bool? IsChecked { get; set; }

    /// <summary>Gets or sets the expand/collapse state.</summary>
    public string ExpandCollapseState { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the control value is read-only.</summary>
    public bool? IsReadOnly { get; set; }
}
