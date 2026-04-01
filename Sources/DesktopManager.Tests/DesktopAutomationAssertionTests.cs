using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared desktop automation assertions.
/// </summary>
public class DesktopAutomationAssertionTests {
    private const int FocusTimeoutMilliseconds = 5000;

    [TestMethod]
    /// <summary>
    /// Ensures a query by the active window handle reports that the window exists.
    /// </summary>
    public void WindowExists_ActiveWindowHandle_ReturnsTrue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        var automation = new DesktopAutomationService();
        using var session = DesktopManagerTestAppSession.Start("assertion-exists");
        session.FocusEditorWindow(FocusTimeoutMilliseconds);

        WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (activeWindow == null) {
            Assert.Inconclusive("No active window could be resolved.");
        }

        Assert.AreEqual(session.WindowHandle, activeWindow.Handle, "Expected the repo-owned DesktopManager test app window to be active for the assertion test.");

        bool result = automation.WindowExists(new WindowQueryOptions {
            Handle = activeWindow.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.IsTrue(result);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the active window matches a selector targeting its own handle.
    /// </summary>
    public void ActiveWindowMatches_ActiveWindowHandle_ReturnsTrue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        var automation = new DesktopAutomationService();
        using var session = DesktopManagerTestAppSession.Start("assertion-active");
        session.FocusEditorWindow(FocusTimeoutMilliseconds);

        WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (activeWindow == null) {
            Assert.Inconclusive("No active window could be resolved.");
        }

        Assert.AreEqual(session.WindowHandle, activeWindow.Handle, "Expected the repo-owned DesktopManager test app window to be active for the assertion test.");

        bool result = automation.ActiveWindowMatches(new WindowQueryOptions {
            Handle = activeWindow.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.IsTrue(result);
    }
}
