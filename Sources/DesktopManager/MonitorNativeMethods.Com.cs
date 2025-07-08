using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// COM initialization related native methods.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Apartment threaded COM initialization flag.</summary>
    public const uint COINIT_APARTMENTTHREADED = 0x2;

    /// <summary>Initializes the COM library on the current thread.</summary>
    /// <param name="pvReserved">Reserved, must be IntPtr.Zero.</param>
    /// <param name="dwCoInit">Initialization options.</param>
    /// <returns>HRESULT indicating success or failure.</returns>
    [DllImport("ole32.dll")]
    public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    /// <summary>Closes the COM library on the current thread.</summary>
    [DllImport("ole32.dll")]
    public static extern void CoUninitialize();
}
