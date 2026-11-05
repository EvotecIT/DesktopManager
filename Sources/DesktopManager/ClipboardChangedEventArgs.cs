using System;

namespace DesktopManager;

/// <summary>
/// Describes one recordable copy reported by <see cref="ClipboardWatcher"/>: Unicode text or an image.
/// </summary>
public sealed class ClipboardChangedEventArgs : EventArgs {
    /// <summary>Initializes a new instance of the <see cref="ClipboardChangedEventArgs"/> class.</summary>
    /// <param name="text">The copied text, or <c>null</c> when an image was copied.</param>
    /// <param name="deviceIndependentBitmap">The copied image in <c>CF_DIB</c> form, or <c>null</c> when text was copied.</param>
    /// <param name="timestamp">When the copy was read.</param>
    public ClipboardChangedEventArgs(string? text, byte[]? deviceIndependentBitmap, DateTimeOffset timestamp) {
        Text = text;
        DeviceIndependentBitmap = deviceIndependentBitmap;
        Timestamp = timestamp;
    }

    /// <summary>Gets the copied text, or <c>null</c> when the copy is an image.</summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the copied image as <c>CF_DIB</c> bytes (a <c>BITMAPINFO</c> header, any color table, then the pixels),
    /// or <c>null</c> when the copy is text. Prepend a 14-byte <c>BITMAPFILEHEADER</c> to save it as a .bmp file.
    /// </summary>
    public byte[]? DeviceIndependentBitmap { get; }

    /// <summary>Gets when the copy was read.</summary>
    public DateTimeOffset Timestamp { get; }
}
