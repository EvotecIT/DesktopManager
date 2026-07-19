using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

internal sealed partial class NativeDeviceManagementApi {
    private const uint DevPropTypeEmpty = 0x00000000;
    private const uint DevPropTypeStringList = 0x00002012;

    public DesktopDeviceOperationResult EnableDevice(string instanceId) {
        uint locate = DeviceNativeMethods.CM_Locate_DevNodeW(
            out uint deviceInstance,
            instanceId,
            DeviceNativeMethods.CmLocateDevNodePhantom);
        if (locate != DeviceNativeMethods.CrSuccess) {
            return ConfigurationManagerResult("EnableDevice", instanceId, locate, changed: false);
        }
        uint result = DeviceNativeMethods.CM_Enable_DevNode(deviceInstance, 0);
        return ConfigurationManagerResult("EnableDevice", instanceId, result);
    }

    public DesktopDeviceOperationResult DisableDevice(string instanceId, bool force, bool persist) {
        uint locate = DeviceNativeMethods.CM_Locate_DevNodeW(
            out uint deviceInstance,
            instanceId,
            DeviceNativeMethods.CmLocateDevNodePhantom);
        if (locate != DeviceNativeMethods.CrSuccess) {
            return ConfigurationManagerResult("DisableDevice", instanceId, locate, changed: false);
        }
        uint flags = DeviceNativeMethods.CmDisableUiNotOk;
        if (force) {
            flags |= DeviceNativeMethods.CmDisableAbsolute;
        }
        if (persist) {
            flags |= DeviceNativeMethods.CmDisablePersist;
        }
        uint result = DeviceNativeMethods.CM_Disable_DevNode(deviceInstance, flags);
        return ConfigurationManagerResult("DisableDevice", instanceId, result);
    }

    public DesktopDeviceOperationResult RestartDevice(string instanceId) {
        using SafeDeviceInfoSetHandle deviceInfoSet = OpenDevice(instanceId, out DeviceNativeMethods.SpDevInfoData deviceInfoData);
        DeviceNativeMethods.SpPropChangeParams parameters =
            DeviceNativeMethods.SpPropChangeParams.Create(DeviceNativeMethods.DicsPropertyChange);
        if (!DeviceNativeMethods.SetupDiSetClassInstallParamsW(
            deviceInfoSet,
            ref deviceInfoData,
            ref parameters,
            (uint)Marshal.SizeOf(typeof(DeviceNativeMethods.SpPropChangeParams)))) {
            return Win32Result("RestartDevice", instanceId, succeeded: false);
        }
        bool succeeded = DeviceNativeMethods.SetupDiCallClassInstaller(
            DeviceNativeMethods.DifPropertyChange,
            deviceInfoSet,
            ref deviceInfoData);
        if (!succeeded) {
            return Win32Result("RestartDevice", instanceId, succeeded: false);
        }

        DeviceNativeMethods.SpDevInstallParams installParameters = DeviceNativeMethods.SpDevInstallParams.Create();
        if (!DeviceNativeMethods.SetupDiGetDeviceInstallParamsW(
            deviceInfoSet,
            ref deviceInfoData,
            ref installParameters)) {
            int error = Marshal.GetLastWin32Error();
            return new DesktopDeviceOperationResult {
                Operation = "RestartDevice",
                Target = instanceId,
                Succeeded = true,
                Changed = null,
                RebootRequired = null,
                Win32Error = error,
                Message = $"Windows accepted the restart request, but its reboot requirement could not be read: {new Win32Exception(error).Message}"
            };
        }
        bool rebootRequired = (installParameters.Flags &
            (DeviceNativeMethods.DiNeedReboot | DeviceNativeMethods.DiNeedRestart)) != 0;
        return Win32Result("RestartDevice", instanceId, succeeded: true, rebootRequired);
    }

