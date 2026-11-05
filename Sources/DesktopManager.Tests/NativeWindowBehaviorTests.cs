using System.Collections.Concurrent;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>Exercises the window event watcher and activation styles against a real, never-activated window.</summary>
public class NativeWindowBehaviorTests {
    private const int WsPopup = unchecked((int)0x80000000);
    private const int SwShowNoActivate = 4;

    [TestMethod]
    /// <summary>The watcher's own thread reports show and destroy for the window that caused them.</summary>
    public void WindowEventWatcher_ReportsShownAndDestroyedForWindow() {
        var events = new BlockingCollection<WindowChangedEventArgs>();
        using var watcher = new WindowEventWatcher(WindowEventKinds.Shown | WindowEventKinds.Destroyed);
        watcher.WindowChanged += (_, e) => events.Add(e);

        IntPtr window = CreateHiddenWindow();
        MonitorNativeMethods.ShowWindow(window, SwShowNoActivate);
        MonitorNativeMethods.DestroyWindow(window);

        Assert.IsTrue(WaitFor(events, window, WindowEventKinds.Shown), "No Shown event arrived for the window.");
        Assert.IsTrue(WaitFor(events, window, WindowEventKinds.Destroyed), "No Destroyed event arrived for the window.");
    }

    [TestMethod]
    /// <summary>Removing only the activation style keeps the window out of the task switcher.</summary>
    public void NonActivatingWindow_AppliesAndRemovesStylesIndependently() {
        IntPtr window = CreateHiddenWindow();
        try {
            NonActivatingWindow.Apply(window);
            Assert.IsTrue(NonActivatingWindow.IsApplied(window));
            Assert.IsTrue(HasToolWindowStyle(window));

            NonActivatingWindow.Remove(window, showInTaskSwitcher: false);
            Assert.IsFalse(NonActivatingWindow.IsApplied(window));
            Assert.IsTrue(HasToolWindowStyle(window));

            NonActivatingWindow.Remove(window);
            Assert.IsFalse(HasToolWindowStyle(window));
        } finally {
            MonitorNativeMethods.DestroyWindow(window);
        }
    }

    private static IntPtr CreateHiddenWindow() {
        IntPtr window = MonitorNativeMethods.CreateWindowExW(0, "STATIC", "DesktopManager behaviour test", WsPopup,
            -32000, -32000, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (window == IntPtr.Zero) {
            Assert.Inconclusive("This session cannot create a window.");
        }

        return window;
    }

    private static bool HasToolWindowStyle(IntPtr window) =>
        (MonitorNativeMethods.GetWindowLongPtr(window, MonitorNativeMethods.GWL_EXSTYLE).ToInt64() & 0x80) != 0;

    private static bool WaitFor(BlockingCollection<WindowChangedEventArgs> events, IntPtr window, WindowEventKinds kind) {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline) {
            if (events.TryTake(out WindowChangedEventArgs? next, TimeSpan.FromMilliseconds(100)) && next.Handle == window && next.Kind == kind) {
                return true;
            }
        }

        return false;
    }
}
