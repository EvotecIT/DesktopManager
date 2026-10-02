using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlServiceTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    public void ControlClick_CancelButton_ClosesDialog() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        using Form targetForm = new NonActivatingTestForm() { Text = "Target Form", ShowInTaskbar = false };
        using Button cancelButton = new() { Text = "Cancel" };
        using Form foregroundForm = new NonActivatingTestForm() { Text = "Foreground Form", ShowInTaskbar = false };
        int clicks = 0;
        cancelButton.Click += (_, _) => clicks++;

        targetForm.Controls.Add(cancelButton);
        targetForm.Show();
        foregroundForm.Show();
        cancelButton.CreateControl();
        foregroundForm.Activate();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowControlInfo control = new() {
            ParentWindowHandle = targetForm.Handle,
            Handle = cancelButton.Handle,
            ClassName = "Button",
            Id = MonitorNativeMethods.GetDlgCtrlID(cancelButton.Handle),
            Text = cancelButton.Text
        };

        WindowControlService.ControlClick(control, MouseButton.Left);
        Application.DoEvents();
        Thread.Sleep(100);

        Assert.AreEqual(1, clicks, "Button click handler must be invoked exactly once.");
    }
}
