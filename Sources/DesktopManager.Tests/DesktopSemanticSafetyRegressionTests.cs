using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>Protects deadline, input-delivery, retry-quality, and live password-safety regressions.</summary>
public class DesktopSemanticSafetyRegressionTests {
    [TestMethod]
    public void DesktopAutomationService_WeakObservationIdentity_RejectsAmbiguousMatches() {
        var identity = new DesktopControlIdentity {
            AutomationId = "editor",
            ControlType = "Document",
            FrameworkId = "WPF",
            ClassName = "RichTextBox",
            AncestorPath = "Window#root/TabItem#second/Document#editor",
            Left = 10,
            Top = 20,
            Width = 300,
            Height = 200
        };
        var first = new WindowControlInfo {
            AutomationId = "editor",
            ControlType = "Document",
            FrameworkId = "WPF",
            ClassName = "RichTextBox",
            Left = 10,
            Top = 20,
            Width = 300,
            Height = 200
        };
        var second = new WindowControlInfo {
            AutomationId = "editor",
            ControlType = "Document",
            FrameworkId = "WPF",
            ClassName = "RichTextBox",
            Left = 10,
            Top = 20,
            Width = 300,
            Height = 200
        };

        WindowControlInfo? resolved = DesktopAutomationService.ResolveUniqueObservedControl(
            new[] { first, second },
            identity,
            out bool ambiguous);

        Assert.IsNull(resolved);
        Assert.IsTrue(ambiguous);
        StringAssert.Contains(UiAutomationControlService.CreateObservationSessionKey(identity), "|x:Window#root/TabItem#second/Document#editor");
    }

    [TestMethod]
    public void UiAutomationControlService_ControlWithoutReadableText_IsUnavailable() {
        var errors = new List<string>();
        DesktopControlTextObservation observation = new UiAutomationControlService().ReadControlTextObservation(
            new NoTextAutomationElementStub(),
            new Dictionary<string, object>(),
            new DesktopControlObservationOptions { ExpectedText = "needle" },
            errors);

        Assert.AreEqual("uia.textUnavailable", observation.Source);
        Assert.IsFalse(observation.IsComplete);
        Assert.AreEqual(string.Empty, observation.ContentFingerprint);
        Assert.IsNull(observation.ContainsExpected);
        CollectionAssert.Contains(errors, "text.unavailable");
    }

