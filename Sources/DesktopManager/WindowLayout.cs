using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Represents a collection of window positions that define a desktop layout.
/// </summary>
public class WindowLayout {
    /// <summary>Gets or sets when this snapshot was captured.</summary>
    public DateTimeOffset? CapturedAt { get; set; }

    /// <summary>Gets or sets captured virtual desktops. Desktop GUIDs, rather than indices, identify targets.</summary>
    public List<VirtualDesktopInfo> Desktops { get; set; } = new();

    /// <summary>Gets or sets capture limitations that must remain visible to the caller.</summary>
    public List<string> CaptureWarnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of window positions in this layout.
    /// </summary>
    public List<WindowPosition> Windows { get; set; } = new();
}
