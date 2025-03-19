using System;

namespace DesktopManager {
    /// <summary>
    /// Represents basic information about a window.
    /// </summary>
    public class WindowInfo {
        /// <summary>
        /// Gets or sets the window title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the window handle.
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Gets or sets the process ID of the window.
        /// </summary>
        public uint ProcessId { get; set; }

        /// <summary>
        /// Gets the width of the window.
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// Gets the height of the window.
        /// </summary>
        public int Height => Bottom - Top;

        /// <summary>
        /// Gets or sets the left position of the window.
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Gets or sets the top position of the window.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Gets or sets the right position of the window.
        /// </summary>
        public int Right { get; set; }

        /// <summary>
        /// Gets or sets the bottom position of the window.
        /// </summary>
        public int Bottom { get; set; }
    }

    /// <summary>
    /// Represents the position and size information of a window.
    /// </summary>
    public class WindowPosition : WindowInfo {
    }
}