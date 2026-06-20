namespace DesktopManager.App.Core;

/// <summary>
/// Groups rules that describe one saved monitor/workstation layout.
/// </summary>
public sealed class WindowLayoutProfileDefinition {
    /// <summary>
    /// Gets or sets whether this layout profile can be applied.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the stable layout identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the display name shown in the app.
    /// </summary>
    public string Name { get; set; } = "Window layout";

    /// <summary>
    /// Gets or sets the monitor stable keys this layout was designed for.
    /// </summary>
    public List<string> MonitorStableKeys { get; set; } = new();

    /// <summary>
    /// Gets or sets the window placement rules for this layout.
    /// </summary>
    public List<WindowRuleDefinition> Rules { get; set; } = new();
}
