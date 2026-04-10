using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for control focus and state helpers on DesktopAutomationService.
/// </summary>
public class DesktopAutomationControlStateTests {
    [TestMethod]
    /// <summary>
    /// Ensures control-state lookup rejects invalid control handles.
    /// </summary>
    public void DesktopAutomationService_GetControlState_ZeroControlHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetControlState(new IntPtr(1), IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control focus rejects invalid window handles.
    /// </summary>
    public void DesktopAutomationService_FocusControl_ZeroWindowHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.FocusControl(IntPtr.Zero, new IntPtr(1)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control-state observation reflects live enabled and visible flags.
    /// </summary>
    public void DesktopAutomationService_GetControlState_ReturnsLiveFlags() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Control State Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "state"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        DesktopControlState? state = new DesktopAutomationService().GetControlState(harness.Window.Handle, editor.Handle);

        Assert.IsNotNull(state);
        Assert.AreEqual(true, state.IsEnabled);
        Assert.AreEqual(true, state.IsVisible);
        Assert.AreEqual(editor.Handle, state.ControlHandle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control enablement can be toggled for a live WinForms textbox.
    /// </summary>
    public void DesktopAutomationService_SetControlEnabled_TogglesLiveTextbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowMutationTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Enable Control Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "toggle"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        DesktopAutomationService automation = new();
        DesktopControlState disabledState = automation.SetControlEnabled(harness.Window.Handle, editor.Handle, false);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(false, disabledState.IsEnabled);

        DesktopControlState enabledState = automation.SetControlEnabled(harness.Window.Handle, editor.Handle, true);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(true, enabledState.IsEnabled);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control visibility can be toggled for a live WinForms textbox.
    /// </summary>
    public void DesktopAutomationService_SetControlVisibility_TogglesLiveTextbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowMutationTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Visibility Control Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "toggle"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        DesktopAutomationService automation = new();
        DesktopControlState hiddenState = automation.SetControlVisibility(harness.Window.Handle, editor.Handle, false);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(false, hiddenState.IsVisible);

        DesktopControlState visibleState = automation.SetControlVisibility(harness.Window.Handle, editor.Handle, true);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(true, visibleState.IsVisible);
    }

    [TestMethod]
    /// <summary>
    /// Ensures check-state mutation can toggle a live WinForms checkbox resolved by handle.
    /// </summary>
    public void DesktopAutomationService_SetControlCheckState_TogglesLiveCheckbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowMutationTests();

        const int wsChild = 0x40000000;
        const int wsVisible = 0x10000000;
        const int bsAutoCheckBox = 0x00000003;

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Check Control Harness");
        IntPtr checkBoxHandle = MonitorNativeMethods.CreateWindowExW(
            0,
            "Button",
            "Enable automation",
            wsChild | wsVisible | bsAutoCheckBox,
            10,
            10,
            160,
            24,
            harness.Form.Handle,
            new IntPtr(1002),
            IntPtr.Zero,
            IntPtr.Zero);
        if (checkBoxHandle == IntPtr.Zero) {
            Assert.Inconclusive("Failed to create a native checkbox control for DesktopAutomationService testing.");
        }

        try {
            DesktopAutomationService automation = new();
            automation.SetControlCheckState(harness.Window.Handle, checkBoxHandle, false);
            Application.DoEvents();
            Task.Delay(100).Wait();

            WindowControlInfo control = new() {
                ParentWindowHandle = harness.Form.Handle,
                Handle = checkBoxHandle,
                ClassName = "Button",
                Id = MonitorNativeMethods.GetDlgCtrlID(checkBoxHandle),
                Text = "Enable automation"
            };
            Assert.IsFalse(WindowControlService.GetCheckState(control));

            automation.SetControlCheckState(harness.Window.Handle, checkBoxHandle, true);
            Application.DoEvents();
            Task.Delay(100).Wait();

            Assert.IsTrue(WindowControlService.GetCheckState(control));
        } finally {
            MonitorNativeMethods.DestroyWindow(checkBoxHandle);
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures combo-box selection can be changed through DesktopAutomationService using exact control handles.
    /// </summary>
    public void DesktopAutomationService_SetControlSelectedValue_UpdatesLiveComboBox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowMutationTests();

        ComboBox? comboBox = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Combo Control Harness",
            form => {
                comboBox = new ComboBox {
                    Name = "OptionsComboBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                comboBox.Items.AddRange(["Alpha", "Beta", "Gamma"]);
                comboBox.SelectedIndex = 0;
                form.Controls.Add(comboBox);
            });

        Assert.IsNotNull(comboBox);
        DesktopAutomationService automation = new();
        automation.SetControlSelectedValue(harness.Window.Handle, comboBox.Handle, "Beta");
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual("Beta", comboBox.Text);

        DesktopControlState? state = automation.GetControlState(harness.Window.Handle, comboBox.Handle);
        Assert.IsNotNull(state);
        Assert.AreEqual("Beta", state.SelectedValue);
        Assert.AreEqual("Beta", state.Value);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control focus can be redirected between live controls.
    /// </summary>
    public void DesktopAutomationService_FocusControl_FocusesLiveTextbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();

        TextBox? firstEditor = null;
        TextBox? secondEditor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Focus Control Harness",
            form => {
                firstEditor = new TextBox {
                    Name = "FirstEditor",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "first"
                };
                secondEditor = new TextBox {
                    Name = "SecondEditor",
                    Left = 12,
                    Top = 42,
                    Width = 180,
                    Text = "second"
                };
                form.Controls.Add(firstEditor);
                form.Controls.Add(secondEditor);
                form.Shown += (_, _) => {
                    firstEditor.Focus();
                };
            });

        Assert.IsNotNull(firstEditor);
        Assert.IsNotNull(secondEditor);
        WindowManager manager = new();
        manager.ActivateWindow(harness.Window);
        Application.DoEvents();
        Task.Delay(150).Wait();

        DesktopAutomationService automation = new();
        DesktopControlState focusedState = automation.FocusControl(harness.Window.Handle, secondEditor.Handle, ensureForegroundWindow: true);

        Application.DoEvents();
        Task.Delay(150).Wait();

        Assert.AreEqual(true, focusedState.IsFocused);
        Assert.AreEqual(secondEditor.Handle, automation.GetFocusedControlObservation(harness.Window.Handle)?.FocusedHandle);
    }
}
