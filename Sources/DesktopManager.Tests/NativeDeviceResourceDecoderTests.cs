using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public sealed class NativeDeviceResourceDecoderTests {
    [TestMethod]
    public void MemoryDescriptorUsesAllocatedRangeAndFlags() {
        byte[] buffer = new byte[32];
        WriteUInt64(buffer, 8, 0x100000000UL);
        WriteUInt64(buffer, 16, 0x10000FFFFUL);
        WriteUInt32(buffer, 24, 0x42);

        DesktopDeviceResourceInfo? resource = Decode(DeviceNativeMethods.ResourceTypeMemory, buffer);

        Assert.IsNotNull(resource);
        Assert.AreEqual("Memory", resource.Kind);
        Assert.AreEqual(0x100000000UL, resource.Start);
        Assert.AreEqual(0x10000FFFFUL, resource.End);
        Assert.AreEqual(0x42U, resource.Flags);
        Assert.AreEqual("0x100000000-0x10000FFFF", resource.DisplayValue);

        DesktopDeviceResourceInfo? largeMemory = Decode(DeviceNativeMethods.ResourceTypeLargeMemory, buffer);
        Assert.IsNotNull(largeMemory);
        Assert.AreEqual(resource.Start, largeMemory.Start);
        Assert.AreEqual(resource.End, largeMemory.End);
    }

    [TestMethod]
    public void IoDescriptorUsesAllocatedPortRange() {
        byte[] buffer = new byte[32];
        WriteUInt64(buffer, 8, 0x3F8);
        WriteUInt64(buffer, 16, 0x3FF);
        WriteUInt32(buffer, 24, 0x11);

        DesktopDeviceResourceInfo? resource = Decode(DeviceNativeMethods.ResourceTypeIo, buffer);

        Assert.IsNotNull(resource);
        Assert.AreEqual("IoPort", resource.Kind);
        Assert.AreEqual(0x3F8UL, resource.Start);
        Assert.AreEqual(0x3FFUL, resource.End);
        Assert.AreEqual(0x11U, resource.Flags);
    }

    [TestMethod]
    public void DmaDescriptorUsesAllocatedChannel() {
        byte[] buffer = new byte[16];
        WriteUInt32(buffer, 8, 0x21);
        WriteUInt32(buffer, 12, 7);

        DesktopDeviceResourceInfo? resource = Decode(DeviceNativeMethods.ResourceTypeDma, buffer);

        Assert.IsNotNull(resource);
        Assert.AreEqual("Dma", resource.Kind);
        Assert.AreEqual(7UL, resource.Start);
        Assert.AreEqual(7UL, resource.End);
        Assert.AreEqual(0x21U, resource.Flags);
        Assert.AreEqual("7", resource.DisplayValue);
    }

    [TestMethod]
    public void IrqDescriptorKeepsProcessorGroupSeparateFromFlags() {
        byte[] buffer = new byte[16];
        WriteUInt16(buffer, 8, 0x34);
        WriteUInt16(buffer, 10, 0x1234);
        WriteUInt32(buffer, 12, 19);

        DesktopDeviceResourceInfo? resource = Decode(DeviceNativeMethods.ResourceTypeIrq, buffer);

        Assert.IsNotNull(resource);
        Assert.AreEqual("Irq", resource.Kind);
        Assert.AreEqual(19UL, resource.Start);
        Assert.AreEqual(0x34U, resource.Flags);
    }

    [TestMethod]
    public void BusNumberDescriptorUsesAllocatedRange() {
        byte[] buffer = new byte[20];
        WriteUInt32(buffer, 8, 0x08);
        WriteUInt32(buffer, 12, 4);
        WriteUInt32(buffer, 16, 9);

        DesktopDeviceResourceInfo? resource = Decode(DeviceNativeMethods.ResourceTypeBusNumber, buffer);

        Assert.IsNotNull(resource);
        Assert.AreEqual("BusNumber", resource.Kind);
        Assert.AreEqual(4UL, resource.Start);
        Assert.AreEqual(9UL, resource.End);
        Assert.AreEqual(0x08U, resource.Flags);
        Assert.AreEqual("4-9", resource.DisplayValue);
    }

    [TestMethod]
    public void TruncatedOrUnknownDescriptorsAreIgnored() {
        byte[] truncated = new byte[8];

        Assert.IsNull(Decode(DeviceNativeMethods.ResourceTypeMemory, truncated));
        Assert.IsNull(Decode(DeviceNativeMethods.ResourceTypeIo, truncated));
        Assert.IsNull(Decode(DeviceNativeMethods.ResourceTypeDma, truncated));
        Assert.IsNull(Decode(DeviceNativeMethods.ResourceTypeIrq, truncated));
        Assert.IsNull(Decode(DeviceNativeMethods.ResourceTypeBusNumber, truncated));
        Assert.IsNull(Decode(uint.MaxValue, new byte[32]));
    }

    private static DesktopDeviceResourceInfo? Decode(uint resourceType, byte[] buffer) {
        IntPtr pointer = Marshal.AllocHGlobal(buffer.Length);
        try {
            Marshal.Copy(buffer, 0, pointer, buffer.Length);
            return NativeDeviceResourceDecoder.Decode(resourceType, pointer, (uint)buffer.Length);
        } finally {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value) {
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(ushort));
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value) {
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(uint));
    }

    private static void WriteUInt64(byte[] buffer, int offset, ulong value) {
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(ulong));
    }
}
