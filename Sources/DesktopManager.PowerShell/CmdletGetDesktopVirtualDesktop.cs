namespace DesktopManager.PowerShell;

/// <summary>Gets supported virtual-desktop state for a top-level window.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopVirtualDesktop")]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletGetDesktopVirtualDesktop : PSCmdlet {
    /// <summary><para type="description">The top-level window handle.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public IntPtr Handle;

    /// <summary>Gets the owning desktop identifier and current-desktop state.</summary>
    protected override void BeginProcessing() {
        using var service = new VirtualDesktopService();
        WriteObject(new {
            Handle,
            DesktopId = service.GetWindowDesktopId(Handle),
            OnCurrentDesktop = service.IsWindowOnCurrentDesktop(Handle)
        });
    }
}
