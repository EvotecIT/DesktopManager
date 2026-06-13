namespace DesktopManager.App;

internal sealed class HotkeyExecutionResult {
    public HotkeyExecutionResult(
        string functionName,
        string windowTitle,
        IntPtr windowHandle,
        int? monitorIndex,
        string placement,
        bool verified,
        int attempts,
        string diagnosticPath,
        string diagnosticSummary) {
        FunctionName = functionName;
        WindowTitle = windowTitle;
        WindowHandle = windowHandle;
        MonitorIndex = monitorIndex;
        Placement = placement;
        Verified = verified;
        Attempts = attempts;
        DiagnosticPath = diagnosticPath;
        DiagnosticSummary = diagnosticSummary;
    }

    public string FunctionName { get; }

    public string WindowTitle { get; }

    public IntPtr WindowHandle { get; }

    public int? MonitorIndex { get; }

    public string Placement { get; }

    public bool Verified { get; }

    public int Attempts { get; }

    public string DiagnosticPath { get; }

    public string DiagnosticSummary { get; }

    public string ToStatusMessage() {
        string monitor = MonitorIndex.HasValue ? $"monitor {MonitorIndex.Value}" : "current monitor";
        string verified = Verified ? "verified" : "not verified";
        return $"Executed {FunctionName} for 0x{WindowHandle.ToInt64():X} on {monitor} ({Placement}, {verified}, attempt {Attempts}): {WindowTitle}. {DiagnosticSummary}. Diagnostic: {DiagnosticPath}";
    }
}
