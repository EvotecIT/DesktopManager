namespace DesktopManager;

/// <summary>Messages for APIs that trimmed and Native AOT consumers cannot use yet.</summary>
internal static class AotCompatibility {
    internal const string BuiltInComInterop =
        "Uses built-in COM interop, which trimmed and Native AOT applications do not support.";

    internal const string UiAutomationUnavailableWhenTrimmed =
        "UIAutomationClient cannot load in trimmed or Native AOT applications, so IsAvailable is false and reflective UI Automation paths do not run.";
}
