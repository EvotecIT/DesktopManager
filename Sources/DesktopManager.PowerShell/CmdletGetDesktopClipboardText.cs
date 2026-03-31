using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets Unicode text from the desktop clipboard.</summary>
/// <para type="synopsis">Gets Unicode text from the desktop clipboard.</para>
/// <para type="description">Returns the current Unicode clipboard text when available. When the clipboard does not currently contain Unicode text, the cmdlet returns no output.</para>
/// <example>
///   <summary>Read the current clipboard text</summary>
///   <code>Get-DesktopClipboardText</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopClipboardText")]
public sealed class CmdletGetDesktopClipboardText : PSCmdlet {
    /// <summary>
    /// <para type="description">Number of attempts to open the clipboard.</para>
    /// </summary>
    [Parameter]
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// <para type="description">Delay between clipboard retry attempts in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int RetryDelayMilliseconds { get; set; } = 50;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        string text = new DesktopAutomationService().GetClipboardText(RetryCount, RetryDelayMilliseconds);
        if (text != null) {
            WriteObject(text);
        }
    }
}
