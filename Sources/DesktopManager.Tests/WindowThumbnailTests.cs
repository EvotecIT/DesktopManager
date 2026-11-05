using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
public sealed class WindowThumbnailTests {
    [TestMethod]
    public void ThumbnailProperties_NativeLayout_MatchesWindowsAbi() {
        // DWM_THUMBNAIL_PROPERTIES: flags, two RECTs, a BYTE opacity padded to the following BOOLs.
        Assert.AreEqual(48, Marshal.SizeOf<DwmThumbnailProperties>());
        Assert.AreEqual(new IntPtr(36), Marshal.OffsetOf<DwmThumbnailProperties>(nameof(DwmThumbnailProperties.Opacity)));
        Assert.AreEqual(new IntPtr(40), Marshal.OffsetOf<DwmThumbnailProperties>(nameof(DwmThumbnailProperties.Visible)));
        Assert.AreEqual(new IntPtr(44), Marshal.OffsetOf<DwmThumbnailProperties>(nameof(DwmThumbnailProperties.SourceClientAreaOnly)));
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void Thumbnail_ShowsSourceInsideDestination_AndRejectsUseAfterDispose() {
        TestHelper.RequireOwnedWindowUiTests();
        using Form destination = new NonActivatingTestForm { ShowInTaskbar = false, Width = 320, Height = 240, StartPosition = FormStartPosition.Manual, Left = 0, Top = 0 };
        using Form source = new NonActivatingTestForm { ShowInTaskbar = false, Width = 400, Height = 300, StartPosition = FormStartPosition.Manual, Left = 40, Top = 40 };
        destination.Show();
        source.Show();

        var thumbnail = new WindowThumbnail(destination.Handle, source.Handle);
        try {
            // DWM reports physical pixels, which differ from a DPI-virtualized caller's window size; compare live changes instead.
            Assert.IsTrue(thumbnail.TryGetSourceSize(out int width, out int height));
            source.Width = 800;
            Assert.IsTrue(thumbnail.TryGetSourceSize(out int widerWidth, out int widerHeight));
            Assert.IsTrue(widerWidth > width * 3 / 2, $"The source size must follow the live window: {width}x{height} then {widerWidth}x{widerHeight}.");
            Assert.AreEqual(height, widerHeight);

            thumbnail.Update(new WindowThumbnailProperties {
                Destination = new RECT { Left = 0, Top = 0, Right = 200, Bottom = 150 },
                Source = new RECT { Left = 0, Top = 0, Right = width / 2, Bottom = height / 2 },
                Opacity = 200,
                Visible = true,
                SourceClientAreaOnly = true
            });
            thumbnail.Update(new WindowThumbnailProperties { Visible = false });
        } finally {
            thumbnail.Dispose();
            thumbnail.Dispose();
        }
        Assert.ThrowsExactly<ObjectDisposedException>(() => thumbnail.Update(new WindowThumbnailProperties { Visible = true }));
        Assert.ThrowsExactly<ArgumentException>(() => new WindowThumbnail(IntPtr.Zero, source.Handle));
    }
}
