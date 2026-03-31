using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.PowerShell;

internal static class DesktopControlMutationVerifier {
    internal static DesktopControlMutationRecord Verify(
        DesktopAutomationService automation,
        string action,
        WindowControlInfo requestedControl,
        string expectedText = null,
        bool? expectedCheckState = null,
        bool requireForeground = false) {
        WindowInfo parentWindow = ObserveParentWindow(automation, requestedControl.ParentWindowHandle);
        WindowControlInfo observedControl = ObserveControl(automation, requestedControl, parentWindow);
        WindowInfo activeWindow = SafeGetActiveWindow(automation);
        var notes = new List<string>();

        if (observedControl == null) {
            return new DesktopControlMutationRecord {
                Action = action,
                Success = true,
                VerificationPerformed = true,
                Verified = false,
                VerificationMode = "presence",
                VerificationSummary = $"DesktopManager requested '{action}', but the control was no longer observable afterward.",
                RequestedControl = requestedControl,
                ParentWindow = parentWindow,
                ActiveWindow = activeWindow,
                VerificationNotes = new[] { BuildMissingControlNote(requestedControl) }
            };
        }

        bool hasSpecificExpectation = expectedText != null || expectedCheckState.HasValue || requireForeground;
        if (!hasSpecificExpectation) {
            return new DesktopControlMutationRecord {
                Action = action,
                Success = true,
                VerificationPerformed = true,
                Verified = true,
                VerificationMode = "presence",
                VerificationSummary = $"Observed the control after '{action}'.",
                RequestedControl = requestedControl,
                ObservedControl = observedControl,
                ParentWindow = parentWindow,
                ActiveWindow = activeWindow
            };
        }

        bool verified = true;
        string mode = "postcondition";
        if (expectedText != null) {
            string observedText = GetObservedControlText(observedControl);
            if (!string.Equals(observedText, expectedText, StringComparison.Ordinal)) {
                verified = false;
                mode = "text";
                notes.Add($"Observed text/value '{observedText}' instead of '{expectedText}'.");
            } else {
                mode = "text";
            }
        }

        if (expectedCheckState.HasValue) {
            bool? observedCheckState = TryGetObservedCheckState(automation, observedControl);
            if (!observedCheckState.HasValue) {
                verified = false;
                mode = "check";
                notes.Add("The observed control did not expose a check state that could be re-queried.");
            } else if (observedCheckState.Value != expectedCheckState.Value) {
                verified = false;
                mode = "check";
                notes.Add($"Observed check state {observedCheckState.Value} instead of {expectedCheckState.Value}.");
            } else {
                mode = "check";
            }
        }

        if (requireForeground) {
            mode = mode == "postcondition" ? "foreground" : mode + "-foreground";
            if (parentWindow == null || activeWindow == null || activeWindow.Handle != parentWindow.Handle) {
                verified = false;
                notes.Add(activeWindow == null
                    ? "Windows did not report an active foreground window after the mutation."
                    : $"Foreground window was '{activeWindow.Title}' [{activeWindow.Handle.ToInt64():X}] instead of the control's parent window.");
            }
        }

        return new DesktopControlMutationRecord {
            Action = action,
            Success = true,
            VerificationPerformed = true,
            Verified = verified,
            VerificationMode = mode,
            VerificationSummary = verified
                ? $"Observed the requested postcondition after '{action}'."
                : $"DesktopManager requested '{action}', but the observed postcondition did not match.",
            RequestedControl = requestedControl,
            ObservedControl = observedControl,
            ParentWindow = parentWindow,
            ActiveWindow = activeWindow,
            VerificationNotes = notes
        };
    }

    internal static DesktopControlMutationRecord CreateFailureRecord(string action, WindowControlInfo requestedControl, string message, bool verificationPerformed) {
        return new DesktopControlMutationRecord {
            Action = action,
            Success = false,
            VerificationPerformed = verificationPerformed,
            Verified = verificationPerformed ? false : null,
            VerificationMode = verificationPerformed ? "error" : string.Empty,
            VerificationSummary = message,
            RequestedControl = requestedControl,
            VerificationNotes = new[] { message }
        };
    }

    private static WindowInfo ObserveParentWindow(DesktopAutomationService automation, IntPtr parentWindowHandle) {
        if (parentWindowHandle == IntPtr.Zero) {
            return null;
        }

        IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
            Handle = parentWindowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        return windows.Count > 0 ? windows[0] : null;
    }

