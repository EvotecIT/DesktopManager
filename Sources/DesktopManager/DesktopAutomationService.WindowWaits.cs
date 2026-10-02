using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    /// <summary>Asynchronously waits for matching windows with cooperative cancellation and an optional deadline.</summary>
    public async Task<DesktopWindowWaitResult> WaitForWindowsAsync(WindowQueryOptions options, int timeoutMilliseconds,
        int intervalMilliseconds = 100, bool all = false, CancellationToken cancellationToken = default) {
        Stopwatch elapsed = Stopwatch.StartNew();
        var windows = await _windowManager.WaitWindowsAsync(options, timeoutMilliseconds,
            intervalMilliseconds, all, cancellationToken).ConfigureAwait(false);
        return new DesktopWindowWaitResult { Windows = windows, ElapsedMilliseconds = (int)elapsed.ElapsedMilliseconds };
    }
}
