using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public sealed class ScreenshotDpiTests {
    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);

    [TestMethod]
    public void CaptureWindow_UnawareCallerGetsPhysicalPixelsAndConsistentClientGeometry() {
        TestHelper.RequireOwnedWindowUiTests();
        RunOnSta(() => {
            IntPtr original = MonitorNativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-3));
            Assert.AreNotEqual(IntPtr.Zero, original);
            try {
                using var harness = WinFormsWindowHarness.Create("DPI capture owned window", 440, 280);
                using var button = new Button { Text = "Physical pixel control", Width = 180, Height = 50 };
                harness.Form.Controls.Add(button);
                button.CreateControl();
                Application.DoEvents();
                Assert.IsTrue(MonitorNativeMethods.GetWindowRect(harness.Window.Handle, out RECT physical));
                Assert.IsTrue(MonitorNativeMethods.GetWindowRect(button.Handle, out RECT physicalControl));
                MonitorNativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-1));
                Assert.IsTrue(MonitorNativeMethods.GetWindowRect(harness.Window.Handle, out RECT virtualized));
                TestContext.WriteLine($"Physical {physical.Right - physical.Left}x{physical.Bottom - physical.Top}; unaware {virtualized.Right - virtualized.Left}x{virtualized.Bottom - virtualized.Top}");

                using WindowCaptureResult capture = ScreenshotService.CaptureWindowDetailed(harness.Window.Handle);
                Assert.AreEqual(physical.Right - physical.Left, capture.Bitmap.Width);
                Assert.AreEqual(physical.Bottom - physical.Top, capture.Bitmap.Height);
                Assert.AreEqual(new Rectangle(physical.Left, physical.Top, capture.Bitmap.Width, capture.Bitmap.Height), capture.ScreenBounds);
                AssertUnaware();
                var automation = new DesktopAutomationService();
                using DesktopCapture full = automation.CaptureWindow(harness.Window.Handle);
                Assert.AreEqual(capture.ScreenBounds, full.CapturedScreenBounds);
                Assert.AreEqual(capture.Bitmap.Width, full.Geometry!.WindowWidth);
                Assert.AreEqual(capture.Bitmap.Height, full.Geometry.WindowHeight);
                AssertUnaware();
                using DesktopCapture client = automation.CaptureWindowClientArea(harness.Window.Handle);
                Assert.AreEqual(full.Geometry.ClientWidth, client.Bitmap.Width);
                Assert.AreEqual(full.Geometry.ClientHeight, client.Bitmap.Height);
                Assert.AreEqual(full.Geometry.ClientLeft, client.CapturedScreenBounds!.Value.Left);
                Assert.AreEqual(full.Geometry.ClientTop, client.CapturedScreenBounds.Value.Top);
                AssertUnaware();
                WindowControlInfo descriptor = new ControlEnumerator().EnumerateControls(harness.Window.Handle).Single(item => item.Handle == button.Handle);
                descriptor.ParentWindowHandle = harness.Window.Handle;
                int originalWidth = descriptor.Width;
                using DesktopCapture control = automation.CaptureControl(descriptor);
                Assert.AreEqual(physicalControl.Right - physicalControl.Left, control.Bitmap.Width);
                Assert.AreEqual(physicalControl.Bottom - physicalControl.Top, control.Bitmap.Height);
                Assert.AreEqual(full.Geometry.WindowWidth, control.Geometry!.WindowWidth);
                Assert.AreEqual(physicalControl.Left, control.Control!.Left);
                Assert.AreEqual(physicalControl.Top, control.Control.Top);
                Assert.AreEqual(control.Bitmap.Width, control.Control.Width);
                Assert.AreEqual(control.Bitmap.Height, control.Control.Height);
                Assert.AreEqual(originalWidth, descriptor.Width, "Capture must not rewrite a caller's enumerated descriptor.");
                Assert.AreNotSame(descriptor, control.Control);
                using DesktopCapture byHandle = automation.CaptureControl(harness.Window.Handle, button.Handle, useUiAutomation: false, includeUiAutomation: false);
                Assert.AreEqual(control.CapturedScreenBounds, byHandle.CapturedScreenBounds);
                Assert.AreEqual(control.Control.Width, byHandle.Control!.Width);
                AssertUnaware();
                string targetName = "dpi-owned-" + Guid.NewGuid().ToString("N");
                string targetPath = DesktopStateStore.GetTargetPath(targetName);
                try {
                    automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
                        X = 0, Y = 0, WidthRatio = 1, HeightRatio = 1, ClientArea = true
                    });
                    using DesktopCapture target = automation.CaptureWindowTarget(new WindowQueryOptions { Handle = harness.Window.Handle }, targetName);
                    Assert.AreEqual(client.CapturedScreenBounds, target.CapturedScreenBounds);
                    Assert.AreEqual(client.Bitmap.Size, target.Bitmap.Size);
                    AssertUnaware();
                } finally { File.Delete(targetPath); }
                Assert.ThrowsExactly<InvalidOperationException>(() => ScreenshotService.CaptureWindowDetailed(new IntPtr(-100)));
                AssertUnaware();
                Assert.ThrowsExactly<InvalidOperationException>(() => automation.CaptureWindow(new IntPtr(-100)));
                AssertUnaware();
            } finally { MonitorNativeMethods.SetThreadDpiAwarenessContext(original); }
        });
    }

    public TestContext TestContext { get; set; } = null!;

    private static void AssertUnaware() => Assert.AreEqual(0, GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()), "Capture must restore its caller's DPI context.");

    private static void RunOnSta(Action action) {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) { ExceptionDispatchInfo.Capture(failure).Throw(); }
    }
}
