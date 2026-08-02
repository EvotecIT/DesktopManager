using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>Protects native mutation, focused-window ownership, and nested notification contracts.</summary>
public class DesktopSemanticNativeRegressionTests {
    private const int WmCommand = 0x0111;

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_TrySetTextIfUnchanged_MissingFingerprintAllowsReplacement() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "No Fingerprint Replacement Test", ShowInTaskbar = false };
        using var textBox = new TextBox { Text = "long current value" };
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();
        var control = new WindowControlInfo {
            ParentWindowHandle = form.Handle,
            Handle = textBox.Handle,
            ClassName = "Edit",
            IsPassword = false
        };

        bool applied = WindowControlService.TrySetTextIfUnchanged(
            control,
            "replacement",
            expectedContentFingerprint: string.Empty,
            maxTextLength: 4,
            out string failureCode,
            out string observedFingerprint);

        Assert.IsTrue(applied);
        Assert.AreEqual(string.Empty, failureCode);
        Assert.AreEqual(string.Empty, observedFingerprint);
        Assert.AreEqual("replacement", textBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowActivationService_GetFocusedControlHandle_RejectsSiblingTopLevelWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form selectedWindow = new() { Text = "Selected Focus Owner", ShowInTaskbar = false };
        using Form siblingWindow = new() { Text = "Sibling Focus Owner", ShowInTaskbar = false };
        using TextBox selectedTextBox = new();
        using TextBox siblingTextBox = new();
        selectedWindow.Controls.Add(selectedTextBox);
        siblingWindow.Controls.Add(siblingTextBox);
        selectedWindow.Show();
        siblingWindow.Show();
        selectedTextBox.CreateControl();
        siblingTextBox.CreateControl();
        siblingWindow.Activate();
        MonitorNativeMethods.SetFocus(siblingTextBox.Handle);
        Application.DoEvents();

        Assert.AreEqual(
            siblingWindow.Handle,
            MonitorNativeMethods.GetAncestor(siblingTextBox.Handle, MonitorNativeMethods.GA_ROOT));
        Assert.AreEqual(IntPtr.Zero, WindowActivationService.GetFocusedControlHandle(selectedWindow.Handle));
        Assert.AreEqual(siblingTextBox.Handle, WindowActivationService.GetFocusedControlHandle(siblingWindow.Handle));
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_SetSelectedValue_NotifiesImmediateContainer() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Nested Combo Notification Test", ShowInTaskbar = false };
        using var panel = new SelectionNotificationPanel();
        using var comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.AddRange(["Alpha", "Beta"]);
        comboBox.SelectedIndex = 0;
        panel.Controls.Add(comboBox);
        form.Controls.Add(panel);
        form.Show();
        panel.CreateControl();
        comboBox.CreateControl();
        Application.DoEvents();
        var control = new WindowControlInfo {
            ParentWindowHandle = form.Handle,
            Handle = comboBox.Handle,
            ClassName = "ComboBox",
            Id = MonitorNativeMethods.GetDlgCtrlID(comboBox.Handle),
            IsPassword = false
        };

        WindowControlService.SetSelectedValue(control, "Beta");
        Application.DoEvents();

        Assert.AreEqual(panel.Handle, MonitorNativeMethods.GetParent(comboBox.Handle));
        Assert.AreEqual(1, panel.SelectionChangeNotifications);
        Assert.AreEqual("Beta", comboBox.Text);
    }

    private sealed class SelectionNotificationPanel : Panel {
        internal int SelectionChangeNotifications;

        protected override void WndProc(ref Message message) {
            if (message.Msg == WmCommand && ((message.WParam.ToInt64() >> 16) & 0xFFFF) == 1) {
                SelectionChangeNotifications++;
            }

            base.WndProc(ref message);
        }
    }
}
