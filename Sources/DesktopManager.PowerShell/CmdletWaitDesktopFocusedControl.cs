using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Waits until a desktop window exposes a focused control.</summary>
/// <para type="synopsis">Waits until a desktop window exposes a focused control.</para>
/// <para type="description">Polls DesktopManager focused-control observation until the selected window exposes a focused child control.</para>
/// <example>
///   <summary>Wait for the active window to expose a focused control</summary>
///   <code>Wait-DesktopFocusedControl -ActiveWindow -TimeoutMs 5000</code>
/// </example>
/// <example>
///   <summary>Wait for a specific window handle to expose a focused control</summary>
///   <code>Wait-DesktopFocusedControl -Handle 0x123456 -TimeoutMs 5000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Wait, "DesktopFocusedControl")]
public sealed class CmdletWaitDesktopFocusedControl : PSCmdlet {
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
    /// <para type="description">Timeout in milliseconds. Zero waits indefinitely.</para>
    /// </summary>
    [Parameter]
    public int TimeoutMs { get; set; } = 10000;

    /// <summary>
    /// <para type="description">Polling interval in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int IntervalMs { get; set; } = 200;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        DesktopFocusedControlObservation observation = new DesktopAutomationService().WaitForFocusedControlObservation(
            CreateWindowQuery(),
            TimeoutMs,
            IntervalMs);
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
