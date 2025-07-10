using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Reflection;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
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
        var original = Console.Out;
        Console.SetOut(sw);
        var method = typeof(MonitorWatcher).GetMethod("OnDisplaySettingsChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        method.Invoke(watcher, new object?[] { null, EventArgs.Empty });
        Console.SetOut(original);

        var after = field.GetValue(watcher);
        Assert.AreSame(before, after);
        StringAssert.Contains(sw.ToString(), "GetCurrentStates failed");
    }
}
