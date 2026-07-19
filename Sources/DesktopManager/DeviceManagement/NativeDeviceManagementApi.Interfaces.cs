using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal sealed partial class NativeDeviceManagementApi {
    private const uint InterfaceActive = 0x00000001;
    private const uint InterfaceDefault = 0x00000002;
    private const uint InterfaceRemoved = 0x00000004;

    private static IReadOnlyList<DesktopDeviceInterfaceInfo> ReadInterfaces(string instanceId) {
        var interfaces = new List<DesktopDeviceInterfaceInfo>();
        foreach (Guid value in GetInterfaceClassGuids()) {
            Guid interfaceClassGuid = value;
            using SafeDeviceInfoSetHandle deviceInfoSet = DeviceNativeMethods.SetupDiGetClassDevsW(
                ref interfaceClassGuid,
                null,
                IntPtr.Zero,
                DeviceNativeMethods.DigcfDeviceInterface);
            if (deviceInfoSet.IsInvalid) {
                continue;
            }

            for (uint index = 0; ; index++) {
                DeviceNativeMethods.SpDeviceInterfaceData interfaceData =
                    DeviceNativeMethods.SpDeviceInterfaceData.Create();
                if (!DeviceNativeMethods.SetupDiEnumDeviceInterfaces(
                    deviceInfoSet,
                    IntPtr.Zero,
                    ref interfaceClassGuid,
                    index,
                    ref interfaceData)) {
                    if (Marshal.GetLastWin32Error() == DeviceNativeMethods.ErrorNoMoreItems) {
                        break;
                    }
                    break;
                }

                DesktopDeviceInterfaceInfo? item = ReadInterface(
                    deviceInfoSet,
                    ref interfaceData,
                    instanceId);
                if (item != null) {
                    interfaces.Add(item);
                }
            }
        }
        return interfaces;
    }

    private static DesktopDeviceInterfaceInfo? ReadInterface(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDeviceInterfaceData interfaceData,
        string expectedInstanceId) {
        DeviceNativeMethods.SpDevInfoData deviceInfoData = DeviceNativeMethods.SpDevInfoData.Create();
        DeviceNativeMethods.SetupDiGetDeviceInterfaceDetailW(
            deviceInfoSet,
            ref interfaceData,
            IntPtr.Zero,
            0,
            out uint requiredSize,
            ref deviceInfoData);
        if (requiredSize == 0) {
            return null;
        }

        IntPtr detail = Marshal.AllocHGlobal(checked((int)requiredSize));
        try {
            Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
            deviceInfoData = DeviceNativeMethods.SpDevInfoData.Create();
            if (!DeviceNativeMethods.SetupDiGetDeviceInterfaceDetailW(
                deviceInfoSet,
                ref interfaceData,
                detail,
                requiredSize,
                out _,
                ref deviceInfoData)) {
                return null;
            }
            string actualInstanceId = GetInstanceId(deviceInfoSet, ref deviceInfoData);
            if (!string.Equals(actualInstanceId, expectedInstanceId, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }
            string path = Marshal.PtrToStringUni(IntPtr.Add(detail, 4)) ?? string.Empty;
            return new DesktopDeviceInterfaceInfo {
                ClassGuid = interfaceData.InterfaceClassGuid,
                Path = path,
                Enabled = (interfaceData.Flags & InterfaceActive) != 0,
                Default = (interfaceData.Flags & InterfaceDefault) != 0,
                Removed = (interfaceData.Flags & InterfaceRemoved) != 0
            };
        } finally {
            Marshal.FreeHGlobal(detail);
        }
    }

    private static IReadOnlyList<Guid> GetInterfaceClassGuids() {
        var values = new List<Guid>();
        using RegistryKey? classes = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\DeviceClasses",
            writable: false);
        if (classes == null) {
            return values;
        }
        foreach (string name in classes.GetSubKeyNames()) {
            string candidate = name.Trim('{', '}');
            if (Guid.TryParse(candidate, out Guid classGuid)) {
                values.Add(classGuid);
            }
        }
        return values;
    }
}
