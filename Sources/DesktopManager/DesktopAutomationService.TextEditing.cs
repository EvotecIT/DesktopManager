using System;
using System.Linq;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    /// <summary>
    /// Applies a safe text edit to a previously resolved control.
    /// </summary>
    public DesktopTextEditResult EditControlText(
        WindowControlInfo control,
        DesktopTextEditRequest request,
        DesktopControlObservationOptions? observationOptions = null) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateTextEditRequest(request);
        DesktopControlObservationOptions settings = CreateTextEditObservationOptions(observationOptions, request.Text.Length);
        return EditResolvedControlText(ResolveParentWindow(control), control, request, settings, priorEditContextFingerprint: null);
    }

    /// <summary>
    /// Applies a safe text edit to the live control identified by a prior observation.
    /// </summary>
    public DesktopTextEditResult EditControlText(
        DesktopControlObservation observation,
        DesktopTextEditRequest request,
        DesktopControlObservationOptions? observationOptions = null) {
        if (observation == null) {
            throw new ArgumentNullException(nameof(observation));
        }

        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateTextEditRequest(request);
        DesktopControlObservationOptions settings = CreateTextEditObservationOptions(observationOptions, request.Text.Length);
        WindowInfo window;
        try {
            window = ResolveWindowByHandle(observation.Identity.WindowHandle);
        } catch (Exception ex) {
            return CreateTextEditFailure("window-not-found", ex.Message);
        }

        WindowControlInfo? control = GetObservationTargets(
                new WindowQueryOptions {
                    Handle = window.Handle,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                },
                new WindowControlQueryOptions {
                    UseUiAutomation = settings.UseUiAutomation,
                    IncludeUiAutomation = settings.UseUiAutomation
                },
                settings,
                allWindows: false,
                allControls: true)
            .Select(target => target.Control)
            .FirstOrDefault(candidate => MatchesObservedIdentity(candidate, observation.Identity));
        return control == null
            ? CreateTextEditFailure("control-not-found", "The observed control identity is no longer present in the target window.")
            : EditResolvedControlText(window, control, request, settings, observation.Text.EditContextFingerprint);
    }

    /// <summary>
    /// Applies a safe text edit to the first matching control and optionally verifies the result.
    /// </summary>
    public DesktopTextEditResult EditControlText(
        WindowQueryOptions windowOptions,
        WindowControlQueryOptions? controlOptions,
        DesktopTextEditRequest request,
        DesktopControlObservationOptions? observationOptions = null) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateTextEditRequest(request);
        DesktopControlObservationOptions settings = CreateTextEditObservationOptions(observationOptions, request.Text.Length);
        WindowControlTargetInfo? target = GetObservationTargets(
            windowOptions,
            controlOptions,
            settings,
            allWindows: false,
            allControls: false).FirstOrDefault();
        return target == null
            ? CreateTextEditFailure("control-not-found", "No matching control was found.")
            : EditResolvedControlText(target.Window, target.Control, request, settings, priorEditContextFingerprint: null);
    }

    /// <summary>
    /// Applies a safe text edit to the focused control of the first matching window.
    /// </summary>
    public DesktopTextEditResult EditFocusedControlText(
        WindowQueryOptions windowOptions,
        DesktopTextEditRequest request,
        DesktopControlObservationOptions? observationOptions = null) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateTextEditRequest(request);
        DesktopControlObservationOptions settings = CreateTextEditObservationOptions(observationOptions, request.Text.Length);
        WindowInfo? window = GetMatchingWindows(windowOptions, all: false).FirstOrDefault();
        if (window == null) {
            return CreateTextEditFailure("window-not-found", "No matching window was found.");
        }

        IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
        WindowControlInfo? control = settings.UseUiAutomation
            ? new UiAutomationControlService().TryGetFocusedControl(window.Handle, focusedHandle, settings.MaxTextLength, settings.ExpectedText)?.Control
            : null;
        if (control == null && focusedHandle != IntPtr.Zero && settings.IncludeNativeFallback) {
            control = GetControl(
                window.Handle,
                focusedHandle,
                useUiAutomation: false,
                includeUiAutomation: false,
                maxTextLength: settings.MaxTextLength);
        }

        return control == null
            ? CreateTextEditFailure("control-not-found", "The focused child control could not be resolved.")
            : EditResolvedControlText(window, control, request, settings, priorEditContextFingerprint: null);
    }

    internal DesktopTextEditResult EditResolvedControlText(
        WindowInfo window,
        WindowControlInfo control,
        DesktopTextEditRequest request,
        DesktopControlObservationOptions settings,
        string? priorEditContextFingerprint) {
        DesktopControlObservation? before = ObserveResolvedControl(window, control, settings);
        if (before == null) {
            return CreateTextEditFailure("observation-unavailable", "The control could not be observed before editing.");
        }

        if (before.IsPassword != false) {
            DesktopTextEditResult restricted = CreateTextEditFailure(
                before.IsPassword == true ? "password-control" : "password-state-unavailable",
                before.IsPassword == true
                    ? "Password controls cannot be read or edited through semantic text operations."
                    : "The provider password state was unavailable, so the edit was refused.");
            restricted.Before = before;
            return restricted;
        }

        if (!string.IsNullOrWhiteSpace(request.ExpectedFingerprint)) {
            if (!before.Text.IsComplete || string.IsNullOrWhiteSpace(before.Text.ContentFingerprint)) {
                DesktopTextEditResult incomplete = CreateTextEditFailure(
                    "incomplete-precondition",
                    "The complete current text was not available, so the fingerprint precondition could not be evaluated.");
                incomplete.Before = before;
                incomplete.PreconditionMatched = false;
                return incomplete;
            }

            if (!string.Equals(request.ExpectedFingerprint, before.Text.ContentFingerprint, StringComparison.OrdinalIgnoreCase)) {
                DesktopTextEditResult stale = CreateTextEditFailure(
                    "content-changed",
                    "The current text fingerprint does not match the requested precondition.");
                stale.Before = before;
                stale.PreconditionMatched = false;
                return stale;
            }
        }

        if (!TryCalculateExpectedEditedText(before.Text, request, out string expectedText, out string? validationError)) {
            DesktopTextEditResult invalid = CreateTextEditFailure("edit-range-unavailable", validationError!);
            invalid.Before = before;
            return invalid;
        }

        string expectedEditContextFingerprint = !string.IsNullOrWhiteSpace(request.ExpectedEditContextFingerprint)
            ? request.ExpectedEditContextFingerprint!
            : !string.IsNullOrWhiteSpace(priorEditContextFingerprint)
                ? priorEditContextFingerprint!
                : before.Text.EditContextFingerprint;
        if (request.Mode != DesktopTextEditMode.ReplaceDocument) {
            if (string.IsNullOrWhiteSpace(expectedEditContextFingerprint) || string.IsNullOrWhiteSpace(before.Text.EditContextFingerprint)) {
                DesktopTextEditResult incomplete = CreateTextEditFailure(
                    "edit-context-unavailable",
                    "A complete selection or caret context fingerprint is required for a range edit.");
                incomplete.Before = before;
                incomplete.PreconditionMatched = false;
                return incomplete;
            }

            if (!string.Equals(expectedEditContextFingerprint, before.Text.EditContextFingerprint, StringComparison.OrdinalIgnoreCase)) {
                DesktopTextEditResult changed = CreateTextEditFailure(
                    "edit-context-changed",
                    "The selection or caret changed after the prior observation.");
                changed.Before = before;
                changed.PreconditionMatched = false;
                return changed;
            }
        }

        bool applied = false;
        string method = string.Empty;
        var uiAutomation = new UiAutomationControlService();
        try {
            if (request.Mode == DesktopTextEditMode.ReplaceDocument) {
                if (settings.UseUiAutomation && uiAutomation.TrySetValue(window, control, request.Text)) {
                    applied = true;
                    method = "uia.value";
                } else if (control.Handle != IntPtr.Zero) {
                    try {
                        _windowManager.SetControlText(control, request.Text);
                        applied = true;
                        method = "win32.message";
                    } catch {
                        applied = false;
                    }
                }

                if (!applied && request.AllowForegroundInputFallback) {
                    applied = uiAutomation.TrySetText(window, control, request.Text, request.EnsureForegroundWindow);
                    method = applied ? "foreground.replaceDocument" : string.Empty;
                }
            } else {
                if (!request.AllowForegroundInputFallback) {
                    DesktopTextEditResult blocked = CreateTextEditFailure(
                        "foreground-input-required",
                        "Selection and caret edits require explicit foreground-input fallback authorization.");
                    blocked.Before = before;
                    return blocked;
                }

                bool selectCaret = request.Mode == DesktopTextEditMode.InsertAtCaret;
                UiAutomationTextEditAttempt attempt = uiAutomation.TryPasteTextAtSelection(
                    window,
                    control,
                    request.Text,
                    request.EnsureForegroundWindow,
                    selectCaretRange: selectCaret,
                    deleteSelectionWhenEmpty: request.Mode == DesktopTextEditMode.ReplaceSelection,
                    expectedEditContextFingerprint: expectedEditContextFingerprint,
                    maxTextLength: settings.MaxTextLength);
                applied = attempt.Applied;
                method = applied
                    ? request.Mode == DesktopTextEditMode.ReplaceSelection
                        ? "foreground.replaceSelection"
                        : "foreground.insertAtCaret"
                    : string.Empty;
                if (!applied && string.Equals(attempt.FailureCode, "edit-context-changed", StringComparison.Ordinal)) {
                    DesktopTextEditResult changed = CreateTextEditFailure(
                        attempt.FailureCode,
                        $"The selection or caret changed before foreground input was applied (expected {expectedEditContextFingerprint}, observed {attempt.ObservedEditContextFingerprint}).");
                    changed.Before = before;
                    changed.PreconditionMatched = false;
                    return changed;
                }
            }
        } catch (UiAutomationOperationInFlightException ex) {
            DesktopTextEditResult uncertain = CreateTextEditFailure("mutation-outcome-unknown", ex.Message);
            uncertain.Before = before;
            return uncertain;
        }

        if (!applied) {
            DesktopTextEditResult failed = CreateTextEditFailure("edit-failed", "No permitted provider path could apply the text edit.");
            failed.Before = before;
            return failed;
        }

        var result = new DesktopTextEditResult {
            Applied = true,
            Success = !request.VerifyAfterEdit,
            Method = method,
            Before = before
        };
        if (!request.VerifyAfterEdit) {
            return result;
        }

        DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(request.VerificationTimeoutMilliseconds);
        do {
            DesktopControlObservation? after = ObserveResolvedControl(window, control, settings);
            result.After = after;
            if (after?.Text.IsComplete == true && string.Equals(after.Text.Value, expectedText, StringComparison.Ordinal)) {
                result.Success = true;
                return result;
            }

            UiAutomationControlService.WaitWithCurrentUiMessagePump(request.VerificationIntervalMilliseconds);
        } while (DateTime.UtcNow <= deadlineUtc);

        result.FailureCode = "verification-failed";
        result.FailureReason = "The edit was applied, but the complete observed text did not reach the expected value before the timeout.";
        return result;
    }

    internal static bool TryCalculateExpectedEditedText(
        DesktopControlTextObservation before,
        DesktopTextEditRequest request,
        out string expectedText,
        out string? error) {
        expectedText = string.Empty;
        error = null;
        if (request.Mode == DesktopTextEditMode.ReplaceDocument) {
            expectedText = request.Text;
            return true;
        }

        if (!before.IsComplete) {
            error = "Complete text is required for safe edit verification.";
            return false;
        }

        int? offset;
        int length;
        if (request.Mode == DesktopTextEditMode.ReplaceSelection) {
            if (before.SelectionRanges.Count != 1 || !before.SelectionRanges[0].Offset.HasValue) {
                error = "Exactly one selected text range with a known offset is required.";
                return false;
            }

            offset = before.SelectionRanges[0].Offset;
            length = before.SelectionRanges[0].Length;
        } else {
            offset = before.CaretOffset;
            length = 0;
            if (!offset.HasValue) {
                error = "A known caret offset is required for insertion.";
                return false;
            }
        }

        if (!offset.HasValue) {
            error = "The provider did not return a text range offset.";
            return false;
        }

        int resolvedOffset = offset.Value;
        if (resolvedOffset < 0 || resolvedOffset > before.Value.Length || resolvedOffset + length > before.Value.Length) {
            error = "The provider returned an invalid text range.";
            return false;
        }

        expectedText = before.Value.Remove(resolvedOffset, length).Insert(resolvedOffset, request.Text);
        return true;
    }

    internal static bool MatchesObservedIdentity(WindowControlInfo control, DesktopControlIdentity identity) {
        if (!string.IsNullOrWhiteSpace(identity.RuntimeId)) {
            return string.Equals(control.RuntimeId, identity.RuntimeId, StringComparison.Ordinal);
        }

        if (identity.ControlHandle != IntPtr.Zero) {
            return control.Handle == identity.ControlHandle;
        }

        return string.Equals(control.AutomationId, identity.AutomationId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(control.ControlType, identity.ControlType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(control.FrameworkId, identity.FrameworkId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(control.ClassName, identity.ClassName, StringComparison.OrdinalIgnoreCase) &&
            control.Left == identity.Left &&
            control.Top == identity.Top &&
            control.Width == identity.Width &&
            control.Height == identity.Height;
    }

    private static DesktopControlObservationOptions CreateTextEditObservationOptions(
        DesktopControlObservationOptions? options,
        int requestedTextLength) {
        DesktopControlObservationOptions source = options ?? new DesktopControlObservationOptions {
            MaxTextLength = DesktopTextObservationOptions.MaximumTextLength
        };
        int requiredLength = Math.Max(source.MaxTextLength, requestedTextLength);
        if (requiredLength > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(nameof(requestedTextLength), $"Text edits are limited to {DesktopTextObservationOptions.MaximumTextLength} characters.");
        }

        var settings = new DesktopControlObservationOptions {
            UseUiAutomation = source.UseUiAutomation,
            IncludeNativeFallback = source.IncludeNativeFallback,
            MaxTextLength = requiredLength,
            ExpectedText = source.ExpectedText,
            IgnoreCase = source.IgnoreCase,
            MaxMatches = source.MaxMatches,
            MatchContextLength = source.MatchContextLength,
            IncludeTextRanges = true,
            IncludeSemanticState = source.IncludeSemanticState,
            RealizeVirtualizedItem = source.RealizeVirtualizedItem,
            MaxAncestorDepth = source.MaxAncestorDepth
        };
        UiAutomationControlService.ValidateObservationOptions(settings);
        return settings;
    }

    private static void ValidateTextEditRequest(DesktopTextEditRequest request) {
        if (request.Text == null) {
            throw new ArgumentNullException(nameof(request.Text));
        }

        if (!Enum.IsDefined(typeof(DesktopTextEditMode), request.Mode)) {
            throw new ArgumentOutOfRangeException(nameof(request.Mode), "Mode must be ReplaceDocument, ReplaceSelection, or InsertAtCaret.");
        }

        if (request.VerificationTimeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(request.VerificationTimeoutMilliseconds));
        }

        if (request.VerificationIntervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(request.VerificationIntervalMilliseconds));
        }
    }

    private static DesktopTextEditResult CreateTextEditFailure(string code, string reason) {
        return new DesktopTextEditResult {
            FailureCode = code,
            FailureReason = reason
        };
    }
}
