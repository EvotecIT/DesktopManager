using System.Diagnostics;
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
    private const int WmGetTextLength = 0x000E;
    private const int WmCommand = 0x0111;

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
    public void WindowControlService_SetText_IgnoredMessagesDoNotReportSuccess() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Ignored SetText Test Form", ShowInTaskbar = false };
        using var textBox = new IgnoringTextBox { Text = "original" };
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();
        textBox.IgnoreTextMutations = true;
        var control = new WindowControlInfo {
            ParentWindowHandle = form.Handle,
            Handle = textBox.Handle,
            ClassName = "Edit",
            IsPassword = false
        };

        NativeTextMutationOutcomeUnknownException exception = Assert.ThrowsExactly<NativeTextMutationOutcomeUnknownException>(() =>
            WindowControlService.SetText(control, "ignored"));

        StringAssert.Contains(exception.Message, "outcome is unknown");
        Assert.AreEqual("original", textBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_SetText_PartiallyAdoptedValueReportsUnknownOutcome() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Partial SetText Test Form", ShowInTaskbar = false };
        using var textBox = new TruncatingTextBox { Text = "original", MaximumAcceptedLength = 3 };
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

        NativeTextMutationOutcomeUnknownException exception = Assert.ThrowsExactly<NativeTextMutationOutcomeUnknownException>(() =>
            WindowControlService.SetText(control, "changed"));

        StringAssert.Contains(exception.Message, "outcome is unknown");
        Assert.AreEqual("cha", textBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_SetText_ValueBeyondDefaultObservationLimitReportsSuccess() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Long SetText Test Form", ShowInTaskbar = false };
        using var textBox = new TextBox();
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
        string expected = new('x', 5000);

        WindowControlService.SetText(control, expected);

        Assert.AreEqual(expected, textBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void DesktopAutomationService_LiveMutationTarget_RejectsControlFromDifferentSameProcessWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form expectedWindow = new() { Text = "Expected Mutation Owner", ShowInTaskbar = false };
        using Form replacementWindow = new() { Text = "Replacement Mutation Owner", ShowInTaskbar = false };
        using TextBox replacementControl = new() { Text = "unrelated" };
        replacementWindow.Controls.Add(replacementControl);
        expectedWindow.Show();
        replacementWindow.Show();
        replacementControl.CreateControl();
        Application.DoEvents();
        WindowControlInfo control = new ControlEnumerator().GetControlMetadata(replacementWindow.Handle, replacementControl.Handle);
        var window = new WindowInfo {
            Handle = expectedWindow.Handle,
            ProcessId = unchecked((uint)Process.GetCurrentProcess().Id)
        };

        bool valid = DesktopAutomationService.TryValidateLiveMutationTarget(
            window,
            control,
            new UiAutomationControlService(),
            out string failureCode,
            out _);

        Assert.IsFalse(valid);
        Assert.AreEqual("control-owner-changed", failureCode);
        Assert.AreEqual("unrelated", replacementControl.Text);

        DesktopControlObservation observation = DesktopAutomationService.CreateNativeControlObservation(
            window,
            control,
            new DesktopControlObservationOptions { ExpectedText = "unrelated" });
        Assert.AreEqual("partial", observation.Status);
        Assert.AreEqual("native.windowText.unavailable", observation.Text.Source);
        Assert.AreEqual(string.Empty, observation.Text.Value);
        Assert.AreEqual(null, observation.Text.ContainsExpected);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_SetText_HungControlFailsWithinBoundedTimeout() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using var ready = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        IntPtr formHandle = IntPtr.Zero;
        IntPtr textBoxHandle = IntPtr.Zero;
        Exception? startupFailure = null;
        var thread = new Thread(() => {
            try {
                using Form form = new() { Text = "Hung Native Text Test", ShowInTaskbar = false };
                using var textBox = new BlockingTextBox(release);
                form.Controls.Add(textBox);
                form.Shown += (_, _) => {
                    formHandle = form.Handle;
                    textBoxHandle = textBox.Handle;
                    textBox.BlockTextMessages = true;
                    ready.Set();
                };
                Application.Run(form);
            } catch (Exception ex) {
                startupFailure = ex;
                ready.Set();
            }
        }) {
            IsBackground = true,
            Name = "DesktopManager hung native text harness"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(10)), "The hung native text harness did not start.");
        Assert.IsNull(startupFailure);

        try {
            var control = new WindowControlInfo {
                ParentWindowHandle = formHandle,
                Handle = textBoxHandle,
                ClassName = "Edit",
                IsPassword = false
            };
            Stopwatch stopwatch = Stopwatch.StartNew();

            Assert.ThrowsExactly<NativeTextMutationOutcomeUnknownException>(() => WindowControlService.SetText(control, "must-not-block"));

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(4), $"Native text timeout took {stopwatch.Elapsed}.");
        } finally {
            release.Set();
            MonitorNativeMethods.PostMessage(formHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "The hung native text harness did not stop.");
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void DesktopAutomationService_FocusedNativeTextRead_UsesRemainingDeadline() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using var ready = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        IntPtr formHandle = IntPtr.Zero;
        IntPtr textBoxHandle = IntPtr.Zero;
        Exception? startupFailure = null;
        var thread = new Thread(() => {
            try {
                using Form form = new() { Text = "Hung Focused Native Read Test", ShowInTaskbar = false };
                using var textBox = new BlockingTextBox(release) { Text = "must-not-block" };
                form.Controls.Add(textBox);
                form.Shown += (_, _) => {
                    formHandle = form.Handle;
                    textBoxHandle = textBox.Handle;
                    textBox.BlockTextMessages = true;
                    ready.Set();
                };
                Application.Run(form);
            } catch (Exception ex) {
                startupFailure = ex;
                ready.Set();
            }
        }) {
            IsBackground = true,
            Name = "DesktopManager hung focused native read harness"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(10)), "The hung focused native read harness did not start.");
        Assert.IsNull(startupFailure);

        try {
            var control = new WindowControlInfo {
                ParentWindowHandle = formHandle,
                Handle = textBoxHandle,
                ClassName = "Edit",
                IsPassword = false
            };
            Stopwatch stopwatch = Stopwatch.StartNew();

            bool available = DesktopAutomationService.TryReadFocusedNativeText(
                control,
                maxObservedTextLength: 2048,
                getRemainingProviderTimeoutMilliseconds: () => 75,
                out string value,
                out bool isTruncated);

            stopwatch.Stop();
            Assert.IsFalse(available);
            Assert.AreEqual(string.Empty, value);
            Assert.IsFalse(isTruncated);
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Focused native text read took {stopwatch.Elapsed}.");
        } finally {
            release.Set();
            MonitorNativeMethods.PostMessage(formHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "The hung focused native read harness did not stop.");
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void DesktopAutomationService_WaitForObservedText_BoundsBlockingNativeReadsAndRetries() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using var ready = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        IntPtr formHandle = IntPtr.Zero;
        Exception? startupFailure = null;
        var thread = new Thread(() => {
            try {
                using Form form = new() { Text = "Bounded Observed Text Wait Test", ShowInTaskbar = false };
                using var textBox = new BlockingTextBox(release) { Text = "must-not-match" };
                form.Controls.Add(textBox);
                form.Shown += (_, _) => {
                    formHandle = form.Handle;
                    textBox.Focus();
                    textBox.BlockTextMessages = true;
                    ready.Set();
                };
                Application.Run(form);
            } catch (Exception ex) {
                startupFailure = ex;
                ready.Set();
            }
        }) {
            IsBackground = true,
            Name = "DesktopManager bounded observed text wait harness"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(10)), "The bounded observed text wait harness did not start.");
        Assert.IsNull(startupFailure);

        try {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Assert.ThrowsExactly<TimeoutException>(() => new DesktopAutomationService().WaitForObservedText(
                formHandle,
                "never-match",
                timeoutMilliseconds: 100,
                intervalMilliseconds: 10,
                new DesktopTextObservationOptions {
                    RetryCount = 3,
                    RetryDelayMilliseconds = 1000
                }));

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Observed text wait took {stopwatch.Elapsed}.");
        } finally {
            release.Set();
            MonitorNativeMethods.PostMessage(formHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "The bounded observed text wait harness did not stop.");
        }
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
            Text = comboBox.Text,
            IsPassword = false
        };

        WindowControlService.SetSelectedValue(control, "Beta");
        Application.DoEvents();
        Thread.Sleep(100);

        Assert.AreEqual("Beta", comboBox.Text);
        Assert.AreEqual("Beta", WindowControlService.GetSelectedValue(control));
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_SetSelectedValue_SkipsResponsiveOversizedItems() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Oversized Combo Lookup Test Form", ShowInTaskbar = false };
        using ComboBox comboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.AddRange(["oversized", "Beta"]);
        comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);
        form.Show();
        comboBox.CreateControl();
        Application.DoEvents();
        WindowControlInfo control = new() {
            ParentWindowHandle = form.Handle,
            Handle = comboBox.Handle,
            ClassName = "ComboBox",
            Id = MonitorNativeMethods.GetDlgCtrlID(comboBox.Handle),
            Text = comboBox.Text,
            IsPassword = false
        };

        WindowControlService.SetSelectedValue(control, "Beta", maxItemTextLength: 4);
        Application.DoEvents();

        Assert.AreEqual("Beta", comboBox.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowControlService_SetSelectedValue_HungParentNotificationReportsUnknownOutcome() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using var ready = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        IntPtr formHandle = IntPtr.Zero;
        IntPtr comboBoxHandle = IntPtr.Zero;
        int controlId = 0;
        Exception? startupFailure = null;
        var thread = new Thread(() => {
            try {
                using var form = new BlockingSelectionNotificationForm(release) {
                    Text = "Hung Combo Notification Test",
                    ShowInTaskbar = false
                };
                using var comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                comboBox.Items.AddRange(["Alpha", "Beta"]);
                comboBox.SelectedIndex = 0;
                form.Controls.Add(comboBox);
                form.Shown += (_, _) => {
                    formHandle = form.Handle;
                    comboBoxHandle = comboBox.Handle;
                    controlId = MonitorNativeMethods.GetDlgCtrlID(comboBox.Handle);
                    form.BlockSelectionNotifications = true;
                    ready.Set();
                };
                Application.Run(form);
            } catch (Exception ex) {
                startupFailure = ex;
                ready.Set();
            }
        }) {
            IsBackground = true,
            Name = "DesktopManager hung combo notification harness"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(10)), "The hung combo notification harness did not start.");
        Assert.IsNull(startupFailure);

        try {
            var control = new WindowControlInfo {
                ParentWindowHandle = formHandle,
                Handle = comboBoxHandle,
                ClassName = "ComboBox",
                Id = controlId,
                IsPassword = false
            };
            Stopwatch stopwatch = Stopwatch.StartNew();

            Assert.ThrowsExactly<NativeTextMutationOutcomeUnknownException>(() =>
                WindowControlService.SetSelectedValue(control, "Beta"));

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(4), $"Combo notification timeout took {stopwatch.Elapsed}.");
        } finally {
            release.Set();
            MonitorNativeMethods.PostMessage(formHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "The hung combo notification harness did not stop.");
        }
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

    private sealed class BlockingTextBox : TextBox {
        private readonly ManualResetEventSlim _release;

        internal BlockingTextBox(ManualResetEventSlim release) {
            _release = release;
        }

        internal bool BlockTextMessages;

        protected override void WndProc(ref Message message) {
            if (BlockTextMessages &&
                (message.Msg == MonitorNativeMethods.WM_SETTEXT ||
                    message.Msg == MonitorNativeMethods.WM_GETTEXT ||
                    message.Msg == WmGetTextLength)) {
                _release.Wait(TimeSpan.FromSeconds(10));
            }

            base.WndProc(ref message);
        }
    }

    private sealed class IgnoringTextBox : TextBox {
        internal bool IgnoreTextMutations;

        protected override void WndProc(ref Message message) {
            if (IgnoreTextMutations &&
                (message.Msg == MonitorNativeMethods.WM_SETTEXT ||
                    message.Msg == 0x00B1 ||
                    message.Msg == 0x00C2)) {
                message.Result = new IntPtr(1);
                return;
            }

            base.WndProc(ref message);
        }
    }

    private sealed class TruncatingTextBox : TextBox {
        internal int MaximumAcceptedLength = int.MaxValue;

        protected override void WndProc(ref Message message) {
            if ((message.Msg == MonitorNativeMethods.WM_SETTEXT || message.Msg == 0x00C2) &&
                message.LParam != IntPtr.Zero) {
                string requested = Marshal.PtrToStringUni(message.LParam) ?? string.Empty;
                string accepted = requested.Length > MaximumAcceptedLength
                    ? requested.Substring(0, MaximumAcceptedLength)
                    : requested;
                IntPtr acceptedPointer = Marshal.StringToHGlobalUni(accepted);
                try {
                    Message bounded = Message.Create(message.HWnd, message.Msg, message.WParam, acceptedPointer);
                    base.WndProc(ref bounded);
                    message.Result = bounded.Result;
                } finally {
                    Marshal.FreeHGlobal(acceptedPointer);
                }
                return;
            }

            base.WndProc(ref message);
        }
    }

    private sealed class BlockingSelectionNotificationForm : Form {
        private readonly ManualResetEventSlim _release;

        internal BlockingSelectionNotificationForm(ManualResetEventSlim release) {
            _release = release;
        }

        internal bool BlockSelectionNotifications;

        protected override void WndProc(ref Message message) {
            if (BlockSelectionNotifications && message.Msg == WmCommand) {
                _release.Wait(TimeSpan.FromSeconds(10));
            }

            base.WndProc(ref message);
        }
    }
}
