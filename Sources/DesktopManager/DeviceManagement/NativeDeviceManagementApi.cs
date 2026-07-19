using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

internal sealed partial class NativeDeviceManagementApi : IDeviceManagementApi {
    private const uint DeviceHasProblemStatus = 0x00000400;

    private static SafeDeviceInfoSetHandle OpenAllDevices(string? enumerator = null) {
        SafeDeviceInfoSetHandle deviceInfoSet = DeviceNativeMethods.SetupDiGetClassDevsW(
            IntPtr.Zero,
            enumerator,
            IntPtr.Zero,
            DeviceNativeMethods.DigcfAllClasses);
        if (deviceInfoSet.IsInvalid) {
            ThrowLastWin32("SetupDiGetClassDevs");
        }
        return deviceInfoSet;
    }

    private static SafeDeviceInfoSetHandle OpenDevice(
        string instanceId,
        out DeviceNativeMethods.SpDevInfoData deviceInfoData) {
        SafeDeviceInfoSetHandle deviceInfoSet = OpenAllDevices();
        deviceInfoData = DeviceNativeMethods.SpDevInfoData.Create();
        if (!DeviceNativeMethods.SetupDiOpenDeviceInfoW(
            deviceInfoSet,
            instanceId,
            IntPtr.Zero,
            0,
            ref deviceInfoData)) {
            int error = Marshal.GetLastWin32Error();
            deviceInfoSet.Dispose();
            throw new Win32Exception(error, $"Unable to open device instance '{instanceId}'.");
        }
        return deviceInfoSet;
    }

    private static string GetInstanceId(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData) {
        DeviceNativeMethods.SetupDiGetDeviceInstanceIdW(
            deviceInfoSet,
            ref deviceInfoData,
            null,
            0,
            out uint requiredSize);
        if (requiredSize == 0) {
            ThrowLastWin32("SetupDiGetDeviceInstanceId");
        }
        var instanceId = new StringBuilder(checked((int)requiredSize));
        if (!DeviceNativeMethods.SetupDiGetDeviceInstanceIdW(
            deviceInfoSet,
            ref deviceInfoData,
            instanceId,
            requiredSize,
            out _)) {
            ThrowLastWin32("SetupDiGetDeviceInstanceId");
        }
        return instanceId.ToString();
    }

    private static DesktopDeviceOperationResult ConfigurationManagerResult(
        string operation,
        string target,
        uint configurationManagerCode,
        bool? changed = null) {
        if (configurationManagerCode == DeviceNativeMethods.CrSuccess ||
            configurationManagerCode == DeviceNativeMethods.CrNeedRestart) {
            return new DesktopDeviceOperationResult {
                Operation = operation,
                Target = target,
                Succeeded = true,
                Changed = changed,
                RebootRequired = configurationManagerCode == DeviceNativeMethods.CrNeedRestart,
                ConfigurationManagerCode = configurationManagerCode,
                Message = configurationManagerCode == DeviceNativeMethods.CrNeedRestart
                    ? "Windows requires a restart to finish the operation."
                    : "The native Configuration Manager operation completed successfully."
            };
        }
        int error = unchecked((int)DeviceNativeMethods.CM_MapCrToWin32Err(configurationManagerCode, 31));
        return new DesktopDeviceOperationResult {
            Operation = operation,
            Target = target,
            Succeeded = false,
            Changed = changed,
            ConfigurationManagerCode = configurationManagerCode,
            Win32Error = error,
            Message = new Win32Exception(error).Message
        };
    }

    private static DesktopDeviceOperationResult Win32Result(
        string operation,
        string target,
        bool succeeded,
        bool? rebootRequired = false,
        string? message = null) {
        int error = succeeded ? 0 : Marshal.GetLastWin32Error();
        return new DesktopDeviceOperationResult {
            Operation = operation,
            Target = target,
            Succeeded = succeeded,
            Changed = null,
            RebootRequired = rebootRequired,
            Win32Error = succeeded ? null : error,
            Message = message ?? (succeeded ? "The native Windows operation completed successfully." : new Win32Exception(error).Message)
        };
    }

    private static void ThrowLastWin32(string operation) {
        int error = Marshal.GetLastWin32Error();
        throw new Win32Exception(error, $"{operation} failed: {new Win32Exception(error).Message}");
    }

    private static T? Property<T>(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        DeviceNativeMethods.DevPropKey key) {
        object? value = DevicePropertyReader.Get(deviceInfoSet, ref deviceInfoData, key);
        return value is T typed ? typed : default;
    }

    private static IReadOnlyList<string> StringListProperty(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        DeviceNativeMethods.DevPropKey key) {
        object? value = DevicePropertyReader.Get(deviceInfoSet, ref deviceInfoData, key);
        return value is string[] values ? values : Array.Empty<string>();
    }
}
