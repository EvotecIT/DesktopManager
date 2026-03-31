using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Waits for a desktop window to become inactive.</summary>
/// <para type="synopsis">Waits for a desktop window to become inactive.</para>
/// <para type="description">Tracks the selected window until it no longer owns the foreground focus.</para>
/// <example>
///   <summary>Wait for the active window to lose focus</summary>
///   <code>Wait-DesktopWindowInactive -ActiveWindow -TimeoutMs 5000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Wait, "DesktopWindowInactive")]
public sealed class CmdletWaitDesktopWindowInactive : PSCmdlet {
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
        WriteObject(new DesktopAutomationService().WaitForWindowToLoseFocus(CreateWindowQuery(), TimeoutMs, IntervalMs));
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