    public DesktopDeviceOperationResult RemoveDevice(string instanceId, bool removeSubtree) {
        if (removeSubtree) {
            uint locate = DeviceNativeMethods.CM_Locate_DevNodeW(
                out uint deviceInstance,
                instanceId,
                DeviceNativeMethods.CmLocateDevNodePhantom);
            if (locate != DeviceNativeMethods.CrSuccess) {
                return ConfigurationManagerResult("RemoveDeviceSubtree", instanceId, locate, changed: false);
            }
            var vetoName = new StringBuilder(512);
            uint result = DeviceNativeMethods.CM_Query_And_Remove_SubTreeW(
                deviceInstance,
                out uint vetoType,
                vetoName,
                (uint)vetoName.Capacity,
                DeviceNativeMethods.CmRemoveUiNotOk | DeviceNativeMethods.CmRemoveNoRestart);
            DesktopDeviceOperationResult operation = ConfigurationManagerResult(
                "RemoveDeviceSubtree",
                instanceId,
                result);
            if (!operation.Succeeded) {
                operation.VetoType = GetVetoTypeName(vetoType);
                operation.VetoName = vetoName.ToString();
            }
            return operation;
        }

        using SafeDeviceInfoSetHandle deviceInfoSet = OpenDevice(instanceId, out DeviceNativeMethods.SpDevInfoData deviceInfoData);
        bool succeeded = DeviceNativeMethods.DiUninstallDevice(
            IntPtr.Zero,
            deviceInfoSet,
            ref deviceInfoData,
            0,
            out bool rebootRequired);
        return Win32Result("RemoveDevice", instanceId, succeeded, rebootRequired);
    }

    public DesktopDeviceOperationResult ScanDevices(string? instanceId, bool asynchronous) {
        uint locate = DeviceNativeMethods.CM_Locate_DevNodeW(
            out uint deviceInstance,
            string.IsNullOrWhiteSpace(instanceId) ? null : instanceId,
            DeviceNativeMethods.CmLocateDevNodeNormal);
        string target = instanceId ?? "ROOT";
        if (locate != DeviceNativeMethods.CrSuccess) {
            return ConfigurationManagerResult("ScanDevices", target, locate, changed: false);
        }
        uint flags = asynchronous
            ? DeviceNativeMethods.CmReenumerateAsynchronous
            : DeviceNativeMethods.CmReenumerateSynchronous;
        uint result = DeviceNativeMethods.CM_Reenumerate_DevNode(deviceInstance, flags);
        return ConfigurationManagerResult("ScanDevices", target, result);
    }

    public DesktopDeviceOperationResult StageDriver(string infPath) {
        var destination = new StringBuilder(1024);
        bool succeeded = DeviceNativeMethods.SetupCopyOEMInfW(
            infPath,
            Path.GetDirectoryName(infPath),
            DeviceNativeMethods.SpostPath,
            0,
            destination,
            (uint)destination.Capacity,
            out _,
            IntPtr.Zero);
        DesktopDeviceOperationResult result = Win32Result("StageDriver", infPath, succeeded);
        if (succeeded) {
            result.PublishedInfName = Path.GetFileName(destination.ToString());
        }
        return result;
    }

    public DesktopDeviceOperationResult InstallDriver(string infPath, bool force) {
        uint flags = force ? DeviceNativeMethods.DiirFlagForceInf : 0;
        bool succeeded = DeviceNativeMethods.DiInstallDriverW(
            IntPtr.Zero,
            infPath,
            flags,
            out bool rebootRequired);
        return Win32Result("InstallDriver", infPath, succeeded, rebootRequired);
    }

    public DesktopDeviceOperationResult UpdateDriver(string infPath, string hardwareId, bool force) {
        uint flags = DeviceNativeMethods.InstallFlagNonInteractive;
        if (force) {
            flags |= DeviceNativeMethods.InstallFlagForce;
        }
        bool succeeded = DeviceNativeMethods.UpdateDriverForPlugAndPlayDevicesW(
            IntPtr.Zero,
            hardwareId,
            infPath,
            flags,
            out bool rebootRequired);
        return Win32Result("UpdateDriver", hardwareId, succeeded, rebootRequired);
    }

