namespace DesktopManager.PowerShell;

/// <summary>Disables an exact Windows Plug and Play device instance.</summary>
/// <para>Force requests absolute disable semantics. Windows can still reject critical or non-disableable devices.</para>
[Cmdlet(VerbsLifecycle.Disable, "DesktopDevice", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletDisableDesktopDevice : PSCmdlet {
    /// <summary><para type="description">The exact device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary><para type="description">Requests absolute disable semantics.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary><para type="description">Does not persist the disabled state across restart.</para></summary>
    [Parameter]
    public SwitchParameter Temporary;

    /// <summary>Disables the selected device.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(InstanceId, Force ? "Force-disable Plug and Play device" : "Disable Plug and Play device")) {
            WriteObject(new DeviceManagementService().DisableDevice(InstanceId, Force, !Temporary));
        }
    }
}
