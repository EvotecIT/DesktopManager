using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Waits for semantic desktop-control state.</summary>
/// <para type="synopsis">Waits on UI Automation events with bounded polling fallback until a matching control reaches the requested state.</para>
/// <example>
///   <code>Wait-DesktopControlObservation -ActiveWindow -ControlType Document -ExpectedText 'Ready' -TimeoutMs 10000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Wait, "DesktopControlObservation")]
public sealed class CmdletWaitDesktopControlObservation : PSCmdlet {
    /// <summary><para type="description">Window title pattern.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
    public string Name { get; set; } = "*";
    /// <summary><para type="description">Window handle.</para></summary>
    [Parameter(Mandatory = true, ParameterSetName = "ByHandle")]
    public string Handle { get; set; } = string.Empty;
    /// <summary><para type="description">Use the current foreground window.</para></summary>
    [Parameter(Mandatory = true, ParameterSetName = "ActiveWindow")]
    public SwitchParameter ActiveWindow { get; set; }
    /// <summary><para type="description">Control class pattern.</para></summary>
    [Parameter] public string ClassName { get; set; } = "*";
    /// <summary><para type="description">Control text pattern.</para></summary>
    [Parameter] public string TextPattern { get; set; } = "*";
    /// <summary><para type="description">Control value pattern.</para></summary>
    [Parameter] public string ValuePattern { get; set; } = "*";
    /// <summary><para type="description">UI Automation identifier pattern.</para></summary>
    [Parameter] public string AutomationId { get; set; } = "*";
    /// <summary><para type="description">UI Automation control type pattern.</para></summary>
    [Parameter] public string ControlType { get; set; } = "*";
    /// <summary><para type="description">UI framework identifier pattern.</para></summary>
    [Parameter] public string FrameworkId { get; set; } = "*";
    /// <summary><para type="description">Literal text required in the observation.</para></summary>
    [Parameter] public string ExpectedText { get; set; } = string.Empty;
    /// <summary><para type="description">Ignore case while matching expected text.</para></summary>
    [Parameter] public SwitchParameter IgnoreCase { get; set; }
    /// <summary><para type="description">Required complete-text state.</para></summary>
    [Parameter] public bool? IsTextComplete { get; set; }
    /// <summary><para type="description">Required text-truncation state.</para></summary>
    [Parameter] public bool? IsTextTruncated { get; set; }
    /// <summary><para type="description">Required enabled state.</para></summary>
    [Parameter] public bool? IsEnabled { get; set; }
    /// <summary><para type="description">Required focused state.</para></summary>
    [Parameter] public bool? IsFocused { get; set; }
    /// <summary><para type="description">Required checked state.</para></summary>
    [Parameter] public bool? IsChecked { get; set; }
    /// <summary><para type="description">Required selected state.</para></summary>
    [Parameter] public bool? IsSelected { get; set; }
    /// <summary><para type="description">Required expand or collapse state.</para></summary>
    [Parameter] public string ExpandCollapseState { get; set; } = string.Empty;
    /// <summary><para type="description">Minimum acceptable numeric range value.</para></summary>
    [Parameter] public double? MinimumRangeValue { get; set; }
    /// <summary><para type="description">Maximum acceptable numeric range value.</para></summary>
    [Parameter] public double? MaximumRangeValue { get; set; }
    /// <summary><para type="description">Maximum observed text length.</para></summary>
    [Parameter]
    [ValidateRange(1, DesktopTextObservationOptions.MaximumTextLength)]
    public int MaxTextLength { get; set; } = 4096;
    /// <summary><para type="description">Timeout in milliseconds. Zero waits indefinitely.</para></summary>
    [Parameter] public int TimeoutMs { get; set; } = 10000;
    /// <summary><para type="description">Maximum polling fallback interval.</para></summary>
    [Parameter] public int IntervalMs { get; set; } = 200;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var condition = new DesktopControlObservationCondition {
            ExpectedText = string.IsNullOrEmpty(ExpectedText) ? null : ExpectedText,
            IgnoreCase = IgnoreCase,
            IsTextComplete = IsTextComplete,
            IsTextTruncated = IsTextTruncated,
            IsEnabled = IsEnabled,
            IsFocused = IsFocused,
            IsChecked = IsChecked,
            IsSelected = IsSelected,
            ExpandCollapseState = string.IsNullOrWhiteSpace(ExpandCollapseState) ? null : ExpandCollapseState,
            MinimumRangeValue = MinimumRangeValue,
            MaximumRangeValue = MaximumRangeValue
        };
        DesktopControlObservation result = new DesktopAutomationService().WaitForControlObservation(
            DesktopControlObservationCmdletOptions.CreateWindowQuery(ParameterSetName, Name, Handle, ActiveWindow),
            DesktopControlObservationCmdletOptions.CreateControlQuery(ClassName, TextPattern, ValuePattern, null, AutomationId, ControlType, FrameworkId, ensureForeground: false),
            condition,
            TimeoutMs,
            IntervalMs,
            DesktopControlObservationCmdletOptions.CreateObservationOptions(MaxTextLength, ExpectedText, IgnoreCase, includeTextRanges: true, realizeVirtualizedItem: false));
        WriteObject(result);
    }
}