    public DesktopDeviceOperationResult DeleteDriver(string publishedInfName, bool uninstallDevices, bool force) {
        string infPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "INF",
            publishedInfName);
        bool succeeded;
        bool rebootRequired = false;
        if (uninstallDevices) {
            succeeded = DeviceNativeMethods.DiUninstallDriverW(
                IntPtr.Zero,
                infPath,
                0,
                out rebootRequired);
        } else {
            succeeded = DeviceNativeMethods.SetupUninstallOEMInfW(
                publishedInfName,
                force ? DeviceNativeMethods.SuoiForceDelete : 0,
                IntPtr.Zero);
        }
        return Win32Result("DeleteDriver", publishedInfName, succeeded, rebootRequired);
    }

    public DesktopDeviceOperationResult ExportDriver(string publishedInfName, string destinationDirectory, bool overwrite) {
        DesktopDriverPackageInfo? package = GetDriverPackages(new DesktopDriverPackageQuery {
            PublishedInfName = publishedInfName,
            IncludeFiles = true
        }).FirstOrDefault();
        if (package == null || string.IsNullOrWhiteSpace(package.DriverStoreInfPath)) {
            return new DesktopDeviceOperationResult {
                Operation = "ExportDriver",
                Target = publishedInfName,
                Succeeded = false,
                Changed = false,
                Message = "The published driver package was not found in the Driver Store."
            };
        }

        string sourceDirectory = Path.GetDirectoryName(package.DriverStoreInfPath)!;
        string packageDirectory = Path.Combine(
            destinationDirectory,
            Path.GetFileNameWithoutExtension(publishedInfName));
        Directory.CreateDirectory(packageDirectory);
        foreach (string sourceFile in package.Files) {
            string relativePath = sourceFile.Substring(sourceDirectory.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string destinationFile = Path.GetFullPath(Path.Combine(packageDirectory, relativePath));
            if (!destinationFile.StartsWith(
                Path.GetFullPath(packageDirectory) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("A package file resolved outside the export directory.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile, overwrite);
        }
        return DesktopDeviceOperationResult.Success(
            "ExportDriver",
            publishedInfName,
            message: $"Exported to '{packageDirectory}'.");
    }

    public DesktopDeviceOperationResult RollbackDriver(string instanceId) {
        using SafeDeviceInfoSetHandle deviceInfoSet = OpenDevice(instanceId, out DeviceNativeMethods.SpDevInfoData deviceInfoData);
        bool succeeded = DeviceNativeMethods.DiRollbackDriver(
            deviceInfoSet,
            ref deviceInfoData,
            IntPtr.Zero,
            0,
            out bool rebootRequired);
        return Win32Result("RollbackDriver", instanceId, succeeded, rebootRequired);
    }

    public DesktopDeviceOperationResult CreateRootDevice(string infPath, string hardwareId) {
        var className = new StringBuilder(256);
        if (!DeviceNativeMethods.SetupDiGetINFClassW(
            infPath,
            out Guid classGuid,
            className,
            (uint)className.Capacity,
            out _)) {
            return Win32Result("CreateRootDevice", hardwareId, succeeded: false);
        }
        using SafeDeviceInfoSetHandle deviceInfoSet = DeviceNativeMethods.SetupDiCreateDeviceInfoList(
            ref classGuid,
            IntPtr.Zero);
        if (deviceInfoSet.IsInvalid) {
            return Win32Result("CreateRootDevice", hardwareId, succeeded: false);
        }

        DeviceNativeMethods.SpDevInfoData deviceInfoData = DeviceNativeMethods.SpDevInfoData.Create();
        if (!DeviceNativeMethods.SetupDiCreateDeviceInfoW(
            deviceInfoSet,
            className.ToString(),
            ref classGuid,
            null,
            IntPtr.Zero,
            DeviceNativeMethods.DicdGenerateId,
            ref deviceInfoData)) {
            return Win32Result("CreateRootDevice", hardwareId, succeeded: false);
        }
        using (NativeStringList hardwareIds = NativeStringList.Create(new[] { hardwareId })) {
            if (!DeviceNativeMethods.SetupDiSetDeviceRegistryPropertyW(
                deviceInfoSet,
                ref deviceInfoData,
                DeviceNativeMethods.SpdrpHardwareId,
                hardwareIds.Pointer,
                hardwareIds.ByteCount)) {
                return Win32Result("CreateRootDevice", hardwareId, succeeded: false);
            }
        }
        if (!DeviceNativeMethods.SetupDiCallClassInstaller(
            DeviceNativeMethods.DifRegisterDevice,
            deviceInfoSet,
            ref deviceInfoData)) {
            return Win32Result("CreateRootDevice", hardwareId, succeeded: false);
        }

        string? instanceId = null;
        DesktopDeviceOperationResult update;
        try {
            instanceId = GetInstanceId(deviceInfoSet, ref deviceInfoData);
            update = InstallCreatedDeviceDriver(
                deviceInfoSet,
                ref deviceInfoData,
                infPath,
                hardwareId);
        } catch (Exception exception) when (
            exception is Win32Exception ||
            exception is DllNotFoundException ||
            exception is EntryPointNotFoundException ||
            exception is BadImageFormatException) {
            update = new DesktopDeviceOperationResult {
                Operation = "CreateRootDevice",
                Target = hardwareId,
                Succeeded = false,
                Changed = null,
                Win32Error = (exception as Win32Exception)?.NativeErrorCode,
                Message = $"The ROOT device was registered, but post-registration setup failed: {exception.Message}"
            };
        }
        if (!update.Succeeded) {
            ApplyRootDeviceCleanup(update, deviceInfoSet, ref deviceInfoData, instanceId);
        } else {
            update.Changed = true;
        }
        update.Operation = "CreateRootDevice";
        update.Target = hardwareId;
        update.AffectedDeviceInstanceIds = instanceId == null ? Array.Empty<string>() : new[] { instanceId };
        return update;
    }

    private static void ApplyRootDeviceCleanup(
        DesktopDeviceOperationResult operation,
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        string? instanceId) {
        bool cleanupSucceeded = DeviceNativeMethods.DiUninstallDevice(
            IntPtr.Zero,
            deviceInfoSet,
            ref deviceInfoData,
            0,
            out bool cleanupRebootRequired);
        if (cleanupSucceeded && cleanupRebootRequired) {
            operation.Changed = true;
            operation.RebootRequired = true;
            operation.Message = $"{operation.Message} Windows accepted cleanup of the newly registered ROOT device, " +
                "but a restart is required to complete its removal.";
        } else if (cleanupSucceeded) {
            operation.Changed = false;
            operation.RebootRequired = false;
            operation.Message = $"{operation.Message} The newly registered ROOT device was removed after installation failed.";
        } else {
            int cleanupError = Marshal.GetLastWin32Error();
            string target = instanceId == null ? "The registered ROOT device" : $"Device instance '{instanceId}'";
            operation.Changed = null;
            operation.Message = $"{operation.Message} Cleanup failed: {new Win32Exception(cleanupError).Message} " +
                $"{target} may remain registered.";
        }
    }

    private static DesktopDeviceOperationResult InstallCreatedDeviceDriver(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        string infPath,
        string hardwareId) {
        DeviceNativeMethods.SpDevInstallParams installParameters = DeviceNativeMethods.SpDevInstallParams.Create();
        installParameters.Flags = DeviceNativeMethods.DiEnumSingleInf | DeviceNativeMethods.DiQuietInstall;
        installParameters.DriverPath = infPath;
        if (!DeviceNativeMethods.SetupDiSetDeviceInstallParamsW(
            deviceInfoSet,
            ref deviceInfoData,
            ref installParameters)) {
            return Win32Result("InstallRootDeviceDriver", hardwareId, succeeded: false);
        }
        if (!DeviceNativeMethods.SetupDiBuildDriverInfoList(
            deviceInfoSet,
            ref deviceInfoData,
            DeviceNativeMethods.SpditCompatibleDriver)) {
            return Win32Result("InstallRootDeviceDriver", hardwareId, succeeded: false);
        }

        try {
            if (!DeviceNativeMethods.SetupDiSelectBestCompatDrv(deviceInfoSet, ref deviceInfoData)) {
                return Win32Result("InstallRootDeviceDriver", hardwareId, succeeded: false);
            }
            if (!DeviceNativeMethods.SetupDiCallClassInstaller(
                DeviceNativeMethods.DifInstallDevice,
                deviceInfoSet,
                ref deviceInfoData)) {
                return Win32Result("InstallRootDeviceDriver", hardwareId, succeeded: false);
            }

            installParameters = DeviceNativeMethods.SpDevInstallParams.Create();
            if (!DeviceNativeMethods.SetupDiGetDeviceInstallParamsW(
                deviceInfoSet,
                ref deviceInfoData,
                ref installParameters)) {
                int error = Marshal.GetLastWin32Error();
                return new DesktopDeviceOperationResult {
                    Operation = "InstallRootDeviceDriver",
                    Target = hardwareId,
                    Succeeded = true,
                    Changed = true,
                    RebootRequired = null,
                    Win32Error = error,
                    Message = $"The ROOT device driver was installed, but its reboot requirement could not be read: {new Win32Exception(error).Message}"
                };
            }
            bool rebootRequired = (installParameters.Flags &
                (DeviceNativeMethods.DiNeedReboot | DeviceNativeMethods.DiNeedRestart)) != 0;
            return Win32Result("InstallRootDeviceDriver", hardwareId, succeeded: true, rebootRequired);
        } finally {
            DeviceNativeMethods.SetupDiDestroyDriverInfoList(
                deviceInfoSet,
                ref deviceInfoData,
                DeviceNativeMethods.SpditCompatibleDriver);
        }
    }

    public DesktopDeviceOperationResult SetRootHardwareIds(string instanceId, IReadOnlyList<string> hardwareIds) {
        using SafeDeviceInfoSetHandle deviceInfoSet = OpenDevice(instanceId, out DeviceNativeMethods.SpDevInfoData deviceInfoData);
        using NativeStringList values = NativeStringList.Create(hardwareIds);
        bool succeeded = DeviceNativeMethods.SetupDiSetDeviceRegistryPropertyW(
            deviceInfoSet,
            ref deviceInfoData,
            DeviceNativeMethods.SpdrpHardwareId,
            values.Pointer,
            values.ByteCount);
        return Win32Result("SetRootHardwareIds", instanceId, succeeded);
    }

    public DesktopDeviceOperationResult SetClassFilters(
        Guid classGuid,
        DesktopDeviceClassFilterKind kind,
        IReadOnlyList<string> serviceNames) {
        foreach (string serviceName in serviceNames) {
            using RegistryKey? service = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\" + serviceName,
                writable: false);
            if (service == null) {
                throw new InvalidOperationException($"Windows service '{serviceName}' does not exist.");
            }
        }

        DeviceNativeMethods.DevPropKey propertyKey = kind == DesktopDeviceClassFilterKind.Upper
            ? DevicePropertyKeys.ClassUpperFilters
            : DevicePropertyKeys.ClassLowerFilters;
        bool succeeded;
        if (serviceNames.Count == 0) {
            succeeded = DeviceNativeMethods.SetupDiSetClassPropertyW(
                ref classGuid,
                ref propertyKey,
                DevPropTypeEmpty,
                IntPtr.Zero,
                0,
                0);
        } else {
            using NativeStringList values = NativeStringList.Create(serviceNames);
            succeeded = DeviceNativeMethods.SetupDiSetClassPropertyW(
                ref classGuid,
                ref propertyKey,
                DevPropTypeStringList,
                values.Pointer,
                values.ByteCount,
                0);
        }
        return Win32Result("SetClassFilters", $"{classGuid:D}:{kind}", succeeded);
    }

    private static string GetVetoTypeName(uint vetoType) {
        string[] names = {
            "Unknown", "LegacyDevice", "PendingClose", "WindowsApp", "WindowsService",
            "OutstandingOpen", "Device", "Driver", "IllegalDeviceRequest", "InsufficientPower",
            "NonDisableable", "LegacyDriver", "InsufficientRights", "AlreadyRemoved"
        };
        return vetoType < names.Length ? names[vetoType] : $"Unknown({vetoType})";
    }

    private sealed class NativeStringList : IDisposable {
        internal IntPtr Pointer { get; private set; }
        internal uint ByteCount { get; private set; }

        private NativeStringList(IntPtr pointer, uint byteCount) {
            Pointer = pointer;
            ByteCount = byteCount;
        }

        internal static NativeStringList Create(IEnumerable<string> values) {
            string text = string.Join("\0", values) + "\0\0";
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            return new NativeStringList(pointer, checked((uint)bytes.Length));
        }

        public void Dispose() {
            if (Pointer != IntPtr.Zero) {
                Marshal.FreeHGlobal(Pointer);
                Pointer = IntPtr.Zero;
                ByteCount = 0;
            }
        }
    }
}
