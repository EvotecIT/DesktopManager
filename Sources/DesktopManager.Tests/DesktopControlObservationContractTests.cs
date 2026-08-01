using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>
/// Protects the provider-neutral control observation and safe text-edit contracts.
/// </summary>
public class DesktopControlObservationContractTests {
    [TestMethod]
    public void DesktopTextObservationBuilder_Create_NormalizesEscapesMatchesAndFingerprintsCompleteText() {
        DesktopControlTextObservation observation = DesktopTextObservationBuilder.Create(
            "one\r\ntwo\0\ttwo",
            "test",
            isTruncated: false,
            expectedText: "TWO",
            ignoreCase: true,
            maxMatches: 10,
            contextLength: 2);

        Assert.AreEqual("one\ntwo\ttwo", observation.NormalizedValue);
        Assert.AreEqual("one\\r\\ntwo\\0\\ttwo", observation.EscapedValue);
        Assert.AreEqual(true, observation.ContainsExpected);
        Assert.AreEqual(2, observation.Matches.Count);
        Assert.AreEqual(5, observation.Matches[0].Offset);
        Assert.AreEqual(10, observation.Matches[1].Offset);
        Assert.IsTrue(observation.HasNonPrintingCharacters);
        Assert.AreEqual(64, observation.ContentFingerprint.Length);
    }

    [TestMethod]
    public void DesktopTextObservationBuilder_Create_TruncatedProviderMatchBeyondPrefix_DoesNotExposeFingerprint() {
        DesktopControlTextObservation observation = DesktopTextObservationBuilder.Create(
            "prefix",
            "uia.text",
            isTruncated: true,
            expectedText: "hidden",
            ignoreCase: false,
            maxMatches: 10,
            contextLength: 5,
            containsExpected: true);

        Assert.IsFalse(observation.IsComplete);
        Assert.AreEqual(true, observation.ContainsExpected);
        Assert.IsTrue(observation.MatchFoundBeyondObservedPrefix);
        Assert.AreEqual(string.Empty, observation.ContentFingerprint);
    }

    [TestMethod]
    public void DesktopTextObservationBuilder_Create_ZeroMatchDetailsStillDetectsVisibleExpectedText() {
        DesktopControlTextObservation observation = DesktopTextObservationBuilder.Create(
            "alpha beta",
            "test",
            isTruncated: false,
            expectedText: "beta",
            ignoreCase: false,
            maxMatches: 0,
            contextLength: 0);

        Assert.AreEqual(true, observation.ContainsExpected);
        Assert.IsFalse(observation.MatchFoundBeyondObservedPrefix);
        Assert.AreEqual(0, observation.Matches.Count);
    }

    [TestMethod]
    public void DesktopTextObservationBuilder_CreateEditContextFingerprint_ChangesWhenRangeMovesWithoutTextChange() {
        DesktopControlTextObservation observation = DesktopTextObservationBuilder.Create(
            "alpha beta gamma", "test", false, null, false, 0, 0);
        observation.SelectionRanges = new[] {
            new DesktopTextRangeObservation { Offset = 6, Length = 4, Text = "beta" }
        };
        string original = DesktopTextObservationBuilder.CreateEditContextFingerprint(observation);

        observation.SelectionRanges = new[] {
            new DesktopTextRangeObservation { Offset = 11, Length = 5, Text = "gamma" }
        };
        string moved = DesktopTextObservationBuilder.CreateEditContextFingerprint(observation);

        Assert.AreEqual(64, original.Length);
        Assert.AreEqual(64, moved.Length);
        Assert.AreNotEqual(original, moved);
        Assert.AreEqual(DesktopTextObservationBuilder.CreateFingerprint("alpha beta gamma"), observation.ContentFingerprint);
    }

