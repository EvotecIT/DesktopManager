using System.Runtime.InteropServices;

namespace DesktopManager;

internal static class DevicePropertyReader {
    private const uint TypeMask = 0x00000FFF;
    private const uint TypeByte = 0x00000003;
    private const uint TypeInt16 = 0x00000004;
    private const uint TypeUInt16 = 0x00000005;
    private const uint TypeInt32 = 0x00000006;
    private const uint TypeUInt32 = 0x00000007;
    private const uint TypeInt64 = 0x00000008;
    private const uint TypeUInt64 = 0x00000009;
    private const uint TypeFloat = 0x0000000A;
    private const uint TypeDouble = 0x0000000B;
    private const uint TypeGuid = 0x0000000D;
    private const uint TypeFileTime = 0x00000010;
    private const uint TypeBoolean = 0x00000011;
    private const uint TypeString = 0x00000012;
    private const uint TypeError = 0x00000017;
    private const uint TypeNtStatus = 0x00000018;
    private const uint ModifierArray = 0x00001000;
    private const uint ModifierList = 0x00002000;

    internal static object? Get(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        DeviceNativeMethods.DevPropKey propertyKey) {
        if (!TryGet(deviceInfoSet, ref deviceInfoData, propertyKey, out _, out object? value)) {
            return null;
        }
        return value;
    }

    internal static bool TryGet(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        DeviceNativeMethods.DevPropKey propertyKey,
        out uint propertyType,
        out object? value) {
        DeviceNativeMethods.DevPropKey key = propertyKey;
        DeviceNativeMethods.SetupDiGetDevicePropertyW(
            deviceInfoSet,
            ref deviceInfoData,
            ref key,
            out propertyType,
            IntPtr.Zero,
            0,
            out uint requiredSize,
            0);
        int error = Marshal.GetLastWin32Error();
        if (requiredSize == 0) {
            value = null;
            return false;
        }
        if (error != DeviceNativeMethods.ErrorInsufficientBuffer) {
            value = null;
            return false;
        }

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)requiredSize));
        try {
            if (!DeviceNativeMethods.SetupDiGetDevicePropertyW(
                deviceInfoSet,
                ref deviceInfoData,
                ref key,
                out propertyType,
                buffer,
                requiredSize,
                out _,
                0)) {
                value = null;
                return false;
            }
            value = ConvertValue(propertyType, buffer, requiredSize);
            return true;
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    internal static IReadOnlyList<DesktopDevicePropertyInfo> GetAll(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData) {
        DeviceNativeMethods.SetupDiGetDevicePropertyKeys(
            deviceInfoSet,
            ref deviceInfoData,
            IntPtr.Zero,
            0,
            out uint requiredCount,
            0);
        if (requiredCount == 0) {
            return Array.Empty<DesktopDevicePropertyInfo>();
        }

        int keySize = Marshal.SizeOf(typeof(DeviceNativeMethods.DevPropKey));
        IntPtr keys = Marshal.AllocHGlobal(checked((int)requiredCount * keySize));
        try {
            if (!DeviceNativeMethods.SetupDiGetDevicePropertyKeys(
                deviceInfoSet,
                ref deviceInfoData,
                keys,
                requiredCount,
                out uint returnedCount,
                0)) {
                return Array.Empty<DesktopDevicePropertyInfo>();
            }
            var properties = new List<DesktopDevicePropertyInfo>(checked((int)returnedCount));
            for (int index = 0; index < returnedCount; index++) {
                IntPtr address = IntPtr.Add(keys, index * keySize);
                var key = (DeviceNativeMethods.DevPropKey)Marshal.PtrToStructure(
                    address,
                    typeof(DeviceNativeMethods.DevPropKey))!;
                if (TryGet(deviceInfoSet, ref deviceInfoData, key, out uint propertyType, out object? value)) {
                    properties.Add(new DesktopDevicePropertyInfo {
                        Key = key.ToString(),
                        PropertyType = propertyType,
                        Value = value
                    });
                }
            }
            return properties;
        } finally {
            Marshal.FreeHGlobal(keys);
        }
    }

    internal static object? GetClass(Guid classGuid, DeviceNativeMethods.DevPropKey propertyKey) {
        DeviceNativeMethods.DevPropKey key = propertyKey;
        DeviceNativeMethods.SetupDiGetClassPropertyW(
            ref classGuid,
            ref key,
            out uint propertyType,
            IntPtr.Zero,
            0,
            out uint requiredSize,
            0);
        if (requiredSize == 0 || Marshal.GetLastWin32Error() != DeviceNativeMethods.ErrorInsufficientBuffer) {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)requiredSize));
        try {
            if (!DeviceNativeMethods.SetupDiGetClassPropertyW(
                ref classGuid,
                ref key,
                out propertyType,
                buffer,
                requiredSize,
                out _,
                0)) {
                return null;
            }
            return ConvertValue(propertyType, buffer, requiredSize);
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static object? ConvertValue(uint propertyType, IntPtr buffer, uint size) {
        uint type = propertyType & TypeMask;
        uint modifier = propertyType & ~TypeMask;
        if (modifier == ModifierList && type == TypeString) {
            return ReadMultiString(buffer, size);
        }
        if (modifier == ModifierArray || (modifier != 0 && modifier != ModifierList)) {
            return ReadBytes(buffer, size);
        }

        switch (type) {
            case TypeByte:
                return Marshal.ReadByte(buffer);
            case TypeInt16:
                return Marshal.ReadInt16(buffer);
            case TypeUInt16:
                return unchecked((ushort)Marshal.ReadInt16(buffer));
            case TypeInt32:
            case TypeError:
            case TypeNtStatus:
                return Marshal.ReadInt32(buffer);
            case TypeUInt32:
                return unchecked((uint)Marshal.ReadInt32(buffer));
            case TypeInt64:
                return Marshal.ReadInt64(buffer);
            case TypeUInt64:
                return unchecked((ulong)Marshal.ReadInt64(buffer));
            case TypeFloat:
                return BitConverter.ToSingle(ReadBytes(buffer, size), 0);
            case TypeDouble:
                return BitConverter.ToDouble(ReadBytes(buffer, size), 0);
            case TypeGuid:
                return (Guid)Marshal.PtrToStructure(buffer, typeof(Guid))!;
            case TypeFileTime:
                long fileTime = Marshal.ReadInt64(buffer);
                return fileTime <= 0 ? null : DateTime.FromFileTimeUtc(fileTime);
            case TypeBoolean:
                return Marshal.ReadByte(buffer) != 0;
            case TypeString:
                return Marshal.PtrToStringUni(buffer)?.TrimEnd('\0');
            default:
                byte[] bytes = ReadBytes(buffer, size);
                return Convert.ToBase64String(bytes);
        }
    }

    private static byte[] ReadBytes(IntPtr buffer, uint size) {
        var bytes = new byte[checked((int)size)];
        Marshal.Copy(buffer, bytes, 0, bytes.Length);
        return bytes;
    }

    private static string[] ReadMultiString(IntPtr buffer, uint size) {
        string text = Marshal.PtrToStringUni(buffer, checked((int)size / 2)) ?? string.Empty;
        return text.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
