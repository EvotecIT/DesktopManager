using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

internal sealed partial class NativeDeviceManagementApi {
    public IReadOnlyList<DesktopDeviceDriverInfo> GetCompatibleDrivers(string instanceId) {
        using SafeDeviceInfoSetHandle deviceInfoSet = OpenDevice(instanceId, out DeviceNativeMethods.SpDevInfoData deviceInfoData);
        if (!DeviceNativeMethods.SetupDiBuildDriverInfoList(
            deviceInfoSet,
            ref deviceInfoData,
            DeviceNativeMethods.SpditCompatibleDriver)) {
            ThrowLastWin32("SetupDiBuildDriverInfoList");
        }

        try {
            var drivers = new List<DesktopDeviceDriverInfo>();
            for (uint index = 0; ; index++) {
                DeviceNativeMethods.SpDrvInfoData driverInfo = DeviceNativeMethods.SpDrvInfoData.Create();
                if (!DeviceNativeMethods.SetupDiEnumDriverInfoW(
                    deviceInfoSet,
                    ref deviceInfoData,
                    DeviceNativeMethods.SpditCompatibleDriver,
                    index,
                    ref driverInfo)) {
                    int error = Marshal.GetLastWin32Error();
                    if (error == DeviceNativeMethods.ErrorNoMoreItems) {
                        break;
                    }
                    throw new System.ComponentModel.Win32Exception(error);
                }

                DeviceNativeMethods.SpDriverInstallParams installParams =
                    DeviceNativeMethods.SpDriverInstallParams.Create();
                DeviceNativeMethods.SetupDiGetDriverInstallParamsW(
                    deviceInfoSet,
                    ref deviceInfoData,
                    ref driverInfo,
                    ref installParams);
                DriverDetail detail = ReadDriverDetail(deviceInfoSet, ref deviceInfoData, ref driverInfo);
                drivers.Add(new DesktopDeviceDriverInfo {
                    InstanceId = instanceId,
                    Description = driverInfo.Description ?? string.Empty,
                    Manufacturer = driverInfo.ManufacturerName,
                    Provider = driverInfo.ProviderName,
                    Date = FileTimeToDateTime(driverInfo.DriverDate),
                    Version = FormatDriverVersion(driverInfo.DriverVersion),
                    Rank = installParams.Rank,
                    Flags = installParams.Flags,
                    InfPath = detail.InfPath,
                    InfSection = detail.InfSection,
                    HardwareId = detail.HardwareId,
                    CompatibleIds = detail.CompatibleIds
                });
            }
            return drivers.OrderBy(driver => driver.Rank).ThenByDescending(driver => driver.Date).ToArray();
        } finally {
            DeviceNativeMethods.SetupDiDestroyDriverInfoList(
                deviceInfoSet,
                ref deviceInfoData,
                DeviceNativeMethods.SpditCompatibleDriver);
        }
    }

    public IReadOnlyList<DesktopDriverPackageInfo> GetDriverPackages(DesktopDriverPackageQuery query) {
        string infDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "INF");
        IEnumerable<string> infPaths = Directory.Exists(infDirectory)
            ? Directory.EnumerateFiles(infDirectory, "oem*.inf", SearchOption.TopDirectoryOnly)
            : Enumerable.Empty<string>();
        if (!string.IsNullOrWhiteSpace(query.PublishedInfName)) {
            infPaths = infPaths.Where(path => string.Equals(
                Path.GetFileName(path),
                query.PublishedInfName,
                StringComparison.OrdinalIgnoreCase));
        }