    [TestMethod]
    public void DesktopTextObservationBuilder_CreateFingerprint_PreservesUnpairedUtf16CodeUnits() {
        string first = DesktopTextObservationBuilder.CreateFingerprint("\uD800");
        string second = DesktopTextObservationBuilder.CreateFingerprint("\uD801");

        Assert.AreEqual(64, first.Length);
        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void DesktopTextObservationBuilder_CreateUnavailable_PreservesEvidenceWithoutClaimingCompleteness() {
        DesktopControlTextObservation observation = DesktopTextObservationBuilder.CreateUnavailable(
            "uia.textUnavailable",
            "needle",
            ignoreCase: true,
            containsExpected: true);

        Assert.AreEqual(string.Empty, observation.Value);
        Assert.AreEqual("uia.textUnavailable", observation.Source);
        Assert.IsFalse(observation.IsComplete);
        Assert.IsFalse(observation.IsTruncated);
        Assert.AreEqual(string.Empty, observation.ContentFingerprint);
        Assert.AreEqual(true, observation.ContainsExpected);
        Assert.AreEqual("needle", observation.ExpectedText);
        Assert.IsTrue(observation.ExpectedTextIgnoreCase);
    }

    [TestMethod]
    public void DesktopAutomationService_CreateNativeControlObservation_UnknownPasswordState_FailsClosed() {
        DesktopControlObservation observation = DesktopAutomationService.CreateNativeControlObservation(
            new WindowInfo { Handle = new IntPtr(10), ProcessId = 20 },
            new WindowControlInfo {
                ParentWindowHandle = new IntPtr(10),
                Text = "must-not-read-name",
                Value = "must-not-read-value",
                IsPassword = null,
                SupportsBackgroundText = true
            },
            new DesktopControlObservationOptions());

        Assert.AreEqual("restricted", observation.Status);
        Assert.AreEqual(string.Empty, observation.Text.Value);
        Assert.AreEqual(string.Empty, observation.Text.ContentFingerprint);
        Assert.IsFalse(observation.Text.IsComplete);
        Assert.IsFalse(observation.Capabilities.CanReadText);
        Assert.IsFalse(observation.Capabilities.CanSetValue);
        Assert.IsFalse(observation.Capabilities.SupportsBackgroundText);
    }

    [TestMethod]
    public void DesktopAutomationService_ShouldUseNativeTextFallback_ProviderEmptyText_RemainsAuthoritative() {
        var observation = new DesktopControlObservation {
            Text = DesktopTextObservationBuilder.Create(
                string.Empty,
                "uia.valuePattern",
                isTruncated: false,
                expectedText: null,
                ignoreCase: false,
                maxMatches: 0,
                contextLength: 0)
        };
        var control = new WindowControlInfo { Handle = new IntPtr(42) };
        var settings = new DesktopControlObservationOptions { IncludeNativeFallback = true };

        Assert.IsFalse(DesktopAutomationService.ShouldUseNativeTextFallback(observation, control, settings));
        observation.Text.Source = string.Empty;
        Assert.IsTrue(DesktopAutomationService.ShouldUseNativeTextFallback(observation, control, settings));
    }

    [TestMethod]
    public void DesktopAutomationService_EditControlText_UndefinedMode_Throws() {
        var request = new DesktopTextEditRequest {
            Text = "value",
            Mode = (DesktopTextEditMode)3
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new DesktopAutomationService().EditControlText(new WindowControlInfo(), request));
    }

#if NET8_0_OR_GREATER
    [TestMethod]
    public void DesktopOperations_EditControlText_NumericUndefinedMode_ThrowsCommandLineException() {
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() =>
            global::DesktopManager.Cli.DesktopOperations.EditControlText(
                new global::DesktopManager.Cli.WindowSelectionCriteria(),
                new global::DesktopManager.Cli.ControlSelectionCriteria(),
                text: "value",
                mode: "3",
                expectedFingerprint: null,
                expectedEditContextFingerprint: null,
                ensureForegroundWindow: false,
                allowForegroundInputFallback: false,
                verifyAfterEdit: true,
                maxTextLength: 1024));
    }
#endif

    [TestMethod]
    public void DesktopControlObservationCondition_MatchesSemanticStateAndRange() {
        var observation = new DesktopControlObservation {
            IsEnabled = true,
            IsFocused = true,
            IsChecked = false,
            ExpandCollapseState = "Expanded",
            Text = DesktopTextObservationBuilder.Create("Ready", "test", false, "ready", true, 2, 2),
            Selection = new DesktopControlSelectionObservation { IsSelected = true },
            Range = new DesktopControlRangeObservation { Value = 42 }
        };
        var condition = new DesktopControlObservationCondition {
            ExpectedText = "READY",
            IgnoreCase = true,
            IsTextComplete = true,
            IsEnabled = true,
            IsFocused = true,
            IsChecked = false,
            IsSelected = true,
            ExpandCollapseState = "expanded",
            MinimumRangeValue = 40,
            MaximumRangeValue = 45
        };

        Assert.IsTrue(condition.Matches(observation));
        condition.MaximumRangeValue = 41;
        Assert.IsFalse(condition.Matches(observation));
    }

