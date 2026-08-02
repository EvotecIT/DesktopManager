using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>Protects deadline, input-delivery, retry-quality, and live password-safety regressions.</summary>
public class DesktopSemanticSafetyRegressionTests {
    [TestMethod]
    public void KeyboardInputService_PartialDelivery_ReportsUnknownOutcome() {
        KeyboardInputDeliveryException exception = Assert.ThrowsExactly<KeyboardInputDeliveryException>(
            () => KeyboardInputService.EnsureInputDelivery(requested: 2, delivered: 1, operation: "Unicode text input"));

        StringAssert.Contains(exception.Message, "1 of 2");
        KeyboardInputService.EnsureInputDelivery(requested: 2, delivered: 2, operation: "Unicode text input");
    }

    [TestMethod]
    public void DesktopAutomationService_ExpiredWaitMatch_IsNotReturned() {
        var observation = new DesktopControlObservation();

        Assert.IsFalse(DesktopAutomationService.CanReturnWaitObservation(observation, remainingMilliseconds: 0));
        Assert.IsTrue(DesktopAutomationService.CanReturnWaitObservation(observation, remainingMilliseconds: 1));
        Assert.IsFalse(DesktopAutomationService.CanReturnWaitObservation(null, remainingMilliseconds: 1));
    }

    [TestMethod]
    public void DesktopAutomationService_SelectBetterTextObservation_PrefersNewerCompleteControlEvidence() {
        var stale = new DesktopWindowTextObservation {
            Value = "window title",
            Source = "window.title",
            IsTruncated = true
        };
        var current = new DesktopWindowTextObservation {
            ControlHandle = new IntPtr(42),
            Value = "new control value",
            Source = "native.windowText",
            IsTruncated = false
        };

        Assert.AreSame(current, DesktopAutomationService.SelectBetterTextObservation(stale, current));
        Assert.AreSame(current, DesktopAutomationService.SelectBetterTextObservation(current, null));
    }

    [TestMethod]
    public void DesktopAutomationService_CreateNativeControlObservation_RevalidatesStaleNonPasswordMetadata() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        TextBox? editor = null;
        using PumpingWinFormsHarness harness = PumpingWinFormsHarness.Create(
            "DesktopManager Live Password Revalidation Harness",
            form => {
                editor = new TextBox {
                    Text = "must-never-be-observed",
                    UseSystemPasswordChar = true
                };
                form.Controls.Add(editor);
            });
        Assert.IsNotNull(editor);
        IntPtr editorHandle = harness.Invoke(() => editor.Handle);
        WindowControlInfo control = new ControlEnumerator().GetControlMetadata(harness.Window.Handle, editorHandle);
        control.IsPassword = false;
        control.Text = "stale leaked text";
        control.Value = "stale leaked value";

        DesktopControlObservation observation = DesktopAutomationService.CreateNativeControlObservation(
            harness.Window,
            control,
            new DesktopControlObservationOptions { UseUiAutomation = false });

        Assert.AreEqual(true, observation.IsPassword);
        Assert.AreEqual("restricted", observation.Status);
        Assert.AreEqual(string.Empty, observation.Text.Value);
        Assert.AreEqual(string.Empty, control.Text);
        Assert.AreEqual(string.Empty, control.Value);

        control.IsPassword = false;
        control.Text = "second stale leak";
        control.Value = "second stale leak";
        DesktopControlState state = new DesktopAutomationService().GetControlState(control);
        Assert.AreEqual(string.Empty, state.Text);
        Assert.AreEqual(string.Empty, state.Value);
    }
}
