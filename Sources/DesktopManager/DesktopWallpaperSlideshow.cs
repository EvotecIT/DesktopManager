using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Represents the current desktop wallpaper slideshow configuration and runtime state.
/// </summary>
public sealed class DesktopWallpaperSlideshow {
    /// <summary>
    /// Gets or sets the slideshow item paths.
    /// </summary>
    public IReadOnlyList<string> ImagePaths { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the current slideshow state flags.
    /// </summary>
    public DesktopSlideshowState State { get; set; }

    /// <summary>
    /// Gets or sets the current slideshow options.
    /// </summary>
    public DesktopSlideshowOptions Options { get; set; }

    /// <summary>
    /// Gets or sets the slideshow tick interval in milliseconds.
    /// </summary>
    public uint SlideshowTick { get; set; }

    /// <summary>
    /// Gets a value indicating whether slideshow support is enabled.
    /// </summary>
    public bool IsEnabled => State.HasFlag(DesktopSlideshowState.Enabled);

    /// <summary>
    /// Gets a value indicating whether a slideshow is currently running.
    /// </summary>
    public bool IsRunning => State.HasFlag(DesktopSlideshowState.Slideshow);

    /// <summary>
    /// Gets a value indicating whether slideshow is disabled because the session is remote.
    /// </summary>
    public bool IsDisabledByRemoteSession => State.HasFlag(DesktopSlideshowState.DisabledByRemoteSession);

    /// <summary>
    /// Gets a value indicating whether slideshow images can be randomized.
    /// </summary>
    public bool ShuffleImages => Options.HasFlag(DesktopSlideshowOptions.ShuffleImages);
}