    [TestMethod]
    public void DesktopControlObservationCondition_DoesNotReuseProviderEvidenceForDifferentLiteral() {
        DesktopControlTextObservation text = DesktopTextObservationBuilder.Create(
            "bounded prefix",
            "uia.textPattern",
            isTruncated: true,
            expectedText: "bar",
            ignoreCase: false,
            maxMatches: 0,
            contextLength: 0,
            containsExpected: true);
        var observation = new DesktopControlObservation { Text = text };

        Assert.IsTrue(new DesktopControlObservationCondition { ExpectedText = "bar" }.Matches(observation));
        Assert.IsFalse(new DesktopControlObservationCondition { ExpectedText = "foo" }.Matches(observation));
        Assert.IsFalse(new DesktopControlObservationCondition { ExpectedText = "BAR", IgnoreCase = true }.Matches(observation));
    }

    [TestMethod]
    public void ControlEnumerator_PopulateControlValues_UnknownPasswordStateFailsClosed() {
        var control = new WindowControlInfo {
            Handle = new IntPtr(123),
            IsPassword = null,
            Text = "stale text",
            Value = "stale value"
        };

        ControlEnumerator.PopulateControlValues(new[] { control });

        Assert.AreEqual(string.Empty, control.Text);
        Assert.AreEqual(string.Empty, control.Value);
    }

    [TestMethod]
    public void DesktopAutomationService_GetProviderInvocationTimeout_UsesRemainingOverallWaitBudget() {
        Assert.AreEqual(75, DesktopAutomationService.GetProviderInvocationTimeout(100, 25));
        Assert.AreEqual(0, DesktopAutomationService.GetProviderInvocationTimeout(100, 100));
        Assert.AreEqual(UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds, DesktopAutomationService.GetProviderInvocationTimeout(0, 50000));
        Assert.AreEqual(UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds, DesktopAutomationService.GetProviderInvocationTimeout(30000, 0));
    }

    [TestMethod]
    public void DesktopAutomationService_DisposeAutomationChangeSubscription_UsesRemainingWaitBudget() {
        var subscription = new RecordingBoundedDisposable();

        DesktopAutomationService.DisposeAutomationChangeSubscription(subscription, () => 37);

        Assert.AreEqual(37, subscription.TimeoutMilliseconds);
        Assert.AreEqual(1, subscription.DisposeCount);
    }

    [TestMethod]
    public void UiAutomationControlService_MatchesForegroundInputTarget_RequiresExactFocusedTarget() {
        IntPtr windowHandle = new(100);
        IntPtr controlHandle = new(200);

        Assert.IsTrue(UiAutomationControlService.MatchesForegroundInputTarget(
            windowHandle, controlHandle, windowHandle, controlHandle, hasKeyboardFocus: true, elementNativeHandle: 200));
        Assert.IsFalse(UiAutomationControlService.MatchesForegroundInputTarget(
            windowHandle, controlHandle, new IntPtr(101), controlHandle, hasKeyboardFocus: true, elementNativeHandle: 200));
        Assert.IsFalse(UiAutomationControlService.MatchesForegroundInputTarget(
            windowHandle, controlHandle, windowHandle, new IntPtr(201), hasKeyboardFocus: true, elementNativeHandle: 200));
        Assert.IsFalse(UiAutomationControlService.MatchesForegroundInputTarget(
            windowHandle, controlHandle, windowHandle, controlHandle, hasKeyboardFocus: false, elementNativeHandle: 200));
        Assert.IsTrue(UiAutomationControlService.MatchesForegroundInputTarget(
            windowHandle, IntPtr.Zero, windowHandle, IntPtr.Zero, hasKeyboardFocus: true, elementNativeHandle: 0));
    }

    [TestMethod]
    public void WindowControlService_TryGetCheckState_ExpiredDeadlineSkipsNativeMessage() {
        bool success = WindowControlService.TryGetCheckState(
            new WindowControlInfo { Handle = new IntPtr(123) },
            timeoutMilliseconds: 0,
            out bool isChecked);

        Assert.IsFalse(success);
        Assert.IsFalse(isChecked);
    }

    [TestMethod]
    public void WindowControlService_TryGetSelectedValue_ExpiredDeadlineSkipsNativeMessages() {
        bool success = WindowControlService.TryGetSelectedValue(
            new WindowControlInfo { Handle = new IntPtr(123), IsPassword = false },
            maxTextLength: 32,
            timeoutMilliseconds: 0,
            out string value,
            out bool isTruncated);

        Assert.IsFalse(success);
        Assert.AreEqual(string.Empty, value);
        Assert.IsFalse(isTruncated);
    }

