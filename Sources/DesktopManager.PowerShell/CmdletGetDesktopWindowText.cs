using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Observes the best available text for a desktop window.</summary>
/// <para type="synopsis">Observes the best available text for a desktop window.</para>
/// <para type="description">Returns a text observation from the best available source for a selected window, such as the focused control value, control text, or window title.</para>
/// <example>
///   <summary>Observe text from the active window</summary>
///   <code>Get-DesktopWindowText -ActiveWindow</code>
/// </example>
/// <example>
///   <summary>Prefer text containing a specific value</summary>
///   <code>Get-DesktopWindowText -Name "*Notepad*" -ExpectedText "hello"</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWindowText")]
public sealed class CmdletGetDesktopWindowText : PSCmdlet {
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
    /// <para type="description">Optional text that should be preferred when present.</para>
    /// </summary>
    [Parameter]
    public string ExpectedText { get; set; }

    /// <summary>
    /// <para type="description">Maximum number of characters to return in the observed value.</para>
    /// </summary>
    [Parameter]
    public int MaxObservedTextLength { get; set; } = 2048;

    /// <summary>
    /// <para type="description">Number of observation retries.</para>
    /// </summary>
    [Parameter]
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// <para type="description">Delay in milliseconds between observation retries.</para>
    /// </summary>
    [Parameter]
    public int RetryDelayMilliseconds { get; set; } = 50;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        DesktopWindowTextObservation observation = new DesktopAutomationService().ObserveWindowText(
            CreateWindowQuery(),
            ExpectedText,
            new DesktopTextObservationOptions {
                MaxObservedTextLength = MaxObservedTextLength,
                RetryCount = RetryCount,
                RetryDelayMilliseconds = RetryDelayMilliseconds
            });
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
