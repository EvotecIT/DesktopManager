using System.Collections.Generic;

namespace DesktopManager.Cli;

internal sealed class WindowSelectionCriteria {
    public string TitlePattern { get; set; } = "*";
    public string ProcessNamePattern { get; set; } = "*";
    public string ClassNamePattern { get; set; } = "*";
    public int? ProcessId { get; set; }
    public string? Handle { get; set; }
    public bool Active { get; set; }
    public bool IncludeHidden { get; set; }
    public bool IncludeCloaked { get; set; } = true;
    public bool IncludeOwned { get; set; } = true;
    public bool IncludeEmptyTitles { get; set; }
    public bool All { get; set; }
}

internal sealed class WindowTextCommandOptions {
    public string Text { get; set; } = string.Empty;
    public bool Paste { get; set; }
    public int DelayMilliseconds { get; set; }
    public bool ForegroundInput { get; set; }
    public bool PhysicalKeys { get; set; }
    public bool HostedSession { get; set; }
    public bool ScriptMode { get; set; }
    public int ScriptChunkLength { get; set; } = 120;
    public int ScriptLineDelayMilliseconds { get; set; }
}

internal sealed class ControlSelectionCriteria {
    public string ClassNamePattern { get; set; } = "*";
    public string TextPattern { get; set; } = "*";
    public string ValuePattern { get; set; } = "*";
    public int? Id { get; set; }
    public string? Handle { get; set; }
    public string AutomationIdPattern { get; set; } = "*";
    public string ControlTypePattern { get; set; } = "*";
    public string FrameworkIdPattern { get; set; } = "*";
    public bool? IsEnabled { get; set; }
    public bool? IsKeyboardFocusable { get; set; }
    public bool? SupportsBackgroundClick { get; set; }
    public bool? SupportsBackgroundText { get; set; }
    public bool? SupportsBackgroundKeys { get; set; }
    public bool? SupportsForegroundInputFallback { get; set; }
    public bool EnsureForegroundWindow { get; set; }
    public bool AllowForegroundInputFallback { get; set; }
    public bool UiAutomation { get; set; }
    public bool IncludeUiAutomation { get; set; }
    public bool All { get; set; }
}

