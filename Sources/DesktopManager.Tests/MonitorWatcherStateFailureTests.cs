using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Reflection;

namespace DesktopManager.Tests;

[TestClass]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public class MonitorWatcherStateFailureTests {
    [TestMethod]
    public void OnDisplaySettingsChanged_DoesNotUpdateState_WhenProviderFails() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        var field = typeof(MonitorWatcher).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field);
        var before = field.GetValue(watcher);

        watcher.StateProvider = () => throw new InvalidOperationException("fail");

        using var sw = new System.IO.StringWriter();
        using var listener = new TextWriterTraceListener(sw);
        Trace.Listeners.Add(listener);
        var method = typeof(MonitorWatcher).GetMethod("OnDisplaySettingsChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        try {
            method.Invoke(watcher, new object?[] { null, EventArgs.Empty });
            Trace.Flush();
        } finally {
            Trace.Listeners.Remove(listener);
        }

        var after = field.GetValue(watcher);
        Assert.AreSame(before, after);
        StringAssert.Contains(sw.ToString(), "GetCurrentStates failed");
    }
}
