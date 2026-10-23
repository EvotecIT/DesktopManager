using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public sealed class WindowLayoutPlacementTests {
    [TestMethod]
    [DataRow(WindowState.Normal, false)]
    [DataRow(WindowState.Maximize, false)]
    [DataRow(WindowState.Minimize, false)]
    [DataRow(WindowState.Minimize, true)]
    public void SaveAndLoadLayout_PreservesNormalBoundsStateAndNextRestore(WindowState state, bool restoreMaximized) {
        TestHelper.RequireOwnedWindowMutationTests();
        Exception? failure = null;
        var thread = new Thread(() => {
            try { VerifyRoundTrip(state, restoreMaximized); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) { ExceptionDispatchInfo.Capture(failure).Throw(); }
    }

    private static void VerifyRoundTrip(WindowState state, bool restoreMaximized) {
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Layout placement " + Guid.NewGuid().ToString("N"), 420, 300);
        var manager = new WindowManager();
        // Every mutation is confined to this test's window, including foreground suppression.
        manager.SetWindowStyle(harness.Window, 0x08000000, true, extended: true);
        WindowPosition original = manager.GetWindowPosition(harness.Window);
        string path = Path.GetTempFileName();
        try {
            if (state == WindowState.Maximize || restoreMaximized) { MonitorNativeMethods.ShowWindow(harness.Window.Handle, 3); }
            if (state == WindowState.Minimize) { MonitorNativeMethods.ShowWindow(harness.Window.Handle, 7); }
            Application.DoEvents();
            Assert.AreEqual(state, manager.GetWindowPosition(harness.Window).State);

            manager.SaveLayout(path);
            WindowLayout full = JsonSerializer.Deserialize<WindowLayout>(File.ReadAllText(path))!;
            WindowPosition saved = full.Windows.Single(window => window.Title == original.Title && window.ProcessId == original.ProcessId);
            AssertBounds(original, saved);
            Assert.AreEqual(state, saved.State);
            if (state == WindowState.Minimize) { Assert.AreEqual(restoreMaximized, saved.RestoreToMaximized); }

            // Persist only the owned window before loading, so unrelated windows are never moved.
            File.WriteAllText(path, JsonSerializer.Serialize(new WindowLayout { Windows = new List<WindowPosition> { saved } }));
            MonitorNativeMethods.ShowWindow(harness.Window.Handle, 4);
            manager.SetWindowRectangle(harness.Window, original.Left + 30, original.Top + 30, original.Width + 40, original.Height + 40);
            manager.LoadLayout(path, validate: true);
            Application.DoEvents();
            Assert.AreEqual(state, manager.GetWindowPosition(harness.Window).State);

            // Read the OS placement directly rather than comparing capture with itself.
            var native = new NativePlacement { Length = Marshal.SizeOf<NativePlacement>() };
            Assert.IsTrue(GetWindowPlacement(harness.Window.Handle, ref native));
            Assert.AreEqual(restoreMaximized, state == WindowState.Minimize && (native.Flags & 2) != 0);
            MonitorNativeMethods.ShowWindow(harness.Window.Handle, 9);
            Application.DoEvents();
            Assert.AreEqual(restoreMaximized ? WindowState.Maximize : WindowState.Normal,
                manager.GetWindowPosition(harness.Window).State);
            MonitorNativeMethods.ShowWindow(harness.Window.Handle, 4);
            Application.DoEvents();
            AssertBounds(original, manager.GetWindowPosition(harness.Window));
        } finally { File.Delete(path); }
    }

    private static void AssertBounds(WindowPosition expected, WindowPosition actual) {
        Assert.AreEqual(expected.Left, actual.Left, "Normal left");
        Assert.AreEqual(expected.Top, actual.Top, "Normal top");
        Assert.AreEqual(expected.Right, actual.Right, "Normal right");
        Assert.AreEqual(expected.Bottom, actual.Bottom, "Normal bottom");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePlacement {
        internal int Length, Flags, ShowCommand;
        internal System.Drawing.Point Minimum, Maximum;
        internal RECT Normal;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr window, ref NativePlacement placement);
}
