using System;
using System.Collections;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private T RunInSta<T>(
        Func<UiAutomationControlService, T> operation,
        IntPtr targetWindowHandle = default,
        bool isMutation = false,
        int invocationTimeoutMilliseconds = UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds,
        Action<T>? abandonedResultHandler = null) {
        if (!IsAvailable) {
            return default!;
        }

        if (invocationTimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(invocationTimeoutMilliseconds));
        }

        LastOperationTimedOut = false;
        if (ShouldRunProviderOperationInline(
            StaDispatcher.Value.IsCurrentThread,
            IsWindowOwnedByCurrentThread(targetWindowHandle),
            isMutation)) {
            return operation(this);
        }

        try {
            if (TryRunWithCurrentUiMessagePump(
                    () => StaDispatcher.Value.Invoke(operation, invocationTimeoutMilliseconds, abandonedResultHandler),
                    out T pumpedResult)) {
                return pumpedResult;
            }

            return StaDispatcher.Value.Invoke(operation, invocationTimeoutMilliseconds, abandonedResultHandler);
        } catch (UiAutomationOperationInFlightException) when (isMutation) {
            LastOperationTimedOut = true;
            throw;
        } catch (TimeoutException) {
            LastOperationTimedOut = true;
            return CreateTimedOutOperationFallback<T>();
        }
    }

    internal static bool ShouldRunProviderOperationInline(
        bool isDispatcherThread,
        bool isWindowOwnedByCallingThread,
        bool isMutation) {
        return isDispatcherThread || isMutation && isWindowOwnedByCallingThread;
    }

    private static bool IsWindowOwnedByCurrentThread(IntPtr windowHandle) {
        return windowHandle != IntPtr.Zero &&
            MonitorNativeMethods.GetWindowThreadProcessId(windowHandle, out _) == MonitorNativeMethods.GetCurrentThreadId();
    }

    internal static T CreateTimedOutOperationFallback<T>() {
        Type resultType = typeof(T);
        if (resultType == typeof(DesktopUiAutomationActionDiagnostic)) {
            return (T)(object)new DesktopUiAutomationActionDiagnostic {
                Attempted = true,
                TimedOut = true,
                SearchMode = "timeout"
            };
        }

        if (resultType == typeof(UiAutomationTextEditAttempt)) {
            return (T)(object)UiAutomationTextEditAttempt.Failed("provider-timeout");
        }

        if (resultType.IsArray) {
            return (T)(object)Array.CreateInstance(resultType.GetElementType()!, 0);
        }

        if (typeof(IList).IsAssignableFrom(resultType) &&
            !resultType.IsAbstract &&
            !resultType.IsInterface &&
            resultType.GetConstructor(Type.EmptyTypes) != null) {
            return (T)Activator.CreateInstance(resultType)!;
        }

        return default!;
    }
}
