namespace DesktopManager.PowerShell;

/// <summary>Gets taskbar windows and global auto-hide state.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopTaskbar")]
public sealed class CmdletGetDesktopTaskbar : PSCmdlet {
    /// <summary>Gets taskbar state.</summary>
    protected override void BeginProcessing() {
        var service = new TaskbarService();
        WriteObject(new {
            AutoHide = service.GetTaskbarAutoHide(),
            Taskbars = service.GetTaskbars()
        });
    }
}
