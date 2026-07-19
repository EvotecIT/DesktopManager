using System.Runtime.InteropServices;

namespace DesktopManager;

internal sealed partial class NativeDeviceManagementApi {
    private static IReadOnlyList<DesktopDeviceResourceInfo> ReadResources(uint deviceInstance) {
        uint result = DeviceNativeMethods.CM_Get_First_Log_Conf(
            out IntPtr logConfiguration,
            deviceInstance,
            DeviceNativeMethods.AllocatedLogConfiguration);
        if (result != DeviceNativeMethods.CrSuccess) {
            return Array.Empty<DesktopDeviceResourceInfo>();
        }

        var resources = new List<DesktopDeviceResourceInfo>();
        try {
            result = DeviceNativeMethods.CM_Get_Next_Res_Des(
                out IntPtr resourceDescriptor,
                logConfiguration,
                DeviceNativeMethods.ResourceTypeAll,
                out uint resourceType,
                0);
            while (result == DeviceNativeMethods.CrSuccess) {
                try {
                    DesktopDeviceResourceInfo? resource = ReadResource(resourceDescriptor, resourceType);
                    if (resource != null) {
                        resources.Add(resource);
                    }
                    result = DeviceNativeMethods.CM_Get_Next_Res_Des(
                        out IntPtr nextResourceDescriptor,
                        resourceDescriptor,
                        DeviceNativeMethods.ResourceTypeAll,
                        out uint nextResourceType,
                        0);
                    DeviceNativeMethods.CM_Free_Res_Des_Handle(resourceDescriptor);
                    resourceDescriptor = nextResourceDescriptor;
                    resourceType = nextResourceType;
                } catch {
                    DeviceNativeMethods.CM_Free_Res_Des_Handle(resourceDescriptor);
                    throw;
                }
            }
        } finally {
            DeviceNativeMethods.CM_Free_Log_Conf_Handle(logConfiguration);
        }
        return resources;
    }

    private static DesktopDeviceResourceInfo? ReadResource(IntPtr resourceDescriptor, uint resourceType) {
        uint result = DeviceNativeMethods.CM_Get_Res_Des_Data_Size(out uint size, resourceDescriptor, 0);
        if (result != DeviceNativeMethods.CrSuccess || size == 0) {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)size));
        try {
            result = DeviceNativeMethods.CM_Get_Res_Des_Data(resourceDescriptor, buffer, size, 0);
            if (result != DeviceNativeMethods.CrSuccess) {
                return null;
            }
            return NativeDeviceResourceDecoder.Decode(resourceType, buffer, size);
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
