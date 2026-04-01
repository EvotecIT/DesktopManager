using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowTopMostActivationTests.
/// </summary>
public class WindowTopMostActivationTests {
    private const int FocusTimeoutMilliseconds = 5000;

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Test for SetWindowTopMost_TogglesState.
    /// </summary>
    public void SetWindowTopMost_TogglesState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        using var session = DesktopManagerTestAppSession.Start("topmost-toggle");
        WindowInfo window = session.ResolveWindowInfo();

        var manager = new WindowManager();

        long originalStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool wasTop = (originalStyle & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        // Set to opposite of current state
        manager.SetWindowTopMost(window, !wasTop);

        // Give the system time to process the change
        Thread.Sleep(100);

        long toggled = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool newIsTop = (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        Assert.AreEqual(!wasTop, newIsTop, $"Expected topmost to change from {wasTop} to {!wasTop}, but got {newIsTop}");

        // Test changing back
        manager.SetWindowTopMost(window, wasTop);
        Thread.Sleep(100);

        long restored = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool restoredIsTop = (restored & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        Assert.AreEqual(wasTop, restoredIsTop, $"Expected topmost to restore to {wasTop}, but got {restoredIsTop}");
    }

    [TestMethod]
    /// <summary>
    /// Test for ActivateWindow_BringsToFront.
    /// </summary>
    public void ActivateWindow_BringsToFront() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        var manager = new WindowManager();
        using var firstSession = DesktopManagerTestAppSession.Start("activation-first");
        using var secondSession = DesktopManagerTestAppSession.Start("activation-second");
        WindowInfo firstWindow = firstSession.ResolveWindowInfo();
        WindowInfo secondWindow = secondSession.ResolveWindowInfo();

        try {
            manager.ActivateWindow(secondWindow);
            secondSession.WaitForEditorForeground(
                FocusTimeoutMilliseconds,
                "The second repo-owned DesktopManager test app window did not become the foreground target.");

            manager.ActivateWindow(firstWindow);
            firstSession.WaitForEditorForeground(
                FocusTimeoutMilliseconds,
                "The first repo-owned DesktopManager test app window did not become the foreground target.");

            IntPtr newForeground = MonitorNativeMethods.GetForegroundWindow();
            if (newForeground == IntPtr.Zero) {
                Assert.Inconclusive("GetForegroundWindow returned 0 after activation attempt.");
            }

            Assert.AreEqual(firstSession.WindowHandle, newForeground,
                $"Expected the repo-owned DesktopManager test app window to become foreground. Expected: {firstSession.WindowHandle:X8}, Actual: {newForeground:X8}");
        } catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to activate window")) {
            Assert.Inconclusive($"Window activation failed due to Windows security policies. Target window: Handle={firstSession.WindowHandle:X8}. This is expected behavior in many Windows configurations due to User Interface Privilege Isolation (UIPI) or other focus management policies.");
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures window activation preserves a maximized repo-owned harness window.
    /// </summary>
    public void ActivateWindow_MaximizedWindow_RemainsMaximized() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        using var session = DesktopManagerTestAppSession.Start("activation-maximized");
        WindowManager manager = new();
        WindowInfo window = session.ResolveWindowInfo();

        manager.MaximizeWindow(window);
        Thread.Sleep(150);
        manager.ActivateWindow(window);
        session.WaitForEditorForeground(
            FocusTimeoutMilliseconds,
            "The maximized repo-owned DesktopManager test app window did not become the foreground target.");

        WindowInfo refreshedWindow = session.ResolveWindowInfo();
        Assert.AreEqual(WindowState.Maximize, refreshedWindow.State, "Expected activation to preserve the maximized window state.");
    }
}
