using System;
using System.Drawing;

namespace DesktopManager;

/// <summary>Selects whether window capture may include visible desktop pixels.</summary>
public sealed class WindowCaptureOptions {
    /// <summary>Gets or sets whether failed or dark PrintWindow captures may use the visible desktop. Defaults to false.</summary>
    public bool AllowDesktopFallback { get; set; }
}

/// <summary>Identifies the API that supplied captured window pixels.</summary>
public enum WindowCaptureBackend {
    /// <summary>The window supplied pixels through PrintWindow.</summary>
    PrintWindow,
    /// <summary>Pixels came from the visible desktop and may include covering windows.</summary>
    Desktop
}

/// <summary>Owns a captured bitmap and records its source and screen bounds.</summary>
public sealed class WindowCaptureResult : IDisposable {
    internal WindowCaptureResult(Bitmap bitmap, WindowCaptureBackend backend, Rectangle bounds, string? fallbackReason, bool isDark) {
        Bitmap = bitmap;
        Backend = backend;
        ScreenBounds = bounds;
        FallbackReason = fallbackReason;
        IsDark = isDark;
    }
    /// <summary>Gets the owned bitmap. Dispose this result when finished.</summary>
    public Bitmap Bitmap { get; }
    /// <summary>Gets the source of the captured pixels.</summary>
    public WindowCaptureBackend Backend { get; }
    /// <summary>Gets the screen rectangle represented by the bitmap.</summary>
    public Rectangle ScreenBounds { get; }
    /// <summary>Gets why desktop fallback was used, or null for PrintWindow output.</summary>
    public string? FallbackReason { get; }
    /// <summary>Gets whether sampled pixels are predominantly dark. This does not establish capture failure.</summary>
    public bool IsDark { get; }
    /// <inheritdoc/>
    public void Dispose() { Bitmap.Dispose(); }
}
