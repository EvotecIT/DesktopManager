using System;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>
/// Tests for UI Automation helper behavior.
/// </summary>
public class UiAutomationControlServiceTests {
    [TestMethod]
    public void UiAutomationControlService_WindowsRuntime_LoadsClientAndTypesAssemblies() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        Assert.IsTrue(new UiAutomationControlService().IsAvailable);
    }

    [TestMethod]
    /// <summary>
    /// Ensures Chromium render widget roots are prioritized for fallback probing.
    /// </summary>
    public void GetFallbackRootHandles_ChromeRenderWidget_IsPrioritized() {
        IReadOnlyList<IntPtr> handles = UiAutomationControlService.GetFallbackRootHandles(new IntPtr(100), new[] {
            new WindowControlInfo {
                Handle = new IntPtr(200),
                ClassName = "Intermediate D3D Window"
            },
            new WindowControlInfo {
                Handle = new IntPtr(300),
                ClassName = "Chrome_RenderWidgetHostHWND"
            }
        });

        CollectionAssert.AreEqual(new[] { new IntPtr(300), new IntPtr(200) }, handles.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid or duplicate handles are excluded from fallback probing.
    /// </summary>
    public void GetFallbackRootHandles_ZeroDuplicateAndParentHandles_AreExcluded() {
        IReadOnlyList<IntPtr> handles = UiAutomationControlService.GetFallbackRootHandles(new IntPtr(100), new[] {
            new WindowControlInfo {
                Handle = IntPtr.Zero,
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(100),
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(200),
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(200),
                ClassName = "Chrome_RenderWidgetHostHWND"
            }
        });

        CollectionAssert.AreEqual(new[] { new IntPtr(200) }, handles.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures action search roots include the top-level window before fallback roots.
    /// </summary>
    public void GetSearchRootHandles_WindowHandle_IsFirst() {
        IReadOnlyList<IntPtr> handles = UiAutomationControlService.GetSearchRootHandles(new IntPtr(100), new[] {
            new WindowControlInfo {
                Handle = new IntPtr(300),
                ClassName = "Chrome_RenderWidgetHostHWND"
            }
        });

        CollectionAssert.AreEqual(new[] { new IntPtr(100), new IntPtr(300) }, handles.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures a remembered fallback root is preferred for repeated UI Automation probing.
    /// </summary>
    public void GetSearchRootHandles_PreferredFallbackRoot_IsReturnedFirst() {
        IntPtr windowHandle = new IntPtr(101);
        IntPtr preferredHandle = new IntPtr(301);
        UiAutomationControlService.RememberPreferredSearchRootHandle(windowHandle, preferredHandle);

        IReadOnlyList<IntPtr> handles = UiAutomationControlService.GetSearchRootHandles(windowHandle, new[] {
            new WindowControlInfo {
                Handle = preferredHandle,
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(302),
                ClassName = "Intermediate D3D Window"
            }
        });

        CollectionAssert.AreEqual(new[] { preferredHandle, windowHandle, new IntPtr(302) }, handles.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures stale preferred roots are discarded when they are no longer valid fallback handles.
    /// </summary>
    public void GetPreferredSearchRootHandle_StaleFallback_IsCleared() {
        IntPtr windowHandle = new IntPtr(102);
        IntPtr staleHandle = new IntPtr(399);
        UiAutomationControlService.RememberPreferredSearchRootHandle(windowHandle, staleHandle);

        IntPtr preferred = UiAutomationControlService.GetPreferredSearchRootHandle(windowHandle, new[] { new IntPtr(300) });
        IntPtr repeated = UiAutomationControlService.GetPreferredSearchRootHandle(windowHandle, new[] { new IntPtr(300) });

        Assert.AreEqual(IntPtr.Zero, preferred);
        Assert.AreEqual(IntPtr.Zero, repeated);
    }

    [TestMethod]
    /// <summary>
    /// Ensures UI Automation operations reuse one STA worker instead of creating a thread per call.
    /// </summary>
    public void UiAutomationStaDispatcher_RepeatedCalls_UseSameStaThread() {
        using var dispatcher = new UiAutomationStaDispatcher();

        int firstThreadId = dispatcher.Invoke(_ => Thread.CurrentThread.ManagedThreadId);
        int secondThreadId = dispatcher.Invoke(_ => Thread.CurrentThread.ManagedThreadId);
        ApartmentState apartmentState = dispatcher.Invoke(_ => Thread.CurrentThread.GetApartmentState());

        Assert.AreEqual(firstThreadId, secondThreadId);
        Assert.AreEqual(ApartmentState.STA, apartmentState);
    }

    [TestMethod]
    /// <summary>
    /// Ensures stale window handles cannot grow the preferred-root cache without a bound.
    /// </summary>
    public void RememberPreferredSearchRootHandle_ManyWindows_BoundsCache() {
        int entryCount = UiAutomationControlService.PreferredSearchRootsMaximumCount + 32;
        for (int index = 1; index <= entryCount; index++) {
            UiAutomationControlService.RememberPreferredSearchRootHandle(
                new IntPtr(100000 + index),
                new IntPtr(200000 + index));
        }

        Assert.IsTrue(
            UiAutomationControlService.PreferredSearchRootCacheCount <= UiAutomationControlService.PreferredSearchRootsMaximumCount,
            $"Preferred-root cache grew to {UiAutomationControlService.PreferredSearchRootCacheCount} entries.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures repeated action cache keys are stable for identical control signatures.
    /// </summary>
    public void GetActionMatchCacheKey_IdenticalControlSignature_ReturnsSameKey() {
        IntPtr windowHandle = new IntPtr(103);
        var first = new WindowControlInfo {
            AutomationId = "toggle-sidebar",
            ControlType = "Button",
            ClassName = "Button",
            Text = "Hide sidebar",
            FrameworkId = "Chrome"
        };
        var second = new WindowControlInfo {
            AutomationId = "toggle-sidebar",
            ControlType = "Button",
            ClassName = "Button",
            Text = "Hide sidebar",
            FrameworkId = "Chrome"
        };

        string firstKey = UiAutomationControlService.GetActionMatchCacheKey(windowHandle, first);
        string secondKey = UiAutomationControlService.GetActionMatchCacheKey(windowHandle, second);

        Assert.AreEqual(firstKey, secondKey);
    }

    [TestMethod]
    /// <summary>
    /// Ensures privacy-safe bounds distinguish handleless controls whose readable identity is intentionally empty.
    /// </summary>
    public void GetActionMatchCacheKey_HandlelessPasswordControlsAtDifferentBounds_ReturnsDifferentKeys() {
        IntPtr windowHandle = new IntPtr(104);
        var first = new WindowControlInfo {
            ControlType = "Edit",
            IsPassword = true,
            Left = 10,
            Top = 20,
            Width = 120,
            Height = 24
        };
        var second = new WindowControlInfo {
            ControlType = "Edit",
            IsPassword = true,
            Left = 10,
            Top = 64,
            Width = 120,
            Height = 24
        };

        string firstKey = UiAutomationControlService.GetActionMatchCacheKey(windowHandle, first);
        string secondKey = UiAutomationControlService.GetActionMatchCacheKey(windowHandle, second);

        Assert.AreNotEqual(firstKey, secondKey);
    }

    [TestMethod]
    /// <summary>
    /// Ensures richer UIA metadata improves match scoring.
    /// </summary>
    public void ScoreMatch_WithMetadataAndBounds_ReturnsPositiveScore() {
        var expected = new WindowControlInfo {
            AutomationId = "toggle-sidebar",
            ControlType = "Button",
            ClassName = "Button",
            Text = "Hide sidebar",
            FrameworkId = "Chrome",
            IsEnabled = true,
            IsKeyboardFocusable = true,
            Left = 100,
            Top = 20,
            Width = 24,
            Height = 24
        };
        var candidate = new WindowControlInfo {
            AutomationId = "toggle-sidebar",
            ControlType = "Button",
            ClassName = "Button",
            Text = "Hide sidebar",
            FrameworkId = "Chrome",
            IsEnabled = true,
            IsKeyboardFocusable = true,
            Left = 102,
            Top = 20,
            Width = 24,
            Height = 24
        };

        int score = UiAutomationControlService.ScoreMatch(expected, candidate);

        Assert.IsTrue(score >= 80, $"Expected a strong score for a near-identical control, but got {score}.");
    }

    [TestMethod]
    public void ScoreMatch_WithRuntimeId_RejectsDifferentOrUnavailableCandidateIdentity() {
        var expected = new WindowControlInfo {
            RuntimeId = "1.2.3",
            AutomationId = "duplicate",
            ControlType = "Edit"
        };
        var matching = new WindowControlInfo {
            RuntimeId = "1.2.3",
            AutomationId = "duplicate",
            ControlType = "Edit"
        };
        var different = new WindowControlInfo {
            RuntimeId = "1.2.4",
            AutomationId = "duplicate",
            ControlType = "Edit"
        };
        var unavailable = new WindowControlInfo {
            AutomationId = "duplicate",
            ControlType = "Edit"
        };

        Assert.IsTrue(UiAutomationControlService.ScoreMatch(expected, matching) >= 200);
        Assert.AreEqual(-1, UiAutomationControlService.ScoreMatch(expected, different));
        Assert.AreEqual(-1, UiAutomationControlService.ScoreMatch(expected, unavailable));
    }

    [TestMethod]
    /// <summary>
    /// Ensures editable UIA controls can opt into explicit foreground fallback even when focusability metadata is missing.
    /// </summary>
    public void SupportsForegroundFallback_EditableControlWithoutHandle_ReturnsTrue() {
        bool supported = UiAutomationControlService.SupportsForegroundFallback(
            hasNativeHandle: false,
            isKeyboardFocusable: null,
            isEnabled: true,
            controlType: "Edit",
            className: "Chrome_RenderWidgetHostHWND");

        Assert.IsTrue(supported);
    }

    [TestMethod]
    /// <summary>
    /// Ensures disabled controls do not advertise explicit foreground fallback.
    /// </summary>
    public void SupportsForegroundFallback_DisabledControl_ReturnsFalse() {
        bool supported = UiAutomationControlService.SupportsForegroundFallback(
            hasNativeHandle: false,
            isKeyboardFocusable: true,
            isEnabled: false,
            controlType: "Edit",
            className: "TextBox");

        Assert.IsFalse(supported);
    }

    [TestMethod]
    /// <summary>
    /// Ensures native-handle controls do not advertise foreground fallback because they should use direct Win32 routing.
    /// </summary>
    public void SupportsForegroundFallback_NativeHandleControl_ReturnsFalse() {
        bool supported = UiAutomationControlService.SupportsForegroundFallback(
            hasNativeHandle: true,
            isKeyboardFocusable: true,
            isEnabled: true,
            controlType: "Edit",
            className: "TextBox");

        Assert.IsFalse(supported);
    }

    [TestMethod]
    /// <summary>
    /// Ensures explicit non-focusable metadata disables foreground fallback even for editable-looking controls.
    /// </summary>
    public void SupportsForegroundFallback_NotKeyboardFocusable_ReturnsFalse() {
        bool supported = UiAutomationControlService.SupportsForegroundFallback(
            hasNativeHandle: false,
            isKeyboardFocusable: false,
            isEnabled: true,
            controlType: "Edit",
            className: "TextBox");

        Assert.IsFalse(supported);
    }

    [TestMethod]
    /// <summary>
    /// Ensures obvious editable control shapes are detected for modern-app fallback heuristics.
    /// </summary>
    public void IsLikelyEditableControl_EditAndTextBoxShapes_ReturnTrue() {
        Assert.IsTrue(UiAutomationControlService.IsLikelyEditableControl("Edit", string.Empty));
        Assert.IsTrue(UiAutomationControlService.IsLikelyEditableControl("Document", string.Empty));
        Assert.IsTrue(UiAutomationControlService.IsLikelyEditableControl("ComboBox", string.Empty));
        Assert.IsTrue(UiAutomationControlService.IsLikelyEditableControl(string.Empty, "InputTextBox"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures non-editable control shapes are not treated as safe foreground-fallback targets by default.
    /// </summary>
    public void IsLikelyEditableControl_ButtonShape_ReturnsFalse() {
        Assert.IsFalse(UiAutomationControlService.IsLikelyEditableControl("Button", "Button"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop-root fallback prefers a window-sized container over a small inner child.
    /// </summary>
    public void ScoreDesktopSearchRootCandidate_WindowSizedPane_OutranksSmallInnerEdit() {
        var windowRect = new RECT {
            Left = 100,
            Top = 200,
            Right = 860,
            Bottom = 1100
        };
        var largePane = new WindowControlInfo {
            ControlType = "Pane",
            Left = 108,
            Top = 208,
            Width = 744,
            Height = 884
        };
        var smallEdit = new WindowControlInfo {
            ControlType = "Edit",
            Left = 560,
            Top = 1010,
            Width = 220,
            Height = 44
        };

        int largePaneScore = UiAutomationControlService.ScoreDesktopSearchRootCandidate(windowRect, largePane);
        int smallEditScore = UiAutomationControlService.ScoreDesktopSearchRootCandidate(windowRect, smallEdit);

        Assert.IsTrue(largePaneScore > smallEditScore,
            $"Expected the large container to outrank the small inner control, but got pane={largePaneScore}, edit={smallEditScore}.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures non-overlapping or offscreen candidates are rejected by desktop-root scoring.
    /// </summary>
    public void ScoreDesktopSearchRootCandidate_OffscreenOrNonIntersecting_ReturnsZero() {
        var windowRect = new RECT {
            Left = 100,
            Top = 200,
            Right = 860,
            Bottom = 1100
        };
        var offscreenCandidate = new WindowControlInfo {
            ControlType = "Pane",
            Left = 108,
            Top = 208,
            Width = 744,
            Height = 884,
            IsOffscreen = true
        };
        var nonIntersectingCandidate = new WindowControlInfo {
            ControlType = "Pane",
            Left = 1200,
            Top = 1600,
            Width = 300,
            Height = 200
        };

        Assert.AreEqual(0, UiAutomationControlService.ScoreDesktopSearchRootCandidate(windowRect, offscreenCandidate));
        Assert.AreEqual(0, UiAutomationControlService.ScoreDesktopSearchRootCandidate(windowRect, nonIntersectingCandidate));
    }

    [TestMethod]
    public void UiAutomationStaDispatcher_InFlightTimeout_ReportsUnknownOutcomeAndQueueRecovers() {
        using var dispatcher = new UiAutomationStaDispatcher();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        Task<Exception?> timedOutInvocation = Task.Run(() => {
            try {
                dispatcher.Invoke(
                    _ => {
                        started.Set();
                        release.Wait();
                        return true;
                    },
                    timeoutMilliseconds: 50);
                return null;
            } catch (Exception ex) {
                return ex;
            }
        });

        Assert.IsTrue(started.Wait(1000), "The dispatcher operation did not start.");
        Assert.IsTrue(timedOutInvocation.Wait(1000), "The in-flight invocation did not time out.");
        Assert.IsInstanceOfType<UiAutomationOperationInFlightException>(timedOutInvocation.Result);

        release.Set();
        int result = dispatcher.Invoke(_ => 42, timeoutMilliseconds: 1000);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void UiAutomationStaDispatcher_InFlightTimeout_CleansUpLateResult() {
        using var dispatcher = new UiAutomationStaDispatcher();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var cleaned = new ManualResetEventSlim(false);

        Task<Exception?> timedOutInvocation = Task.Run(() => {
            try {
                dispatcher.Invoke(
                    _ => {
                        started.Set();
                        release.Wait();
                        return (Action)(() => cleaned.Set());
                    },
                    timeoutMilliseconds: 50,
                    abandonedResultHandler: cleanup => cleanup());
                return null;
            } catch (Exception ex) {
                return ex;
            }
        });

        Assert.IsTrue(started.Wait(1000), "The dispatcher operation did not start.");
        Assert.IsTrue(timedOutInvocation.Wait(1000), "The in-flight invocation did not time out.");
        Assert.IsInstanceOfType<UiAutomationOperationInFlightException>(timedOutInvocation.Result);

        release.Set();
        Assert.IsTrue(cleaned.Wait(1000), "The abandoned result was not cleaned up when the operation completed.");
        Assert.AreEqual(7, dispatcher.Invoke(_ => 7, timeoutMilliseconds: 1000));
    }

    [TestMethod]
    public void UiAutomationStaDispatcher_QueuedTimeout_CancelsWorkBeforeExecution() {
        using var dispatcher = new UiAutomationStaDispatcher();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        int mutationCount = 0;

        Task firstInvocation = Task.Run(() => dispatcher.Invoke(
            _ => {
                started.Set();
                release.Wait();
                return true;
            },
            timeoutMilliseconds: 2000));

        Assert.IsTrue(started.Wait(1000), "The blocking dispatcher operation did not start.");
        Assert.ThrowsExactly<TimeoutException>(() => dispatcher.Invoke(
            _ => {
                Interlocked.Increment(ref mutationCount);
                return true;
            },
            timeoutMilliseconds: 50));

        release.Set();
        Assert.IsTrue(firstInvocation.Wait(1000), "The blocking dispatcher operation did not finish.");
        dispatcher.Invoke(_ => true, timeoutMilliseconds: 1000);
        Assert.AreEqual(0, mutationCount);
    }

    [TestMethod]
    public void ShouldRunProviderOperationInline_CallingThreadOwnsWindow_RemainsDeadlineBound() {
        Assert.IsFalse(UiAutomationControlService.ShouldRunProviderOperationInline(
            isDispatcherThread: false,
            isWindowOwnedByCallingThread: true,
            isMutation: false));
        Assert.IsTrue(UiAutomationControlService.ShouldRunProviderOperationInline(
            isDispatcherThread: false,
            isWindowOwnedByCallingThread: true,
            isMutation: true));
        Assert.IsTrue(UiAutomationControlService.ShouldRunProviderOperationInline(
            isDispatcherThread: true,
            isWindowOwnedByCallingThread: false,
            isMutation: false));
    }

    [TestMethod]
    public void TryReadPasswordState_NullProviderValue_ReturnsUnavailable() {
        bool available = UiAutomationControlService.TryReadPasswordState(
            new UnknownPasswordState(),
            out bool? isPassword);

        Assert.IsFalse(available);
        Assert.IsNull(isPassword);
    }

    [TestMethod]
    public void CreateGuardedEventSignal_AfterDisposal_IgnoresLateCallback() {
        int signalCount = 0;
        IDisposable guard = UiAutomationControlService.CreateGuardedEventSignal(
            () => signalCount++,
            out Action guardedSignal);

        guardedSignal();
        guard.Dispose();
        guardedSignal();

        Assert.AreEqual(1, signalCount);
    }

    [TestMethod]
    public void GetEnumeratedControlsCacheKey_MetadataOnlyAndValueReadsDoNotShareCache() {
        string metadataKey = UiAutomationControlService.GetEnumeratedControlsCacheKey(new IntPtr(42), includeRoot: false, readValues: false);
        string valueKey = UiAutomationControlService.GetEnumeratedControlsCacheKey(new IntPtr(42), includeRoot: false, readValues: true);

        Assert.AreNotEqual(metadataKey, valueKey);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void EnumerateControls_MetadataOnlyDoesNotReadValuesAndStructureSignalInvalidatesCache() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using System.Windows.Forms.Form form = new() { Text = "UIA Metadata Enumeration Harness", ShowInTaskbar = false };
        using System.Windows.Forms.TextBox textBox = new() { Text = "value-must-not-be-read" };
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        System.Windows.Forms.Application.DoEvents();

        UiAutomationControlService.InvalidateControlCaches();
        var service = new UiAutomationControlService();
        WindowControlInfo? metadata = service
            .EnumerateControls(form.Handle, fallbackRootHandles: null, readValues: false)
            .FirstOrDefault(control => control.Handle == textBox.Handle);

        Assert.IsNotNull(metadata);
        Assert.AreEqual(string.Empty, metadata.Value);
        Assert.IsTrue(UiAutomationControlService.EnumeratedControlsCacheCount > 0);

        WindowControlInfo? boundedValueMatch = new WindowManager()
            .GetControls(
                new WindowInfo {
                    Handle = form.Handle,
                    ProcessId = unchecked((uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                },
                new WindowControlQueryOptions {
                    IncludeUiAutomation = true,
                    ValuePattern = "value-must-not-be-read"
                },
                maxTextLength: 64)
            .FirstOrDefault(control => control.Handle == textBox.Handle);
        Assert.IsNotNull(boundedValueMatch);
        Assert.AreEqual("value-must-not-be-read", boundedValueMatch.Value);

        int cacheCountSeenByWaiter = -1;
        Action structureSignal = UiAutomationControlService.CreateStructureChangedSignal(
            () => cacheCountSeenByWaiter = UiAutomationControlService.EnumeratedControlsCacheCount);
        structureSignal();

        Assert.AreEqual(0, cacheCountSeenByWaiter);
        Assert.AreEqual(0, UiAutomationControlService.EnumeratedControlsCacheCount);
    }

    [TestMethod]
    public void IsTextMutationAllowed_RequiresLiveExplicitNonPasswordState() {
        Assert.IsTrue(UiAutomationControlService.IsTextMutationAllowed(new AutomationElementStub(false)));
        Assert.IsFalse(UiAutomationControlService.IsTextMutationAllowed(new AutomationElementStub(true)));
        Assert.IsFalse(UiAutomationControlService.IsTextMutationAllowed(new AutomationElementStub(null)));
    }

    private sealed class UnknownPasswordState {
        public bool? IsPassword => null;
    }

    private sealed class AutomationElementStub {
        internal AutomationElementStub(bool? isPassword) {
            Current = new PasswordStateStub(isPassword);
        }

        public PasswordStateStub Current { get; }
    }

    private sealed class PasswordStateStub {
        internal PasswordStateStub(bool? isPassword) {
            IsPassword = isPassword;
        }

        public bool? IsPassword { get; }
    }
}
