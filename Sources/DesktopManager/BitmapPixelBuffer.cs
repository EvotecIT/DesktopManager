using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Provides a managed BGRA snapshot for fast repeated bitmap sampling.
/// </summary>
internal sealed class BitmapPixelBuffer {
    private const int BytesPerPixel = 4;
    private readonly byte[] _pixels;

    private BitmapPixelBuffer(int width, int height, int stride, byte[] pixels) {
        Width = width;
        Height = height;
        Stride = stride;
        _pixels = pixels;
    }

    internal int Width { get; }

    internal int Height { get; }

    private int Stride { get; }

    internal static BitmapPixelBuffer Create(Bitmap bitmap) {
        if (bitmap == null) {
            throw new ArgumentNullException(nameof(bitmap));
        }

        Rectangle bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try {
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[checked(stride * bitmap.Height)];
            for (int row = 0; row < bitmap.Height; row++) {
                IntPtr rowAddress = IntPtr.Add(data.Scan0, checked(row * data.Stride));
                Marshal.Copy(rowAddress, pixels, checked(row * stride), stride);
            }

            return new BitmapPixelBuffer(bitmap.Width, bitmap.Height, stride, pixels);
        } finally {
            bitmap.UnlockBits(data);
        }
    }

    internal void GetRgb(int x, int y, out int red, out int green, out int blue) {
        int offset = checked((y * Stride) + (x * BytesPerPixel));
        blue = _pixels[offset];
        green = _pixels[offset + 1];
        red = _pixels[offset + 2];
    }
}
