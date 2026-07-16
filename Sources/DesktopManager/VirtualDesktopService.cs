using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides the virtual-desktop operations Microsoft exposes for desktop applications.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
public sealed class VirtualDesktopService : IDisposable {
    private readonly IVirtualDesktopManagerApi _manager;

    /// <summary>Initializes the supported Windows virtual-desktop service.</summary>
    public VirtualDesktopService()
        : this(new VirtualDesktopManagerApi()) {
    }

    internal VirtualDesktopService(IVirtualDesktopManagerApi manager) {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>Determines whether a top-level window is on the current virtual desktop.</summary>
    /// <param name="windowHandle">The top-level window handle.</param>
    /// <returns><c>true</c> when the window is on the current virtual desktop.</returns>
    public bool IsWindowOnCurrentDesktop(IntPtr windowHandle) {
        ValidateHandle(windowHandle);
        Marshal.ThrowExceptionForHR(_manager.IsWindowOnCurrentVirtualDesktop(windowHandle, out int isCurrent));
        return isCurrent != 0;
    }

    /// <summary>Gets the virtual-desktop identifier that owns a top-level window.</summary>
    /// <param name="windowHandle">The top-level window handle.</param>
    /// <returns>The virtual-desktop identifier.</returns>
    public Guid GetWindowDesktopId(IntPtr windowHandle) {
        ValidateHandle(windowHandle);
        Marshal.ThrowExceptionForHR(_manager.GetWindowDesktopId(windowHandle, out Guid desktopId));
        return desktopId;
    }

    /// <summary>Moves a top-level window to a known virtual desktop.</summary>
    /// <param name="windowHandle">The top-level window handle to move.</param>
    /// <param name="desktopId">A desktop identifier obtained from another top-level window.</param>
    public void MoveWindowToDesktop(IntPtr windowHandle, Guid desktopId) {
        ValidateHandle(windowHandle);
        Marshal.ThrowExceptionForHR(_manager.MoveWindowToDesktop(windowHandle, ref desktopId));
    }

    /// <inheritdoc/>
    public void Dispose() {
        _manager.Dispose();
    }

    private static void ValidateHandle(IntPtr windowHandle) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("A non-zero top-level window handle is required.", nameof(windowHandle));
        }
    }
}

internal interface IVirtualDesktopManagerApi : IDisposable {
    int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out int onCurrentDesktop);
    int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);
    int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}

internal sealed class VirtualDesktopManagerApi : IVirtualDesktopManagerApi {
    private readonly IVirtualDesktopManager _manager;
    private bool _disposed;

    public VirtualDesktopManagerApi() {
        Type managerType = Type.GetTypeFromCLSID(new Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A"), true)
            ?? throw new InvalidOperationException("Windows did not expose the virtual desktop manager COM class.");
        _manager = (IVirtualDesktopManager)(Activator.CreateInstance(managerType)
            ?? throw new InvalidOperationException("Windows did not create the virtual desktop manager COM object."));
    }

    public int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out int onCurrentDesktop) {
        return _manager.IsWindowOnCurrentVirtualDesktop(topLevelWindow, out onCurrentDesktop);
    }

    public int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId) {
        return _manager.GetWindowDesktopId(topLevelWindow, out desktopId);
    }

    public int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId) {
        return _manager.MoveWindowToDesktop(topLevelWindow, ref desktopId);
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }
        if (Marshal.IsComObject(_manager)) {
            Marshal.FinalReleaseComObject(_manager);
        }
        _disposed = true;
    }
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
internal interface IVirtualDesktopManager {
    [PreserveSig]
    int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out int onCurrentDesktop);

    [PreserveSig]
    int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

    [PreserveSig]
    int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}
