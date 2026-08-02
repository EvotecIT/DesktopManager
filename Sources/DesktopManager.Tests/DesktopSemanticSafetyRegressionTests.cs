using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>Protects deadline, input-delivery, retry-quality, and live password-safety regressions.</summary>
public class DesktopSemanticSafetyRegressionTests {
    [TestMethod]
    public void WindowControlService_ComboItemLength_FailedOrOversizedReadIsUnavailable() {
        Assert.IsFalse(WindowControlService.CanReadComboBoxItemText(-1, 4));
        Assert.IsFalse(WindowControlService.CanReadComboBoxItemText(5, 4));
        Assert.IsTrue(WindowControlService.CanReadComboBoxItemText(4, 4));
    }

    [TestMethod]
    public void UiAutomationControlService_TimedOutFallback_OnlyConstructsCollectionResults() {
        DesktopControlObservation? semantic = UiAutomationControlService.CreateTimedOutOperationFallback<DesktopControlObservation?>();
        List<WindowControlInfo> controls = UiAutomationControlService.CreateTimedOutOperationFallback<List<WindowControlInfo>>();
        WindowControlInfo[] controlArray = UiAutomationControlService.CreateTimedOutOperationFallback<WindowControlInfo[]>();

        Assert.IsNull(semantic);
        Assert.AreEqual(0, controls.Count);
        Assert.AreEqual(0, controlArray.Length);
    }

    [TestMethod]
    public void UiAutomationControlService_ReadTextSelections_ExactBudgetWithMoreRangesIsIncomplete() {
        var errors = new List<string>();
        var pattern = new TextSelectionPatternStub("abcd", "efgh");

        IReadOnlyList<DesktopTextRangeObservation> ranges = UiAutomationControlService.ReadTextSelections(
            pattern,
            maxLength: 4,
            errors,
            out bool isComplete);

        Assert.AreEqual(1, ranges.Count);
        Assert.AreEqual("abcd", ranges[0].Text);
        Assert.IsFalse(ranges[0].IsTruncated);
        Assert.IsFalse(isComplete);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void DesktopTextObservationBuilder_CreateEditContextFingerprint_RejectsIncompleteProviderRanges() {
        DesktopControlTextObservation observation = DesktopTextObservationBuilder.Create("complete", "test", false, null, false, 0, 0);

        observation.AreSelectionRangesComplete = false;
        Assert.AreEqual(string.Empty, DesktopTextObservationBuilder.CreateEditContextFingerprint(observation));

        observation.AreSelectionRangesComplete = true;
        observation.IsActiveCompositionComplete = false;
        Assert.AreEqual(string.Empty, DesktopTextObservationBuilder.CreateEditContextFingerprint(observation));

        observation.IsActiveCompositionComplete = true;
        observation.IsConversionTargetComplete = false;
        Assert.AreEqual(string.Empty, DesktopTextObservationBuilder.CreateEditContextFingerprint(observation));
    }

    [TestMethod]
    public void WindowActivationService_PreparationRetriesRespectRemainingDeadline() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool prepared = WindowActivationService.TryPrepareWindowForAutomation(
            new IntPtr(123),
            retryCount: 3,
            retryDelayMilliseconds: 100,
            getRemainingMilliseconds: () => Math.Max(0, 10 - (int)stopwatch.ElapsedMilliseconds));

        Assert.IsFalse(prepared);
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromMilliseconds(500), $"Preparation exceeded its deadline: {stopwatch.Elapsed}.");
    }

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

    private sealed class TextSelectionPatternStub {
        private readonly object[] _ranges;

        internal TextSelectionPatternStub(params string[] values) {
            _ranges = new object[values.Length];
            for (int i = 0; i < values.Length; i++) {
                _ranges[i] = new TextRangeStub(values[i]);
            }
        }

        public object? DocumentRange => null;

        public object[] GetSelection() {
            return _ranges;
        }
    }

    private sealed class TextRangeStub {
        private readonly string _value;

        internal TextRangeStub(string value) {
            _value = value;
        }

        public string GetText(int maximumLength) {
            return _value.Length > maximumLength ? _value.Substring(0, maximumLength) : _value;
        }
    }
}
