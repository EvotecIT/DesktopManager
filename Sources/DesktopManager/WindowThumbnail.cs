using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>A live picture of one window drawn by DWM inside another window, the way Alt+Tab and taskbar previews are.</summary>
/// <remarks>
/// DWM composites the picture over the destination window's own content. A new thumbnail is not drawn until
/// <see cref="Update"/> sets at least a destination rectangle and visibility. Dispose the thumbnail when it is no
/// longer shown; DWM also releases it when the destination window is destroyed.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowThumbnail : IDisposable {
    private IntPtr _handle;

    /// <summary>Registers a thumbnail of <paramref name="source"/> drawn inside <paramref name="destination"/>.</summary>
    /// <param name="destination">A top-level window owned by the calling process that shows the thumbnail.</param>
    /// <param name="source">The top-level window to show.</param>
    /// <exception cref="ArgumentException">Either window handle is zero.</exception>
    /// <exception cref="COMException">DWM rejected the registration, for example when composition is unavailable or a window is not top-level.</exception>
    public WindowThumbnail(IntPtr destination, IntPtr source) {
        if (destination == IntPtr.Zero) {
            throw new ArgumentException("A destination window handle is required.", nameof(destination));
        }
        if (source == IntPtr.Zero) {
            throw new ArgumentException("A source window handle is required.", nameof(source));
        }
        Marshal.ThrowExceptionForHR(DwmThumbnailNative.DwmRegisterThumbnail(destination, source, out IntPtr handle));
        _handle = handle;
        Destination = destination;
        Source = source;
    }

    /// <summary>Gets the window that shows the thumbnail.</summary>
    public IntPtr Destination { get; }

    /// <summary>Gets the window shown by the thumbnail.</summary>
    public IntPtr Source { get; }

    /// <summary>Changes where and how the thumbnail is drawn. Only properties that are set are changed.</summary>
    /// <param name="properties">The properties to change.</param>
    /// <exception cref="ArgumentNullException"><paramref name="properties"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The thumbnail has been disposed.</exception>
    /// <exception cref="COMException">DWM rejected the change.</exception>
    public void Update(WindowThumbnailProperties properties) {
        if (properties == null) {
            throw new ArgumentNullException(nameof(properties));
        }
        DwmThumbnailProperties native = properties.ToNative();
        Marshal.ThrowExceptionForHR(DwmThumbnailNative.DwmUpdateThumbnailProperties(GetHandle(), ref native));
    }

    /// <summary>Reads the source window's current size in physical pixels, for fitting the thumbnail while keeping its aspect ratio.</summary>
    /// <param name="width">Receives the source width, or 0 when it cannot be read.</param>
    /// <param name="height">Receives the source height, or 0 when it cannot be read.</param>
    /// <returns>True when DWM reported a non-empty size; false when it failed, for example after the source window closed.</returns>
    /// <exception cref="ObjectDisposedException">The thumbnail has been disposed.</exception>
    public bool TryGetSourceSize(out int width, out int height) {
        int result = DwmThumbnailNative.DwmQueryThumbnailSourceSize(GetHandle(), out DwmThumbnailSize size);
        bool known = result >= 0 && size.Width > 0 && size.Height > 0;
        width = known ? size.Width : 0;
        height = known ? size.Height : 0;
        return known;
    }

    /// <summary>Unregisters the thumbnail so DWM stops drawing it. Repeated calls have no effect.</summary>
    public void Dispose() {
        IntPtr handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
        if (handle != IntPtr.Zero) {
            DwmThumbnailNative.DwmUnregisterThumbnail(handle);
        }
    }

    private IntPtr GetHandle() {
        IntPtr handle = Volatile.Read(ref _handle);
        if (handle == IntPtr.Zero) {
            throw new ObjectDisposedException(nameof(WindowThumbnail));
        }
        return handle;
    }
}
