using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

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
    /// <exception cref="UnauthorizedAccessException">
    /// The window belongs to another process. The supported Windows API only moves the calling process's own windows.
    /// </exception>
    public void MoveWindowToDesktop(IntPtr windowHandle, Guid desktopId) {
        ValidateHandle(windowHandle);
        int result = _manager.MoveWindowToDesktop(windowHandle, ref desktopId);
        if (result == AccessDenied) {
            throw new UnauthorizedAccessException(
                "Windows only lets a process move its own windows to another virtual desktop with the supported API, " +
                "and this window belongs to another process. Moving it needs the experimental shell contract " +
                "(ExperimentalVirtualDesktopService, or Move-DesktopWindowToVirtualDesktop -Experimental in PowerShell).");
        }
        Marshal.ThrowExceptionForHR(result);
    }

    private const int AccessDenied = unchecked((int)0x80070005);

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
        _manager = (IVirtualDesktopManager)NativeComActivation.CreateInstance(new Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A"));
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
        NativeComActivation.Release(_manager);
        _disposed = true;
    }
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
internal partial interface IVirtualDesktopManager {
    [PreserveSig]
    int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out int onCurrentDesktop);

    [PreserveSig]
    int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

    [PreserveSig]
    int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}
