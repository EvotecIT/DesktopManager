using System;

namespace DesktopManager;

/// <summary>
/// Provides COM initialization helpers for <see cref="MonitorService"/>.
/// </summary>
public partial class MonitorService {
    /// <summary>Initializes COM on the current thread.</summary>
    /// <returns><c>true</c> if initialization succeeded.</returns>
    protected virtual bool InitializeCom() {
        return MonitorNativeMethods.CoInitializeEx(IntPtr.Zero, MonitorNativeMethods.COINIT_APARTMENTTHREADED) >= 0;
    }

    /// <summary>Uninitializes COM on the current thread.</summary>
    protected virtual void UninitializeCom() {
        MonitorNativeMethods.CoUninitialize();
    }
}
