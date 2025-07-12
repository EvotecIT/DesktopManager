using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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
        if (string.IsNullOrEmpty(name)) {
            throw new ArgumentNullException(nameof(name));
        }

        var sw = Stopwatch.StartNew();
        while (true) {
            var window = GetWindows(name).FirstOrDefault();
            if (window != null) {
                return window;
            }

            if (timeoutMs > 0 && sw.ElapsedMilliseconds >= timeoutMs) {
                throw new TimeoutException($"Window '{name}' not found within {timeoutMs} ms");
            }

            Thread.Sleep(100);
        }
    }
}
