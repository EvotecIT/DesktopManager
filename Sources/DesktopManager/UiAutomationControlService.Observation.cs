using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    internal const int MaximumSelectionRangeCount = 256;

    /// <summary>
    /// Observes one resolved control through UI Automation without exposing provider-specific objects.
    /// </summary>
    public DesktopControlObservation? TryObserveControl(
        WindowInfo window,
        WindowControlInfo control,
        DesktopControlObservationOptions? options = null) {
        return TryObserveControl(window, control, options, UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds);
    }

    internal DesktopControlObservation? TryObserveControl(
        WindowInfo window,
        WindowControlInfo control,
        DesktopControlObservationOptions? options,
        int invocationTimeoutMilliseconds) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        DesktopControlObservationOptions settings = options ?? new DesktopControlObservationOptions();
        ValidateObservationOptions(settings);
        if (!IsAvailable) {
            return null;
        }

        return RunInSta(
            service => service.TryObserveControlCore(window, control, settings),
            window.Handle,
            invocationTimeoutMilliseconds: invocationTimeoutMilliseconds);
    }

    private DesktopControlObservation? TryObserveControlCore(
        WindowInfo window,
        WindowControlInfo control,
        DesktopControlObservationOptions options) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return null;
        }

        var errors = new List<string>();
        Dictionary<string, object> patterns = ReadObservationPatterns(element, errors);
        if (options.RealizeVirtualizedItem && patterns.TryGetValue("VirtualizedItem", out object? virtualizedItem)) {
            try {
#if NET8_0_OR_GREATER
                ((UiaPattern)virtualizedItem).Realize();
#else
                virtualizedItem.GetType().GetMethod("Realize", Type.EmptyTypes)?.Invoke(virtualizedItem, null);
#endif
                match = ResolveMatchingElement(window.Handle, control);
                element = match.Element ?? element;
                patterns = ReadObservationPatterns(element, errors);
            } catch (Exception ex) {
                AddObservationError(errors, "virtualizedItem.realize", ex);
            }
        }

        WindowControlInfo refreshedControl = control;
        try {
            WindowControlInfo? refreshed = CreateControlInfo(element, readValue: false);
            if (refreshed != null) {
                refreshed.ParentWindowHandle = window.Handle;
                refreshedControl = refreshed;
            }
        } catch (Exception ex) {
            AddObservationError(errors, "control.metadata", ex);
        }

        object? current;
        try {
            current = GetAutomationProperty(element, "Current");
        } catch (Exception ex) {
            AddObservationError(errors, "control.current", ex);
            return null;
        }

        if (current == null) {
            return null;
        }

        bool passwordStateAvailable = TryReadPasswordState(current, out bool? isPassword);
        if (!passwordStateAvailable) {
            AddObservationError(errors, "control.isPassword", new InvalidOperationException("The provider did not expose a readable password state."));
        }

        var observation = new DesktopControlObservation {
            Identity = CreateObservationIdentity(window, refreshedControl, element, options, errors),
            Capabilities = CreateObservationCapabilities(refreshedControl, patterns, isPassword == false),
            Source = "uia",
            ObservedAtUtc = DateTime.UtcNow,
            UsedCachedMetadata = match.UsedCachedActionMatch,
            IsPassword = isPassword,
            IsEnabled = ReadObservationBoolean(current, "IsEnabled", errors),
            IsOffscreen = ReadObservationBoolean(current, "IsOffscreen", errors),
            IsFocused = ReadObservationBoolean(current, "HasKeyboardFocus", errors),
            IsKeyboardFocusable = ReadObservationBoolean(current, "IsKeyboardFocusable", errors)
        };
        observation.IsVisible = observation.IsOffscreen.HasValue ? !observation.IsOffscreen.Value : null;

        if (isPassword == true || !passwordStateAvailable) {
            observation.Text = DesktopTextObservationBuilder.CreateRestricted(
                isPassword == true ? "uia.password" : "uia.passwordStateUnavailable");
        } else {
            observation.Text = ReadControlTextObservation(element, patterns, options, errors);
        }

        if (options.IncludeSemanticState) {
            PopulateSemanticState(observation, patterns, options.MaxTextLength, errors);
        }

        observation.Status = !passwordStateAvailable
            ? "restricted"
            : errors.Count > 0
                ? "partial"
                : "available";
        observation.FailureReason = FormatObservationErrors(errors);
        return observation;
    }

    internal DesktopControlTextObservation ReadControlTextObservation(
        object element,
        Dictionary<string, object> patterns,
        DesktopControlObservationOptions options,
        List<string> errors) {
        UiAutomationTextReadResult? textResult = ReadElementText(element, options.MaxTextLength, options.ExpectedText, options.IgnoreCase);
        string value = textResult?.Value ?? string.Empty;
        bool providerContains = textResult?.ContainsExpected == true;
        if (!string.IsNullOrEmpty(options.ExpectedText) && !providerContains) {
            providerContains = TryFindTextWithProvider(patterns, options.ExpectedText!, options.IgnoreCase, errors);
        }

        if (textResult == null) {
            errors.Add("text.unavailable");
            return DesktopTextObservationBuilder.CreateUnavailable(
                "uia.textUnavailable",
                options.ExpectedText,
                options.IgnoreCase,
                providerContains ? true : null);
        }

        DesktopControlTextObservation observation = DesktopTextObservationBuilder.Create(
            value,
            textResult?.Source ?? string.Empty,
            textResult?.IsTruncated == true,
            options.ExpectedText,
            options.IgnoreCase,
            options.MaxMatches,
            options.MatchContextLength,
            providerContains ? true : null);

        if (options.IncludeTextRanges && patterns.TryGetValue("Text", out object? textPattern)) {
            observation.SelectionRanges = ReadTextSelections(
                textPattern,
                options.MaxTextLength,
                errors,
                out bool selectionRangesComplete);
            observation.AreSelectionRangesComplete = selectionRangesComplete;
            var selectedText = new List<string>(observation.SelectionRanges.Count);
            foreach (DesktopTextRangeObservation range in observation.SelectionRanges) {
                selectedText.Add(range.Text);
            }
            observation.SelectedText = selectedText;
            observation.SupportedSelection = ReadPatternCurrentString(textPattern, "SupportedTextSelection", errors, "text.supportedSelection");
        }

        if (options.IncludeTextRanges && patterns.TryGetValue("Text2", out object? textPattern2)) {
            ReadCaret(textPattern2, options.MaxTextLength, observation, errors);
        }

        if (options.IncludeTextRanges && !observation.CaretOffset.HasValue && observation.SelectionRanges.Count == 1) {
            DesktopTextRangeObservation selection = observation.SelectionRanges[0];
            if (selection.Length == 0 && selection.Offset.HasValue && selection.Offset.Value >= 0 && selection.Offset.Value <= observation.Value.Length) {
                observation.CaretOffset = selection.Offset;
                int contextStart = Math.Max(0, selection.Offset.Value - 128);
                int contextLength = Math.Min(observation.Value.Length - contextStart, 256);
                observation.CaretContext = contextLength > 0
                    ? observation.Value.Substring(contextStart, contextLength)
                    : string.Empty;
            }
        }

        if (options.IncludeTextRanges && patterns.TryGetValue("TextEdit", out object? textEditPattern)) {
            observation.ActiveComposition = ReadTextRangeFromMethod(
                textEditPattern,
                "GetActiveComposition",
                options.MaxTextLength,
                errors,
                "textEdit.activeComposition",
                out bool activeCompositionComplete);
            observation.IsActiveCompositionComplete = activeCompositionComplete;
            observation.ConversionTarget = ReadTextRangeFromMethod(
                textEditPattern,
                "GetConversionTarget",
                options.MaxTextLength,
                errors,
                "textEdit.conversionTarget",
                out bool conversionTargetComplete);
            observation.IsConversionTargetComplete = conversionTargetComplete;
        }

        observation.EditContextFingerprint = DesktopTextObservationBuilder.CreateEditContextFingerprint(observation);

        return observation;
    }

    internal static IReadOnlyList<DesktopTextRangeObservation> ReadTextSelections(
        object textPattern,
        int maxLength,
        List<string> errors,
        out bool isComplete) {
        isComplete = true;
        try {
#if NET8_0_OR_GREATER
            object? result = ((UiaPattern)textPattern).GetSelection();
#else
            object? result = textPattern.GetType().GetMethod("GetSelection", Type.EmptyTypes)?.Invoke(textPattern, null);
#endif
            if (result is not IEnumerable ranges) {
                isComplete = false;
                return Array.Empty<DesktopTextRangeObservation>();
            }

            object? documentRange = GetAutomationProperty(textPattern, "DocumentRange");
            var values = new List<DesktopTextRangeObservation>();
            int remaining = maxLength;
            int inspectedRangeCount = 0;
            foreach (object? range in ranges) {
                if (inspectedRangeCount >= MaximumSelectionRangeCount) {
                    isComplete = false;
                    break;
                }

                inspectedRangeCount++;
                if (range == null) {
                    continue;
                }
                if (remaining <= 0) {
                    isComplete = false;
                    break;
                }

                string value = ReadTextRange(range, remaining, out bool truncated);
                values.Add(new DesktopTextRangeObservation {
                    Offset = documentRange == null ? null : TryGetTextRangeOffset(documentRange, range, maxLength),
                    Length = value.Length,
                    Text = value,
                    IsTruncated = truncated
                });
                remaining = Math.Max(0, remaining - value.Length);
                if (truncated) {
                    isComplete = false;
                    break;
                }
            }

            return values;
        } catch (Exception ex) {
            AddObservationError(errors, "text.selection", ex);
            isComplete = false;
            return Array.Empty<DesktopTextRangeObservation>();
        }
    }

    private void ReadCaret(
        object textPattern2,
        int maxLength,
        DesktopControlTextObservation observation,
        List<string> errors) {
#if NET8_0_OR_GREATER
        try {
            UiaPattern pattern = (UiaPattern)textPattern2;
            UiaRange? caret = pattern.GetCaretRange(out bool active);
            observation.IsCaretActive = active;
            if (caret == null) { return; }
            observation.CaretOffset = TryGetTextRangeOffset(pattern.DocumentRange, caret, maxLength);
            UiaRange context = caret.Clone();
            context.ExpandToEnclosingUnit(UiaTextUnit.Line);
            observation.CaretContext = ReadTextRange(context, Math.Min(maxLength, 512), out _);
        } catch (Exception ex) { AddObservationError(errors, "text.caret", ex); }
#else
        try {
            MethodInfo? method = textPattern2.GetType().GetMethod("GetCaretRange");
            if (method == null) {
                return;
            }

            object?[] arguments = { false };
            object? caretRange = method.Invoke(textPattern2, arguments);
            observation.IsCaretActive = arguments[0] is bool active ? active : null;
            if (caretRange == null) {
                return;
            }

            object? documentRange = GetAutomationProperty(textPattern2, "DocumentRange");
            observation.CaretOffset = documentRange == null ? null : TryGetTextRangeOffset(documentRange, caretRange, maxLength);

            object? contextRange = caretRange.GetType().GetMethod("Clone", Type.EmptyTypes)?.Invoke(caretRange, null) ?? caretRange;
            var textUnitType = ResolveAutomationType("System.Windows.Automation.Text.TextUnit", throwOnError: false)
                ?? ResolveAutomationType("System.Windows.Automation.Text.TextUnit", throwOnError: false);
            if (textUnitType != null) {
                object line = Enum.Parse(textUnitType, "Line", ignoreCase: false);
                contextRange.GetType().GetMethod("ExpandToEnclosingUnit", new[] { textUnitType })?.Invoke(contextRange, new[] { line });
            }

            observation.CaretContext = ReadTextRange(contextRange, Math.Min(maxLength, 512), out _);
        } catch (Exception ex) {
            AddObservationError(errors, "text.caret", ex);
        }
    #endif
    }

    private static string ReadTextRangeFromMethod(
        object pattern,
        string methodName,
        int maxLength,
        List<string> errors,
        string scope,
        out bool isComplete) {
        isComplete = true;
        try {
#if NET8_0_OR_GREATER
            UiaPattern native = (UiaPattern)pattern;
            object? range = methodName switch { "GetActiveComposition" => native.GetActiveComposition(), "GetConversionTarget" => native.GetConversionTarget(), _ => throw new ArgumentException("Unknown UIA text range method.", nameof(methodName)) };
#else
            MethodInfo? method = pattern.GetType().GetMethod(methodName, Type.EmptyTypes);
            if (method == null) {
                isComplete = false;
                return string.Empty;
            }

            object? range = method.Invoke(pattern, null);
#endif
            if (range == null) {
                return string.Empty;
            }

            string value = ReadTextRange(range, maxLength, out bool isTruncated);
            isComplete = !isTruncated;
            return value;
        } catch (Exception ex) {
            AddObservationError(errors, scope, ex);
            isComplete = false;
            return string.Empty;
        }
    }

    internal static string ReadTextRange(object range, int maxLength, out bool isTruncated) {
        int providerLimit = maxLength == int.MaxValue ? int.MaxValue : maxLength + 1;
#if NET8_0_OR_GREATER
        string value = ((UiaRange)range).GetText(providerLimit);
#else
        string value = range.GetType().GetMethod("GetText", new[] { typeof(int) })?.Invoke(range, new object[] { providerLimit }) as string ?? string.Empty;
#endif
        isTruncated = value.Length > maxLength;
        return isTruncated ? value.Substring(0, maxLength) : value;
    }

    internal static int? TryGetTextRangeOffset(object documentRange, object targetRange, int maxLength) {
#if NET8_0_OR_GREATER
        try {
            UiaRange prefix = ((UiaRange)documentRange).Clone();
            prefix.MoveEndpointByRange(UiaEndpoint.End, (UiaRange)targetRange, UiaEndpoint.Start);
            string value = ReadTextRange(prefix, maxLength, out bool truncated);
            return truncated ? null : value.Length;
        } catch { return null; }
#else
        try {
            object? prefix = documentRange.GetType().GetMethod("Clone", Type.EmptyTypes)?.Invoke(documentRange, null);
            if (prefix == null) {
                return null;
            }

            MethodInfo? moveEndpoint = prefix.GetType().GetMethod("MoveEndpointByRange");
            ParameterInfo[] parameters = moveEndpoint?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (moveEndpoint == null || parameters.Length != 3) {
                return null;
            }

            Type endpointType = parameters[0].ParameterType;
            object end = Enum.Parse(endpointType, "End", ignoreCase: false);
            object start = Enum.Parse(endpointType, "Start", ignoreCase: false);
            moveEndpoint.Invoke(prefix, new[] { end, targetRange, start });
            string value = ReadTextRange(prefix, maxLength, out bool isTruncated);
            return isTruncated ? null : value.Length;
        } catch {
            return null;
        }
    #endif
    }

    private static bool TryFindTextWithProvider(
        Dictionary<string, object> patterns,
        string expectedText,
        bool ignoreCase,
        List<string> errors) {
        if (!patterns.TryGetValue("Text", out object? textPattern)) {
            return false;
        }

        try {
            object? documentRange = GetAutomationProperty(textPattern, "DocumentRange");
#if NET8_0_OR_GREATER
            return ((UiaRange?)documentRange)?.FindText(expectedText, false, ignoreCase) != null;
#else
            MethodInfo? findText = documentRange?.GetType().GetMethod("FindText", new[] { typeof(string), typeof(bool), typeof(bool) });
            return findText?.Invoke(documentRange, new object[] { expectedText, false, ignoreCase }) != null;
#endif
        } catch (Exception ex) {
            AddObservationError(errors, "text.find", ex);
            return false;
        }
    }

    private static bool? ReadObservationBoolean(object current, string propertyName, List<string> errors) {
        try {
            object? value = GetAutomationProperty(current, propertyName);
            return value is bool result ? result : null;
        } catch (Exception ex) {
            AddObservationError(errors, $"control.{propertyName}", ex);
            return null;
        }
    }

    internal static void ValidateObservationOptions(DesktopControlObservationOptions options) {
        if (options.MaxTextLength <= 0 || options.MaxTextLength > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(nameof(options.MaxTextLength), $"MaxTextLength must be between 1 and {DesktopTextObservationOptions.MaximumTextLength}.");
        }

        if (options.MaxMatches < 0 || options.MaxMatches > 1000) {
            throw new ArgumentOutOfRangeException(nameof(options.MaxMatches), "MaxMatches must be between 0 and 1000.");
        }

        if (options.MatchContextLength < 0 || options.MatchContextLength > 4096) {
            throw new ArgumentOutOfRangeException(nameof(options.MatchContextLength), "MatchContextLength must be between 0 and 4096.");
        }

        if (options.MaxAncestorDepth < 0 || options.MaxAncestorDepth > MaximumAutomationParentDepth) {
            throw new ArgumentOutOfRangeException(nameof(options.MaxAncestorDepth), $"MaxAncestorDepth must be between 0 and {MaximumAutomationParentDepth}.");
        }
    }

    private static void AddObservationError(List<string> errors, string scope, Exception exception) {
        if (errors.Count >= 20) {
            return;
        }

        Exception resolved = exception.InnerException ?? exception;
        string message = resolved.Message ?? resolved.GetType().Name;
        if (message.Length > 160) {
            message = message.Substring(0, 160);
        }

        errors.Add($"{scope}: {message}");
    }

    private static string FormatObservationErrors(IReadOnlyList<string> errors) {
        string value = string.Join("; ", errors);
        return value.Length > 1024 ? value.Substring(0, 1024) : value;
    }
}
