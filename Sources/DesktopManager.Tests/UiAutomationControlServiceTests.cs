using System;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for UI Automation helper behavior.
/// </summary>
public class UiAutomationControlServiceTests {
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
}
