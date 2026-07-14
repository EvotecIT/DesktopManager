using System.Diagnostics;

namespace DesktopManager;

/// <summary>
/// Routes best-effort core diagnostics without writing to a host's standard output stream.
/// </summary>
internal static class DesktopManagerDiagnostics {
    internal static void Report(string message) {
        Trace.TraceWarning("DesktopManager: " + message);
    }
}
