using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class ControlEnumeratorTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    public void Enumerate_WinFormsControls_ReturnsEdit() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Control Enumerator Form", ShowInTaskbar = false };
        using TextBox textBox = new() { Text = "DesktopManager" };
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();

        var enumerator = new ControlEnumerator();
        var controls = enumerator.EnumerateControls(form.Handle);

        WindowControlInfo? textBoxControl = controls.FirstOrDefault(c => c.Handle == textBox.Handle);
        Assert.IsNotNull(textBoxControl);
        StringAssert.Contains(textBoxControl.ClassName, "EDIT", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void IsPasswordStyle_EditWithPasswordStyle_ReturnsTrue() {
        Assert.IsTrue(ControlEnumerator.IsPasswordStyle("WindowsForms10.EDIT.app", 0x0020));
        Assert.IsFalse(ControlEnumerator.IsPasswordStyle("WindowsForms10.EDIT.app", 0));
        Assert.IsFalse(ControlEnumerator.IsPasswordStyle("Button", 0x0020));
    }

    [TestMethod]
    public void MergeControlMetadata_PasswordSource_ClearsNativeText() {
        WindowControlInfo nativeControl = new() {
            Text = "must-not-survive",
            Value = "must-not-survive"
        };
        WindowControlInfo automationControl = new() {
            IsPassword = true,
            Text = "Password",
            Value = string.Empty
        };

        WindowManager.MergeControlMetadata(nativeControl, automationControl);

        Assert.AreEqual(true, nativeControl.IsPassword);
        Assert.AreEqual(string.Empty, nativeControl.Text);
        Assert.AreEqual(string.Empty, nativeControl.Value);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void Enumerate_WinFormsPasswordControl_DoesNotReadValue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Control Enumerator Password Form", ShowInTaskbar = false };
        using TextBox passwordBox = new() { Text = "password-sentinel", UseSystemPasswordChar = true };
        form.Controls.Add(passwordBox);
        form.Show();
        passwordBox.CreateControl();
        Application.DoEvents();

        WindowControlInfo? control = new ControlEnumerator()
            .EnumerateControls(form.Handle)
            .FirstOrDefault(candidate => candidate.Handle == passwordBox.Handle);

        Assert.IsNotNull(control);
        Assert.AreEqual(true, control.IsPassword);
        Assert.AreEqual(string.Empty, control.Text);
        Assert.AreEqual(string.Empty, control.Value);
    }
}
