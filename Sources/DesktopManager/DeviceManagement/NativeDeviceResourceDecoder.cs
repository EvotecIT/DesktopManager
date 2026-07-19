using System.Globalization;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Decodes Configuration Manager resource data using the native descriptor layout for each resource type.</summary>
internal static class NativeDeviceResourceDecoder {
    /// <summary>Decodes one resource descriptor after its native buffer has been populated.</summary>
    internal static DesktopDeviceResourceInfo? Decode(uint resourceType, IntPtr buffer, uint size) {
        if (buffer == IntPtr.Zero) {
            return null;
        }
        switch (resourceType) {
            case DeviceNativeMethods.ResourceTypeMemory:
            case DeviceNativeMethods.ResourceTypeLargeMemory:
                return DecodeMemory(buffer, size);
            case DeviceNativeMethods.ResourceTypeIo:
                return DecodeIoPort(buffer, size);
            case DeviceNativeMethods.ResourceTypeDma:
                return DecodeDma(buffer, size);
            case DeviceNativeMethods.ResourceTypeIrq:
                return DecodeIrq(buffer, size);
            case DeviceNativeMethods.ResourceTypeBusNumber:
                return DecodeBusNumber(buffer, size);
            default:
                return null;
        }
    }

    private static DesktopDeviceResourceInfo? DecodeMemory(IntPtr buffer, uint size) {
        if (!TryRead(buffer, size, out MemoryResourceDescriptor descriptor)) {
            return null;
        }
        return RangeResource("Memory", descriptor.AllocatedBase, descriptor.AllocatedEnd, descriptor.Flags);
    }

    private static DesktopDeviceResourceInfo? DecodeIoPort(IntPtr buffer, uint size) {
        if (!TryRead(buffer, size, out IoResourceDescriptor descriptor)) {
            return null;
        }
        return RangeResource("IoPort", descriptor.AllocatedBase, descriptor.AllocatedEnd, descriptor.Flags);
    }

    private static DesktopDeviceResourceInfo? DecodeDma(IntPtr buffer, uint size) {
        if (!TryRead(buffer, size, out DmaResourceDescriptor descriptor)) {
            return null;
        }
        return ScalarResource("Dma", descriptor.AllocatedChannel, descriptor.Flags);
    }

    private static DesktopDeviceResourceInfo? DecodeIrq(IntPtr buffer, uint size) {
        if (!TryRead(buffer, size, out IrqResourceDescriptorHeader descriptor)) {
            return null;
        }
        return ScalarResource("Irq", descriptor.AllocatedNumber, descriptor.Flags);
    }

    private static DesktopDeviceResourceInfo? DecodeBusNumber(IntPtr buffer, uint size) {
        if (!TryRead(buffer, size, out BusNumberResourceDescriptor descriptor)) {
            return null;
        }
        string displayValue = descriptor.AllocatedBase == descriptor.AllocatedEnd
            ? descriptor.AllocatedBase.ToString(CultureInfo.InvariantCulture)
            : string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}",
                descriptor.AllocatedBase,
                descriptor.AllocatedEnd);
        return new DesktopDeviceResourceInfo {
            Kind = "BusNumber",
            Start = descriptor.AllocatedBase,
            End = descriptor.AllocatedEnd,
            Flags = descriptor.Flags,
            DisplayValue = displayValue
        };
    }

    private static DesktopDeviceResourceInfo RangeResource(string kind, ulong start, ulong end, uint flags) {
        return new DesktopDeviceResourceInfo {
            Kind = kind,
            Start = start,
            End = end,
            Flags = flags,
            DisplayValue = string.Format(CultureInfo.InvariantCulture, "0x{0:X}-0x{1:X}", start, end)
        };
    }

    private static DesktopDeviceResourceInfo ScalarResource(string kind, uint value, uint flags) {
        return new DesktopDeviceResourceInfo {
            Kind = kind,
            Start = value,
            End = value,
            Flags = flags,
            DisplayValue = value.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static bool TryRead<T>(IntPtr buffer, uint size, out T value) where T : struct {
        int requiredSize = Marshal.SizeOf(typeof(T));
        if (size < requiredSize) {
            value = default;
            return false;
        }
        object? native = Marshal.PtrToStructure(buffer, typeof(T));
        if (native is T descriptor) {
            value = descriptor;
            return true;
        }
        value = default;
        return false;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct MemoryResourceDescriptor {
        internal uint Count;
        internal uint Type;
        internal ulong AllocatedBase;
        internal ulong AllocatedEnd;
        internal uint Flags;
        internal uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct IoResourceDescriptor {
        internal uint Count;
        internal uint Type;
        internal ulong AllocatedBase;
        internal ulong AllocatedEnd;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct DmaResourceDescriptor {
        internal uint Count;
        internal uint Type;
        internal uint Flags;
        internal uint AllocatedChannel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct IrqResourceDescriptorHeader {
        internal uint Count;
        internal uint Type;
        internal ushort Flags;
        internal ushort Group;
        internal uint AllocatedNumber;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct BusNumberResourceDescriptor {
        internal uint Count;
        internal uint Type;
        internal uint Flags;
        internal uint AllocatedBase;
        internal uint AllocatedEnd;
    }
}
