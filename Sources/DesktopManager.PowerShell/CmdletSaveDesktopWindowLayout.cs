using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Saves the current desktop window layout.</summary>
/// <para type="synopsis">Saves window positions to a file.</para>
/// <example>
///   <para>Save layout to a file</para>
///   <code>Save-DesktopWindowLayout -Path C:\layout.json</code>
/// </example>
[Cmdlet(VerbsData.Save, "DesktopWindowLayout")]
public sealed class CmdletSaveDesktopWindowLayout : PSCmdlet {
    /// <summary>
    /// <para type="description">Path to save the layout.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        manager.SaveLayout(Path);
    }
}
