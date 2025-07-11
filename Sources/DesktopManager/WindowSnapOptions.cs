namespace DesktopManager;

/// <summary>
/// Options controlling automatic window snapping behavior.
/// </summary>
public class WindowSnapOptions {
    /// <summary>
    /// Threshold in pixels for snapping windows to monitor edges.
    /// </summary>
    public int SnapThreshold { get; set; } = 20;
}
