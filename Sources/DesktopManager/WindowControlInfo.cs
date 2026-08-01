using System;

namespace DesktopManager;

/// <summary>
/// Represents a child window control.
/// </summary>
public class WindowControlInfo {
    /// <summary>Handle of the parent window that exposed this control.</summary>
    public IntPtr ParentWindowHandle { get; internal set; }
    /// <summary>Handle of the control.</summary>
    public IntPtr Handle { get; internal set; }
    /// <summary>Class name of the control.</summary>
    public string ClassName { get; internal set; } = string.Empty;
    /// <summary>Control identifier.</summary>
    public int Id { get; internal set; }
    /// <summary>Displayed text of the control.</summary>
    public string Text { get; internal set; } = string.Empty;
    /// <summary>Current control value when available.</summary>
    public string Value { get; internal set; } = string.Empty;
    /// <summary>Origin of the control metadata.</summary>
    public WindowControlSource Source { get; internal set; } = WindowControlSource.Win32;
    internal bool HasUiAutomationIdentity { get; set; }
    /// <summary>UI Automation runtime identifier scoped to the current provider session.</summary>
    public string RuntimeId { get; internal set; } = string.Empty;
    /// <summary>UI Automation automation identifier when available.</summary>
    public string AutomationId { get; internal set; } = string.Empty;
    /// <summary>UI Automation control type when available.</summary>
    public string ControlType { get; internal set; } = string.Empty;
    /// <summary>UI Automation framework identifier when available.</summary>
    public string FrameworkId { get; internal set; } = string.Empty;
    /// <summary>Whether the control can receive keyboard focus when available.</summary>
    public bool? IsKeyboardFocusable { get; internal set; }
    /// <summary>Whether the control is enabled when available.</summary>
    public bool? IsEnabled { get; internal set; }
    /// <summary>Whether UI Automation marks the control as password-protected.</summary>
    public bool? IsPassword { get; internal set; }
    /// <summary>Whether the control supports background-safe click/invoke actions.</summary>
    public bool SupportsBackgroundClick { get; internal set; }
    /// <summary>Whether the control supports background-safe text updates.</summary>
    public bool SupportsBackgroundText { get; internal set; }
    /// <summary>Whether the control supports background-safe key delivery.</summary>
    public bool SupportsBackgroundKeys { get; internal set; }
    /// <summary>Whether the control may be driven through explicit foreground input fallback.</summary>
    public bool SupportsForegroundInputFallback { get; internal set; }
    /// <summary>Screen X coordinate of the control bounds when available.</summary>
    public int Left { get; internal set; }
    /// <summary>Screen Y coordinate of the control bounds when available.</summary>
    public int Top { get; internal set; }
    /// <summary>Control width when available.</summary>
    public int Width { get; internal set; }
    /// <summary>Control height when available.</summary>
    public int Height { get; internal set; }
    /// <summary>Whether the control is reported as off-screen when available.</summary>
    public bool? IsOffscreen { get; internal set; }
}
