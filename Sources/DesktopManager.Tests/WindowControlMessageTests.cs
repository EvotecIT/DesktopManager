using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for direct Win32 control messaging helpers.
/// </summary>
public class WindowControlMessageTests {
    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures printable key sends append buffered text to a standard edit control.
    /// </summary>
    public void WindowControlService_SendKeys_AppendsPrintableTextToTextBox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Message Test Form", ShowInTaskbar = false };
        using TextBox textBox = new();
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowControlInfo control = new() {
            ParentWindowHandle = form.Handle,
            Handle = textBox.Handle,
            ClassName = "Edit",
            Id = MonitorNativeMethods.GetDlgCtrlID(textBox.Handle),
            Text = textBox.Text
        };

        WindowControlService.SendKeys(control, VirtualKey.VK_H, VirtualKey.VK_I, VirtualKey.VK_SPACE, VirtualKey.VK_1, VirtualKey.VK_2);
        Application.DoEvents();
        Thread.Sleep(100);

        Assert.AreEqual("HI 12", textBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures direct control text updates still work for standard edit controls.
    /// </summary>
    public void WindowControlService_SetText_UpdatesTextBoxText() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "SetText Test Form", ShowInTaskbar = false };
        using TextBox textBox = new();
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowControlInfo control = new() {
            ParentWindowHandle = form.Handle,
            Handle = textBox.Handle,
            ClassName = "Edit",
            Id = MonitorNativeMethods.GetDlgCtrlID(textBox.Handle),
            Text = textBox.Text
        };

        WindowControlService.SetText(control, "DesktopManager");
        Application.DoEvents();
        Thread.Sleep(100);

        Assert.AreEqual("DesktopManager", textBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures direct combo-box selection can switch the current item by displayed text.
    /// </summary>
    public void WindowControlService_SetSelectedValue_UpdatesComboBoxSelection() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Combo Selection Test Form", ShowInTaskbar = false };
        using ComboBox comboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.AddRange(["Alpha", "Beta", "Gamma"]);
        comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);
        form.Show();
        comboBox.CreateControl();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowControlInfo control = new() {
            ParentWindowHandle = form.Handle,
            Handle = comboBox.Handle,
            ClassName = "ComboBox",
            Id = MonitorNativeMethods.GetDlgCtrlID(comboBox.Handle),
            Text = comboBox.Text
        };

        WindowControlService.SetSelectedValue(control, "Beta");
        Application.DoEvents();
        Thread.Sleep(100);

        Assert.AreEqual("Beta", comboBox.Text);
        Assert.AreEqual("Beta", WindowControlService.GetSelectedValue(control));
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures control enumeration includes parent window metadata and shared capabilities.
    /// </summary>
    public void ControlEnumerator_EnumerateControls_PopulatesParentWindowMetadata() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Enumerator Test Form", ShowInTaskbar = false };
        using TextBox textBox = new() { Text = "sample" };
        using Button button = new() { Text = "Go" };
        form.Controls.Add(textBox);
        form.Controls.Add(button);
        form.Show();
        textBox.CreateControl();
        button.CreateControl();
        Application.DoEvents();
        Thread.Sleep(100);

        ControlEnumerator enumerator = new();
        var controls = enumerator.EnumerateControls(form.Handle);

        WindowControlInfo? textBoxControl = controls.FirstOrDefault(control => control.Handle == textBox.Handle);
        Assert.IsNotNull(textBoxControl, "Expected the TextBox control to be enumerated.");
        Assert.AreEqual(form.Handle, textBoxControl.ParentWindowHandle);
        Assert.IsTrue(textBoxControl.SupportsBackgroundClick);
        Assert.IsTrue(textBoxControl.SupportsBackgroundText);
        Assert.IsTrue(textBoxControl.SupportsBackgroundKeys);
        Assert.AreEqual(textBox.Text, textBoxControl.Value);
    }
}
