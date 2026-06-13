using System;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Gets the current foreground window.
    /// </summary>
    /// <param name="includeHidden">Whether to include hidden windows.</param>
    /// <param name="includeCloaked">Whether to include cloaked windows.</param>
    /// <param name="includeOwned">Whether to include owned windows.</param>
    /// <param name="includeEmptyTitles">Whether to include windows with empty titles.</param>
    /// <returns>The active window when it can be resolved; otherwise null.</returns>
    public WindowInfo? GetActiveWindow(bool includeHidden = true, bool includeCloaked = true, bool includeOwned = true, bool includeEmptyTitles = true) {
        return GetWindows(new WindowQueryOptions {
            ActiveWindow = true,
            IncludeHidden = includeHidden,
            IncludeCloaked = includeCloaked,
            IncludeOwned = includeOwned,
            IncludeEmptyTitles = includeEmptyTitles
        }).FirstOrDefault();
    }

    /// <summary>
    /// Resolves a possibly child or hosted-session window handle to its root top-level window.
    /// </summary>
    /// <param name="handle">The source window handle.</param>
    /// <returns>The root top-level window handle, or the source handle when no ancestor is available.</returns>
    public static IntPtr GetRootWindowHandle(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return IntPtr.Zero;
        }

        IntPtr root = MonitorNativeMethods.GetAncestor(handle, MonitorNativeMethods.GA_ROOT);
        return root == IntPtr.Zero ? handle : root;
    }

    /// <summary>
    /// Gets a window by its exact handle.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <param name="includeHidden">Whether to include hidden windows.</param>
    /// <param name="includeCloaked">Whether to include cloaked windows.</param>
    /// <param name="includeOwned">Whether to include owned windows.</param>
    /// <param name="includeEmptyTitles">Whether to include windows with empty titles.</param>
    /// <returns>The matching window when found; otherwise null.</returns>
    public WindowInfo? GetWindow(IntPtr handle, bool includeHidden = true, bool includeCloaked = true, bool includeOwned = true, bool includeEmptyTitles = true) {
        if (handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(handle));
        }

        return GetWindows(new WindowQueryOptions {
            Handle = handle,
            IncludeHidden = includeHidden,
            IncludeCloaked = includeCloaked,
            IncludeOwned = includeOwned,
            IncludeEmptyTitles = includeEmptyTitles
        }).FirstOrDefault();
    }
}
