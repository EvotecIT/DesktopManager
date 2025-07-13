using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for cross-bitness window title retrieval.
/// </summary>
public class WindowTextFallbackTests {
    public TestContext TestContext { get; set; }

    [TestMethod]
    /// <summary>
    /// Ensures SendMessageTimeout fallback works when bitness differs.
    /// </summary>
    public void GetWindowText_Fallback_WorksAcrossBitness() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        if (!Environment.Is64BitProcess) {
            Assert.Inconclusive("Test requires 64-bit process");
        }

        using var proc = Process.Start(new ProcessStartInfo("notepad.exe") {
            WindowStyle = ProcessWindowStyle.Normal
        });
        if (proc == null) {
            Assert.Inconclusive("Failed to start notepad");
        }
        proc.WaitForInputIdle(2000);

        try {
            var manager = new WindowManager();
            var window = manager.GetWindows(processId: proc.Id).First();
            string expected = window.Title;

            string helperPath = Path.Combine(TestContext.DeploymentDirectory, "WindowTextHelper32.exe");
            if (!File.Exists(helperPath)) {
                helperPath = Path.Combine(AppContext.BaseDirectory, "WindowTextHelper32.exe");
            }

            using var helper = Process.Start(new ProcessStartInfo(helperPath, window.Handle.ToInt64().ToString()) {
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (helper == null) {
                Assert.Inconclusive("Failed to start helper");
            }

            string output = helper.StandardOutput.ReadToEnd().Trim();
            helper.WaitForExit();

            Assert.AreEqual(expected, output);
        } finally {
            if (!proc.HasExited) {
                proc.Kill();
                proc.WaitForExit();
            }
        }
    }
}