        IReadOnlyList<DesktopDeviceInfo>? devices = query.IncludeDevices
            ? GetDevices(new DesktopDeviceQuery())
            : null;
        var packages = new List<DesktopDriverPackageInfo>();
        foreach (string infPath in infPaths) {
            DesktopDriverPackageInfo? package = ReadDriverPackage(infPath, query, devices);
            if (package != null && (!query.ClassGuid.HasValue || package.ClassGuid == query.ClassGuid.Value)) {
                packages.Add(package);
            }
        }
        return packages.OrderBy(package => package.PublishedInfName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<DesktopDeviceClassInfo> GetDeviceClasses() {
        DeviceNativeMethods.SetupDiBuildClassInfoList(
            DeviceNativeMethods.DibciNoInstallClass | DeviceNativeMethods.DibciNoDisplayClass,
            null,
            0,
            out uint requiredSize);
        if (requiredSize == 0) {
            return Array.Empty<DesktopDeviceClassInfo>();
        }
        var classGuids = new Guid[requiredSize];
        if (!DeviceNativeMethods.SetupDiBuildClassInfoList(
            DeviceNativeMethods.DibciNoInstallClass | DeviceNativeMethods.DibciNoDisplayClass,
            classGuids,
            requiredSize,
            out uint returnedSize)) {
            ThrowLastWin32("SetupDiBuildClassInfoList");
        }

        var classes = new List<DesktopDeviceClassInfo>(checked((int)returnedSize));
        for (int index = 0; index < returnedSize; index++) {
            Guid classGuid = classGuids[index];
            classes.Add(new DesktopDeviceClassInfo {
                ClassGuid = classGuid,
                Name = ReadClassName(classGuid),
                Description = ReadClassDescription(classGuid),
                DefaultService = DevicePropertyReader.GetClass(classGuid, DevicePropertyKeys.ClassDefaultService) as string,
                UpperFilters = DevicePropertyReader.GetClass(classGuid, DevicePropertyKeys.ClassUpperFilters) as string[] ?? Array.Empty<string>(),
                LowerFilters = DevicePropertyReader.GetClass(classGuid, DevicePropertyKeys.ClassLowerFilters) as string[] ?? Array.Empty<string>()
            });
        }
        return classes.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static DriverDetail ReadDriverDetail(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        ref DeviceNativeMethods.SpDrvInfoData driverInfo) {
        DeviceNativeMethods.SetupDiGetDriverInfoDetailW(
            deviceInfoSet,
            ref deviceInfoData,
            ref driverInfo,
            IntPtr.Zero,
            0,
            out uint requiredSize);
        int baseSize = Marshal.SizeOf(typeof(DeviceNativeMethods.SpDrvInfoDetailData));
        int bufferSize = Math.Max(baseSize, checked((int)requiredSize));
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try {
            Marshal.WriteInt32(buffer, baseSize);
            if (!DeviceNativeMethods.SetupDiGetDriverInfoDetailW(
                deviceInfoSet,
                ref deviceInfoData,
                ref driverInfo,
                buffer,
                (uint)bufferSize,
                out _)) {
                return new DriverDetail();
            }
            var native = (DeviceNativeMethods.SpDrvInfoDetailData)Marshal.PtrToStructure(
                buffer,
                typeof(DeviceNativeMethods.SpDrvInfoDetailData))!;
            int hardwareOffset = Marshal.OffsetOf(
                typeof(DeviceNativeMethods.SpDrvInfoDetailData),
                nameof(DeviceNativeMethods.SpDrvInfoDetailData.HardwareId)).ToInt32();
            string? hardwareId = Marshal.PtrToStringUni(IntPtr.Add(buffer, hardwareOffset));
            string[] compatibleIds = ReadCompatibleIds(buffer, hardwareOffset, native);
            return new DriverDetail {
                InfPath = native.InfFileName,
                InfSection = native.SectionName,
                HardwareId = hardwareId,
                CompatibleIds = compatibleIds
            };
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string[] ReadCompatibleIds(
        IntPtr buffer,
        int hardwareOffset,
        DeviceNativeMethods.SpDrvInfoDetailData detail) {
        if (detail.CompatibleIdsLength == 0) {
            return Array.Empty<string>();
        }
        IntPtr compatibleIds = IntPtr.Add(buffer, checked(hardwareOffset + (int)detail.CompatibleIdsOffset * 2));
        string text = Marshal.PtrToStringUni(compatibleIds, checked((int)detail.CompatibleIdsLength)) ?? string.Empty;
        return text.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static DesktopDriverPackageInfo? ReadDriverPackage(
        string infPath,
        DesktopDriverPackageQuery query,
        IReadOnlyList<DesktopDeviceInfo>? devices) {
        if (!TryGetInfClass(infPath, out Guid classGuid, out string? className)) {
            return null;
        }
        GetOriginalInfNames(infPath, out string? originalInfName, out string? catalogName);
        string? driverStorePath = GetDriverStoreInfPath(infPath);
        ParseDriverVersion(ReadInfValue(infPath, "DriverVer"), out DateTime? driverDate, out string? driverVersion);
        string publishedName = Path.GetFileName(infPath);
        string[] files = query.IncludeFiles && !string.IsNullOrWhiteSpace(driverStorePath)
            ? GetPackageFiles(driverStorePath!)
            : Array.Empty<string>();
        string[] deviceIds = devices == null
            ? Array.Empty<string>()
            : devices.Where(device => string.Equals(
                    device.Driver?.PublishedInfName,
                    publishedName,
                    StringComparison.OrdinalIgnoreCase))
                .Select(device => device.InstanceId)
                .ToArray();
        return new DesktopDriverPackageInfo {
            PublishedInfName = publishedName,
            OriginalInfName = originalInfName,
            CatalogName = catalogName,
            Provider = ReadInfValue(infPath, "Provider"),
            ClassName = className,
            ClassGuid = classGuid,
            DriverDate = driverDate,
            DriverVersion = driverVersion,
            DriverStoreInfPath = driverStorePath,
            Files = files,
            DeviceInstanceIds = deviceIds
        };
    }

    private static bool TryGetInfClass(string infPath, out Guid classGuid, out string? className) {
        var name = new StringBuilder(256);
        bool succeeded = DeviceNativeMethods.SetupDiGetINFClassW(
            infPath,
            out classGuid,
            name,
            (uint)name.Capacity,
            out _);
        className = succeeded ? name.ToString() : null;
        return succeeded;
    }

    private static void GetOriginalInfNames(string infPath, out string? originalInfName, out string? catalogName) {
        originalInfName = null;
        catalogName = null;
        DeviceNativeMethods.SetupGetInfInformationW(
            infPath,
            DeviceNativeMethods.InfInfoNameIsAbsolute,
            IntPtr.Zero,
            0,
            out uint requiredSize);
        if (requiredSize == 0) {
            return;
        }
        IntPtr buffer = Marshal.AllocHGlobal(checked((int)requiredSize));
        try {
            if (!DeviceNativeMethods.SetupGetInfInformationW(
                infPath,
                DeviceNativeMethods.InfInfoNameIsAbsolute,
                buffer,
                requiredSize,
                out _)) {
                return;
            }
            DeviceNativeMethods.SpOriginalFileInfo original = DeviceNativeMethods.SpOriginalFileInfo.Create();
            if (DeviceNativeMethods.SetupQueryInfOriginalFileInformationW(
                buffer,
                0,
                IntPtr.Zero,
                ref original)) {
                originalInfName = original.OriginalInfName;
                catalogName = original.OriginalCatalogName;
            }
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? GetDriverStoreInfPath(string infPath) {
        DeviceNativeMethods.SetupGetInfDriverStoreLocationW(
            infPath,
            IntPtr.Zero,
            null,
            null,
            0,
            out uint requiredSize);
        if (requiredSize == 0) {
            return null;
        }
        var path = new StringBuilder(checked((int)requiredSize));
        return DeviceNativeMethods.SetupGetInfDriverStoreLocationW(
            infPath,
            IntPtr.Zero,
            null,
            path,
            requiredSize,
            out _)
            ? path.ToString()
            : null;
    }

    private static string? ReadInfValue(string infPath, string key) {
        using SafeInfHandle infHandle = DeviceNativeMethods.SetupOpenInfFileW(
            infPath,
            null,
            DeviceNativeMethods.InfStyleWin4,
            out _);
        if (infHandle.IsInvalid) {
            return null;
        }
        DeviceNativeMethods.SetupGetLineTextW(
            IntPtr.Zero,
            infHandle,
            "Version",
            key,
            null,
            0,
            out uint requiredSize);
        if (requiredSize == 0) {
            return null;
        }
        var value = new StringBuilder(checked((int)requiredSize));
        return DeviceNativeMethods.SetupGetLineTextW(
            IntPtr.Zero,
            infHandle,
            "Version",
            key,
            value,
            requiredSize,
            out _)
            ? value.ToString()
            : null;
    }

    private static void ParseDriverVersion(string? value, out DateTime? date, out string? version) {
        date = null;
        version = null;
        if (string.IsNullOrWhiteSpace(value)) {
            return;
        }
        string[] parts = value!.Split(new[] { ',' }, 2);
        if (DateTime.TryParse(parts[0].Trim(), CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out DateTime parsed)) {
            date = parsed;
        }
        if (parts.Length == 2) {
            version = parts[1].Trim();
        }
    }

    private static string[] GetPackageFiles(string driverStoreInfPath) {
        string? directory = Path.GetDirectoryName(driverStoreInfPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) {
            return Array.Empty<string>();
        }
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToArray();
    }

    private static string? ReadClassName(Guid classGuid) {
        var value = new StringBuilder(256);
        return DeviceNativeMethods.SetupDiClassNameFromGuidW(
            ref classGuid,
            value,
            (uint)value.Capacity,
            out _)
            ? value.ToString()
            : null;
    }

    private static string? ReadClassDescription(Guid classGuid) {
        var value = new StringBuilder(512);
        return DeviceNativeMethods.SetupDiGetClassDescriptionW(
            ref classGuid,
            value,
            (uint)value.Capacity,
            out _)
            ? value.ToString()
            : null;
    }

    private static DateTime? FileTimeToDateTime(System.Runtime.InteropServices.ComTypes.FILETIME fileTime) {
        long value = ((long)fileTime.dwHighDateTime << 32) | unchecked((uint)fileTime.dwLowDateTime);
        return value > 0 ? DateTime.FromFileTimeUtc(value) : null;
    }

    private static string FormatDriverVersion(ulong value) {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}.{1}.{2}.{3}",
            (value >> 48) & 0xFFFF,
            (value >> 32) & 0xFFFF,
            (value >> 16) & 0xFFFF,
            value & 0xFFFF);
    }

    private sealed class DriverDetail {
        internal string? InfPath { get; set; }
        internal string? InfSection { get; set; }
        internal string? HardwareId { get; set; }
        internal IReadOnlyList<string> CompatibleIds { get; set; } = Array.Empty<string>();
    }
}
