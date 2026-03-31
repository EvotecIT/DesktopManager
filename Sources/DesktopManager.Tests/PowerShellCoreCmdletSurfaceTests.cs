#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for PowerShell cmdlets that expose shared DesktopManager core helpers.
/// </summary>
public class PowerShellCoreCmdletSurfaceTests {
    [DataTestMethod]
    [DataRow("CmdletGetDesktopClipboardText", "RetryCount", "RetryDelayMilliseconds")]
    [DataRow("CmdletSetDesktopClipboardText", "Text", "RetryCount", "RetryDelayMilliseconds", "PassThru")]
    [DataRow("CmdletGetDesktopBackgroundColor")]
    [DataRow("CmdletGetDesktopBrightness", "Index", "DeviceId", "DeviceName", "ConnectedOnly", "PrimaryOnly")]
    [DataRow("CmdletSetDesktopDpiScaling", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Scaling")]
    [DataRow("CmdletSetDesktopBrightness", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Brightness")]
    [DataRow("CmdletSetDesktopBackgroundColor", "Color")]
    [DataRow("CmdletSetDesktopPosition", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Left", "Top", "Right", "Bottom")]
    [DataRow("CmdletGetDesktopWallpaper", "Index", "DeviceId", "DeviceName", "ConnectedOnly", "PrimaryOnly")]
    [DataRow("CmdletSetDesktopResolution", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Width", "Height", "Orientation")]
    [DataRow("CmdletSetTaskbarPosition", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "All", "Position", "Hide", "Show")]
    [DataRow("CmdletSetDesktopWallpaper", "Index", "DeviceId", "DeviceName", "ConnectedOnly", "PrimaryOnly", "All", "AllUsers", "ExcludeDefaultUserProfile", "WallpaperPosition", "WallpaperPath", "Url", "ImageData")]
    [DataRow("CmdletGetLogonWallpaper")]
    [DataRow("CmdletSetLogonWallpaper", "ImagePath")]
    [DataRow("CmdletAdvanceDesktopSlideshow", "Direction")]
    [DataRow("CmdletStartDesktopSlideshow", "ImagePath")]
    [DataRow("CmdletStopDesktopSlideshow")]
    [DataRow("CmdletInvokeDesktopScreenshot", "Path", "Index", "DeviceId", "DeviceName", "PrimaryOnly", "Left", "Top", "Width", "Height")]
    [DataRow("CmdletInvokeDesktopWindowScreenshot", "Window", "Control", "Path")]
    [DataRow("CmdletStartDesktopProcess", "Path", "ArgumentList", "WorkingDirectory", "WaitForInputIdleMilliseconds", "WaitForWindowMilliseconds", "WaitForWindowIntervalMilliseconds", "WindowTitle", "WindowClassName", "RequireWindow")]
    [DataRow("CmdletStartDesktopProcessAndWait", "Path", "ArgumentList", "WorkingDirectory", "WaitForInputIdleMilliseconds", "LaunchWaitForWindowMilliseconds", "LaunchWaitForWindowIntervalMilliseconds", "LaunchWindowTitle", "LaunchWindowClassName", "WindowTitle", "WindowClassName", "IncludeHidden", "IncludeEmptyTitles", "All", "FollowProcessFamily", "TimeoutMilliseconds", "IntervalMilliseconds")]
    [DataRow("CmdletStopDesktopWindowProcess", "InputObject", "EntireProcessTree", "WaitForExitMilliseconds", "PassThru")]
    [DataRow("CmdletTestDesktopElevation")]
    [DataRow("CmdletWaitDesktopFocusedControl", "Name", "Handle", "ActiveWindow", "TimeoutMs", "IntervalMs")]
    /// <summary>
    /// Ensures the newer core-wrapper cmdlets expose the expected PowerShell parameters.
    /// </summary>
    public void CoreWrapperCmdlets_ExposeExpectedParameters(string typeName, params string[] parameterNames) {
        Type? cmdletType = Type.GetType($"DesktopManager.PowerShell.{typeName}, DesktopManager.PowerShell", throwOnError: true);
        Assert.IsNotNull(cmdletType);

        object? instance = Activator.CreateInstance(cmdletType);
        Assert.IsNotNull(instance);

        foreach (string parameterName in parameterNames) {
            System.Reflection.PropertyInfo? property = cmdletType.GetProperty(parameterName);
            Assert.IsNotNull(property, $"Expected parameter '{parameterName}' on '{typeName}'.");
        }
    }
}
#endif
