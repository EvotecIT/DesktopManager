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
    [TestCategory("UITest")]
    public void Enumerate_WinFormsControls_WithMaximumLength_BoundsTextAndValue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Bounded Control Enumerator Form", ShowInTaskbar = false };
        using TextBox textBox = new() { Text = "DesktopManager" };
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();

        WindowControlInfo? control = new ControlEnumerator()
            .EnumerateControls(form.Handle, maxTextLength: 4)
            .FirstOrDefault(candidate => candidate.Handle == textBox.Handle);

        Assert.IsNotNull(control);
        Assert.AreEqual("Desk", control.Text);
        Assert.AreEqual("Desk", control.Value);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void Enumerate_WinFormsComboBox_LongSelectionReportsIncompleteWithoutSubstitutingWindowText() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Bounded Combo Control Enumerator Form", ShowInTaskbar = false };
        using ComboBox comboBox = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.Add("selected-value-beyond-bound");
        comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);
        form.Show();
        comboBox.CreateControl();
        Application.DoEvents();

        WindowControlInfo? control = new ControlEnumerator()
            .EnumerateControls(form.Handle, maxTextLength: 4)
            .FirstOrDefault(candidate => candidate.Handle == comboBox.Handle);

        Assert.IsNotNull(control);
        Assert.AreEqual(string.Empty, control.Value);
        Assert.IsTrue(control.ValueIsTruncated);

        DesktopControlObservation observation = DesktopAutomationService.CreateNativeControlObservation(
            new WindowInfo { Handle = form.Handle, ProcessId = unchecked((uint)System.Diagnostics.Process.GetCurrentProcess().Id) },
            control,
            new DesktopControlObservationOptions { MaxTextLength = 4 });
        Assert.AreEqual(string.Empty, observation.Text.Value);
        Assert.AreEqual("native.selection", observation.Text.Source);
        Assert.IsTrue(observation.Text.IsTruncated);
        Assert.IsFalse(observation.Text.IsComplete);
    }

    [TestMethod]
    public void IsPasswordStyle_EditWithPasswordStyle_ReturnsTrue() {
        Assert.IsTrue(ControlEnumerator.IsPasswordStyle("WindowsForms10.EDIT.app", 0x0020));
        Assert.IsFalse(ControlEnumerator.IsPasswordStyle("WindowsForms10.EDIT.app", 0));
        Assert.IsFalse(ControlEnumerator.IsPasswordStyle("Button", 0x0020));
    }

    [TestMethod]
    public void ResolvePasswordState_FailedStyleLookup_RemainsUnknown() {
        Assert.IsNull(ControlEnumerator.ResolvePasswordState("Edit", classNameLength: 4, styleAvailable: false, style: 0));
        Assert.AreEqual(false, ControlEnumerator.ResolvePasswordState("Edit", classNameLength: 4, styleAvailable: true, style: 0));
        Assert.AreEqual(true, ControlEnumerator.ResolvePasswordState("Edit", classNameLength: 4, styleAvailable: true, style: 0x0020));
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

    [TestMethod]
    [TestCategory("UITest")]
    public void ValidateLiveMutationTarget_StaleNonPasswordMetadataRefusesCurrentPasswordControl() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Live Password Revalidation Form", ShowInTaskbar = false };
        using TextBox passwordBox = new() { Text = "password-sentinel", UseSystemPasswordChar = true };
        form.Controls.Add(passwordBox);
        form.Show();
        passwordBox.CreateControl();
        Application.DoEvents();

        var staleControl = new WindowControlInfo {
            ParentWindowHandle = form.Handle,
            Handle = passwordBox.Handle,
            ClassName = passwordBox.GetType().Name,
            IsPassword = false,
            Source = WindowControlSource.Win32
        };
        var window = new WindowInfo {
            Handle = form.Handle,
            ProcessId = unchecked((uint)System.Diagnostics.Process.GetCurrentProcess().Id)
        };

        bool safe = DesktopAutomationService.TryValidateLiveMutationTarget(
            window,
            staleControl,
            new UiAutomationControlService(),
            out string code,
            out _);

        Assert.IsFalse(safe);
        Assert.AreEqual("password-control", code);
        Assert.AreEqual("password-sentinel", passwordBox.Text);
    }
}
