using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DesktopManager.Portable.Tests;

[TestClass]
public sealed class DesktopCommandDeadlineTests {
    [TestMethod]
    public void ExitedLauncherWithInheritedPipes_StillHasOneCompletionDeadline() {
        RequireUnixFixture();
        var directory = Path.Combine(Path.GetTempPath(), "desktop-pipes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var pidPath = Path.Combine(directory, "owned-descendant.pid");
        var elapsed = Stopwatch.StartNew();
        try {
            // This deliberately launched test shell exits immediately. Only its
            // owned sleep child retains the two redirected pipe descriptors.
            Assert.ThrowsExactly<TimeoutException>(() => new DesktopCommandRunner().Run("/bin/sh", "-c",
                "sleep 30 & printf '%s' \"$!\" > \"$1\"; exit 0", "desktop-pipe-fixture", pidPath));
            Assert.IsLessThan(TimeSpan.FromSeconds(18), elapsed.Elapsed,
                "The 15-second command deadline also bounds inherited-pipe draining.");
        } finally {
            if (File.Exists(pidPath) && int.TryParse(File.ReadAllText(pidPath), NumberStyles.None, CultureInfo.InvariantCulture, out var pid)) {
                try {
                    using var descendant = Process.GetProcessById(pid);
                    if (!descendant.HasExited) {
                        descendant.Kill();
                    }
                } catch (ArgumentException) {
                    // The test-owned descendant may already have exited.
                } catch (InvalidOperationException) {
                    // It may exit between lookup and Kill.
                }
            }
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void OversizedNativeOutput_IsRejectedWithoutReturningAnUnboundedCapture() {
        RequireUnixFixture();
        var failure = Assert.ThrowsExactly<InvalidOperationException>(() =>
            new DesktopCommandRunner().Run("/usr/bin/head", "-c", "1100000", "/dev/zero"));
        StringAssert.Contains(failure.Message, "output limit");
    }

    private static void RequireUnixFixture() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("These process-boundary fixtures use system-provided Unix shell, sleep and head commands.");
        }
    }
}
