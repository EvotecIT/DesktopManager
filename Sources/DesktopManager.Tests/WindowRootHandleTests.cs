using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests root window handle resolution used by active-window hotkey targeting.
/// </summary>
public class WindowRootHandleTests {
    [TestMethod]
    /// <summary>
    /// Ensures child control handles resolve to the owning top-level window handle.
    /// </summary>
    public void GetRootWindowHandle_ChildControl_ReturnsTopLevelWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Root Handle Harness",
            form => {
                Button button = new() {
                    Text = "Session Surface",
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(button);
            });

        Control child = harness.Form.Controls[0];
        Assert.AreNotEqual(IntPtr.Zero, child.Handle);

        IntPtr root = WindowManager.GetRootWindowHandle(child.Handle);

        Assert.AreEqual(harness.Form.Handle, root);
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero handles remain zero during root resolution.
    /// </summary>
    public void GetRootWindowHandle_Zero_ReturnsZero() {
        Assert.AreEqual(IntPtr.Zero, WindowManager.GetRootWindowHandle(IntPtr.Zero));
    }
}
