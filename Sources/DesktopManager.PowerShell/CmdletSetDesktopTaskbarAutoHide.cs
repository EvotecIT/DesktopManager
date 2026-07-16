namespace DesktopManager.PowerShell;

/// <summary>Sets the global Windows taskbar auto-hide state.</summary>
[Cmdlet(VerbsCommon.Set, "DesktopTaskbarAutoHide", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopTaskbarAutoHide : PSCmdlet {
    /// <summary><para type="description">The explicit auto-hide state.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public bool Enabled;

    /// <summary>Applies auto-hide.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Windows taskbar", $"Set auto-hide {Enabled}")) {
            var service = new TaskbarService();
            service.SetTaskbarAutoHide(Enabled);
            WriteObject(service.GetTaskbarAutoHide());
        }
    }
}
