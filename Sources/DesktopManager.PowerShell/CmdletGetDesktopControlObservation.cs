using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets provider-neutral semantic observations for desktop controls.</summary>
/// <para type="synopsis">Gets identity, capabilities, text ranges, and semantic state from matching Win32 and UI Automation controls.</para>
/// <example>
///   <summary>Observe the focused editor in the active window</summary>
///   <code>Get-DesktopControlObservation -ActiveWindow -ControlType Document -All</code>
/// </example>
/// <example>
///   <summary>Read text selection and search a bounded document</summary>
///   <code>Get-DesktopControlObservation -Name '*Outlook*' -ControlType Document -ExpectedText 'project' -MaxTextLength 65536</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopControlObservation")]
public sealed class CmdletGetDesktopControlObservation : PSCmdlet {
    /// <summary><para type="description">Window title pattern.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
    public string Name { get; set; } = "*";

    /// <summary><para type="description">Window handle in decimal or hexadecimal form.</para></summary>
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
    /// <summary><para type="description">Native control identifier.</para></summary>
    [Parameter] public int? Id { get; set; }
    /// <summary><para type="description">UI Automation identifier pattern.</para></summary>
    [Parameter] public string AutomationId { get; set; } = "*";
    /// <summary><para type="description">UI Automation control type pattern.</para></summary>
    [Parameter] public string ControlType { get; set; } = "*";
    /// <summary><para type="description">UI framework identifier pattern.</para></summary>
    [Parameter] public string FrameworkId { get; set; } = "*";
    /// <summary><para type="description">Optional literal text to find in complete provider text.</para></summary>
    [Parameter] public string ExpectedText { get; set; } = string.Empty;
    /// <summary><para type="description">Ignore case while finding expected text.</para></summary>
    [Parameter] public SwitchParameter IgnoreCase { get; set; }
    /// <summary><para type="description">Maximum observed text length.</para></summary>
    [Parameter]
    [ValidateRange(1, DesktopTextObservationOptions.MaximumTextLength)]
    public int MaxTextLength { get; set; } = 4096;
    /// <summary><para type="description">Include selected ranges and caret context.</para></summary>
    [Parameter] public SwitchParameter IncludeTextRanges { get; set; }
    /// <summary><para type="description">Realize a virtualized item before observation.</para></summary>
    [Parameter] public SwitchParameter RealizeVirtualizedItem { get; set; }
    /// <summary><para type="description">Prepare the window before UI Automation discovery.</para></summary>
    [Parameter] public SwitchParameter EnsureForeground { get; set; }
    /// <summary><para type="description">Return every matching control.</para></summary>
    [Parameter] public SwitchParameter All { get; set; }
    /// <summary><para type="description">Inspect every matching window.</para></summary>
    [Parameter] public SwitchParameter AllWindows { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        WriteObject(automation.ObserveControls(
            DesktopControlObservationCmdletOptions.CreateWindowQuery(ParameterSetName, Name, Handle, ActiveWindow),
            DesktopControlObservationCmdletOptions.CreateControlQuery(ClassName, TextPattern, ValuePattern, Id, AutomationId, ControlType, FrameworkId, EnsureForeground),
            DesktopControlObservationCmdletOptions.CreateObservationOptions(MaxTextLength, ExpectedText, IgnoreCase, IncludeTextRanges, RealizeVirtualizedItem),
            AllWindows,
            All), true);
    }
}