    [TestMethod]
    public void UiAutomationControlService_ZeroLengthSelections_AreRangeBounded() {
        string[] emptySelections = Enumerable
            .Repeat(string.Empty, UiAutomationControlService.MaximumSelectionRangeCount + 1)
            .ToArray();
        var errors = new List<string>();

        IReadOnlyList<DesktopTextRangeObservation> selections = UiAutomationControlService.ReadTextSelections(
            new TextSelectionPatternStub(emptySelections),
            maxLength: 4096,
            errors,
            out bool isComplete);

        Assert.AreEqual(UiAutomationControlService.MaximumSelectionRangeCount, selections.Count);
        Assert.IsFalse(isComplete);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void UiAutomationControlService_ProviderMutationException_PreservesUnknownOutcome() {
        var provider = new ThrowingMutationPattern();
        MethodInfo? method = provider.GetType().GetMethod(nameof(ThrowingMutationPattern.SetValue));
        Assert.IsNotNull(method);

        UiAutomationMutationOutcomeUnknownException exception = Assert.ThrowsExactly<UiAutomationMutationOutcomeUnknownException>(() =>
            UiAutomationControlService.InvokeProviderMutation(
                provider,
                method,
                new object[] { "changed" },
                "System.Windows.Automation.ValuePattern",
                nameof(ThrowingMutationPattern.SetValue)));

        Assert.AreEqual("changed", provider.Value);
        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        StringAssert.Contains(exception.Message, "outcome is unknown");
    }

    [TestMethod]
    public void DesktopAutomationService_FocusedEmptyProviderValue_RemainsAuthoritative() {
        Assert.IsTrue(DesktopAutomationService.HasAuthoritativeFocusedValue(new DesktopFocusedControlObservation {
            Value = string.Empty,
            ValueSource = "uia.textPattern",
            Text = "accessible label"
        }));
        Assert.IsTrue(DesktopAutomationService.HasAuthoritativeFocusedValue(new DesktopFocusedControlObservation {
            Value = string.Empty,
            ValueSource = "native.windowText",
            Text = "stale metadata"
        }));
        Assert.IsFalse(DesktopAutomationService.HasAuthoritativeFocusedValue(new DesktopFocusedControlObservation {
            Value = string.Empty,
            ValueSource = "uia.textUnavailable",
            Text = "accessible label"
        }));
        Assert.IsFalse(DesktopAutomationService.HasAuthoritativeFocusedValue(new DesktopFocusedControlObservation {
            Value = string.Empty,
            ValueSource = "uia.password",
            Text = string.Empty
        }));
    }

    [TestMethod]
    public void DesktopAutomationService_MergeNativeObservationState_DisabledFallbackLeavesProviderStateUntouched() {
        var observation = new DesktopControlObservation {
            Status = "available",
            IsPassword = false,
            IsEnabled = true,
            IsVisible = true,
            IsFocused = true,
            IsChecked = true,
            Text = DesktopTextObservationBuilder.Create("provider", "uia.textPattern", false, null, false, 0, 0),
            Capabilities = new DesktopControlCapabilities {
                CanReadText = true,
                CanToggle = false
            }
        };

        DesktopAutomationService.MergeNativeObservationState(
            observation,
            new WindowInfo { Handle = new IntPtr(10), ProcessId = 20 },
            new WindowControlInfo {
                ParentWindowHandle = new IntPtr(10),
                Handle = new IntPtr(30),
                IsPassword = null,
                SupportsBackgroundClick = true
            },
            new DesktopControlObservationOptions { IncludeNativeFallback = false },
            nativeTimeoutMilliseconds: 1,
            getRemainingProviderTimeoutMilliseconds: null);

        Assert.AreEqual("available", observation.Status);
        Assert.AreEqual("provider", observation.Text.Value);
        Assert.AreEqual("uia.textPattern", observation.Text.Source);
        Assert.AreEqual(false, observation.IsPassword);
        Assert.AreEqual(true, observation.IsEnabled);
        Assert.AreEqual(true, observation.IsVisible);
        Assert.AreEqual(true, observation.IsFocused);
        Assert.AreEqual(true, observation.IsChecked);
        Assert.IsTrue(observation.Capabilities.CanReadText);
        Assert.IsFalse(observation.Capabilities.CanToggle);
    }

    [TestMethod]
    public void UiAutomationControlService_ForegroundPreparation_PrecedesElementFocus() {
        var operations = new List<string>();

        bool result = UiAutomationControlService.TryPrepareForegroundAndFocus(
            ensureForegroundWindow: true,
            prepareForeground: () => {
                operations.Add("activate");
                return true;
            },
            prepareElement: () => operations.Add("scroll"),
            focusElement: () => {
                operations.Add("focus");
                return true;
            },
            out bool foregroundPreparationFailed);

        Assert.IsTrue(result);
        Assert.IsFalse(foregroundPreparationFailed);
        CollectionAssert.AreEqual(new[] { "activate", "scroll", "focus" }, operations);

        operations.Clear();
        result = UiAutomationControlService.TryPrepareForegroundAndFocus(
            ensureForegroundWindow: true,
            prepareForeground: () => {
                operations.Add("activate");
                return false;
            },
            prepareElement: () => operations.Add("scroll"),
            focusElement: () => {
                operations.Add("focus");
                return true;
            },
            out foregroundPreparationFailed);

        Assert.IsFalse(result);
        Assert.IsTrue(foregroundPreparationFailed);
        CollectionAssert.AreEqual(new[] { "activate" }, operations);
    }

    [TestMethod]
    public void DesktopAutomationService_FocusedHandlelessUiAutomationMetadata_IsNotText() {
        var automationResult = new UiAutomationFocusedControlResult {
            Control = new WindowControlInfo {
                Handle = IntPtr.Zero,
                Text = "accessible label",
                Value = "cached metadata",
                IsPassword = false,
                Source = WindowControlSource.UiAutomation
            }
        };

        Assert.IsFalse(DesktopAutomationService.CanUseFocusedMetadataText(
            automationResult,
            automationText: null,
            nativeTextReadAttempted: false));
        Assert.IsTrue(DesktopAutomationService.CanUseFocusedMetadataText(
            automationResult: null,
            automationText: null,
            nativeTextReadAttempted: false));
    }

    [TestMethod]
    public void UiAutomationControlService_ObservationIdentity_PreservesDiscoveredRuntimeIdWhenRefreshFails() {
        Assert.AreEqual("1.2.3", UiAutomationControlService.ResolveObservationRuntimeId(string.Empty, "1.2.3"));
        Assert.AreEqual("4.5.6", UiAutomationControlService.ResolveObservationRuntimeId("4.5.6", "1.2.3"));
    }

    [TestMethod]
    public void UiAutomationControlService_BestMatch_PrefersStrictIdentityOverPreferredRootPartialMatch() {
        var expected = new WindowControlInfo {
            AutomationId = "TargetEditor",
            ControlType = "Edit",
            ClassName = "EditorClass"
        };
        var preferredRootPartial = new WindowControlInfo {
            ControlType = "Edit"
        };
        var strictMatch = new WindowControlInfo {
            AutomationId = "TargetEditor",
            ControlType = "Edit",
            ClassName = "EditorClass"
        };

        int partialScore = UiAutomationControlService.ScoreMatch(expected, preferredRootPartial);
        int strictScore = UiAutomationControlService.ScoreMatch(expected, strictMatch);

        Assert.IsTrue(UiAutomationControlService.ShouldReplaceBestMatch(
            expected,
            strictMatch,
            strictScore,
            preferredRootPartial,
            partialScore));
        Assert.IsFalse(UiAutomationControlService.ShouldReplaceBestMatch(
            expected,
            preferredRootPartial,
            partialScore,
            strictMatch,
            strictScore));
    }

    [TestMethod]
    public void DesktopAutomationService_HandlelessMetadataText_RemainsUnavailable() {
        DesktopControlObservation observation = DesktopAutomationService.CreateNativeControlObservation(
            new WindowInfo { Handle = new IntPtr(10), ProcessId = 20 },
            new WindowControlInfo {
                Handle = IntPtr.Zero,
                Text = "accessible name",
                Value = "cached value",
                IsPassword = false,
                Source = WindowControlSource.UiAutomation
            },
            new DesktopControlObservationOptions { ExpectedText = "accessible name" });

        Assert.AreEqual("partial", observation.Status);
        Assert.AreEqual("native.windowText.unavailable", observation.Text.Source);
        Assert.AreEqual(string.Empty, observation.Text.Value);
        Assert.IsFalse(observation.Text.IsComplete);
        Assert.AreEqual(string.Empty, observation.Text.ContentFingerprint);
        Assert.AreEqual(null, observation.Text.ContainsExpected);
        Assert.IsFalse(observation.Capabilities.CanReadText);
    }

    [TestMethod]
    public void UiAutomationControlService_TimedOutFallback_PreservesExplicitResultContracts() {
        DesktopControlObservation? semantic = UiAutomationControlService.CreateTimedOutOperationFallback<DesktopControlObservation?>();
        List<WindowControlInfo> controls = UiAutomationControlService.CreateTimedOutOperationFallback<List<WindowControlInfo>>();
        WindowControlInfo[] controlArray = UiAutomationControlService.CreateTimedOutOperationFallback<WindowControlInfo[]>();
        DesktopUiAutomationActionDiagnostic action = UiAutomationControlService.CreateTimedOutOperationFallback<DesktopUiAutomationActionDiagnostic>();
        UiAutomationTextEditAttempt edit = UiAutomationControlService.CreateTimedOutOperationFallback<UiAutomationTextEditAttempt>();

        Assert.IsNull(semantic);
        Assert.AreEqual(0, controls.Count);
        Assert.AreEqual(0, controlArray.Length);
        Assert.IsTrue(action.Attempted);
        Assert.IsTrue(action.TimedOut);
        Assert.AreEqual("timeout", action.SearchMode);
        Assert.IsFalse(edit.Applied);
        Assert.AreEqual("provider-timeout", edit.FailureCode);
    }

    [TestMethod]
    public void DesktopAutomationService_EditObservationLength_CoversExpectedDocument() {
        Assert.AreEqual(8, DesktopAutomationService.GetRequiredEditObservationLength(4, 8));
        Assert.AreEqual(8, DesktopAutomationService.GetRequiredEditObservationLength(8, 4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            DesktopAutomationService.GetRequiredEditObservationLength(
                4,
                DesktopTextObservationOptions.MaximumTextLength + 1));
    }

    [TestMethod]
    public void WindowControlService_SetText_RejectsOversizedInputBeforeMutation() {
        string oversized = new('x', DesktopTextObservationOptions.MaximumTextLength + 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            WindowControlService.SetText(new WindowControlInfo { Handle = new IntPtr(123) }, oversized));
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

    private sealed class ThrowingMutationPattern {
        internal string Value { get; private set; } = string.Empty;

        public void SetValue(string value) {
            Value = value;
            throw new InvalidOperationException("Provider failed after applying the mutation.");
        }
    }

    private sealed class NoTextAutomationElementStub {
        public NonPasswordStateStub Current { get; } = new();
    }

    private sealed class NonPasswordStateStub {
        public bool IsPassword => false;
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
