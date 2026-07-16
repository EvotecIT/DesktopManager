namespace DesktopManager.PowerShell;

/// <summary>Gets current interactive-session state.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopSession")]
[OutputType(typeof(DesktopSessionInfo))]
public sealed class CmdletGetDesktopSession : PSCmdlet {
    /// <summary>Gets the current session snapshot.</summary>
    protected override void BeginProcessing() {
        WriteObject(new DesktopSessionService().GetCurrentSession());
    }
}
