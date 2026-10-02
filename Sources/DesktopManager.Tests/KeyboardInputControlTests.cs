using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class KeyboardInputControlTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    public void SendToControl_TypesIntoBackgroundControl() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        using Form targetForm = new NonActivatingTestForm() { Text = "Target Form", ShowInTaskbar = false };
        using TextBox textBox = new();
        using Form foregroundForm = new NonActivatingTestForm() { Text = "Foreground Form", ShowInTaskbar = false };

        targetForm.Controls.Add(textBox);
        targetForm.Show();
        foregroundForm.Show();
        textBox.CreateControl();
        foregroundForm.Activate();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowControlInfo control = new() {
            ParentWindowHandle = targetForm.Handle,
            Handle = textBox.Handle,
            ClassName = "Edit",
            Id = MonitorNativeMethods.GetDlgCtrlID(textBox.Handle),
            Text = textBox.Text
        };

        KeyboardInputService.SendToControl(control, VirtualKey.VK_H, VirtualKey.VK_I);
        Application.DoEvents();
        Thread.Sleep(100);

        Assert.AreEqual("HI", textBox.Text);
    }
}
