using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class WindowGeometryTests {
    // A work area with an odd width and a taskbar at the bottom of a monitor left of the primary one.
    private static readonly RECT Area = new() { Left = -1921, Top = 0, Right = 0, Bottom = 1032 };

    [TestMethod]
    [DataRow(SnapPosition.Left, -1921, 0, -961, 1032)]
    [DataRow(SnapPosition.Right, -961, 0, 0, 1032)]
    [DataRow(SnapPosition.TopLeft, -1921, 0, -961, 516)]
    [DataRow(SnapPosition.BottomRight, -961, 516, 0, 1032)]
    [DataRow(SnapPosition.Top, -1921, 0, 0, 516)]
    [DataRow(SnapPosition.Bottom, -1921, 516, 0, 1032)]
    [DataRow(SnapPosition.LeftThird, -1921, 0, -1281, 1032)]
    [DataRow(SnapPosition.CenterThird, -1281, 0, -641, 1032)]
    [DataRow(SnapPosition.RightThird, -641, 0, 0, 1032)]
    public void GetSnapRectangle_CoversTheWorkAreaWithoutGapsBetweenNeighbours(SnapPosition position, int left, int top, int right, int bottom) {
        RECT rect = WindowManager.GetSnapRectangle(Area, position);
        Assert.AreEqual((left, top, right, bottom), (rect.Left, rect.Top, rect.Right, rect.Bottom));
    }

    [TestMethod]
    public void CenterInArea_KeepsTheSizeAndShrinksAWindowLargerThanTheWorkArea() {
        RECT centred = WindowManager.CenterInArea(Area, 801, 400);
        Assert.AreEqual((-1361, 316, -560, 716), (centred.Left, centred.Top, centred.Right, centred.Bottom));

        RECT shrunk = WindowManager.CenterInArea(Area, 2560, 1440);
        Assert.AreEqual((-1921, 0, 0, 1032), (shrunk.Left, shrunk.Top, shrunk.Right, shrunk.Bottom));
    }

    [TestMethod]
    public void ExpandForInvisibleFrame_PlacesTheVisibleFrameOnTheTarget() {
        // Windows 11 draws a 7-pixel invisible resize border left, right and below the visible frame.
        var windowRect = new RECT { Left = 93, Top = 100, Right = 507, Bottom = 407 };
        var frame = new RECT { Left = 100, Top = 100, Right = 500, Bottom = 400 };
        var target = new RECT { Left = 0, Top = 0, Right = 960, Bottom = 1032 };

        RECT placed = WindowManager.ExpandForInvisibleFrame(target, windowRect, frame);
        Assert.AreEqual((-7, 0, 967, 1039), (placed.Left, placed.Top, placed.Right, placed.Bottom));

        // A frame outside the window rectangle, as reported for some minimised or cloaked windows, is ignored.
        var bogus = new RECT { Left = 50, Top = 100, Right = 500, Bottom = 400 };
        RECT unchanged = WindowManager.ExpandForInvisibleFrame(target, windowRect, bogus);
        Assert.AreEqual((0, 0, 960, 1032), (unchanged.Left, unchanged.Top, unchanged.Right, unchanged.Bottom));
    }

    [TestMethod]
    public void KeepOriginOnMonitor_StopsTheBorderOfAnUnawareWindowCrossingOntoTheNextMonitor() {
        // Unclamped, Windows mapped this window through the left monitor's DPI and it doubled in width.
        var expanded = new RECT { Left = -11, Top = 0, Right = 1931, Bottom = 2099 };
        var monitor = new RECT { Left = 0, Top = 0, Right = 3840, Bottom = 2160 };

        RECT kept = WindowManager.KeepOriginOnMonitor(expanded, monitor);
        Assert.AreEqual((0, 0, 1931, 2099), (kept.Left, kept.Top, kept.Right, kept.Bottom));
    }

    [TestMethod]
    public void DescribeWindowOperationFailure_ExplainsAnElevatedTargetAndKeepsTheWin32Error() {
        string blocked = WindowManager.DescribeWindowOperationFailure("Set the window position", 5, targetElevated: true, callerElevated: false);
        StringAssert.Contains(blocked, "elevated");
        StringAssert.Contains(blocked, "Win32 error 5");

        string other = WindowManager.DescribeWindowOperationFailure("Set the window position", 1400, targetElevated: false, callerElevated: false);
        Assert.IsFalse(other.Contains("elevated"), other);
        StringAssert.Contains(other, "Win32 error 1400");

        // An elevated caller is not blocked by UIPI, so access denied is reported as it is.
        string elevatedCaller = WindowManager.DescribeWindowOperationFailure("Set the window position", 5, targetElevated: true, callerElevated: true);
        Assert.IsFalse(elevatedCaller.Contains("not elevated"), elevatedCaller);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void SnapWindow_FillsTheWorkAreaHalfWithTheVisibleFrame() {
        TestHelper.RequireOwnedWindowMutationTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Snap Frame Harness");
        var manager = new WindowManager();
        manager.SnapWindow(harness.Window, SnapPosition.Left);
        Application.DoEvents();

        (RECT window, RECT frame, RECT expected, RECT bounds) = WindowDpiContext.Execute(() => {
            Assert.IsTrue(GetWindowRect(harness.Form.Handle, out RECT rect));
            Assert.AreEqual(0, DwmGetWindowAttribute(harness.Form.Handle, 9, out RECT visible, Marshal.SizeOf<RECT>()));
            Monitor monitor = new Monitors().GetMonitors(refresh: true).First(candidate => candidate.Index == harness.Window.MonitorIndex);
            return (rect, visible, WindowManager.GetSnapRectangle(monitor.GetMonitorWorkArea(), SnapPosition.Left), WindowManager.GetBounds(monitor));
        });

        // The test host is DPI-unaware, so the window must not reach onto a monitor to the left: its own left edge
        // stays on the monitor and the visible frame starts one invisible border inside. The other edges are exact.
        AssertNear(new RECT { Left = Math.Max(expected.Left - (frame.Left - window.Left), bounds.Left), Top = expected.Top, Right = expected.Right, Bottom = expected.Bottom },
            new RECT { Left = window.Left, Top = frame.Top, Right = frame.Right, Bottom = frame.Bottom });
    }
    [TestMethod]
    [TestCategory("UITest")]
    public void SetWindowRectangle_OnAMinimisedWindow_MovesWhereItRestoresAndKeepsItMinimised() {
        TestHelper.RequireOwnedWindowMutationTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Minimised Placement Harness");
        ShowWindow(harness.Form.Handle, 7); // SW_SHOWMINNOACTIVE
        Application.DoEvents();
        Assert.IsTrue(IsIconic(harness.Form.Handle));

        new WindowManager().SetWindowRectangle(harness.Window, 300, 200, 400, 300);
        Application.DoEvents();

        Assert.IsTrue(IsIconic(harness.Form.Handle), "The window must stay minimised.");
        RECT normal = WindowDpiContext.Execute(() => WorkspaceWindowPlacement.GetNormalBounds(harness.Form.Handle));
        AssertNear(new RECT { Left = 300, Top = 200, Right = 700, Bottom = 500 }, normal);
    }

    // A DPI-unaware test window on a scaled monitor can round its logical size by a pixel.
    private static void AssertNear(RECT expected, RECT actual) {
        foreach ((int value, int wanted) in new[] {
            (actual.Left, expected.Left), (actual.Top, expected.Top), (actual.Right, expected.Right), (actual.Bottom, expected.Bottom) }) {
            Assert.IsTrue(Math.Abs(value - wanted) <= 2,
                $"Got {actual.Left},{actual.Top},{actual.Right},{actual.Bottom}; expected {expected.Left},{expected.Top},{expected.Right},{expected.Bottom}.");
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out RECT value, int size);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);
}
