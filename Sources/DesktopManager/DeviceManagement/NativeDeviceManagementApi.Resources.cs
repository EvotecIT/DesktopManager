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
            switch (resourceType) {
                case DeviceNativeMethods.ResourceTypeMemory:
                case DeviceNativeMethods.ResourceTypeLargeMemory:
                    return RangeResource("Memory", buffer);
                case DeviceNativeMethods.ResourceTypeIo:
                    return RangeResource("IoPort", buffer);
                case DeviceNativeMethods.ResourceTypeDma:
                    return ScalarResource("Dma", buffer);
                case DeviceNativeMethods.ResourceTypeIrq:
                    return ScalarResource("Irq", buffer);
                case DeviceNativeMethods.ResourceTypeBusNumber:
                    return BusNumberResource(buffer);
                default:
                    return null;
            }
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static DesktopDeviceResourceInfo RangeResource(string kind, IntPtr buffer) {
        ulong start = unchecked((ulong)Marshal.ReadInt64(buffer, 8));
        ulong end = unchecked((ulong)Marshal.ReadInt64(buffer, 16));
        uint flags = unchecked((uint)Marshal.ReadInt32(buffer, 24));
        return new DesktopDeviceResourceInfo {
            Kind = kind,
            Start = start,
            End = end,
            Flags = flags,
            DisplayValue = $"0x{start:X}-0x{end:X}"
        };
    }

    private static DesktopDeviceResourceInfo ScalarResource(string kind, IntPtr buffer) {
        uint flags = unchecked((uint)Marshal.ReadInt32(buffer, 8));
        uint value = unchecked((uint)Marshal.ReadInt32(buffer, 12));
        return new DesktopDeviceResourceInfo {
            Kind = kind,
            Start = value,
            End = value,
            Flags = flags,
            DisplayValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static DesktopDeviceResourceInfo BusNumberResource(IntPtr buffer) {
        uint flags = unchecked((uint)Marshal.ReadInt32(buffer, 8));
        uint start = unchecked((uint)Marshal.ReadInt32(buffer, 12));
        uint end = unchecked((uint)Marshal.ReadInt32(buffer, 16));
        return new DesktopDeviceResourceInfo {
            Kind = "BusNumber",
            Start = start,
            End = end,
            Flags = flags,
            DisplayValue = start == end ? start.ToString() : $"{start}-{end}"
        };
    }
}
