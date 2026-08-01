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
        IDisposable? subscription = TryCreateAutomationChangeSubscription(windowOptions, signal);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try {
            while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
                DesktopControlObservation? observation = ObserveControls(
                        windowOptions,
                        controlOptions,
                        settings,
                        allWindows: false,
                        allControls: true)
                    .FirstOrDefault(condition.Matches);
                if (observation != null) {
                    observation.WaitStrategy = subscription == null ? "polling" : "uia.events+polling";
                    return observation;
                }

                subscription ??= TryCreateAutomationChangeSubscription(windowOptions, signal);
                UiAutomationControlService.WaitForSignalWithCurrentUiMessagePump(
                    signal,
                    GetRemainingWaitInterval(stopwatch, timeoutMilliseconds, intervalMilliseconds));
            }
        } finally {
            subscription?.Dispose();
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching control observation.");
    }

    internal IDisposable? TryCreateAutomationChangeSubscription(WindowQueryOptions options, EventWaitHandle signal) {
        WindowInfo? window = GetMatchingWindows(options, all: false).FirstOrDefault();
        return window == null
            ? null
            : new UiAutomationControlService().TrySubscribeToChanges(window.Handle, () => signal.Set());
    }

    internal static int GetRemainingWaitInterval(Stopwatch stopwatch, int timeoutMilliseconds, int intervalMilliseconds) {
        if (timeoutMilliseconds == 0) {
            return intervalMilliseconds;
        }

        long remaining = timeoutMilliseconds - stopwatch.ElapsedMilliseconds;
        return (int)Math.Max(1, Math.Min(intervalMilliseconds, remaining));
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
