using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets text in a desktop window.</summary>
/// <para type="synopsis">Pastes or types text into a desktop window.</para>
/// <example>
///   <code>Set-DesktopWindowText -Name "Notepad" -Text "Hello"</code>
/// </example>
/// <example>
///   <code>Set-DesktopWindowText -Name "Notepad" -Text "Hello" -Type</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowText", DefaultParameterSetName = "Paste", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopWindowText : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Text to paste or type.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string Text { get; set; }

    /// <summary>
    /// <para type="description">Use the clipboard paste method.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Paste")]
    public SwitchParameter Paste { get; set; }

    /// <summary>
    /// <para type="description">Simulate typing the text.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter Type { get; set; }

    /// <summary>
    /// <para type="description">Delay in milliseconds between characters when typing.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public int Delay { get; set; } = 0;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        var windows = manager.GetWindows(Name);

        foreach (var window in windows) {
            string action = ParameterSetName == "Type" ? "Type text" : "Paste text";
            if (ShouldProcess(window.Title, action)) {
                if (ParameterSetName == "Type") {
                    manager.TypeText(window, Text, Delay);
                } else {
                    manager.PasteText(window, Text);
                }
            }
        }
    }
}