    [TestMethod]
    public void DesktopAutomationService_TryCalculateExpectedEditedText_ReplacesExactSelection() {
        var before = new DesktopControlTextObservation {
            Value = "alpha beta gamma",
            IsComplete = true,
            SelectionRanges = new[] {
                new DesktopTextRangeObservation { Offset = 6, Length = 4, Text = "beta" }
            }
        };

        bool success = DesktopAutomationService.TryCalculateExpectedEditedText(
            before,
            new DesktopTextEditRequest { Text = "delta", Mode = DesktopTextEditMode.ReplaceSelection },
            out string expected,
            out string? error);

        Assert.IsTrue(success, error);
        Assert.AreEqual("alpha delta gamma", expected);
    }

    [TestMethod]
    public void DesktopAutomationService_TryCalculateExpectedEditedText_RejectsUnknownCaretOffset() {
        var before = new DesktopControlTextObservation {
            Value = "alpha",
            IsComplete = true
        };

        bool success = DesktopAutomationService.TryCalculateExpectedEditedText(
            before,
            new DesktopTextEditRequest { Text = "x", Mode = DesktopTextEditMode.InsertAtCaret },
            out _,
            out string? error);

        Assert.IsFalse(success);
        StringAssert.Contains(error, "caret offset");
    }

    [TestMethod]
    public void DesktopAutomationService_TryCalculateExpectedEditedText_RejectsOversizedCombinedResult() {
        var before = new DesktopControlTextObservation {
            Value = new string('a', DesktopTextObservationOptions.MaximumTextLength),
            IsComplete = true,
            CaretOffset = DesktopTextObservationOptions.MaximumTextLength
        };

        bool success = DesktopAutomationService.TryCalculateExpectedEditedText(
            before,
            new DesktopTextEditRequest { Text = "x", Mode = DesktopTextEditMode.InsertAtCaret },
            out _,
            out string? error);

        Assert.IsFalse(success);
        StringAssert.Contains(error, "semantic text limit");
    }

    [TestMethod]
    public void DesktopAutomationService_VerificationBudget_ClampsProviderAndSleepDurations() {
        var stopwatch = Stopwatch.StartNew();
        Assert.IsTrue(SpinWait.SpinUntil(() => stopwatch.ElapsedMilliseconds >= 25, 1000));

        int providerTimeout = DesktopAutomationService.GetVerificationProviderTimeout(stopwatch, 100);
        int waitInterval = DesktopAutomationService.GetVerificationWaitInterval(stopwatch, 100, 200);

        Assert.IsTrue(providerTimeout > 0 && providerTimeout <= 75);
        Assert.IsTrue(waitInterval > 0 && waitInterval <= 75);
        stopwatch.Stop();
    }

    [TestMethod]
    public void WindowManager_MatchesValuePattern_PreservesProviderEvidenceBeyondPrefix() {
        var control = new WindowControlInfo {
            Value = "prefix",
            ValueIsTruncated = true,
            ValueMatchPattern = "*needle*",
            ValueMatchIgnoreCase = true,
            ValuePatternMatched = true
        };

        Assert.AreEqual("needle", WindowManager.GetProviderContainsLiteral("*needle*"));
        Assert.IsNull(WindowManager.GetProviderContainsLiteral("needle*"));
        Assert.IsNull(WindowManager.GetProviderContainsLiteral("*need?e*"));
        Assert.IsTrue(new WindowManager().MatchesValuePattern(control, "*NEEDLE*"));
        Assert.IsFalse(new WindowManager().MatchesValuePattern(control, "different"));
    }

    [TestMethod]
    public void UiAutomationControlService_IsExpectedCollapsedCaret_RequiresSameContentCaretAndCollapsedRanges() {
        string contentFingerprint = DesktopTextObservationBuilder.CreateFingerprint("alpha beta gamma");
        var observation = new DesktopControlTextObservation {
            IsComplete = true,
            ContentFingerprint = contentFingerprint,
            CaretOffset = 11,
            SelectionRanges = new[] {
                new DesktopTextRangeObservation { Offset = 11, Length = 0 }
            }
        };

        Assert.IsTrue(UiAutomationControlService.IsExpectedCollapsedCaret(observation, contentFingerprint, 11));
        observation.SelectionRanges = new[] {
            new DesktopTextRangeObservation { Offset = 7, Length = 4, Text = "beta" }
        };
        Assert.IsFalse(UiAutomationControlService.IsExpectedCollapsedCaret(observation, contentFingerprint, 11));
        observation.SelectionRanges = Array.Empty<DesktopTextRangeObservation>();
        Assert.IsFalse(UiAutomationControlService.IsExpectedCollapsedCaret(observation, contentFingerprint, 10));
        Assert.IsFalse(UiAutomationControlService.IsExpectedCollapsedCaret(observation, new string('0', 64), 11));
    }

