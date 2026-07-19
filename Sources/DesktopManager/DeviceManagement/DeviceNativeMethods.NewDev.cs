using System.Runtime.InteropServices;

namespace DesktopManager;

internal static partial class DeviceNativeMethods {
    internal const uint InstallFlagForce = 0x00000001;
    internal const uint InstallFlagNonInteractive = 0x00000004;
    internal const uint DiirFlagForceInf = 0x00000002;
    internal const uint DiurFlagNoRemoveInf = 0x00000001;

    [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DiInstallDriverW(
        IntPtr parentWindow,
        string infPath,
        uint flags,
        [MarshalAs(UnmanagedType.Bool)] out bool needReboot);

    [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UpdateDriverForPlugAndPlayDevicesW(
        IntPtr parentWindow,
        string hardwareId,
        string fullInfPath,
        uint installFlags,
        [MarshalAs(UnmanagedType.Bool)] out bool rebootRequired);

    [DllImport("newdev.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DiUninstallDevice(
        IntPtr parentWindow,
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint flags,
        [MarshalAs(UnmanagedType.Bool)] out bool needReboot);

    [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DiUninstallDriverW(
        IntPtr parentWindow,
        string infPath,
        uint flags,
        [MarshalAs(UnmanagedType.Bool)] out bool needReboot);

    [DllImport("newdev.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DiRollbackDriver(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        IntPtr parentWindow,
        uint flags,
        [MarshalAs(UnmanagedType.Bool)] out bool needReboot);
}
