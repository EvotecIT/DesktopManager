namespace DesktopManager;

/// <summary>
/// Describes text observed from a target window or one of its controls.
/// </summary>
public sealed class DesktopWindowTextObservation {
    /// <summary>
    /// Gets or sets the parent window handle.
    /// </summary>
    public IntPtr WindowHandle { get; set; }

    /// <summary>
    /// Gets or sets the parent window title.
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the control handle when the observation came from a child control.
    /// </summary>
    public IntPtr ControlHandle { get; set; }

    /// <summary>
    /// Gets or sets the control class name when available.
    /// </summary>
    public string ControlClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the control automation identifier when available.
    /// </summary>
    public string ControlAutomationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the control type when available.
    /// </summary>
    public string ControlType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observed text value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observation source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the full source value contained the expected text.
    /// </summary>
    public bool? ContainsExpected { get; set; }

    /// <summary>
    /// Gets or sets whether the returned value was truncated.
    /// </summary>
    public bool IsTruncated { get; set; }
}
