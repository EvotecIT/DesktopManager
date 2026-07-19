namespace DesktopManager.PowerShell;

/// <summary>Gets Windows device containers assembled from Plug and Play device instances.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopDeviceContainer")]
[OutputType(typeof(DesktopDeviceContainerInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletGetDesktopDeviceContainer : PSCmdlet {
    /// <summary><para type="description">Returns containers with at least one present device.</para></summary>
    [Parameter]
    public SwitchParameter Present;

    /// <summary><para type="description">Returns containers containing a device problem.</para></summary>
    [Parameter]
    public SwitchParameter Problem;

    /// <summary>Gets matching device containers.</summary>
    protected override void BeginProcessing() {
        WriteObject(new DeviceManagementService().GetDeviceContainers(new DesktopDeviceQuery {
            Present = Present ? true : null,
            HasProblem = Problem ? true : null
        }), true);
    }
}
