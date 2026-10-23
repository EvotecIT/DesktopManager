namespace DesktopManager.PowerShell;

/// <summary>Gets supported virtual-desktop state for a top-level window.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopVirtualDesktop", DefaultParameterSetName = "Window")]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletGetDesktopVirtualDesktop : PSCmdlet {
    /// <summary><para type="description">The top-level window handle.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Window")]
    public IntPtr Handle;

    /// <para>List live virtual desktop identities, names and wallpapers through the experimental shell contract.</para>
    [Parameter(Mandatory = true, ParameterSetName = "List")]
    public SwitchParameter List { get; set; }

    /// <para>Acknowledge the undocumented, build-dependent shell contract used for desktop enumeration.</para>
    [Parameter(Mandatory = true, ParameterSetName = "List")]
    public SwitchParameter Experimental { get; set; }

    /// <summary>Gets the owning desktop identifier and current-desktop state.</summary>
    protected override void BeginProcessing() {
        if (List) {
            using var desktops = new ExperimentalVirtualDesktopService();
            WriteObject(desktops.GetDesktops(), true);
            return;
        }
        using var service = new VirtualDesktopService();
        WriteObject(new {
            Handle,
            DesktopId = service.GetWindowDesktopId(Handle),
            OnCurrentDesktop = service.IsWindowOnCurrentDesktop(Handle)
        });
    }
}
