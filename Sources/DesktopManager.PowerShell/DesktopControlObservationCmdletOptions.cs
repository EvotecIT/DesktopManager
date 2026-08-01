using System.Management.Automation;

namespace DesktopManager.PowerShell;

internal static class DesktopControlObservationCmdletOptions {
    internal static WindowQueryOptions CreateWindowQuery(
        string parameterSetName,
        string name,
        string handle,
        bool activeWindow) {
        return parameterSetName switch {
            "ByHandle" => new WindowQueryOptions {
                Handle = DesktopHandleParser.Parse(handle),
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "ActiveWindow" => new WindowQueryOptions {
                ActiveWindow = activeWindow,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            _ => new WindowQueryOptions {
                TitlePattern = name,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            }
        };
    }

    internal static WindowControlQueryOptions CreateControlQuery(
        string className,
        string textPattern,
        string valuePattern,
        int? id,
        string automationId,
        string controlType,
        string frameworkId,
        bool ensureForeground) {
        return new WindowControlQueryOptions {
            ClassNamePattern = className,
            TextPattern = textPattern,
            ValuePattern = valuePattern,
            Id = id,
            AutomationIdPattern = automationId,
            ControlTypePattern = controlType,
            FrameworkIdPattern = frameworkId,
            UseUiAutomation = true,
            IncludeUiAutomation = true,
            EnsureForegroundWindow = ensureForeground
        };
    }

    internal static DesktopControlObservationOptions CreateObservationOptions(
        int maxTextLength,
        string expectedText,
        bool ignoreCase,
        bool includeTextRanges,
        bool realizeVirtualizedItem) {
        return new DesktopControlObservationOptions {
            MaxTextLength = maxTextLength,
            ExpectedText = string.IsNullOrEmpty(expectedText) ? null : expectedText,
            IgnoreCase = ignoreCase,
            IncludeTextRanges = includeTextRanges,
            IncludeSemanticState = true,
            RealizeVirtualizedItem = realizeVirtualizedItem
        };
    }
}