    [TestMethod]
    public void DesktopAutomationService_MatchesObservedIdentity_PrefersRuntimeIdOverHandle() {
        var control = new WindowControlInfo {
            Handle = new IntPtr(42),
            RuntimeId = "1.2.3",
            AutomationId = "editor"
        };
        var identity = new DesktopControlIdentity {
            ControlHandle = new IntPtr(999),
            RuntimeId = "1.2.3",
            AutomationId = "different"
        };

        Assert.IsTrue(DesktopAutomationService.MatchesObservedIdentity(control, identity));
        identity.RuntimeId = "1.2.4";
        Assert.IsFalse(DesktopAutomationService.MatchesObservedIdentity(control, identity));
    }

    [TestMethod]
    public void DesktopAutomationService_MatchesObservedWindowOwner_RequiresStablePositiveProcessId() {
        var window = new WindowInfo { ProcessId = 42 };
        var identity = new DesktopControlIdentity { ProcessId = 42 };

        Assert.IsTrue(DesktopAutomationService.MatchesObservedWindowOwner(window, identity));
        identity.ProcessId = 43;
        Assert.IsFalse(DesktopAutomationService.MatchesObservedWindowOwner(window, identity));
        identity.ProcessId = 0;
        Assert.IsFalse(DesktopAutomationService.MatchesObservedWindowOwner(window, identity));
    }

