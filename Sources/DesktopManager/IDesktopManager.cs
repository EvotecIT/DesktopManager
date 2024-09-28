namespace DesktopManager;

[ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopManager {
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
    [return: MarshalAs(UnmanagedType.LPWStr)]

    string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

    /// <summary>
    /// Gets the monitor device path.
    /// </summary>
    /// <param name="monitorIndex">Index of the monitor device in the monitor device list.</param>
    /// <returns></returns>
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);
    /// <summary>
    /// Gets number of monitor device paths.
    /// </summary>
    /// <returns></returns>
    [return: MarshalAs(UnmanagedType.U4)]


    uint GetMonitorDevicePathCount();
    [return: MarshalAs(UnmanagedType.Struct)]


    RECT GetMonitorBounds([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

    void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
    [return: MarshalAs(UnmanagedType.U4)]


    uint GetBackgroundColor();

    void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);
    [return: MarshalAs(UnmanagedType.I4)]

    DesktopWallpaperPosition GetPosition(); // this only returns single value, when multiple monitors are connected it's the same value for all monitors

    void SetSlideshow(IntPtr items);

    IntPtr GetSlideshow();

    void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);

    [PreserveSig]
    uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);

    void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

    DesktopSlideshowDirection GetStatus();

    bool Enable();
}

[ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
internal class DesktopManagerWrapper {
}
