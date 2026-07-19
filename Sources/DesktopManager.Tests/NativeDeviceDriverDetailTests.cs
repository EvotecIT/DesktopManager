using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager.Tests;

[TestClass]
public sealed class NativeDeviceDriverDetailTests {
    [TestMethod]
    public void CompatibleIdsAcceptAnExactUtf16BufferBoundary() {
        const int hardwareOffset = 4;
        const uint compatibleOffsetCharacters = 3;
        const string compatibleIds = "compat-a\0compat-b\0";
        byte[] compatibleBytes = Encoding.Unicode.GetBytes(compatibleIds);
        int compatibleStart = hardwareOffset + checked((int)compatibleOffsetCharacters * sizeof(char));
        byte[] buffer = new byte[compatibleStart + compatibleBytes.Length];
        Buffer.BlockCopy(compatibleBytes, 0, buffer, compatibleStart, compatibleBytes.Length);
        var detail = new DeviceNativeMethods.SpDrvInfoDetailData {
            CompatibleIdsOffset = compatibleOffsetCharacters,
            CompatibleIdsLength = (uint)compatibleIds.Length
        };

        string[] decoded = ReadCompatibleIds(buffer, hardwareOffset, detail);

        CollectionAssert.AreEqual(new[] { "compat-a", "compat-b" }, decoded);
    }

    [TestMethod]
    public void CompatibleIdsRejectMalformedNativeRanges() {
        byte[] buffer = new byte[32];

        Assert.AreEqual(0, ReadCompatibleIds(buffer, 4, Detail(offset: 2, length: 13)).Length);
        Assert.AreEqual(0, ReadCompatibleIds(buffer, 4, Detail(offset: 15, length: 1)).Length);
        Assert.AreEqual(0, ReadCompatibleIds(buffer, 4, Detail(uint.MaxValue, 1)).Length);
        Assert.AreEqual(0, ReadCompatibleIds(buffer, 4, Detail(1, uint.MaxValue)).Length);
    }

    private static DeviceNativeMethods.SpDrvInfoDetailData Detail(uint offset, uint length) {
        return new DeviceNativeMethods.SpDrvInfoDetailData {
            CompatibleIdsOffset = offset,
            CompatibleIdsLength = length
        };
    }

    private static string[] ReadCompatibleIds(
        byte[] buffer,
        int hardwareOffset,
        DeviceNativeMethods.SpDrvInfoDetailData detail) {
        IntPtr pointer = Marshal.AllocHGlobal(buffer.Length);
        try {
            Marshal.Copy(buffer, 0, pointer, buffer.Length);
            return NativeDeviceManagementApi.ReadCompatibleIds(pointer, buffer.Length, hardwareOffset, detail);
        } finally {
            Marshal.FreeHGlobal(pointer);
        }
    }
}
