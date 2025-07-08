using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using DesktopManager;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
/// <summary>
/// Test class for MonitorWatcherTests.
/// </summary>
public class MonitorWatcherTests {
    [TestMethod]
    /// <summary>
    /// Test for MonitorWatcher_CanBeCreated.
    /// </summary>
    public void MonitorWatcher_CanBeCreated() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        Assert.IsNotNull(watcher);
    }

    [TestMethod]
    /// <summary>
    /// Ensures finalizer does not throw after disposal.
    /// </summary>
    public void MonitorWatcher_FinalizerSafeAfterDispose() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var watcher = new MonitorWatcher();
        watcher.Dispose();
        watcher = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.IsTrue(true);
    }

    private class TestWatcher : MonitorWatcher {
        private Queue<Dictionary<string, MonitorWatcher.MonitorState>> _states;

        public TestWatcher(params Dictionary<string, MonitorWatcher.MonitorState>[] states) : base(false) {
            _states = new Queue<Dictionary<string, MonitorWatcher.MonitorState>>(states);
        }

        protected override Dictionary<string, MonitorWatcher.MonitorState> GetCurrentStates() {
            return _states.Peek();
        }

        public void SetState(Dictionary<string, MonitorWatcher.MonitorState> state) {
            _states.Clear();
            _states.Enqueue(state);
        }

        public void Trigger() {
            CheckForChanges();
        }
    }

    [TestMethod]
    /// <summary>
    /// Simulates monitor connection event.
    /// </summary>
    public void MonitorWatcher_RaisesConnectEvent() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var initial = new Dictionary<string, MonitorWatcher.MonitorState> {
            {"DISPLAY1", new MonitorWatcher.MonitorState { Width = 10, Height = 10, Orientation = 0 }}
        };
        var changed = new Dictionary<string, MonitorWatcher.MonitorState> {
            {"DISPLAY1", new MonitorWatcher.MonitorState { Width = 10, Height = 10, Orientation = 0 }},
            {"DISPLAY2", new MonitorWatcher.MonitorState { Width = 10, Height = 10, Orientation = 0 }}
        };

        var watcher = new TestWatcher(initial);
        bool raised = false;
        watcher.MonitorConnected += (_, _) => raised = true;
        watcher.SetState(changed);
        watcher.Trigger();

        Assert.IsTrue(raised);
    }

    [TestMethod]
    /// <summary>
    /// Simulates monitor disconnection event.
    /// </summary>
    public void MonitorWatcher_RaisesDisconnectEvent() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var initial = new Dictionary<string, MonitorWatcher.MonitorState> {
            {"DISPLAY1", new MonitorWatcher.MonitorState { Width = 10, Height = 10, Orientation = 0 }},
            {"DISPLAY2", new MonitorWatcher.MonitorState { Width = 10, Height = 10, Orientation = 0 }}
        };
        var changed = new Dictionary<string, MonitorWatcher.MonitorState> {
            {"DISPLAY1", new MonitorWatcher.MonitorState { Width = 10, Height = 10, Orientation = 0 }}
        };

        var watcher = new TestWatcher(initial);
        bool raised = false;
        watcher.MonitorDisconnected += (_, _) => raised = true;
        watcher.SetState(changed);
        watcher.Trigger();

        Assert.IsTrue(raised);
    }
}
