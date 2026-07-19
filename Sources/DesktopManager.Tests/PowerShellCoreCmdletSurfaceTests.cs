#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for PowerShell cmdlets that expose shared DesktopManager core helpers.
/// </summary>
public class PowerShellCoreCmdletSurfaceTests {
    [TestMethod]
    [DataRow("CmdletGetDesktopClipboardText", "RetryCount", "RetryDelayMilliseconds")]
    [DataRow("CmdletSetDesktopClipboardText", "Text", "RetryCount", "RetryDelayMilliseconds", "PassThru")]
    [DataRow("CmdletGetDesktopBackgroundColor")]
    [DataRow("CmdletGetDesktopBrightness", "Index", "DeviceId", "DeviceName", "ConnectedOnly", "PrimaryOnly")]
    [DataRow("CmdletSetDesktopBrightness", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Brightness")]
    [DataRow("CmdletSetDesktopBackgroundColor", "Color")]
    [DataRow("CmdletSetDesktopPosition", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Left", "Top")]
    [DataRow("CmdletGetDesktopWallpaper", "Index", "DeviceId", "DeviceName", "ConnectedOnly", "PrimaryOnly")]
    [DataRow("CmdletSetDesktopResolution", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Width", "Height", "Orientation")]
    [DataRow("CmdletSetTaskbarPosition", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "All", "Position", "Hide", "Show")]
    [DataRow("CmdletSetDesktopWindowPlacement", "Name", "Placement", "MonitorTarget", "MonitorIndex", "Left", "Top", "Width", "Height", "NoVerify", "PassThru")]
    [DataRow("CmdletSetDesktopWallpaper", "Index", "DeviceId", "DeviceName", "ConnectedOnly", "PrimaryOnly", "All", "AllUsers", "ExcludeDefaultUserProfile", "WallpaperPosition", "WallpaperPath", "Url", "ImageData")]
    [DataRow("CmdletGetLogonWallpaper")]
    [DataRow("CmdletSetLogonWallpaper", "ImagePath")]
    [DataRow("CmdletStepDesktopSlideshow", "Direction")]
    [DataRow("CmdletGetDesktopSlideshow")]
    [DataRow("CmdletSetDesktopSlideshowOptions", "Shuffle", "NoShuffle", "SlideshowTick")]
    [DataRow("CmdletStartDesktopSlideshow", "ImagePath", "Shuffle", "SlideshowTick")]
    [DataRow("CmdletStopDesktopSlideshow")]
    [DataRow("CmdletInvokeDesktopScreenshot", "Path", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Left", "Top", "Width", "Height")]
    [DataRow("CmdletInvokeDesktopWindowScreenshot", "Window", "Control", "Path")]
    [DataRow("CmdletStartDesktopProcess", "Path", "ArgumentList", "WorkingDirectory", "WaitForInputIdleMilliseconds", "WaitForWindowMilliseconds", "WaitForWindowIntervalMilliseconds", "WindowTitle", "WindowClassName", "RequireWindow")]
    [DataRow("CmdletStartDesktopProcessAndWait", "Path", "ArgumentList", "WorkingDirectory", "WaitForInputIdleMilliseconds", "LaunchWaitForWindowMilliseconds", "LaunchWaitForWindowIntervalMilliseconds", "LaunchWindowTitle", "LaunchWindowClassName", "WindowTitle", "WindowClassName", "IncludeHidden", "IncludeEmptyTitles", "All", "FollowProcessFamily", "TimeoutMilliseconds", "IntervalMilliseconds")]
    [DataRow("CmdletStopDesktopWindowProcess", "InputObject", "EntireProcessTree", "WaitForExitMilliseconds", "PassThru")]
    [DataRow("CmdletTestDesktopElevation")]
    [DataRow("CmdletWaitDesktopFocusedControl", "Name", "Handle", "ActiveWindow", "TimeoutMs", "IntervalMs")]
    [DataRow("CmdletGetDesktopAudioEndpoint", "DeviceId", "DataFlow", "ActiveOnly")]
    [DataRow("CmdletSetDesktopAudioEndpoint", "DeviceId", "Volume", "Muted", "PassThru")]
    [DataRow("CmdletSetDefaultAudioDevice", "DeviceId", "Role", "PassThru")]
    [DataRow("CmdletSaveDesktopWorkstationProfile", "Name")]
    [DataRow("CmdletGetDesktopWorkstationProfile", "Name")]
    [DataRow("CmdletRestoreDesktopWorkstationProfile", "Name", "AllowMissingMonitor", "SkipDisplay", "SkipAudio", "SkipPersonalization", "IncludeMachinePolicies", "SkipTaskbar", "NoRollback")]
    [DataRow("CmdletGetDesktopPowerStatus")]
    [DataRow("CmdletGetDesktopSession")]
    [DataRow("CmdletLockDesktopSession")]
    [DataRow("CmdletExitDesktopSession", "Force")]
    [DataRow("CmdletStartDesktopKeepAwake", "Duration", "Display", "AwayMode")]
    [DataRow("CmdletSuspendDesktopSystem", "Hibernate", "Force")]
    [DataRow("CmdletGetDesktopPersonalization", "Name", "List")]
    [DataRow("CmdletSetDesktopPersonalization", "InputObject", "PassThru")]
    [DataRow("CmdletRestoreDesktopPersonalization", "Name", "SkipMachinePolicies")]
    [DataRow("CmdletGetDesktopTaskbar")]
    [DataRow("CmdletSetDesktopTaskbarAutoHide", "Enabled")]
    [DataRow("CmdletGetDesktopRadio")]
    [DataRow("CmdletSetDesktopRadio", "Kind", "State", "Name")]
    [DataRow("CmdletGetDesktopWifiProfile", "InterfaceId")]
    [DataRow("CmdletConnectDesktopWifiProfile", "Name", "InterfaceId", "Timeout")]
    [DataRow("CmdletGetDesktopDevice", "InstanceId", "DeviceId", "Class", "ClassGuid", "Enumerator", "Present", "NonPresent", "Problem", "ProblemCode", "IncludeRelations", "IncludeStack", "IncludeResources", "IncludeInterfaces", "IncludeProperties")]
    [DataRow("CmdletGetDesktopDeviceDriver", "InstanceId")]
    [DataRow("CmdletGetDesktopDriverPackage", "PublishedInfName", "ClassGuid", "IncludeFiles", "IncludeDevices")]
    [DataRow("CmdletGetDesktopDeviceClass")]
    [DataRow("CmdletGetDesktopDeviceContainer", "Present", "Problem")]
    [DataRow("CmdletEnableDesktopDevice", "InstanceId")]
    [DataRow("CmdletDisableDesktopDevice", "InstanceId", "Force", "Temporary")]
    [DataRow("CmdletRestartDesktopDevice", "InstanceId")]
    [DataRow("CmdletRemoveDesktopDevice", "InstanceId", "DeviceOnly")]
    [DataRow("CmdletInvokeDesktopDeviceScan", "InstanceId", "Asynchronous")]
    [DataRow("CmdletAddDesktopDriverPackage", "InfPath", "Install", "Force")]
    [DataRow("CmdletUpdateDesktopDeviceDriver", "InfPath", "HardwareId", "Force")]
    [DataRow("CmdletRemoveDesktopDriverPackage", "PublishedInfName", "UninstallDevices", "Force")]
    [DataRow("CmdletExportDesktopDriverPackage", "PublishedInfName", "Destination", "Force")]
    [DataRow("CmdletRestoreDesktopDeviceDriver", "InstanceId")]
    [DataRow("CmdletNewDesktopRootDevice", "InfPath", "HardwareId")]
    [DataRow("CmdletSetDesktopRootHardwareId", "InstanceId", "HardwareId")]
    [DataRow("CmdletSetDesktopDeviceClassFilter", "ClassGuid", "Kind", "Service")]
    [DataRow("CmdletGetDesktopAirplaneMode", "Experimental")]
    [DataRow("CmdletSetDesktopAirplaneMode", "State", "Experimental")]
    [DataRow("CmdletGetDesktopVirtualDesktop", "Handle")]
    [DataRow("CmdletMoveDesktopWindowToVirtualDesktop", "Handle", "DesktopId")]
    /// <summary>
    /// Ensures the newer core-wrapper cmdlets expose the expected PowerShell parameters.
    /// </summary>
    public void CoreWrapperCmdlets_ExposeExpectedParameters(string typeName, params string[] parameterNames) {
        Type? cmdletType = Type.GetType($"DesktopManager.PowerShell.{typeName}, DesktopManager.PowerShell", throwOnError: true);
        Assert.IsNotNull(cmdletType);

        object? instance = Activator.CreateInstance(cmdletType);
        Assert.IsNotNull(instance);

        foreach (string parameterName in parameterNames) {
            System.Reflection.MemberInfo? parameter = cmdletType.GetProperty(parameterName) ??
                                                      (System.Reflection.MemberInfo?)cmdletType.GetField(parameterName);
            Assert.IsNotNull(parameter, $"Expected parameter '{parameterName}' on '{typeName}'.");
        }
    }
}
#endif
