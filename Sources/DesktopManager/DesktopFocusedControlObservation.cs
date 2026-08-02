namespace DesktopManager;

/// <summary>
/// Describes the currently focused child control for a target window when it can be resolved.
/// </summary>
public sealed class DesktopFocusedControlObservation
{
    /// <summary>
    /// Gets or sets the parent window handle.
    /// </summary>
    public IntPtr WindowHandle { get; set; }

    /// <summary>
    /// Gets or sets the parent window title.
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the focused child handle.
    /// </summary>
    public IntPtr FocusedHandle { get; set; }

    /// <summary>
    /// Gets or sets the focused control class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the focused control automation identifier.
    /// </summary>
    public string AutomationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the focused control type.
    /// </summary>
    public string ControlType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the focused control text when available.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the focused control value when available.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source used to read <see cref="Value"/>.
    /// </summary>
    public string ValueSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the returned value was truncated at the requested observation limit.
    /// </summary>
    public bool IsValueTruncated { get; set; }

    /// <summary>
    /// Gets or sets whether the complete provider range contained the expected text.
    /// </summary>
    public bool? ContainsExpected { get; set; }

    /// <summary>
    /// Gets or sets whether UI Automation marks the focused control as password-protected.
    /// </summary>
    public bool? IsPassword { get; set; }

    /// <summary>
    /// Gets or sets whether the focused control can receive keyboard focus.
    /// </summary>
    public bool? IsKeyboardFocusable { get; set; }

    /// <summary>
    /// Gets or sets whether the focused control is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}