    [TestMethod]
    public void ClipboardSnapshot_RestoresTextAndCustomFormats() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        using PumpingWinFormsHarness harness = PumpingWinFormsHarness.Create("DesktopManager Clipboard Harness", _ => { });
        try {
            harness.Invoke(() => {
                var dataObject = new DataObject();
                dataObject.SetText("original clipboard text");
                dataObject.SetData("DesktopManager.Custom", false, "custom clipboard value");
                Clipboard.SetDataObject(dataObject, copy: true);

                using (ClipboardHelper.ClipboardSnapshot snapshot = ClipboardHelper.CaptureSnapshot()) {
                    ClipboardHelper.SetText("temporary semantic edit text");
                    snapshot.Restore();
                }

                System.Windows.Forms.IDataObject? restored = Clipboard.GetDataObject();
                Assert.IsNotNull(restored);
                Assert.AreEqual("original clipboard text", restored.GetData(DataFormats.UnicodeText));
                Assert.AreEqual("custom clipboard value", restored.GetData("DesktopManager.Custom"));
            });
        } catch (InvalidOperationException ex) when (ex.InnerException is ExternalException) {
            Assert.Inconclusive("The interactive clipboard was unavailable to the test harness.");
        } finally {
            try {
                harness.Invoke(Clipboard.Clear);
            } catch {
                // Clipboard cleanup is best-effort when another desktop process owns it.
            }
        }
    }

    [TestMethod]
    public void DesktopAutomationService_ObserveAndEditWinFormsTextBox_RoundTripsThroughSharedObservation() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        TextBox? editor = null;
        using PumpingWinFormsHarness harness = PumpingWinFormsHarness.Create(
            "DesktopManager Semantic Control Harness",
            form => {
                editor = new TextBox {
                    Name = "SemanticEditor",
                    Left = 12,
                    Top = 12,
                    Width = 240,
                    Text = "before semantic edit"
                };
                form.Controls.Add(editor);
                form.Shown += (_, _) => editor.Focus();
            });

        Assert.IsNotNull(editor);
        harness.Invoke(() => editor.Focus());
        IntPtr editorHandle = harness.Invoke(() => editor.Handle);

        var automation = new DesktopAutomationService();
        DesktopControlObservation before = harness.Invoke(() => automation.ObserveControls(
                CreateWindowQuery(harness.Window.Handle),
                new WindowControlQueryOptions {
                    Handle = editorHandle,
                    IncludeUiAutomation = true
                },
                new DesktopControlObservationOptions { MaxTextLength = 1024 },
                allWindows: false,
                allControls: false)
            .Single());

        Assert.AreEqual(false, before.IsPassword);
        Assert.AreEqual(
            "before semantic edit",
            before.Text.Value,
            $"source={before.Source}; textSource={before.Text.Source}; runtimeId={before.Identity.RuntimeId}; handle={before.Identity.ControlHandleHex}; status={before.Status}; failure={before.FailureReason}");
        Assert.IsTrue(before.Text.IsComplete);
        Assert.AreEqual(64, before.Text.ContentFingerprint.Length);

        DesktopTextEditResult stale = automation.EditControlText(
            before,
            new DesktopTextEditRequest {
                Text = "must not apply",
                ExpectedFingerprint = new string('0', 64)
            });
        Assert.IsFalse(stale.Success);
        Assert.IsFalse(stale.Applied);
        Assert.AreEqual("content-changed", stale.FailureCode);
        Assert.AreEqual("before semantic edit", harness.Invoke(() => editor.Text));

        DesktopTextEditResult result = harness.Invoke(() => automation.EditControlText(
            before,
            new DesktopTextEditRequest {
                Text = "after semantic edit",
                ExpectedFingerprint = before.Text.ContentFingerprint
            }));

        Assert.IsTrue(
            result.Success,
            $"{result.FailureReason} expected={before.Text.ContentFingerprint}; actual={result.Before?.Text.ContentFingerprint}; text={result.Before?.Text.EscapedValue}; source={result.Before?.Text.Source}");
        Assert.IsTrue(result.Applied);
        Assert.AreEqual("after semantic edit", result.After?.Text.Value);
        Assert.AreEqual("after semantic edit", harness.Invoke(() => editor.Text));
    }

    [TestMethod]
    public void DesktopAutomationService_ObservePasswordTextBox_SuppressesEveryTextChannel() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        TextBox? editor = null;
        using PumpingWinFormsHarness harness = PumpingWinFormsHarness.Create(
            "DesktopManager Semantic Password Harness",
            form => {
                editor = new TextBox {
                    Name = "SemanticPassword",
                    Left = 12,
                    Top = 12,
                    Width = 240,
                    Text = "never-expose-this-value",
                    UseSystemPasswordChar = true
                };
                form.Controls.Add(editor);
                form.Shown += (_, _) => editor.Focus();
            });

        Assert.IsNotNull(editor);
        harness.Invoke(() => editor.Focus());
        IntPtr editorHandle = harness.Invoke(() => editor.Handle);

        var automation = new DesktopAutomationService();
        DesktopControlObservation observation = automation.ObserveControls(
            CreateWindowQuery(harness.Window.Handle),
            new WindowControlQueryOptions { Handle = editorHandle, IncludeUiAutomation = true },
            new DesktopControlObservationOptions { ExpectedText = "never-expose" },
            allWindows: false,
            allControls: false).Single();

        Assert.AreEqual(true, observation.IsPassword);
        Assert.AreEqual(string.Empty, observation.Text.Value);
        Assert.AreEqual(string.Empty, observation.Text.NormalizedValue);
        Assert.AreEqual(string.Empty, observation.Text.EscapedValue);
        Assert.AreEqual(string.Empty, observation.Text.ContentFingerprint);
        Assert.IsFalse(observation.Text.IsComplete);
        Assert.AreEqual(0, observation.Text.SelectedText.Count);
        Assert.AreEqual(0, observation.Text.SelectionRanges.Count);
        Assert.AreNotEqual(true, observation.Text.ContainsExpected);

        DesktopTextEditResult semanticEdit = automation.EditControlText(
            observation,
            new DesktopTextEditRequest { Text = "must-not-apply" });
        Assert.IsFalse(semanticEdit.Success);
        Assert.IsFalse(semanticEdit.Applied);
        Assert.AreEqual("password-control", semanticEdit.FailureCode);

        WindowControlInfo control = automation.GetControl(harness.Window.Handle, editorHandle)!;
        Assert.ThrowsExactly<InvalidOperationException>(() => automation.SetControlText(control, "must-not-apply"));
        Assert.AreEqual("never-expose-this-value", harness.Invoke(() => editor.Text));
    }

    [TestMethod]
    public void DesktopAutomationService_WaitForControlObservation_CompletesAfterProviderTextChange() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        TextBox? editor = null;
        using PumpingWinFormsHarness harness = PumpingWinFormsHarness.Create(
            "DesktopManager Semantic Wait Harness",
            form => {
                editor = new TextBox {
                    Name = "SemanticWaitEditor",
                    Left = 12,
                    Top = 12,
                    Width = 240,
                    Text = "waiting"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        IntPtr editorHandle = harness.Invoke(() => editor.Handle);
        Task update = Task.Run(async () => {
            await Task.Delay(250).ConfigureAwait(false);
            harness.Invoke(() => editor.Text = "semantic event complete");
        });

        DesktopControlObservation observation = harness.Invoke(() => new DesktopAutomationService().WaitForControlObservation(
            CreateWindowQuery(harness.Window.Handle),
            new WindowControlQueryOptions { Handle = editorHandle, IncludeUiAutomation = true },
            new DesktopControlObservationCondition { ExpectedText = "event complete" },
            timeoutMilliseconds: 5000,
            intervalMilliseconds: 1000,
            observationOptions: new DesktopControlObservationOptions { MaxTextLength = 1024 }));
        update.GetAwaiter().GetResult();

        Assert.AreEqual(true, observation.Text.ContainsExpected);
        StringAssert.Contains(observation.Text.Value, "semantic event complete");
        Assert.IsTrue(
            observation.WaitStrategy == "uia.events+polling" || observation.WaitStrategy == "polling",
            $"Unexpected wait strategy '{observation.WaitStrategy}'.");
    }

    [TestMethod]
    public void DesktopAutomationService_ObserveStructuredWinFormsControls_ReturnsToggleAndRangeState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        CheckBox? checkBox = null;
        ProgressBar? progress = null;
        using PumpingWinFormsHarness harness = PumpingWinFormsHarness.Create(
            "DesktopManager Structured Control Harness",
            form => {
                checkBox = new CheckBox {
                    Name = "SemanticToggle",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "Structured toggle",
                    Checked = true
                };
                progress = new ProgressBar {
                    Name = "SemanticRange",
                    Left = 12,
                    Top = 48,
                    Width = 220,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 42
                };
                form.Controls.Add(checkBox);
                form.Controls.Add(progress);
            });

        Assert.IsNotNull(checkBox);
        Assert.IsNotNull(progress);
        IntPtr checkBoxHandle = harness.Invoke(() => checkBox.Handle);
        IntPtr progressHandle = harness.Invoke(() => progress.Handle);
        var automation = new DesktopAutomationService();
        DesktopControlObservation toggle = automation.ObserveControls(
            CreateWindowQuery(harness.Window.Handle),
            new WindowControlQueryOptions { Handle = checkBoxHandle, IncludeUiAutomation = true },
            new DesktopControlObservationOptions(),
            allWindows: false,
            allControls: false).Single();
        DesktopControlObservation range = automation.ObserveControls(
            CreateWindowQuery(harness.Window.Handle),
            new WindowControlQueryOptions { Handle = progressHandle, IncludeUiAutomation = true },
            new DesktopControlObservationOptions(),
            allWindows: false,
            allControls: false).Single();

        Assert.IsTrue(toggle.Capabilities.CanToggle, string.Join(",", toggle.Capabilities.Patterns));
        Assert.AreEqual(true, toggle.IsChecked);
        Assert.IsTrue(range.Capabilities.CanReadRange, string.Join(",", range.Capabilities.Patterns));
        Assert.AreEqual(0d, range.Range.Minimum);
        Assert.AreEqual(100d, range.Range.Maximum);
        Assert.AreEqual(42d, range.Range.Value);
    }

    [TestMethod]
    public void DesktopAutomationService_ObserveControl_NativeCheckboxReportsToggleState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        const int wsChild = 0x40000000;
        const int wsVisible = 0x10000000;
        const int bsAutoCheckBox = 0x00000003;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Native Toggle Harness");
        IntPtr checkBoxHandle = MonitorNativeMethods.CreateWindowExW(
            0,
            "Button",
            "Native toggle",
            wsChild | wsVisible | bsAutoCheckBox,
            12,
            12,
            180,
            24,
            harness.Form.Handle,
            new IntPtr(1003),
            IntPtr.Zero,
            IntPtr.Zero);
        IntPtr comboBoxHandle = MonitorNativeMethods.CreateWindowExW(
            0,
            "ComboBox",
            string.Empty,
            wsChild | wsVisible | 0x00000003,
            12,
            48,
            180,
            120,
            harness.Form.Handle,
            new IntPtr(1004),
            IntPtr.Zero,
            IntPtr.Zero);
        if (checkBoxHandle == IntPtr.Zero || comboBoxHandle == IntPtr.Zero) {
            if (checkBoxHandle != IntPtr.Zero) {
                MonitorNativeMethods.DestroyWindow(checkBoxHandle);
            }
            if (comboBoxHandle != IntPtr.Zero) {
                MonitorNativeMethods.DestroyWindow(comboBoxHandle);
            }

            Assert.Inconclusive("Failed to create native checkbox and combo-box controls for observation testing.");
        }

        try {
            var nativeControl = new WindowControlInfo {
                ParentWindowHandle = harness.Window.Handle,
                Handle = checkBoxHandle,
                ClassName = "Button"
            };
            WindowControlService.SetCheckState(nativeControl, true);
            Application.DoEvents();

            DesktopControlObservation? observation = new DesktopAutomationService().ObserveControl(
                harness.Window.Handle,
                checkBoxHandle,
                new DesktopControlObservationOptions {
                    UseUiAutomation = false,
                    IncludeNativeFallback = true
                });

            Assert.IsNotNull(observation);
            Assert.IsTrue(observation.Capabilities.CanToggle);
            Assert.AreEqual(true, observation.IsChecked);
            Assert.IsTrue(new DesktopControlObservationCondition { IsChecked = true }.Matches(observation));

            DesktopControlObservation? comboObservation = new DesktopAutomationService().ObserveControl(
                harness.Window.Handle,
                comboBoxHandle,
                new DesktopControlObservationOptions {
                    UseUiAutomation = false,
                    IncludeNativeFallback = true
                });
            Assert.IsNotNull(comboObservation);
            Assert.AreEqual(string.Empty, comboObservation.Text.Value);
            Assert.IsTrue(comboObservation.Capabilities.CanSelect);
        } finally {
            MonitorNativeMethods.DestroyWindow(checkBoxHandle);
            MonitorNativeMethods.DestroyWindow(comboBoxHandle);
        }
    }

    private static WindowQueryOptions CreateWindowQuery(IntPtr handle) {
        return new WindowQueryOptions {
            Handle = handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }
}

