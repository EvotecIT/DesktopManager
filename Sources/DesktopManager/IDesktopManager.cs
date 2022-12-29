using System;
using System.Runtime.InteropServices;

namespace DesktopManager {
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// This enumeration is used to set and get slide show options.
    /// </summary> 
    public enum DesktopSlideShowOptions {
        /// <summary>
        /// When set, indicates that the order in which images in the slide show are displayed can be randomized
        /// </summary>
        ShuffleImages = 0x01
    }


    /// <summary>
    /// This enumeration is used by GetStatus to indicate the current status of the slideshow.
    /// </summary>
    public enum DesktopSlideShowState {
        Enabled = 0x01,
        Slideshow = 0x02,
        DisabledByRemoteSession = 0x04,
    }


    /// <summary>
    /// This enumeration is used by the AdvanceSlideshow method to indicate whether to advance the slide show forward or backward.
    /// </summary>
    public enum DesktopSlideshowDirection {
        Forward = 0,
        Backward = 1,
    }

    /// <summary>
    /// This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
    /// The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
    /// </summary>
    public enum DesktopWallpaperPosition {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDesktopManager {
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


        Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

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
}
