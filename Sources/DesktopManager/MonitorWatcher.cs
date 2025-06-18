#if !NETSTANDARD2_0 && !NETSTANDARD2_1
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Monitors display change notifications using <c>WM_DISPLAYCHANGE</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MonitorWatcher : IDisposable {
    /// <summary>
    /// Raised when display settings change.
    /// </summary>
    public event EventHandler DisplaySettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorWatcher"/> class.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows.</exception>
    public MonitorWatcher() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("MonitorWatcher is supported only on Windows.");
        }

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object sender, EventArgs e) {
        DisplaySettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Unsubscribes from system events.
    /// </summary>
    public void Dispose() {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        GC.SuppressFinalize(this);
    }
}
#endif
