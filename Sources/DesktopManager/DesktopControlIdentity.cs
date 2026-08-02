using System;
using System.Text.Json.Serialization;

namespace DesktopManager;

/// <summary>
/// Identifies a control across native Win32 and UI Automation providers for the lifetime of an observation session.
/// </summary>
public sealed class DesktopControlIdentity {
    /// <summary>Gets or sets the process identifier that owns the parent window.</summary>
    public uint ProcessId { get; set; }

    /// <summary>Gets or sets the top-level window handle.</summary>
    [JsonIgnore]
    public IntPtr WindowHandle { get; set; }

    /// <summary>Gets the top-level window handle in hexadecimal form for serialized surfaces.</summary>
    public string WindowHandleHex => $"0x{WindowHandle.ToInt64():X}";

    /// <summary>Gets or sets the native control handle when one exists.</summary>
    [JsonIgnore]
    public IntPtr ControlHandle { get; set; }

    /// <summary>Gets the native control handle in hexadecimal form for serialized surfaces.</summary>
    public string ControlHandleHex => $"0x{ControlHandle.ToInt64():X}";

    /// <summary>Gets or sets the UI Automation runtime identifier encoded for the current provider session.</summary>
    public string RuntimeId { get; set; } = string.Empty;

    /// <summary>Gets or sets the UI Automation automation identifier.</summary>
    public string AutomationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the UI Automation control type.</summary>
    public string ControlType { get; set; } = string.Empty;

    /// <summary>Gets or sets the UI framework identifier.</summary>
    public string FrameworkId { get; set; } = string.Empty;

    /// <summary>Gets or sets the native or provider class name.</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Gets or sets a bounded provider-neutral ancestor selector path.</summary>
    public string AncestorPath { get; set; } = string.Empty;

    /// <summary>Gets or sets a deterministic session key assembled from the strongest available identity fields.</summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the left screen coordinate.</summary>
    public int Left { get; set; }

    /// <summary>Gets or sets the top screen coordinate.</summary>
    public int Top { get; set; }

    /// <summary>Gets or sets the control width.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the control height.</summary>
    public int Height { get; set; }
}
