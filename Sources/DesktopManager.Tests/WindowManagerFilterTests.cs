using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for WindowManager filtering features.
/// </summary>
public class WindowManagerFilterTests {
    private const int FocusTimeoutMilliseconds = 5000;

    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    /// <summary>
    /// Ensures filtering by process name returns matching window.
    /// </summary>
    public void GetWindows_ProcessNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowUiTests();

        string title = $"ProcessName Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);
        using var process = Process.GetCurrentProcess();

        var manager = new WindowManager();
        var filtered = manager.GetWindows(processName: process.ProcessName, includeHidden: true);

        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find harness window {harness.Window.Handle:X8} for process '{process.ProcessName}'.");

        foreach (var window in filtered.Take(5)) {
            try {
                using Process matchingProcess = Process.GetProcessById((int)window.ProcessId);
                Assert.AreEqual(process.ProcessName, matchingProcess.ProcessName,
                    $"Window {window.Handle:X8} has process '{matchingProcess.ProcessName}' but filter was for '{process.ProcessName}'.");
            } catch {
                // Process might have exited, skip verification for this window.
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures filtering by process ID returns matching window.
    /// </summary>
    public void GetWindows_ProcessIdFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        string title = $"ProcessId Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);
        using var process = Process.GetCurrentProcess();

        var manager = new WindowManager();
        var windows = manager.GetWindows(processId: process.Id, includeHidden: true);
        Assert.IsTrue(windows.Any(w => w.Handle == harness.Window.Handle));

        var windows2 = manager.GetWindowsForProcess(process, includeHidden: true);
        Assert.IsTrue(windows2.Any(w => w.Handle == harness.Window.Handle));
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by class name returns matching window.
    /// </summary>
    public void GetWindows_ClassNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Class Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        var manager = new WindowManager();
        string classNameFilter = GetClassName(harness.Window.Handle);
        var filtered = manager.GetWindows(className: classNameFilter, includeHidden: true);

        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find window {harness.Window.Handle:X8} with class '{classNameFilter}' in filtered results");
    }

    [TestMethod]
    /// <summary>
    /// Ensures regex filtering returns matching window.
    /// </summary>
    public void GetWindows_RegexFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Regex Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        var manager = new WindowManager();
        var regex = new Regex(Regex.Escape(title), RegexOptions.IgnoreCase);
        var filtered = manager.GetWindows(regex: regex, includeHidden: true);
        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find window {harness.Window.Handle:X8} with title '{title}' in regex filtered results");
    }

    [TestMethod]
    /// <summary>
    /// Ensures combined filters via options return the expected window.
    /// </summary>
    public void GetWindows_OptionsCombinedFilters_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Combined Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);
        using var process = Process.GetCurrentProcess();

        var manager = new WindowManager();
        string selectedClassName = GetClassName(harness.Window.Handle);
        string selectedProcessName = process.ProcessName;

        var options = new WindowQueryOptions {
            TitlePattern = title,
            ProcessNamePattern = selectedProcessName,
            ClassNamePattern = selectedClassName,
            ProcessId = process.Id,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IsVisible = harness.Window.IsVisible,
            State = harness.Window.State,
            IsTopMost = harness.Window.IsTopMost
        };

        var filtered = manager.GetWindows(options);
        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find window {harness.Window.Handle:X8} using combined filters");
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by exact handle returns the matching window.
    /// </summary>
    public void GetWindows_HandleFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Handle Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        var manager = new WindowManager();
        var filtered = manager.GetWindows(new WindowQueryOptions {
            Handle = harness.Window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.AreEqual(1, filtered.Count, "Expected an exact handle filter to return a single window.");
        Assert.AreEqual(harness.Window.Handle, filtered[0].Handle, "Expected the filtered window to match the requested handle.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the active window helper resolves the current foreground window.
    /// </summary>
    public void GetActiveWindow_ReturnsForegroundWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        using var firstSession = DesktopManagerTestAppSession.Start("foreground-filter-first");
        using var secondSession = DesktopManagerTestAppSession.Start("foreground-filter-second");

        var manager = new WindowManager();
        secondSession.FocusEditorWindow(FocusTimeoutMilliseconds);
        firstSession.FocusEditorWindow(FocusTimeoutMilliseconds);

        IntPtr foreground = MonitorNativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero) {
            Assert.Inconclusive("No foreground window found to test");
        }

        WindowInfo? window = manager.GetActiveWindow();
        if (window == null) {
            Assert.Inconclusive("The active window could not be resolved from enumeration");
        }

        Assert.AreEqual(firstSession.WindowHandle, foreground, "Expected the repo-owned DesktopManager test app window to be foreground.");
        Assert.AreEqual(foreground, window.Handle, "Expected GetActiveWindow to resolve the owned foreground harness window.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures Z-order filtering returns a subset within the specified range.
    /// </summary>
    public void GetWindows_OptionsZOrderFilter_ReturnsSubset() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(new WindowQueryOptions { IncludeHidden = true });
        if (windows.Count < 3) {
            Assert.Inconclusive("Not enough windows for Z-order filter test");
        }

        int min = 1;
        int max = windows.Count - 2;

        var filtered = manager.GetWindows(new WindowQueryOptions {
            IncludeHidden = true,
            ZOrderMin = min,
            ZOrderMax = max
        });

        Assert.IsTrue(filtered.All(w => w.ZOrder >= min && w.ZOrder <= max),
            "Expected all windows to be within the specified Z-order range");
    }

    private static string GetClassName(IntPtr handle) {
        var sb = new StringBuilder(256);
        MonitorNativeMethods.GetClassName(handle, sb, sb.Capacity);
        return sb.ToString();
    }
}

