namespace DesktopManager.PowerShell;

/// <summary>Requests Plug and Play re-enumeration for the machine or one device subtree.</summary>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopDeviceScan", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletInvokeDesktopDeviceScan : PSCmdlet {
    /// <summary><para type="description">An optional exact device instance identifier. When omitted, scans from the machine root.</para></summary>
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    public string InstanceId;

    /// <summary><para type="description">Returns after Windows accepts the asynchronous scan request.</para></summary>
    [Parameter]
    public SwitchParameter Asynchronous;

    /// <summary>Requests device re-enumeration.</summary>
    protected override void ProcessRecord() {
        string target = string.IsNullOrWhiteSpace(InstanceId) ? "Local machine" : InstanceId;
        if (ShouldProcess(target, "Scan for Plug and Play hardware changes")) {
            WriteObject(new DeviceManagementService().ScanDevices(InstanceId, Asynchronous));
        }
    }
}
