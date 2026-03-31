#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for launch-and-wait workflow decisions in the desktop automation core.
/// </summary>
public class DesktopAutomationProcessWorkflowTests {
    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait prefers a resolved window process when one is available.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_UsesResolvedProcessIdWhenAvailable() {
        var launch = new DesktopProcessLaunchInfo {
            FilePath = "notepad.exe",
            ProcessId = 100,
            ResolvedProcessId = 200
        };

        DesktopLaunchWaitBindingPlan plan = DesktopAutomationService.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: "*Launch*",
            launchWindowClassNamePattern: "Notepad",
            windowTitlePattern: "*Final*",
            windowClassNamePattern: "NotepadFinal",
            includeHidden: true,
            includeEmptyTitles: false,
            all: true,
            followProcessFamily: true);

        Assert.AreEqual("resolved-process-id", plan.WaitBinding);
        Assert.AreEqual(200, plan.BoundProcessId);
        Assert.IsNull(plan.BoundProcessName);
        Assert.AreEqual(200, plan.Criteria.ProcessId);
        Assert.AreEqual("*Final*", plan.Criteria.TitlePattern);
        Assert.AreEqual("NotepadFinal", plan.Criteria.ClassNamePattern);
        Assert.IsTrue(plan.Criteria.IncludeHidden);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait uses the launcher process by default.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_UsesLauncherProcessIdByDefault() {
        var launch = new DesktopProcessLaunchInfo {
            FilePath = "notepad.exe",
            ProcessId = 100
        };

        DesktopLaunchWaitBindingPlan plan = DesktopAutomationService.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: "*Launch*",
            launchWindowClassNamePattern: "LaunchClass",
            windowTitlePattern: null,
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmptyTitles: true,
            all: false,
            followProcessFamily: false);

        Assert.AreEqual("launcher-process-id", plan.WaitBinding);
        Assert.AreEqual(100, plan.BoundProcessId);
        Assert.IsNull(plan.BoundProcessName);
        Assert.AreEqual(100, plan.Criteria.ProcessId);
        Assert.AreEqual("*Launch*", plan.Criteria.TitlePattern);
        Assert.AreEqual("LaunchClass", plan.Criteria.ClassNamePattern);
        Assert.AreEqual(true, plan.Criteria.IncludeEmptyTitles);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait can opt into following a same-name process family.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_UsesProcessFamilyWhenRequested() {
        var launch = new DesktopProcessLaunchInfo {
            FilePath = "\"C:\\Program Files\\Microsoft VS Code\\Code.exe\"",
            ProcessId = 100
        };

        DesktopLaunchWaitBindingPlan plan = DesktopAutomationService.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: null,
            launchWindowClassNamePattern: null,
            windowTitlePattern: "*Code*",
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmptyTitles: false,
            all: false,
            followProcessFamily: true);

        Assert.AreEqual("process-name-family", plan.WaitBinding);
        Assert.IsNull(plan.BoundProcessId);
        Assert.AreEqual("Code", plan.BoundProcessName);
        Assert.AreEqual(0, plan.Criteria.ProcessId);
        Assert.AreEqual("Code", plan.Criteria.ProcessNamePattern);
        Assert.AreEqual("*Code*", plan.Criteria.TitlePattern);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait falls back to the launcher process when no process-family hint can be inferred.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_FallsBackToLauncherProcessWhenProcessFamilyCannotBeInferred() {
        var launch = new DesktopProcessLaunchInfo {
            FilePath = "\"\"",
            ProcessId = 100
        };

        DesktopLaunchWaitBindingPlan plan = DesktopAutomationService.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: null,
            launchWindowClassNamePattern: null,
            windowTitlePattern: null,
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmptyTitles: false,
            all: false,
            followProcessFamily: true);

        Assert.AreEqual("launcher-process-id", plan.WaitBinding);
        Assert.AreEqual(100, plan.BoundProcessId);
        Assert.IsNull(plan.BoundProcessName);
        Assert.AreEqual(100, plan.Criteria.ProcessId);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait rejects non-positive final wait timeouts.
    /// </summary>
    public void LaunchAndWaitForWindow_ZeroTimeout_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.LaunchAndWaitForWindow(new DesktopProcessLaunchAndWaitOptions {
            FilePath = "notepad.exe",
            TimeoutMilliseconds = 0
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait rejects non-positive final wait intervals.
    /// </summary>
    public void LaunchAndWaitForWindow_ZeroInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.LaunchAndWaitForWindow(new DesktopProcessLaunchAndWaitOptions {
            FilePath = "notepad.exe",
            IntervalMilliseconds = 0
        }));
    }
}
#endif
