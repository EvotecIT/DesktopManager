#if NET8_0_OR_GREATER
using System.Reflection;

namespace DesktopManager.Tests;

[TestClass]
public sealed class PowerShellDeviceManagementSafetyTests {
    [TestMethod]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletEnableDesktopDevice))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletDisableDesktopDevice))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletRestartDesktopDevice))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletRemoveDesktopDevice))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletInvokeDesktopDeviceScan))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletAddDesktopDriverPackage))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletUpdateDesktopDeviceDriver))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletRemoveDesktopDriverPackage))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletRestoreDesktopDeviceDriver))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletNewDesktopRootDevice))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletSetDesktopRootHardwareId))]
    [DataRow(typeof(DesktopManager.PowerShell.CmdletSetDesktopDeviceClassFilter))]
    public void StateChangingCmdletsUseShouldProcessAndHighConfirmation(Type cmdletType) {
        CustomAttributeData? attribute = cmdletType.CustomAttributes.FirstOrDefault(item =>
            item.AttributeType.FullName == "System.Management.Automation.CmdletAttribute");

        Assert.IsNotNull(attribute);
        Assert.AreEqual(true, GetNamedArgument(attribute, "SupportsShouldProcess"), $"{cmdletType.Name} must support -WhatIf and -Confirm.");
        Assert.AreEqual(3, GetNamedArgument(attribute, "ConfirmImpact"));
    }

    [TestMethod]
    public void DriverExportSupportsShouldProcess() {
        CustomAttributeData? attribute = typeof(DesktopManager.PowerShell.CmdletExportDesktopDriverPackage)
            .CustomAttributes.FirstOrDefault(item =>
                item.AttributeType.FullName == "System.Management.Automation.CmdletAttribute");

        Assert.IsNotNull(attribute);
        Assert.AreEqual(true, GetNamedArgument(attribute, "SupportsShouldProcess"));
    }

    private static object? GetNamedArgument(CustomAttributeData attribute, string name) {
        CustomAttributeNamedArgument argument = attribute.NamedArguments.Single(item => item.MemberName == name);
        return argument.TypedValue.Value;
    }
}
#endif
