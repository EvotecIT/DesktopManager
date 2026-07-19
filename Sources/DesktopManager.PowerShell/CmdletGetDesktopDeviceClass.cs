namespace DesktopManager.PowerShell;

/// <summary>Gets Windows device setup classes and their filter chains.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopDeviceClass")]
[OutputType(typeof(DesktopDeviceClassInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletGetDesktopDeviceClass : PSCmdlet {
    /// <summary>Gets setup classes.</summary>
    protected override void BeginProcessing() {
        WriteObject(new DeviceManagementService().GetDeviceClasses(), true);
    }
}
