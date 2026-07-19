using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal sealed partial class NativeDeviceManagementApi {
    public IReadOnlyList<DesktopDeviceInfo> GetDevices(DesktopDeviceQuery query) {
        if (!string.IsNullOrWhiteSpace(query.InstanceId)) {
            string instanceId = query.InstanceId!;
            try {
                using SafeDeviceInfoSetHandle exactDeviceInfoSet = OpenDevice(
                    instanceId,
                    out DeviceNativeMethods.SpDevInfoData exactDeviceInfoData);
                DesktopDeviceInfo exactDevice = ReadDevice(exactDeviceInfoSet, ref exactDeviceInfoData);
                if (!Matches(exactDevice, query)) {
                    return Array.Empty<DesktopDeviceInfo>();
                }
                PopulateOptionalDetails(exactDevice, exactDeviceInfoSet, ref exactDeviceInfoData, query);
                return new[] { exactDevice };
            } catch (Win32Exception exception) when (
                exception.NativeErrorCode == DeviceNativeMethods.SpapiErrorNoSuchDeviceInstance ||
                exception.NativeErrorCode == DeviceNativeMethods.ErrorNotFound) {
                return Array.Empty<DesktopDeviceInfo>();
            }
        }

        using SafeDeviceInfoSetHandle deviceInfoSet = OpenAllDevices(query.EnumeratorName);
        var devices = new List<DesktopDeviceInfo>();
        for (uint index = 0; ; index++) {
            DeviceNativeMethods.SpDevInfoData deviceInfoData = DeviceNativeMethods.SpDevInfoData.Create();
            if (!DeviceNativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData)) {
                int error = Marshal.GetLastWin32Error();
                if (error == DeviceNativeMethods.ErrorNoMoreItems) {
                    break;
                }
                throw new Win32Exception(error);
            }

            DesktopDeviceInfo device = ReadDevice(deviceInfoSet, ref deviceInfoData);
            if (Matches(device, query)) {
                PopulateOptionalDetails(device, deviceInfoSet, ref deviceInfoData, query);
                devices.Add(device);
            }
        }
        return devices
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(device => device.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DesktopDeviceInfo ReadDevice(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData) {
        string instanceId = GetInstanceId(deviceInfoSet, ref deviceInfoData);
        string? description = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Description);
        string? friendlyName = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.FriendlyName);
        uint status = Property<uint>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DevNodeStatus);
        uint problem = Property<uint>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.ProblemCode);
        bool present = Property<bool?>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.IsPresent) ?? false;

        Guid? containerId = Property<Guid?>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.ContainerId) ??
            Property<Guid?>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.BaseContainerId);
        var device = new DesktopDeviceInfo {
            InstanceId = instanceId,
            Name = friendlyName ?? description ?? instanceId,
            Description = description,
            Manufacturer = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Manufacturer),
            ClassName = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.ClassName),
            ClassGuid = deviceInfoData.ClassGuid,
            EnumeratorName = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.EnumeratorName),
            Location = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Location),
            ContainerId = NormalizeContainerId(containerId),
            Present = present,
            HasProblem = problem != 0 || (status & DeviceHasProblemStatus) != 0,
            ProblemCode = problem,
            StatusFlags = status,
            Capabilities = Property<uint>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Capabilities),
            HardwareIds = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.HardwareIds),
            CompatibleIds = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.CompatibleIds),
            Driver = ReadInstalledDriver(deviceInfoSet, ref deviceInfoData)
        };

        return device;
    }

    private static void PopulateOptionalDetails(
        DesktopDeviceInfo device,
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData,
        DesktopDeviceQuery query) {
        if (query.IncludeRelations) {
            device.Relations = new DesktopDeviceRelations {
                Parent = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Parent),
                Children = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Children),
                Siblings = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Siblings),
                Bus = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.BusRelations),
                Removal = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.RemovalRelations),
                Ejection = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.EjectionRelations),
                Power = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.PowerRelations)
            };
        }
        if (query.IncludeStack) {
            device.Stack = StringListProperty(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.Stack);
        }
        if (query.IncludeResources) {
            device.Resources = ReadResources(deviceInfoData.DevInst);
        }
        if (query.IncludeInterfaces) {
            device.Interfaces = ReadInterfaces(device.InstanceId);
        }
        if (query.IncludeProperties) {
            device.Properties = DevicePropertyReader.GetAll(deviceInfoSet, ref deviceInfoData);
        }
    }

    private static DesktopInstalledDriverInfo? ReadInstalledDriver(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref DeviceNativeMethods.SpDevInfoData deviceInfoData) {
        string? inf = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverInfPath);
        string? description = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverDescription);
        if (string.IsNullOrWhiteSpace(inf) && string.IsNullOrWhiteSpace(description)) {
            return null;
        }
        return new DesktopInstalledDriverInfo {
            PublishedInfName = inf,
            InfSection = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverInfSection),
            Description = description,
            Provider = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverProvider),
            Version = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverVersion),
            Date = Property<DateTime?>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverDate),
            MatchingDeviceId = Property<string>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.MatchingDeviceId),
            Rank = Property<uint?>(deviceInfoSet, ref deviceInfoData, DevicePropertyKeys.DriverRank)
        };
    }

    private static bool Matches(DesktopDeviceInfo device, DesktopDeviceQuery query) {
        if (!EqualsOrdinal(device.InstanceId, query.InstanceId) ||
            !EqualsOrdinal(device.ClassName, query.ClassName) ||
            !EqualsOrdinal(device.EnumeratorName, query.EnumeratorName)) {
            return false;
        }
        if (query.ClassGuid.HasValue && device.ClassGuid != query.ClassGuid.Value) {
            return false;
        }
        if (query.Present.HasValue && device.Present != query.Present.Value) {
            return false;
        }
        if (query.HasProblem.HasValue && device.HasProblem != query.HasProblem.Value) {
            return false;
        }
        if (query.ProblemCode.HasValue && device.ProblemCode != query.ProblemCode.Value) {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(query.DeviceId) &&
            !device.HardwareIds.Concat(device.CompatibleIds).Any(id => EqualsOrdinal(id, query.DeviceId))) {
            return false;
        }
        return true;
    }

    private static bool EqualsOrdinal(string? actual, string? expected) {
        return string.IsNullOrWhiteSpace(expected) || string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static Guid? NormalizeContainerId(Guid? value) {
        if (!value.HasValue || value.Value == Guid.Empty ||
            value.Value == new Guid("00000000-0000-0000-ffff-ffffffffffff")) {
            return null;
        }
        return value;
    }
}
