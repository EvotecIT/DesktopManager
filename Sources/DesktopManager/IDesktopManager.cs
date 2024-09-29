namespace DesktopManager;

/// <summary>
/// Provides methods to manage desktop settings and wallpapers.
/// </summary>
[ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopManager {
    /// <summary>
    /// Sets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="wallpaper">The path to the wallpaper image.</param>
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    /// <summary>
    /// Gets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The path to the wallpaper image.</returns>
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

    /// <summary>
    /// Gets the monitor device path.
    /// </summary>
    /// <param name="monitorIndex">Index of the monitor device in the monitor device list.</param>
    /// <returns>The device path of the monitor.</returns>
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    /// <summary>
    /// Gets the number of monitor device paths.
    /// </summary>
    /// <returns>The number of monitor device paths.</returns>
    [return: MarshalAs(UnmanagedType.U4)]
    uint GetMonitorDevicePathCount();

    /// <summary>
    /// Gets the bounds of a monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The bounds of the monitor.</returns>
    RECT GetMonitorBounds([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

    /// <summary>
    /// Sets the background color.
    /// </summary>
    /// <param name="color">The background color.</param>
    void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);

    /// <summary>
    /// Gets the background color.
    /// </summary>
    /// <returns>The background color.</returns>
    [return: MarshalAs(UnmanagedType.U4)]
    uint GetBackgroundColor();

    /// <summary>
    /// Sets the wallpaper position.
    /// </summary>
    /// <param name="position">The wallpaper position.</param>
    void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);

    /// <summary>
    /// Gets the wallpaper position.
    /// </summary>
    /// <returns>The wallpaper position.</returns>
    [return: MarshalAs(UnmanagedType.I4)]
    DesktopWallpaperPosition GetPosition();

    /// <summary>
    /// Sets the slideshow items.
    /// </summary>
    /// <param name="items">The slideshow items.</param>
    void SetSlideshow(IntPtr items);

    /// <summary>
    /// Gets the slideshow items.
    /// </summary>
    /// <returns>The slideshow items.</returns>
    IntPtr GetSlideshow();

    /// <summary>
    /// Sets the slideshow options.
    /// </summary>
    /// <param name="options">The slideshow direction options.</param>
    /// <param name="slideshowTick">The slideshow tick interval.</param>
    void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);

    /// <summary>
    /// Gets the slideshow options.
    /// </summary>
    /// <param name="options">The slideshow direction options.</param>
    /// <param name="slideshowTick">The slideshow tick interval.</param>
    /// <returns>The slideshow options.</returns>
    [PreserveSig]
    uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);

    /// <summary>
    /// Advances the slideshow in the specified direction.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="direction">The direction to advance the slideshow.</param>
    void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

    /// <summary>
    /// Gets the status of the slideshow.
    /// </summary>
    /// <returns>The status of the slideshow.</returns>
    DesktopSlideshowDirection GetStatus();

    /// <summary>
    /// Enables the desktop manager.
    /// </summary>
    /// <returns>True if enabled, otherwise false.</returns>
    bool Enable();
}

/// <summary>
/// Wrapper class for the desktop manager.
/// </summary>
[ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
internal class DesktopManagerWrapper {
}
