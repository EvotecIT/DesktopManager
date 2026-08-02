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
    public void KeyboardInputService_FailedSequence_ReleasesEveryAcceptedKey() {
        var pressed = new List<VirtualKey>();
        var released = new List<VirtualKey>();

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            KeyboardInputService.ExecuteKeySequence(
                new[] { VirtualKey.VK_CONTROL, VirtualKey.VK_A, VirtualKey.VK_B },
                delayMilliseconds: 0,
                key => {
                    if (key == VirtualKey.VK_B) {
                        throw new InvalidOperationException("delivery failed");
                    }

                    pressed.Add(key);
                },
                key => released.Add(key)));

        Assert.AreEqual("delivery failed", exception.Message);
        CollectionAssert.AreEqual(new[] { VirtualKey.VK_CONTROL, VirtualKey.VK_A }, pressed);
        CollectionAssert.AreEqual(new[] { VirtualKey.VK_A, VirtualKey.VK_CONTROL }, released);
    }

    [TestMethod]
    public void KeyboardInputService_ReleaseFailure_StillAttemptsRemainingKeyUps() {
        var released = new List<VirtualKey>();

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            KeyboardInputService.ExecuteKeySequence(
                new[] { VirtualKey.VK_CONTROL, VirtualKey.VK_A },
                delayMilliseconds: 0,
                _ => { },
                key => {
                    released.Add(key);
                    if (key == VirtualKey.VK_A) {
                        throw new InvalidOperationException("release failed");
                    }
                }));

        Assert.AreEqual("release failed", exception.Message);
        CollectionAssert.AreEqual(new[] { VirtualKey.VK_A, VirtualKey.VK_CONTROL }, released);
    }

    [TestMethod]
    public void DesktopAutomationService_ExpiredWaitMatch_IsNotReturned() {
        var observation = new DesktopControlObservation();
        var focusedObservation = new DesktopFocusedControlObservation();

        Assert.IsFalse(DesktopAutomationService.CanReturnWaitObservation(observation, remainingMilliseconds: 0));
        Assert.IsTrue(DesktopAutomationService.CanReturnWaitObservation(observation, remainingMilliseconds: 1));
        Assert.IsFalse(DesktopAutomationService.CanReturnWaitObservation((DesktopControlObservation?)null, remainingMilliseconds: 1));
        Assert.IsFalse(DesktopAutomationService.CanReturnWaitObservation(focusedObservation, remainingMilliseconds: 0));
        Assert.IsTrue(DesktopAutomationService.CanReturnWaitObservation(focusedObservation, remainingMilliseconds: 1));
        Assert.IsFalse(DesktopAutomationService.CanReturnWaitObservation((DesktopFocusedControlObservation?)null, remainingMilliseconds: 1));
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
