using System.Diagnostics;

namespace DesktopManager;

/// <summary>
/// Routes best-effort core diagnostics without writing to a host's standard output stream or changing the operation result.
/// </summary>
internal static class DesktopManagerDiagnostics {
    internal static void Report(string message) => Report(message, warning => Trace.TraceWarning(warning));

    internal static void Report(string message, Action<string> writeWarning) {
        try {
            writeWarning("DesktopManager: " + message);
        } catch {
            // Diagnostic listeners must not replace an operation's result or interrupt cleanup.
        }
    }
}
