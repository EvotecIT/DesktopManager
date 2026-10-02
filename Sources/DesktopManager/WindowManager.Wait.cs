using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Waits until a window matching the provided title appears.
    /// </summary>
    /// <param name="name">Window title filter supporting wildcards.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Zero waits indefinitely.</param>
    /// <returns>The first matching <see cref="WindowInfo"/>.</returns>
    /// <exception cref="TimeoutException">Thrown when the window does not appear within the timeout.</exception>
    public WindowInfo WaitWindow(string name, int timeoutMs = 0) {
        return WaitWindow(name, timeoutMs, CancellationToken.None);
    }

    /// <summary>Waits for a title match with cooperative cancellation. Zero timeout waits indefinitely.</summary>
    public WindowInfo WaitWindow(string name, int timeoutMs, CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(name)) {
            throw new ArgumentNullException(nameof(name));
        }
        ValidateWindowWait(timeoutMs, 100);

        var sw = Stopwatch.StartNew();
        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            var window = GetWindows(name).FirstOrDefault();
            cancellationToken.ThrowIfCancellationRequested();
            if (window != null) {
                return window;
            }

            if (timeoutMs > 0 && sw.ElapsedMilliseconds >= timeoutMs) {
                throw new TimeoutException($"Window '{name}' not found within {timeoutMs} ms");
            }

            int delay = WindowWaitDelay(timeoutMs, 100, sw.ElapsedMilliseconds);
            if (cancellationToken.CanBeCanceled) {
                UiAutomationControlService.WaitForSignalWithCurrentUiMessagePump(cancellationToken.WaitHandle, delay);
            } else {
                UiAutomationControlService.WaitWithCurrentUiMessagePump(delay);
            }
        }
    }

    /// <summary>Asynchronously waits for a title match. Cancellation stops polling; zero timeout waits indefinitely.</summary>
    public async Task<WindowInfo> WaitWindowAsync(string name, int timeoutMs = 0, CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(nameof(name)); }
        IReadOnlyList<WindowInfo> windows = await WaitWindowsAsync(new WindowQueryOptions { TitlePattern = name },
            timeoutMs, 100, false, cancellationToken).ConfigureAwait(false);
        return windows[0];
    }

    /// <summary>Asynchronously polls matching windows without blocking a caller's thread.</summary>
    /// <param name="options">Window selectors.</param>
    /// <param name="timeoutMilliseconds">Polling deadline; zero waits indefinitely.</param>
    /// <param name="intervalMilliseconds">Maximum delay between observations.</param>
    /// <param name="all">Whether to return all matching windows.</param>
    /// <param name="cancellationToken">Cooperative cancellation, checked between bounded native observations.</param>
    public async Task<IReadOnlyList<WindowInfo>> WaitWindowsAsync(WindowQueryOptions options, int timeoutMilliseconds,
        int intervalMilliseconds = 100, bool all = false, CancellationToken cancellationToken = default) {
        if (options == null) { throw new ArgumentNullException(nameof(options)); }
        ValidateWindowWait(timeoutMilliseconds, intervalMilliseconds);
        Stopwatch elapsed = Stopwatch.StartNew();
        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            List<WindowInfo> windows = GetWindows(options);
            cancellationToken.ThrowIfCancellationRequested();
            if (windows.Count > 0) { return all ? windows : new[] { windows[0] }; }
            if (timeoutMilliseconds > 0 && elapsed.ElapsedMilliseconds >= timeoutMilliseconds) {
                throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching window.");
            }
            await Task.Delay(WindowWaitDelay(timeoutMilliseconds, intervalMilliseconds, elapsed.ElapsedMilliseconds),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateWindowWait(int timeoutMilliseconds, int intervalMilliseconds) {
        if (timeoutMilliseconds < 0) { throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds)); }
        if (intervalMilliseconds <= 0) { throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds)); }
    }

    private static int WindowWaitDelay(int timeoutMilliseconds, int intervalMilliseconds, long elapsedMilliseconds) {
        return timeoutMilliseconds == 0 ? intervalMilliseconds :
            (int)Math.Max(1, Math.Min(intervalMilliseconds, timeoutMilliseconds - elapsedMilliseconds));
    }
}
