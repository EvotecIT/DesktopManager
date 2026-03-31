using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets Unicode text on the desktop clipboard.</summary>
/// <para type="synopsis">Sets Unicode text on the desktop clipboard.</para>
/// <para type="description">Writes Unicode text to the Windows clipboard through the DesktopManager automation core.</para>
/// <example>
///   <summary>Replace the current clipboard text</summary>
///   <code>Set-DesktopClipboardText -Text "Hello from DesktopManager"</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopClipboardText", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopClipboardText : PSCmdlet {
    /// <summary>
    /// <para type="description">Text to place on the clipboard.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public string Text { get; set; } = string.Empty;

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

    /// <summary>
    /// <para type="description">Return the clipboard text after the update.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (!ShouldProcess("Desktop clipboard", "Set clipboard text")) {
            return;
        }

        var automation = new DesktopAutomationService();
        automation.SetClipboardText(Text, RetryCount, RetryDelayMilliseconds);
        if (PassThru.IsPresent) {
            WriteObject(automation.GetClipboardText(RetryCount, RetryDelayMilliseconds));
        }
    }
}