internal sealed class RecordingBoundedDisposable : IUiAutomationBoundedDisposable {
    internal int DisposeCount { get; private set; }

    internal int TimeoutMilliseconds { get; private set; } = -1;

    public void Dispose() {
        Dispose(UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds);
    }

    public void Dispose(int timeoutMilliseconds) {
        DisposeCount++;
        TimeoutMilliseconds = timeoutMilliseconds;
    }
}

/// <summary>
/// Hosts an owned WinForms provider on a pumping STA thread while automation runs from the test thread.
/// </summary>
internal sealed class PumpingWinFormsHarness : IDisposable {
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Thread _thread;
    private Form? _form;
    private Exception? _startupFailure;

    private PumpingWinFormsHarness(string title, Action<Form> configure) {
        _thread = new Thread(() => Run(title, configure)) {
            IsBackground = true,
            Name = "DesktopManager semantic WinForms provider"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(10))) {
            throw new TimeoutException("The WinForms provider did not start.");
        }

        if (_startupFailure != null) {
            throw new InvalidOperationException("The WinForms provider failed to start.", _startupFailure);
        }

        IntPtr handle = Invoke(() => _form!.Handle);
        Window = new WindowManager().GetWindows(includeHidden: true).Single(candidate => candidate.Handle == handle);
    }

    internal WindowInfo Window { get; }

    internal static PumpingWinFormsHarness Create(string title, Action<Form> configure) {
        return new PumpingWinFormsHarness(title, configure);
    }

    internal void Invoke(Action action) {
        Invoke(() => {
            action();
            return true;
        });
    }

    internal T Invoke<T>(Func<T> action) {
        if (_form == null) {
            throw new ObjectDisposedException(nameof(PumpingWinFormsHarness));
        }

        if (Thread.CurrentThread == _thread) {
            return action();
        }

        T result = default!;
        Exception? failure = null;
        using var completed = new ManualResetEventSlim(false);
        _form.BeginInvoke(new Action(() => {
            try {
                result = action();
            } catch (Exception ex) {
                failure = ex;
            } finally {
                completed.Set();
            }
        }));
        if (!completed.Wait(TimeSpan.FromSeconds(10))) {
            throw new TimeoutException("The WinForms provider did not complete an invocation.");
        }

        if (failure != null) {
            throw new InvalidOperationException("The WinForms provider invocation failed.", failure);
        }

        return result;
    }

    public void Dispose() {
        Form? form = _form;
        if (form == null) {
            return;
        }

        try {
            form.BeginInvoke(new Action(form.Close));
            _thread.Join(TimeSpan.FromSeconds(10));
        } catch {
            // The test provider is process-local and will be reclaimed with the test host.
        } finally {
            _form = null;
            _ready.Dispose();
        }
    }

    private void Run(string title, Action<Form> configure) {
        try {
            using var form = new Form {
                Text = title,
                Width = 320,
                Height = 240,
                Left = 40,
                Top = 40,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false
            };
            configure(form);
            _form = form;
            form.Shown += (_, _) => _ready.Set();
            Application.Run(form);
        } catch (Exception ex) {
            _startupFailure = ex;
            _ready.Set();
        }
    }
}
