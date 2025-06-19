using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Represents a collection of window positions that define a desktop layout.
/// </summary>
public class WindowLayout {
    /// <summary>
    /// Gets or sets the list of window positions in this layout.
    /// </summary>
    public List<WindowPosition> Windows { get; set; } = new();
}