    private static WindowControlInfo ObserveControl(DesktopAutomationService automation, WindowControlInfo requestedControl, WindowInfo parentWindow) {
        if (parentWindow == null) {
            return null;
        }

        var query = new WindowControlQueryOptions {
            IncludeUiAutomation = requestedControl.Source == WindowControlSource.UiAutomation || requestedControl.Handle == IntPtr.Zero,
            UseUiAutomation = requestedControl.Source == WindowControlSource.UiAutomation || requestedControl.Handle == IntPtr.Zero,
            EnsureForegroundWindow = false
        };

        IReadOnlyList<WindowControlTargetInfo> controls = automation.GetControls(new WindowQueryOptions {
            Handle = parentWindow.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, query, allWindows: false, allControls: true);

        WindowControlInfo bestMatch = null;
        int bestScore = 0;
        foreach (WindowControlTargetInfo target in controls) {
            int score = ScoreControlMatch(requestedControl, target.Control);
            if (score > bestScore) {
                bestScore = score;
                bestMatch = target.Control;
            }
        }

        return bestScore > 0 ? bestMatch : null;
    }

    private static int ScoreControlMatch(WindowControlInfo requestedControl, WindowControlInfo candidate) {
        if (candidate == null) {
            return 0;
        }

        if (requestedControl.Handle != IntPtr.Zero && candidate.Handle == requestedControl.Handle) {
            return 1000;
        }

        int score = 0;
        if (!string.IsNullOrWhiteSpace(requestedControl.AutomationId) &&
            string.Equals(requestedControl.AutomationId, candidate.AutomationId, StringComparison.Ordinal)) {
            score += 400;
        }
        if (!string.IsNullOrWhiteSpace(requestedControl.ControlType) &&
            string.Equals(requestedControl.ControlType, candidate.ControlType, StringComparison.OrdinalIgnoreCase)) {
            score += 200;
        }
        if (!string.IsNullOrWhiteSpace(requestedControl.ClassName) &&
            string.Equals(requestedControl.ClassName, candidate.ClassName, StringComparison.Ordinal)) {
            score += 150;
        }
        if (!string.IsNullOrWhiteSpace(requestedControl.Text) &&
            string.Equals(requestedControl.Text, candidate.Text, StringComparison.Ordinal)) {
            score += 100;
        }
        if (!string.IsNullOrWhiteSpace(requestedControl.Value) &&
            string.Equals(requestedControl.Value, candidate.Value, StringComparison.Ordinal)) {
            score += 100;
        }
        if (requestedControl.Left == candidate.Left &&
            requestedControl.Top == candidate.Top &&
            requestedControl.Width == candidate.Width &&
            requestedControl.Height == candidate.Height &&
            requestedControl.Width > 0 &&
            requestedControl.Height > 0) {
            score += 50;
        }
        if (requestedControl.Id != 0 && requestedControl.Id == candidate.Id) {
            score += 25;
        }

        return score;
    }

    private static string BuildMissingControlNote(WindowControlInfo requestedControl) {
        if (requestedControl.Handle != IntPtr.Zero) {
            return $"Control handle [{requestedControl.Handle.ToInt64():X}] could not be re-queried after the mutation.";
        }

        if (!string.IsNullOrWhiteSpace(requestedControl.AutomationId)) {
            return $"Control automation id '{requestedControl.AutomationId}' could not be re-queried after the mutation.";
        }

        return "The requested control could not be re-queried after the mutation.";
    }

    private static string GetObservedControlText(WindowControlInfo control) {
        if (control == null) {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(control.Value)) {
            return control.Value;
        }

        if (!string.IsNullOrEmpty(control.Text)) {
            return control.Text;
        }

        if (control.Handle != IntPtr.Zero) {
            return WindowTextHelper.GetWindowText(control.Handle);
        }

        return string.Empty;
    }

    private static bool? TryGetObservedCheckState(DesktopAutomationService automation, WindowControlInfo control) {
        if (control == null || control.Handle == IntPtr.Zero) {
            return null;
        }

        try {
            return automation.GetControlCheckState(control);
        } catch {
            return null;
        }
    }

    private static WindowInfo SafeGetActiveWindow(DesktopAutomationService automation) {
        try {
            return automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        } catch {
            return null;
        }
    }
}