internal sealed class WindowResult {
    public string Title { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public uint ThreadId { get; set; }
    public bool IsVisible { get; set; }
    public bool IsTopMost { get; set; }
    public string? State { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int MonitorIndex { get; set; }
    public string MonitorDeviceName { get; set; } = string.Empty;
}

internal sealed class WindowChangeResult {
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Count { get; set; }
    public int ElapsedMilliseconds { get; set; }
    public string SafetyMode { get; set; } = string.Empty;
    public string? TargetName { get; set; }
    public string? TargetKind { get; set; }
    public IReadOnlyList<ScreenshotResult> BeforeScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<ScreenshotResult> AfterScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<string> ArtifactWarnings { get; set; } = new List<string>();
    public IReadOnlyList<WindowResult> Windows { get; set; } = new List<WindowResult>();
    public WindowMutationVerificationResult? Verification { get; set; }
}

internal sealed class WindowGeometryResult {
    public WindowResult Window { get; set; } = new WindowResult();
    public int WindowLeft { get; set; }
    public int WindowTop { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public int ClientLeft { get; set; }
    public int ClientTop { get; set; }
    public int ClientWidth { get; set; }
    public int ClientHeight { get; set; }
    public int ClientOffsetLeft { get; set; }
    public int ClientOffsetTop { get; set; }
}

internal sealed class WindowProcessInfoResult {
    public WindowResult Window { get; set; } = new WindowResult();
    public bool IsOwnerProcess { get; set; }
    public uint ProcessId { get; set; }
    public uint ThreadId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? ProcessPath { get; set; }
    public bool? IsElevated { get; set; }
    public bool? IsWow64 { get; set; }
}

internal sealed class ControlResult {
    public string Handle { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string FrameworkId { get; set; } = string.Empty;
    public bool? IsKeyboardFocusable { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsOffscreen { get; set; }
    public bool SupportsBackgroundClick { get; set; }
    public bool SupportsBackgroundText { get; set; }
    public bool SupportsBackgroundKeys { get; set; }
    public bool SupportsForegroundInputFallback { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public WindowResult ParentWindow { get; set; } = new WindowResult();
}

internal sealed class ControlStateResult {
    public string WindowHandle { get; set; } = string.Empty;
    public string ControlHandle { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool? IsEnabled { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsFocused { get; set; }
    public bool? IsKeyboardFocusable { get; set; }
    public bool? IsOffscreen { get; set; }
    public bool SupportsBackgroundClick { get; set; }
    public bool SupportsBackgroundText { get; set; }
    public bool SupportsBackgroundKeys { get; set; }
    public bool SupportsForegroundInputFallback { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

internal sealed class ControlActionResult {
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Count { get; set; }
    public int ElapsedMilliseconds { get; set; }
    public string SafetyMode { get; set; } = string.Empty;
    public string? TargetName { get; set; }
    public string? TargetKind { get; set; }
    public IReadOnlyList<ScreenshotResult> BeforeScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<ScreenshotResult> AfterScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<string> ArtifactWarnings { get; set; } = new List<string>();
    public IReadOnlyList<ControlResult> Controls { get; set; } = new List<ControlResult>();
}

internal sealed class ControlAssertionResult {
    public bool Matched { get; set; }
    public string Assertion { get; set; } = string.Empty;
    public int Count { get; set; }
    public IReadOnlyList<ControlResult> Controls { get; set; } = new List<ControlResult>();
}

internal sealed class ControlValueAssertionResult {
    public bool Matched { get; set; }
    public string Assertion { get; set; } = string.Empty;
    public string Expected { get; set; } = string.Empty;
    public string MatchMode { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MatchedCount { get; set; }
    public string? TargetName { get; set; }
    public IReadOnlyList<ControlResult> Controls { get; set; } = new List<ControlResult>();
}

internal sealed class ControlDiagnosticResult {
    public WindowResult Window { get; set; } = new WindowResult();
    public bool RequiresUiAutomation { get; set; }
    public bool UseUiAutomation { get; set; }
    public bool IncludeUiAutomation { get; set; }
    public bool EnsureForegroundWindow { get; set; }
    public bool UiAutomationAvailable { get; set; }
    public int ElapsedMilliseconds { get; set; }
    public bool PreparationAttempted { get; set; }
    public bool PreparationSucceeded { get; set; }
    public int UiAutomationFallbackRootCount { get; set; }
    public bool UsedUiAutomationFallbackRoots { get; set; }
    public bool UsedCachedUiAutomationControls { get; set; }
    public bool UsedPreferredUiAutomationRoot { get; set; }
    public string PreferredUiAutomationRootHandle { get; set; } = string.Empty;
    public string EffectiveSource { get; set; } = string.Empty;
    public int Win32ControlCount { get; set; }
    public int UiAutomationControlCount { get; set; }
    public int EffectiveControlCount { get; set; }
    public int MatchedControlCount { get; set; }
    public IReadOnlyList<ControlResult> SampleControls { get; set; } = new List<ControlResult>();
    public IReadOnlyList<UiAutomationRootDiagnosticResult> UiAutomationRoots { get; set; } = new List<UiAutomationRootDiagnosticResult>();
    public UiAutomationActionDiagnosticResult? UiAutomationActionProbe { get; set; }
}

internal sealed class UiAutomationRootDiagnosticResult {
    public int Order { get; set; }
    public string Handle { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public bool IsPrimaryRoot { get; set; }
    public bool IsPreferredRoot { get; set; }
    public bool UsedCachedControls { get; set; }
    public bool IncludeRoot { get; set; }
    public bool ElementResolved { get; set; }
    public int ControlCount { get; set; }
    public string? Error { get; set; }
    public IReadOnlyList<ControlResult> SampleControls { get; set; } = new List<ControlResult>();
}

internal sealed class UiAutomationActionDiagnosticResult {
    public bool Attempted { get; set; }
    public bool Resolved { get; set; }
    public bool UsedCachedActionMatch { get; set; }
    public bool UsedPreferredRoot { get; set; }
    public string RootHandle { get; set; } = string.Empty;
    public int Score { get; set; }
    public string SearchMode { get; set; } = string.Empty;
    public int ElapsedMilliseconds { get; set; }
}

internal sealed class MonitorResult {
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceString { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsPrimary { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }
}

internal sealed class MonitorBrightnessResult {
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int Brightness { get; set; }
}

internal sealed class MonitorWallpaperResult {
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string Wallpaper { get; set; } = string.Empty;
}

internal sealed class DesktopColorResult {
    public uint Value { get; set; }
    public string HexValue { get; set; } = string.Empty;
}

internal sealed class DesktopWallpaperPositionResult {
    public string Position { get; set; } = string.Empty;
}

internal sealed class DesktopSlideshowResult {
    public string Action { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public string? Direction { get; set; }
    public int? ImageCount { get; set; }
}

internal sealed class MouseStateResult {
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsLeftButtonDown { get; set; }
    public bool IsRightButtonDown { get; set; }
    public bool IsCursorVisible { get; set; }
    public string CursorHandle { get; set; } = string.Empty;
}

internal sealed class FocusedControlObservationResult {
    public string WindowHandle { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string FocusedHandle { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool? IsKeyboardFocusable { get; set; }
    public bool? IsEnabled { get; set; }
}

internal sealed class WindowTextObservationResult {
    public string WindowHandle { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ControlHandle { get; set; } = string.Empty;
    public string ControlClassName { get; set; } = string.Empty;
    public string ControlAutomationId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool? ContainsExpected { get; set; }
    public bool IsTruncated { get; set; }
}

internal sealed class WaitCompletionResult {
    public int ElapsedMilliseconds { get; set; }
}

internal sealed class NamedStateResult {
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Scope { get; set; }
}

internal sealed class SavedWindowLayoutEntryResult {
    public string Title { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? State { get; set; }
}

internal sealed class WindowLayoutMismatchResult {
    public SavedWindowLayoutEntryResult Expected { get; set; } = new();
    public WindowResult Current { get; set; } = new();
    public int LeftDelta { get; set; }
    public int TopDelta { get; set; }
    public int WidthDelta { get; set; }
    public int HeightDelta { get; set; }
    public bool StateMatched { get; set; }
}

internal sealed class WindowLayoutAssertionResult {
    public bool Matched { get; set; }
    public string Assertion { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int MatchedCount { get; set; }
    public int MissingCount { get; set; }
    public int MismatchCount { get; set; }
    public int PositionTolerancePixels { get; set; }
    public int SizeTolerancePixels { get; set; }
    public bool CheckState { get; set; }
    public IReadOnlyList<SavedWindowLayoutEntryResult> MissingWindows { get; set; } = new List<SavedWindowLayoutEntryResult>();
    public IReadOnlyList<WindowLayoutMismatchResult> MismatchedWindows { get; set; } = new List<WindowLayoutMismatchResult>();
    public IReadOnlyList<ScreenshotResult> BeforeScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<ScreenshotResult> AfterScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<string> ArtifactWarnings { get; set; } = new List<string>();
}

internal sealed class WindowTargetDefinitionResult {
    public string? Description { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public double? XRatio { get; set; }
    public double? YRatio { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? WidthRatio { get; set; }
    public double? HeightRatio { get; set; }
    public bool ClientArea { get; set; }
}

internal sealed class ControlTargetDefinitionResult {
    public string? Description { get; set; }
    public string ClassNamePattern { get; set; } = "*";
    public string TextPattern { get; set; } = "*";
    public string ValuePattern { get; set; } = "*";
    public int? Id { get; set; }
    public string? Handle { get; set; }
    public string AutomationIdPattern { get; set; } = "*";
    public string ControlTypePattern { get; set; } = "*";
    public string FrameworkIdPattern { get; set; } = "*";
    public bool? IsEnabled { get; set; }
    public bool? IsKeyboardFocusable { get; set; }
    public bool? SupportsBackgroundClick { get; set; }
    public bool? SupportsBackgroundText { get; set; }
    public bool? SupportsBackgroundKeys { get; set; }
    public bool? SupportsForegroundInputFallback { get; set; }
    public bool UseUiAutomation { get; set; }
    public bool IncludeUiAutomation { get; set; }
    public bool EnsureForegroundWindow { get; set; }
}

internal sealed class WindowTargetResult {
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public WindowTargetDefinitionResult Target { get; set; } = new();
}

internal sealed class ControlTargetResult {
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ControlTargetDefinitionResult Target { get; set; } = new();
}

internal sealed class ResolvedWindowTargetResult {
    public string Name { get; set; } = string.Empty;
    public WindowTargetDefinitionResult Target { get; set; } = new();
    public WindowResult Window { get; set; } = new();
    public WindowGeometryResult Geometry { get; set; } = new();
    public int RelativeX { get; set; }
    public int RelativeY { get; set; }
    public int? RelativeWidth { get; set; }
    public int? RelativeHeight { get; set; }
    public int ScreenX { get; set; }
    public int ScreenY { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
}

internal sealed class ResolvedControlTargetResult {
    public string Name { get; set; } = string.Empty;
    public ControlTargetDefinitionResult Target { get; set; } = new();
    public WindowResult Window { get; set; } = new();
    public ControlResult Control { get; set; } = new();
}

internal sealed class ScreenshotResult {
    public string Kind { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int? MonitorIndex { get; set; }
    public string? MonitorDeviceName { get; set; }
    public WindowResult? Window { get; set; }
    public WindowGeometryResult? Geometry { get; set; }
}

internal sealed class ClipboardTextResult {
    public bool HasText { get; set; }
    public string? Text { get; set; }
}

internal sealed class ElevationStatusResult {
    public bool IsElevated { get; set; }
}

internal sealed class DesktopScreenshotCommandOptions {
    public int? MonitorIndex { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public int? Left { get; set; }
    public int? Top { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? OutputPath { get; set; }
}

internal sealed class ProcessStartCommandOptions {
    public string FilePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public int? WaitForInputIdleMilliseconds { get; set; }
    public int? WaitForWindowMilliseconds { get; set; }
    public int? WaitForWindowIntervalMilliseconds { get; set; }
    public string? WindowTitlePattern { get; set; }
    public string? WindowClassNamePattern { get; set; }
    public bool RequireWindow { get; set; }
}

internal sealed class LaunchAndWaitCommandOptions {
    public string FilePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public int? WaitForInputIdleMilliseconds { get; set; }
    public int? LaunchWaitForWindowMilliseconds { get; set; }
    public int? LaunchWaitForWindowIntervalMilliseconds { get; set; }
    public string? LaunchWindowTitlePattern { get; set; }
    public string? LaunchWindowClassNamePattern { get; set; }
    public string? WindowTitlePattern { get; set; }
    public string? WindowClassNamePattern { get; set; }
    public bool IncludeHidden { get; set; }
    public bool IncludeEmpty { get; set; }
    public bool All { get; set; }
    public bool FollowProcessFamily { get; set; }
    public int TimeoutMilliseconds { get; set; }
    public int IntervalMilliseconds { get; set; }
    public MutationArtifactOptions? ArtifactOptions { get; set; }
}

internal sealed class ProcessLaunchResult {
    public string FilePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public int ProcessId { get; set; }
    public int? ResolvedProcessId { get; set; }
    public bool HasExited { get; set; }
    public WindowResult? MainWindow { get; set; }
}

internal sealed class LaunchAndWaitResult {
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ElapsedMilliseconds { get; set; }
    public int WaitTimeoutMilliseconds { get; set; }
    public int WaitIntervalMilliseconds { get; set; }
    public string WaitBinding { get; set; } = string.Empty;
    public int? BoundProcessId { get; set; }
    public string? BoundProcessName { get; set; }
    public ProcessLaunchResult Launch { get; set; } = new();
    public WaitForWindowResult WindowWait { get; set; } = new();
    public IReadOnlyList<string> Notes { get; set; } = new List<string>();
    public IReadOnlyList<ScreenshotResult> BeforeScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<ScreenshotResult> AfterScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<string> ArtifactWarnings { get; set; } = new List<string>();
}

internal sealed class LaunchWaitBindingPlan {
    public WindowSelectionCriteria Criteria { get; set; } = new();
    public string WaitBinding { get; set; } = string.Empty;
    public int? BoundProcessId { get; set; }
    public string? BoundProcessName { get; set; }
}

internal sealed class WaitForWindowResult {
    public int ElapsedMilliseconds { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<WindowResult> Windows { get; set; } = new List<WindowResult>();
}

internal sealed class WaitForControlResult {
    public int ElapsedMilliseconds { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<ControlResult> Controls { get; set; } = new List<ControlResult>();
}

internal sealed class WindowAssertionResult {
    public bool Matched { get; set; }
    public string Assertion { get; set; } = string.Empty;
    public int Count { get; set; }
    public IReadOnlyList<WindowResult> Windows { get; set; } = new List<WindowResult>();
    public WindowResult? ActiveWindow { get; set; }
}

internal sealed class MutationArtifactOptions {
    public bool CaptureBefore { get; set; }
    public bool CaptureAfter { get; set; }
    public string? ArtifactDirectory { get; set; }
    public bool VerifyAfter { get; set; }
    public int VerificationTolerancePixels { get; set; } = 10;
}

internal sealed class WindowMutationVerificationResult {
    public bool Verified { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int ObservedCount { get; set; }
    public int MatchedCount { get; set; }
    public int MismatchCount { get; set; }
    public int TolerancePixels { get; set; }
    public WindowResult? ActiveWindow { get; set; }
    public IReadOnlyList<string> Notes { get; set; } = new List<string>();
    public IReadOnlyList<WindowResult> ObservedWindows { get; set; } = new List<WindowResult>();
}

internal sealed class WorkflowResult {
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ElapsedMilliseconds { get; set; }
    public string? LayoutName { get; set; }
    public bool LayoutApplied { get; set; }
    public int MinimizedCount { get; set; }
    public IReadOnlyList<WindowResult> MinimizedWindows { get; set; } = new List<WindowResult>();
    public WindowResult? ResolvedWindow { get; set; }
    public WindowResult? FocusedWindow { get; set; }
    public IReadOnlyList<string> Notes { get; set; } = new List<string>();
    public IReadOnlyList<ScreenshotResult> BeforeScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<ScreenshotResult> AfterScreenshots { get; set; } = new List<ScreenshotResult>();
    public IReadOnlyList<string> ArtifactWarnings { get; set; } = new List<string>();
}
