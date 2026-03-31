using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the focused control for a desktop window.</summary>
/// <para type="synopsis">Gets the focused control for a desktop window.</para>
/// <para type="description">Returns focused-control metadata for a specific window selected by title, handle, or the current foreground window.</para>
/// <example>
///   <summary>Read the focused control from the active window</summary>
///   <code>Get-DesktopFocusedControl -ActiveWindow</code>
/// </example>
/// <example>
///   <summary>Read the focused control from a specific window handle</summary>
///   <code>Get-DesktopFocusedControl -Handle 0x123456</code>
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

    /// <inheritdoc />
    protected override void BeginProcessing() {
        DesktopFocusedControlObservation observation = new DesktopAutomationService().GetFocusedControlObservation(CreateWindowQuery());
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
