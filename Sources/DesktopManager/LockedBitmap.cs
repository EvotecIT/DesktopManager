using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Reads sampled pixels while holding a bitmap lock, without copying the full image.</summary>
internal sealed class LockedBitmap : IDisposable {
    private readonly Bitmap _bitmap;
    private readonly BitmapData _data;
    internal LockedBitmap(Bitmap bitmap) {
        _bitmap = bitmap;
        Width = bitmap.Width;
        Height = bitmap.Height;
        _data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    }
    internal int Width { get; }
    internal int Height { get; }
    internal void GetRgb(int x, int y, out int red, out int green, out int blue) {
        int pixel = Marshal.ReadInt32(IntPtr.Add(_data.Scan0, checked(y * _data.Stride + x * 4)));
        blue = pixel & 255;
        green = (pixel >> 8) & 255;
        red = (pixel >> 16) & 255;
    }
    public void Dispose() { _bitmap.UnlockBits(_data); }
}
