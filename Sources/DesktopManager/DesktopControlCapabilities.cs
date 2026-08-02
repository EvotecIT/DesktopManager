using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Describes provider-neutral semantic operations and observations supported by a control.
/// </summary>
public sealed class DesktopControlCapabilities {
    /// <summary>Gets or sets the curated UI Automation patterns available on the current element.</summary>
    public IReadOnlyList<string> Patterns { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets whether the control exposes document text.</summary>
    public bool CanReadText { get; set; }

    /// <summary>Gets or sets whether the control exposes text selections or a caret.</summary>
    public bool CanReadTextSelection { get; set; }

    /// <summary>Gets or sets whether the control supports a direct value update.</summary>
    public bool CanSetValue { get; set; }

    /// <summary>Gets or sets whether the control can be invoked.</summary>
    public bool CanInvoke { get; set; }

    /// <summary>Gets or sets whether the control can be toggled.</summary>
    public bool CanToggle { get; set; }

    /// <summary>Gets or sets whether the control participates in selection.</summary>
    public bool CanSelect { get; set; }

    /// <summary>Gets or sets whether the control exposes an expandable state.</summary>
    public bool CanExpandCollapse { get; set; }

    /// <summary>Gets or sets whether the control exposes a numeric range.</summary>
    public bool CanReadRange { get; set; }

    /// <summary>Gets or sets whether the control exposes scroll state.</summary>
    public bool CanScroll { get; set; }

    /// <summary>Gets or sets whether the control exposes grid coordinates or dimensions.</summary>
    public bool CanReadGrid { get; set; }

    /// <summary>Gets or sets whether the control exposes table headers.</summary>
    public bool CanReadTable { get; set; }

    /// <summary>Gets or sets whether a virtualized item can be realized.</summary>
    public bool CanRealizeVirtualizedItem { get; set; }

    /// <summary>Gets or sets whether background-safe click or invoke is available.</summary>
    public bool SupportsBackgroundClick { get; set; }

    /// <summary>Gets or sets whether background-safe text replacement is available.</summary>
    public bool SupportsBackgroundText { get; set; }

    /// <summary>Gets or sets whether background-safe key delivery is available.</summary>
    public bool SupportsBackgroundKeys { get; set; }

    /// <summary>Gets or sets whether explicit foreground input fallback is available.</summary>
    public bool SupportsForegroundInputFallback { get; set; }
}
