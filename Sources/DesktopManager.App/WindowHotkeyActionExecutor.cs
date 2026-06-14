using DesktopManager.App.Core;

namespace DesktopManager.App;

internal sealed class WindowHotkeyActionExecutor {
    private readonly global::DesktopManager.WindowPlacementService _placementService = new();

    public HotkeyExecutionResult Execute(HotkeyFunctionDefinition function, IntPtr targetWindowHandle) {
        if (!string.Equals(function.ActionType, HotkeyActionKinds.ManageWindow, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Unsupported action type '{function.ActionType}'.");
        }

        HotkeyExecutionDiagnostic diagnostic = CreateDiagnostic(function, targetWindowHandle);
        try {
            global::DesktopManager.WindowPlacementRequest request = WindowHotkeyPlacementRequestFactory.Create(function.WindowAction, targetWindowHandle);
            global::DesktopManager.WindowPlacementResult placement = _placementService.Apply(request);

            diagnostic.ResolvedHandle = FormatHandle(placement.ResolvedHandle);
            diagnostic.Attempt = placement.Attempts;
            diagnostic.Verified = placement.Verified;
            foreach (global::DesktopManager.WindowPlacementSnapshot snapshot in placement.Snapshots) {
                diagnostic.AddSnapshot(snapshot);
            }

            string diagnosticPath = HotkeyDiagnosticsWriter.Write(diagnostic);
            HotkeyExecutionResult result = new(
                function.Name,
                placement.Window.Title,
                placement.Window.Handle,
                placement.Window.MonitorIndex,
                function.WindowAction.Placement,
                placement.Verified,
                placement.Attempts,
                diagnosticPath,
                BuildDiagnosticSummary(diagnostic));

            if (!placement.Verified) {
                diagnostic.Error = $"Final geometry was not confirmed for {FormatHandle(placement.Window.Handle)}.";
                HotkeyDiagnosticsWriter.Write(diagnostic);
                throw new InvalidOperationException($"Window action was executed but final geometry was not confirmed for {FormatHandle(placement.Window.Handle)}.");
            }

            return result;
        } catch (Exception ex) {
            diagnostic.Error = ex.Message;
            TryWriteDiagnostic(diagnostic);
            throw;
        }
    }

    private static void TryWriteDiagnostic(HotkeyExecutionDiagnostic diagnostic) {
        try {
            HotkeyDiagnosticsWriter.Write(diagnostic);
        } catch {
            // Diagnostics must never hide the original hotkey failure.
        }
    }

    private static HotkeyExecutionDiagnostic CreateDiagnostic(HotkeyFunctionDefinition function, IntPtr targetWindowHandle) {
        return new HotkeyExecutionDiagnostic {
            FunctionName = function.Name,
            Hotkey = function.Hotkey,
            Placement = function.WindowAction.Placement,
            Target = function.WindowAction.Target,
            Monitor = function.WindowAction.Monitor,
            MonitorIndex = function.WindowAction.MonitorIndex,
            RequestedHandle = FormatHandle(targetWindowHandle)
        };
    }

    private static string BuildDiagnosticSummary(HotkeyExecutionDiagnostic diagnostic) {
        HotkeyWindowSnapshot? first = diagnostic.Snapshots.FirstOrDefault();
        HotkeyWindowSnapshot? last = diagnostic.Snapshots.LastOrDefault();
        if (first == null || last == null) {
            return "No diagnostic snapshots captured";
        }

        return $"Diagnostics {first.State} {first.Left},{first.Top} {first.Width}x{first.Height} -> {last.State} {last.Left},{last.Top} {last.Width}x{last.Height}";
    }

    private static string FormatHandle(IntPtr handle) {
        return handle == IntPtr.Zero ? "0x0" : $"0x{handle.ToInt64():X}";
    }
}
