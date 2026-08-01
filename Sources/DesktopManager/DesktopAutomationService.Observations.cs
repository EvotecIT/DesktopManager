using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    /// <summary>
    /// Observes matching controls through one provider-neutral semantic contract.
    /// </summary>
    /// <param name="windowOptions">Window selector.</param>
    /// <param name="controlOptions">Optional control selector.</param>
    /// <param name="observationOptions">Observation limits and semantic options.</param>
    /// <param name="allWindows">Whether every matching window should be inspected.</param>
    /// <param name="allControls">Whether every matching control should be returned.</param>
    /// <returns>Current semantic control observations.</returns>
    public IReadOnlyList<DesktopControlObservation> ObserveControls(
        WindowQueryOptions windowOptions,
        WindowControlQueryOptions? controlOptions = null,
        DesktopControlObservationOptions? observationOptions = null,
        bool allWindows = false,
        bool allControls = true) {
        return ObserveControls(
            windowOptions,
            controlOptions,
            observationOptions,
            allWindows,
            allControls,
            getUiAutomationTimeoutMilliseconds: null);
    }

    internal IReadOnlyList<DesktopControlObservation> ObserveControls(
        WindowQueryOptions windowOptions,
        WindowControlQueryOptions? controlOptions,
        DesktopControlObservationOptions? observationOptions,
        bool allWindows,
        bool allControls,
        Func<int>? getUiAutomationTimeoutMilliseconds) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        DesktopControlObservationOptions settings = observationOptions ?? new DesktopControlObservationOptions();
        UiAutomationControlService.ValidateObservationOptions(settings);
        IReadOnlyList<WindowControlTargetInfo> targets = GetObservationTargets(
            windowOptions,
            controlOptions,
            settings,
            allWindows,
            allControls,
            getUiAutomationTimeoutMilliseconds);
        var observations = new List<DesktopControlObservation>(targets.Count);
        foreach (WindowControlTargetInfo target in targets) {
            int providerTimeout = getUiAutomationTimeoutMilliseconds?.Invoke() ?? UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds;
            if (providerTimeout <= 0) {
                break;
            }

            DesktopControlObservation? observation = ObserveResolvedControl(
                target.Window,
                target.Control,
                settings,
                providerTimeout,
                getUiAutomationTimeoutMilliseconds);
            if (getUiAutomationTimeoutMilliseconds != null && getUiAutomationTimeoutMilliseconds() <= 0) {
                break;
            }

            if (observation != null) {
                observations.Add(observation);
            }
        }

        return observations;
    }

    /// <summary>
    /// Observes one handle-backed control through the provider-neutral semantic contract.
    /// </summary>
    public DesktopControlObservation? ObserveControl(
        IntPtr windowHandle,
        IntPtr controlHandle,
        DesktopControlObservationOptions? observationOptions = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        if (controlHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle.", nameof(controlHandle));
        }

        DesktopControlObservationOptions settings = observationOptions ?? new DesktopControlObservationOptions();
        UiAutomationControlService.ValidateObservationOptions(settings);
        WindowInfo window = ResolveWindowByHandle(windowHandle);
        WindowControlInfo? control = GetControl(
            windowHandle,
            controlHandle,
            useUiAutomation: settings.UseUiAutomation,
            includeUiAutomation: settings.UseUiAutomation,
            maxTextLength: settings.MaxTextLength);
        return control == null ? null : ObserveResolvedControl(window, control, settings);
    }

    /// <summary>
    /// Observes the focused child of the first matching window through the generic semantic contract.
    /// </summary>
    public DesktopControlObservation? ObserveFocusedControl(
        WindowQueryOptions windowOptions,
        DesktopControlObservationOptions? observationOptions = null) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        DesktopControlObservationOptions settings = observationOptions ?? new DesktopControlObservationOptions();
        UiAutomationControlService.ValidateObservationOptions(settings);
        WindowInfo? window = GetMatchingWindows(windowOptions, all: false).FirstOrDefault();
        if (window == null) {
            return null;
        }

        IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
        WindowControlInfo? focusedControl = null;
        if (settings.UseUiAutomation) {
            UiAutomationFocusedControlResult? result = new UiAutomationControlService().TryGetFocusedControl(
                window.Handle,
                focusedHandle,
                settings.MaxTextLength,
                settings.ExpectedText);
            focusedControl = result?.Control;
        }

        if (focusedControl == null && focusedHandle != IntPtr.Zero && settings.IncludeNativeFallback) {
            focusedControl = GetControl(
                window.Handle,
                focusedHandle,
                useUiAutomation: false,
                includeUiAutomation: false,
                maxTextLength: settings.MaxTextLength);
        }

        return focusedControl == null ? null : ObserveResolvedControl(window, focusedControl, settings);
    }

    /// <summary>
    /// Observes the focused child of a specific window through the generic semantic contract.
    /// </summary>
    public DesktopControlObservation? ObserveFocusedControl(
        IntPtr windowHandle,
        DesktopControlObservationOptions? observationOptions = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return ObserveFocusedControl(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, observationOptions);
    }

    internal DesktopControlObservation? ObserveResolvedControl(
        WindowInfo window,
        WindowControlInfo control,
        DesktopControlObservationOptions settings,
        int uiAutomationTimeoutMilliseconds = UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds,
        Func<int>? getRemainingProviderTimeoutMilliseconds = null) {
        DesktopControlObservation? observation = null;
        if (settings.UseUiAutomation) {
            var uiAutomation = new UiAutomationControlService();
            observation = uiAutomation.TryObserveControl(window, control, settings, uiAutomationTimeoutMilliseconds);
            if (observation == null && uiAutomation.LastOperationTimedOut) {
                observation = settings.IncludeNativeFallback && control.IsPassword == false
                    ? CreateNativeControlObservation(
                        window,
                        control,
                        settings,
                        uiAutomationTimeoutMilliseconds,
                        getRemainingProviderTimeoutMilliseconds)
                    : CreateUnavailableControlObservation(window, control, "uia.timeout");
                observation.Status = "partial";
                observation.FailureReason = $"UI Automation did not complete within {uiAutomationTimeoutMilliseconds}ms.";
            }
        }

        if (observation == null && settings.IncludeNativeFallback) {
            return CreateNativeControlObservation(
                window,
                control,
                settings,
                uiAutomationTimeoutMilliseconds,
                getRemainingProviderTimeoutMilliseconds);
        }

        if (observation == null) {
            return null;
        }

        MergeNativeObservationState(
            observation,
            window,
            control,
            settings,
            uiAutomationTimeoutMilliseconds,
            getRemainingProviderTimeoutMilliseconds);
        return observation;
    }

    internal static DesktopControlObservation CreateNativeControlObservation(
        WindowInfo window,
        WindowControlInfo control,
        DesktopControlObservationOptions settings,
        int nativeTimeoutMilliseconds = UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds,
        Func<int>? getRemainingProviderTimeoutMilliseconds = null) {
        bool canAccessText = control.IsPassword == false;
        string value = string.Empty;
        bool isTruncated = false;
        bool nativeTextAvailable = true;
        string textSource = "native.windowText";
        if (canAccessText) {
            int textTimeoutMilliseconds = getRemainingProviderTimeoutMilliseconds?.Invoke() ?? nativeTimeoutMilliseconds;
            if (control.Handle != IntPtr.Zero && WindowControlService.SupportsSelection(control)) {
                nativeTextAvailable = WindowControlService.TryGetSelectedValue(
                    control,
                    settings.MaxTextLength,
                    textTimeoutMilliseconds,
                    out value,
                    out isTruncated);
                textSource = "native.selection";
            } else if (control.Handle != IntPtr.Zero) {
                nativeTextAvailable = WindowControlService.TryGetControlText(
                    control,
                    settings.MaxTextLength,
                    textTimeoutMilliseconds,
                    out value,
                    out isTruncated);
            }

            if (nativeTextAvailable && string.IsNullOrEmpty(value) && !isTruncated) {
                string candidate = !string.IsNullOrEmpty(control.Value) ? control.Value : control.Text;
                isTruncated = control.ValueIsTruncated || candidate.Length > settings.MaxTextLength;
                value = candidate.Length > settings.MaxTextLength
                    ? candidate.Substring(0, settings.MaxTextLength)
                    : candidate;
            }
        }

        var identity = new DesktopControlIdentity {
            ProcessId = window.ProcessId,
            WindowHandle = window.Handle,
            ControlHandle = control.Handle,
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            ClassName = control.ClassName,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height
        };
        identity.SessionKey = UiAutomationControlService.CreateObservationSessionKey(identity);
        bool supportsCheckState = control.Handle != IntPtr.Zero && WindowControlService.SupportsCheckState(control);
        bool supportsSelection = control.Handle != IntPtr.Zero && WindowControlService.SupportsSelection(control);
        bool nativeCheckState = false;
        int checkStateTimeoutMilliseconds = getRemainingProviderTimeoutMilliseconds?.Invoke() ?? nativeTimeoutMilliseconds;
        bool checkStateAvailable = supportsCheckState && WindowControlService.TryGetCheckState(
            control,
            checkStateTimeoutMilliseconds,
            out nativeCheckState);
        var observation = new DesktopControlObservation {
            Identity = identity,
            Capabilities = new DesktopControlCapabilities {
                CanReadText = canAccessText && nativeTextAvailable && (control.Handle != IntPtr.Zero || !string.IsNullOrEmpty(value)),
                CanSetValue = canAccessText && control.SupportsBackgroundText,
                CanInvoke = control.SupportsBackgroundClick,
                CanToggle = supportsCheckState,
                CanSelect = supportsSelection,
                SupportsBackgroundClick = control.SupportsBackgroundClick,
                SupportsBackgroundText = canAccessText && control.SupportsBackgroundText,
                SupportsBackgroundKeys = control.SupportsBackgroundKeys,
                SupportsForegroundInputFallback = control.SupportsForegroundInputFallback
            },
            Text = !canAccessText
                ? DesktopTextObservationBuilder.CreateRestricted(control.IsPassword == true ? "native.password" : "native.passwordStateUnavailable")
                : !nativeTextAvailable
                    ? DesktopTextObservationBuilder.CreateUnavailable(
                        $"{textSource}.unavailable",
                        settings.ExpectedText,
                        settings.IgnoreCase,
                        containsExpected: null)
                : DesktopTextObservationBuilder.Create(
                    value,
                    textSource,
                    isTruncated,
                    settings.ExpectedText,
                    settings.IgnoreCase,
                    settings.MaxMatches,
                    settings.MatchContextLength),
            Source = control.Source == WindowControlSource.UiAutomation ? "uia.metadata" : "win32",
            ObservedAtUtc = DateTime.UtcNow,
            Status = !canAccessText ? "restricted" : nativeTextAvailable ? "available" : "partial",
            IsPassword = control.IsPassword,
            IsEnabled = control.Handle != IntPtr.Zero ? MonitorNativeMethods.IsWindowEnabled(control.Handle) : control.IsEnabled,
            IsVisible = control.Handle != IntPtr.Zero ? MonitorNativeMethods.IsWindowVisible(control.Handle) : control.IsOffscreen.HasValue ? !control.IsOffscreen.Value : null,
            IsOffscreen = control.IsOffscreen,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsChecked = checkStateAvailable ? nativeCheckState : null
        };
        if (canAccessText && !nativeTextAvailable) {
            AddObservationFailure(
                observation,
                "The native text was unavailable within the observation deadline.");
        }
        if (supportsCheckState && !checkStateAvailable) {
            AddObservationFailure(
                observation,
                "The native check state was unavailable within the observation deadline.");
        }
        IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
        observation.IsFocused = focusedHandle != IntPtr.Zero && control.Handle != IntPtr.Zero ? focusedHandle == control.Handle : null;
        return observation;
    }

    private static DesktopControlObservation CreateUnavailableControlObservation(
        WindowInfo window,
        WindowControlInfo control,
        string source) {
        var identity = new DesktopControlIdentity {
            ProcessId = window.ProcessId,
            WindowHandle = window.Handle,
            ControlHandle = control.Handle,
            RuntimeId = control.RuntimeId,
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            ClassName = control.ClassName,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height
        };
        identity.SessionKey = UiAutomationControlService.CreateObservationSessionKey(identity);
        return new DesktopControlObservation {
            Identity = identity,
            Capabilities = new DesktopControlCapabilities {
                SupportsBackgroundClick = control.SupportsBackgroundClick,
                SupportsBackgroundText = false,
                SupportsBackgroundKeys = control.SupportsBackgroundKeys,
                SupportsForegroundInputFallback = control.SupportsForegroundInputFallback
            },
            Text = DesktopTextObservationBuilder.CreateRestricted(source),
            Source = source,
            ObservedAtUtc = DateTime.UtcNow,
            Status = "partial",
            IsPassword = control.IsPassword,
            IsEnabled = control.IsEnabled,
            IsOffscreen = control.IsOffscreen,
            IsKeyboardFocusable = control.IsKeyboardFocusable
        };
    }

    private static void MergeNativeObservationState(
        DesktopControlObservation observation,
        WindowInfo window,
        WindowControlInfo control,
        DesktopControlObservationOptions settings,
        int nativeTimeoutMilliseconds,
        Func<int>? getRemainingProviderTimeoutMilliseconds) {
        if (observation.IsPassword != false) {
            observation.Text = DesktopTextObservationBuilder.CreateRestricted(
                observation.IsPassword == true ? "password" : "passwordStateUnavailable");
            observation.Capabilities.CanReadText = false;
            observation.Capabilities.CanReadTextSelection = false;
            observation.Capabilities.CanSetValue = false;
            observation.Capabilities.SupportsBackgroundText = false;
            return;
        }

        if (ShouldUseNativeTextFallback(observation, control, settings)) {
            int textTimeoutMilliseconds = getRemainingProviderTimeoutMilliseconds?.Invoke() ?? nativeTimeoutMilliseconds;
            if (WindowControlService.TryGetControlText(
                    control,
                    settings.MaxTextLength,
                    textTimeoutMilliseconds,
                    out string nativeValue,
                    out bool isTruncated)) {
                if (!string.IsNullOrEmpty(nativeValue)) {
                    observation.Text = DesktopTextObservationBuilder.Create(
                        nativeValue,
                        "native.windowText",
                        isTruncated,
                        settings.ExpectedText,
                        settings.IgnoreCase,
                        settings.MaxMatches,
                        settings.MatchContextLength,
                        observation.Text.ContainsExpected);
                }
            } else {
                observation.Text = DesktopTextObservationBuilder.CreateUnavailable(
                    "native.windowText.unavailable",
                    settings.ExpectedText,
                    settings.IgnoreCase,
                    observation.Text.ContainsExpected);
                observation.Capabilities.CanReadText = false;
                AddObservationFailure(
                    observation,
                    "The native text fallback was unavailable within the observation deadline.");
            }
        }

        observation.IsEnabled ??= control.Handle != IntPtr.Zero ? MonitorNativeMethods.IsWindowEnabled(control.Handle) : control.IsEnabled;
        observation.IsVisible ??= control.Handle != IntPtr.Zero ? MonitorNativeMethods.IsWindowVisible(control.Handle) : control.IsOffscreen.HasValue ? !control.IsOffscreen.Value : null;
        observation.IsOffscreen ??= control.IsOffscreen;
        observation.IsKeyboardFocusable ??= control.IsKeyboardFocusable;
        if (control.Handle != IntPtr.Zero && WindowControlService.SupportsSelection(control)) {
            observation.Capabilities.CanSelect = true;
        }
        if (control.Handle != IntPtr.Zero && WindowControlService.SupportsCheckState(control)) {
            observation.Capabilities.CanToggle = true;
            if (!observation.IsChecked.HasValue) {
                int checkStateTimeoutMilliseconds = getRemainingProviderTimeoutMilliseconds?.Invoke() ?? nativeTimeoutMilliseconds;
                if (WindowControlService.TryGetCheckState(control, checkStateTimeoutMilliseconds, out bool nativeCheckState)) {
                    observation.IsChecked = nativeCheckState;
                } else {
                    AddObservationFailure(
                        observation,
                        "The native check state was unavailable within the observation deadline.");
                }
            }
        }
        if (!observation.IsFocused.HasValue && control.Handle != IntPtr.Zero) {
            IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
            observation.IsFocused = focusedHandle != IntPtr.Zero ? focusedHandle == control.Handle : null;
        }
    }

    private static void AddObservationFailure(DesktopControlObservation observation, string reason) {
        observation.Status = "partial";
        observation.FailureReason = string.IsNullOrWhiteSpace(observation.FailureReason)
            ? reason
            : $"{observation.FailureReason} {reason}";
    }

    internal static bool ShouldUseNativeTextFallback(
        DesktopControlObservation observation,
        WindowControlInfo control,
        DesktopControlObservationOptions settings) {
        return settings.IncludeNativeFallback &&
            string.IsNullOrEmpty(observation.Text.Source) &&
            control.Handle != IntPtr.Zero;
    }

    internal IReadOnlyList<WindowControlTargetInfo> GetObservationTargets(
        WindowQueryOptions windowOptions,
        WindowControlQueryOptions? controlOptions,
        DesktopControlObservationOptions settings,
        bool allWindows,
        bool allControls,
        Func<int>? getUiAutomationTimeoutMilliseconds = null) {
        WindowControlQueryOptions discoveryOptions = controlOptions ?? new WindowControlQueryOptions {
            UseUiAutomation = settings.UseUiAutomation,
            IncludeUiAutomation = settings.UseUiAutomation
        };
        IReadOnlyList<WindowInfo> windows = GetMatchingWindows(windowOptions, allWindows);
        IReadOnlyList<WindowControlTargetInfo> targets = GetControls(
            windows,
            discoveryOptions,
            allControls: true,
            maxTextLength: settings.MaxTextLength,
            getUiAutomationTimeoutMilliseconds: getUiAutomationTimeoutMilliseconds);
        if (allControls || targets.Count == 0) {
            return targets;
        }

        return new[] { targets[0] };
    }
}
