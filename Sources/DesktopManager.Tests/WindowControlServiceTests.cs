using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlServiceTests {
    [TestMethod]
    public void ControlClick_CancelButton_ClosesDialog() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var process = Process.Start("notepad.exe");
        if (process == null) {
            Assert.Inconclusive("Failed to start Notepad");
        }

        try {
            var manager = new WindowManager();
            var window = manager.WaitWindow("*Notepad*", 10000);
            KeyboardInputService.PressShortcut(0, VirtualKey.VK_CONTROL, VirtualKey.VK_O);
            var dialog = manager.WaitWindow("*Open*", 5000);

            var enumerator = new ControlEnumerator();
            var controls = enumerator.EnumerateControls(dialog.Handle);
            var cancel = controls.FirstOrDefault(c => c.Text == "Cancel");
            Assert.IsNotNull(cancel, "Cancel control not found");

            WindowControlService.ControlClick(cancel, MouseButton.Left);
            Thread.Sleep(500);
            var openDialogs = manager.GetWindows("*Open*");
            Assert.AreEqual(0, openDialogs.Count, "Dialog was not closed");
        } finally {
            if (process != null && !process.HasExited) {
                process.Kill();
            }
        }
    }

    [TestMethod]
    public void GetSetControlText_EditControl_RoundTrips() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var process = Process.Start("notepad.exe");
        if (process == null) {
            Assert.Inconclusive("Failed to start Notepad");
        }

        try {
            var manager = new WindowManager();
            var window = manager.WaitWindow("*Notepad*", 10000);
            var enumerator = new ControlEnumerator();
            var controls = enumerator.EnumerateControls(window.Handle);
            var edit = controls.FirstOrDefault(c => c.ClassName == "Edit");
            Assert.IsNotNull(edit, "Edit control not found");

            const string sample = "Hello";
            WindowControlService.SetControlText(edit, sample);
            Thread.Sleep(200);
            string read = WindowControlService.GetControlText(edit);
            Assert.AreEqual(sample, read);
        } finally {
            if (process != null && !process.HasExited) {
                process.Kill();
            }
        }
    }
}
