using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    /// <summary>
    /// Waits until a matching control satisfies semantic state, using UI Automation events with bounded polling fallback.
    /// </summary>
    public DesktopControlObservation WaitForControlObservation(
        WindowQueryOptions windowOptions,
        WindowControlQueryOptions? controlOptions,
        DesktopControlObservationCondition condition,
        int timeoutMilliseconds,
        int intervalMilliseconds = 200,
        DesktopControlObservationOptions? observationOptions = null) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        if (condition == null) {
            throw new ArgumentNullException(nameof(condition));
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);
        DesktopControlObservationOptions settings = CloneWaitObservationOptions(observationOptions, condition);
        using var signal = new AutoResetEvent(false);
        Stopwatch stopwatch = Stopwatch.StartNew();
        Func<int> getProviderTimeout = () => GetRemainingProviderTimeout(stopwatch, timeoutMilliseconds);
        IDisposable? subscription = TryCreateAutomationChangeSubscription(windowOptions, signal, getProviderTimeout);
        try {
            while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
                DesktopControlObservation? observation = ObserveControls(
                        windowOptions,
                        controlOptions,
                        settings,
                        allWindows: false,
                        allControls: true,
                        getUiAutomationTimeoutMilliseconds: getProviderTimeout)
                    .FirstOrDefault(condition.Matches);
                if (observation != null && CanReturnWaitObservation(observation, getProviderTimeout())) {
                    observation.WaitStrategy = subscription == null ? "polling" : "uia.events+polling";
                    return observation;
                }

                int providerTimeout = getProviderTimeout();
                if (providerTimeout <= 0) {
                    break;
                }

                subscription ??= TryCreateAutomationChangeSubscription(windowOptions, signal, getProviderTimeout);
                UiAutomationControlService.WaitForSignalWithCurrentUiMessagePump(
                    signal,
                    GetRemainingWaitInterval(stopwatch, timeoutMilliseconds, intervalMilliseconds));
            }
        } finally {
            DisposeAutomationChangeSubscription(subscription, getProviderTimeout);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching control observation.");
    }

    internal IDisposable? TryCreateAutomationChangeSubscription(
        WindowQueryOptions options,
        EventWaitHandle signal,
        int invocationTimeoutMilliseconds = UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds) {
        return TryCreateAutomationChangeSubscription(options, signal, () => invocationTimeoutMilliseconds);
    }

    internal IDisposable? TryCreateAutomationChangeSubscription(
        WindowQueryOptions options,
        EventWaitHandle signal,
        Func<int> getInvocationTimeoutMilliseconds) {
        if (getInvocationTimeoutMilliseconds == null) {
            throw new ArgumentNullException(nameof(getInvocationTimeoutMilliseconds));
        }

        int invocationTimeoutMilliseconds = getInvocationTimeoutMilliseconds();
        if (invocationTimeoutMilliseconds <= 0) {
            return null;
        }

        WindowInfo? window = GetMatchingWindows(options, all: false).FirstOrDefault();
        invocationTimeoutMilliseconds = getInvocationTimeoutMilliseconds();
        return window == null
            || invocationTimeoutMilliseconds <= 0
            ? null
            : new UiAutomationControlService().TrySubscribeToChanges(window.Handle, () => signal.Set(), invocationTimeoutMilliseconds);
    }

    internal static int GetRemainingWaitInterval(Stopwatch stopwatch, int timeoutMilliseconds, int intervalMilliseconds) {
        if (timeoutMilliseconds == 0) {
            return intervalMilliseconds;
        }

        long remaining = timeoutMilliseconds - stopwatch.ElapsedMilliseconds;
        return (int)Math.Max(1, Math.Min(intervalMilliseconds, remaining));
    }

    internal static int GetRemainingProviderTimeout(Stopwatch stopwatch, int timeoutMilliseconds) {
        return GetProviderInvocationTimeout(timeoutMilliseconds, stopwatch.ElapsedMilliseconds);
    }

    internal static bool CanReturnWaitObservation(DesktopControlObservation? observation, int remainingMilliseconds) {
        return observation != null && remainingMilliseconds > 0;
    }

    internal static bool CanReturnWaitObservation(DesktopFocusedControlObservation? observation, int remainingMilliseconds) {
        return observation != null && remainingMilliseconds > 0;
    }

    internal static void DisposeAutomationChangeSubscription(
        IDisposable? subscription,
        Func<int> getInvocationTimeoutMilliseconds) {
        if (subscription == null) {
            return;
        }

        if (getInvocationTimeoutMilliseconds == null) {
            throw new ArgumentNullException(nameof(getInvocationTimeoutMilliseconds));
        }

        if (subscription is IUiAutomationBoundedDisposable boundedSubscription) {
            boundedSubscription.Dispose(getInvocationTimeoutMilliseconds());
        } else {
            subscription.Dispose();
        }
    }

    internal static int GetProviderInvocationTimeout(int timeoutMilliseconds, long elapsedMilliseconds) {
        if (timeoutMilliseconds == 0) {
            return UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds;
        }

        long remaining = timeoutMilliseconds - elapsedMilliseconds;
        return remaining <= 0
            ? 0
            : (int)Math.Min(UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds, remaining);
    }

    private static DesktopControlObservationOptions CloneWaitObservationOptions(
        DesktopControlObservationOptions? options,
        DesktopControlObservationCondition condition) {
        DesktopControlObservationOptions source = options ?? new DesktopControlObservationOptions();
        var settings = new DesktopControlObservationOptions {
            UseUiAutomation = source.UseUiAutomation,
            IncludeNativeFallback = source.IncludeNativeFallback,
            MaxTextLength = source.MaxTextLength,
            ExpectedText = condition.ExpectedText ?? source.ExpectedText,
            IgnoreCase = condition.ExpectedText == null ? source.IgnoreCase : condition.IgnoreCase,
            MaxMatches = source.MaxMatches,
            MatchContextLength = source.MatchContextLength,
            IncludeTextRanges = source.IncludeTextRanges,
            IncludeSemanticState = true,
            RealizeVirtualizedItem = source.RealizeVirtualizedItem,
            MaxAncestorDepth = source.MaxAncestorDepth
        };
        UiAutomationControlService.ValidateObservationOptions(settings);
        return settings;
    }
}
