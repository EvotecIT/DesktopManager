using System;

namespace DesktopManager;

/// <summary>
/// Represents the current observable state of a desktop control.
/// </summary>
public sealed class DesktopControlState {
    /// <summary>
    /// Gets or sets the parent window handle.
    /// </summary>
    public IntPtr WindowHandle { get; set; }

    /// <summary>
    /// Gets or sets the control handle when available.
    /// </summary>
    public IntPtr ControlHandle { get; set; }

    /// <summary>
    /// Gets or sets the control class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UI Automation identifier.
    /// </summary>
    public string AutomationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UI Automation control type.
    /// </summary>
    public string ControlType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the control text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the control value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the control is enabled when available.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether the control is visible when available.
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// Gets or sets whether the control currently has focus when available.
    /// </summary>
    public bool? IsFocused { get; set; }

    /// <summary>
    /// Gets or sets whether the control can receive keyboard focus when available.
    /// </summary>
    public bool? IsKeyboardFocusable { get; set; }

    /// <summary>
    /// Gets or sets whether the control is off-screen when available.
    /// </summary>
    public bool? IsOffscreen { get; set; }

    /// <summary>
    /// Gets or sets whether the control supports background-safe click actions.
    /// </summary>
    public bool SupportsBackgroundClick { get; set; }

    /// <summary>
    /// Gets or sets whether the control supports background-safe text updates.
    /// </summary>
    public bool SupportsBackgroundText { get; set; }

    /// <summary>
    /// Gets or sets whether the control supports background-safe key delivery.
    /// </summary>
    public bool SupportsBackgroundKeys { get; set; }

    /// <summary>
    /// Gets or sets whether the control may be driven through foreground fallback.
    /// </summary>
    public bool SupportsForegroundInputFallback { get; set; }

    /// <summary>
    /// Gets or sets the control left coordinate.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the control top coordinate.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the control width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the control height.
    /// </summary>
    public int Height { get; set; }
}
