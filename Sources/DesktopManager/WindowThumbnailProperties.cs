namespace DesktopManager;

/// <summary>Describes changes to a <see cref="WindowThumbnail"/>. Properties left null keep their current value.</summary>
public sealed class WindowThumbnailProperties {
    /// <summary>Gets or sets where the thumbnail is drawn, in the destination window's client coordinates and physical pixels.</summary>
    public RECT? Destination { get; set; }

    /// <summary>Gets or sets the part of the source window to show, in source window coordinates. Null keeps the whole source.</summary>
    public RECT? Source { get; set; }

    /// <summary>Gets or sets the thumbnail opacity from 0 (transparent) through 255 (opaque).</summary>
    public byte? Opacity { get; set; }

    /// <summary>Gets or sets whether DWM draws the thumbnail.</summary>
    public bool? Visible { get; set; }

    /// <summary>Gets or sets whether only the source window's client area is shown, without its frame.</summary>
    public bool? SourceClientAreaOnly { get; set; }

    internal DwmThumbnailProperties ToNative() {
        var native = new DwmThumbnailProperties();
        if (Destination.HasValue) {
            native.Flags |= DwmThumbnailProperties.DestinationFlag;
            native.Destination = Destination.Value;
        }
        if (Source.HasValue) {
            native.Flags |= DwmThumbnailProperties.SourceFlag;
            native.Source = Source.Value;
        }
        if (Opacity.HasValue) {
            native.Flags |= DwmThumbnailProperties.OpacityFlag;
            native.Opacity = Opacity.Value;
        }
        if (Visible.HasValue) {
            native.Flags |= DwmThumbnailProperties.VisibleFlag;
            native.Visible = Visible.Value ? 1 : 0;
        }
        if (SourceClientAreaOnly.HasValue) {
            native.Flags |= DwmThumbnailProperties.SourceClientAreaOnlyFlag;
            native.SourceClientAreaOnly = SourceClientAreaOnly.Value ? 1 : 0;
        }
        return native;
    }
}
