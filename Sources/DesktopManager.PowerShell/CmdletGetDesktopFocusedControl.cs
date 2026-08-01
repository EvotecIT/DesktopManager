using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the focused control for a desktop window.</summary>
/// <para type="synopsis">Gets the focused control for a desktop window.</para>
/// <para type="description">Returns focused-control metadata and a bounded plain-text value for a specific window selected by title, handle, or the current foreground window. Document editors that expose UI Automation TextPattern are read directly; password controls are never read.</para>
/// <example>
///   <summary>Read the focused control from the active window</summary>
///   <code>Get-DesktopFocusedControl -ActiveWindow</code>
/// </example>
/// <example>
///   <summary>Read the focused control from a specific window handle</summary>
///   <code>Get-DesktopFocusedControl -Handle 0x123456</code>
/// </example>
/// <example>
///   <summary>Read a bounded rich document and search its complete text</summary>
///   <code>Get-DesktopFocusedControl -Name '*Outlook*' -MaxObservedTextLength 4096 -ExpectedText 'matthew'</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopFocusedControl")]
public sealed class CmdletGetDesktopFocusedControl : PSCmdlet {
    /// <summary>
    /// <para type="description">Title of the window to inspect. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
    public string Name { get; set; } = "*";

    /// <summary>
    /// <para type="description">Window handle in decimal or hexadecimal format.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ByHandle")]
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Use the current foreground window.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ActiveWindow")]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Maximum number of focused-control value characters to return. The default is 2048.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int MaxObservedTextLength { get; set; } = 2048;

    /// <summary>
    /// <para type="description">Optional text to search for across the complete UI Automation document range even when the returned value is truncated.</para>
    /// </summary>
    [Parameter]
    public string ExpectedText { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        DesktopFocusedControlObservation observation = new DesktopAutomationService().GetFocusedControlObservation(
            CreateWindowQuery(),
            MaxObservedTextLength,
            string.IsNullOrEmpty(ExpectedText) ? null : ExpectedText);
        if (observation != null) {
            WriteObject(observation);
        }
    }

    private WindowQueryOptions CreateWindowQuery() {
        return ParameterSetName switch {
            "ByHandle" => new WindowQueryOptions {
                Handle = DesktopHandleParser.Parse(Handle),
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            "ActiveWindow" => new WindowQueryOptions {
                ActiveWindow = true,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            },
            _ => new WindowQueryOptions {
                TitlePattern = Name,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            }
        };
    }
}
