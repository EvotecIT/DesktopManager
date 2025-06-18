using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Represents a layout containing positions for multiple windows.
/// </summary>
public class WindowLayout {
    /// <summary>
    /// Gets or sets collection of window positions.
    /// </summary>
    public List<WindowPosition> Windows { get; set; } = new();
}
