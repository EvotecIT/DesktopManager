using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public class KeyboardInputControlTests {
    private static WindowInfo WaitForWindow(Process process) {
        var manager = new WindowManager();
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 10000) {
            var win = manager.GetWindowsForProcess(process!).FirstOrDefault();
            if (win != null) {
                return win;
            }
            Thread.Sleep(100);
        }
        throw new InvalidOperationException("Window not found");
    }
    [TestMethod]
    public void SendToControl_TypesIntoBackgroundControl() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var proc1 = Process.Start("notepad.exe");
        var proc2 = Process.Start("notepad.exe");
        if (proc1 == null || proc2 == null) {
            Assert.Inconclusive("Failed to start Notepad");
        }

        try {
            var win1 = WaitForWindow(proc1!);
            var win2 = WaitForWindow(proc2!);
            MonitorNativeMethods.SetForegroundWindow(win2.Handle);

            var enumerator = new ControlEnumerator();
            var ctrl = enumerator.EnumerateControls(win1.Handle).First(c => c.ClassName == "Edit");
            KeyboardInputService.SendToControl(ctrl, VirtualKey.VK_H, VirtualKey.VK_I);
            Thread.Sleep(500);

            int len = MonitorNativeMethods.GetWindowTextLength(ctrl.Handle);
            StringBuilder sb = new(len + 1);
            MonitorNativeMethods.GetWindowText(ctrl.Handle, sb, sb.Capacity);
            Assert.IsTrue(sb.ToString().EndsWith("HI"), $"Expected text 'HI' but got '{sb}'");
        } finally {
            if (proc1 != null && !proc1.HasExited) proc1.Kill();
            if (proc2 != null && !proc2.HasExited) proc2.Kill();
        }
    }
}
