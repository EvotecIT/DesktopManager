using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Waits until a desktop window exposes the requested observed text.</summary>
/// <para type="synopsis">Waits until a desktop window exposes the requested observed text.</para>
/// <para type="description">Polls the DesktopManager text observation pipeline until the selected window exposes text containing the requested value.</para>
/// <example>
///   <summary>Wait for text in the active window</summary>
///   <code>Wait-DesktopWindowText -ActiveWindow -ExpectedText "Ready"</code>
/// </example>
[Cmdlet(VerbsLifecycle.Wait, "DesktopWindowText")]
public sealed class CmdletWaitDesktopWindowText : PSCmdlet {
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
    /// <para type="description">Text to wait for.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public string ExpectedText { get; set; } = string.Empty;

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

    /// <summary>
    /// <para type="description">Maximum number of characters to return in the observed value.</para>
    /// </summary>
    [Parameter]
    public int MaxObservedTextLength { get; set; } = 2048;

    /// <summary>
    /// <para type="description">Number of observation retries within each polling cycle.</para>
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
        DesktopWindowTextObservation observation = new DesktopAutomationService().WaitForObservedText(
            CreateWindowQuery(),
            ExpectedText,
            TimeoutMs,
            IntervalMs,
            new DesktopTextObservationOptions {
                MaxObservedTextLength = MaxObservedTextLength,
                RetryCount = RetryCount,
                RetryDelayMilliseconds = RetryDelayMilliseconds
            });
        WriteObject(observation);
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
