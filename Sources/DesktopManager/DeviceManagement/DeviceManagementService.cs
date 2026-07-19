using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides one typed owner for local Windows device instances, device classes, and Driver Store packages.
/// </summary>
[SupportedOSPlatform("windows10.0.15063.0")]
public sealed class DeviceManagementService {
    private readonly IDeviceManagementApi _api;
    private readonly Action _ensureElevated;

    /// <summary>Creates a service backed by documented Windows device-installation APIs.</summary>
    public DeviceManagementService() : this(new NativeDeviceManagementApi(), PrivilegeChecker.EnsureElevated) {
    }

    internal DeviceManagementService(IDeviceManagementApi api, Action? ensureElevated = null) {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _ensureElevated = ensureElevated ?? (() => { });
    }

    /// <summary>Gets local device instances matching the supplied query.</summary>
    public IReadOnlyList<DesktopDeviceInfo> GetDevices(DesktopDeviceQuery? query = null) {
        query ??= new DesktopDeviceQuery();
        query.Validate();
        return _api.GetDevices(query);
    }

    /// <summary>Gets one exact device instance with all supported detail families.</summary>
    public DesktopDeviceInfo GetDevice(string instanceId) {
        ValidateExactInstanceId(instanceId);
        var query = new DesktopDeviceQuery {
            InstanceId = instanceId,
            IncludeRelations = true,
            IncludeStack = true,
            IncludeResources = true,
            IncludeInterfaces = true,
            IncludeProperties = true
        };
        DesktopDeviceInfo? device = _api.GetDevices(query).FirstOrDefault();
        return device ?? throw new InvalidOperationException($"Device instance '{instanceId}' was not found.");
    }

    /// <summary>Gets drivers Windows considers compatible with an exact device instance.</summary>
    public IReadOnlyList<DesktopDeviceDriverInfo> GetCompatibleDrivers(string instanceId) {
        ValidateExactInstanceId(instanceId);
        return _api.GetCompatibleDrivers(instanceId);
    }

    /// <summary>Gets third-party packages from the Windows Driver Store.</summary>
    public IReadOnlyList<DesktopDriverPackageInfo> GetDriverPackages(DesktopDriverPackageQuery? query = null) {
        query ??= new DesktopDriverPackageQuery();
        if (!string.IsNullOrWhiteSpace(query.PublishedInfName)) {
            ValidatePublishedInfName(query.PublishedInfName!);
        }
        return _api.GetDriverPackages(query);
    }

    /// <summary>Gets installed Windows device setup classes and their class-filter chains.</summary>
    public IReadOnlyList<DesktopDeviceClassInfo> GetDeviceClasses() {
        return _api.GetDeviceClasses();
    }

    /// <summary>Gets device containers assembled from the selected devices.</summary>
    public IReadOnlyList<DesktopDeviceContainerInfo> GetDeviceContainers(DesktopDeviceQuery? query = null) {
        IReadOnlyList<DesktopDeviceInfo> devices = GetDevices(query);
        return devices
            .Where(device => device.ContainerId.HasValue)
            .GroupBy(device => device.ContainerId!.Value)
            .Select(group => new DesktopDeviceContainerInfo {
                ContainerId = group.Key,
                Connected = group.Any(device => device.Present),
                HasProblem = group.Any(device => device.HasProblem),
                Devices = group.OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ToArray()
            })
            .OrderBy(container => container.ContainerId)
            .ToArray();
    }

    /// <summary>Enables an exact device instance.</summary>
    public DesktopDeviceOperationResult EnableDevice(string instanceId) {
        ValidateExactInstanceId(instanceId);
        _ensureElevated();
        return _api.EnableDevice(instanceId);
    }

    /// <summary>Disables an exact device instance.</summary>
    public DesktopDeviceOperationResult DisableDevice(string instanceId, bool force = false, bool persist = true) {
        ValidateExactInstanceId(instanceId);
        _ensureElevated();
        return _api.DisableDevice(instanceId, force, persist);
    }

    /// <summary>Restarts an exact device instance without rebooting Windows.</summary>
    public DesktopDeviceOperationResult RestartDevice(string instanceId) {
        ValidateExactInstanceId(instanceId);
        _ensureElevated();
        return _api.RestartDevice(instanceId);
    }

    /// <summary>Uninstalls an exact device instance and optionally its present and non-present children.</summary>
    public DesktopDeviceOperationResult RemoveDevice(string instanceId, bool removeSubtree = true) {
        ValidateExactInstanceId(instanceId);
        _ensureElevated();
        return _api.RemoveDevice(instanceId, removeSubtree);
    }

    /// <summary>Requests Plug and Play re-enumeration for the whole machine or one device subtree.</summary>
    public DesktopDeviceOperationResult ScanDevices(string? instanceId = null, bool asynchronous = false) {
        if (!string.IsNullOrWhiteSpace(instanceId)) {
            ValidateExactInstanceId(instanceId!);
        }
        _ensureElevated();
        return _api.ScanDevices(instanceId, asynchronous);
    }

    /// <summary>Stages an INF-based driver package in the Driver Store without selecting it for devices.</summary>
    public DesktopDeviceOperationResult StageDriver(string infPath) {
        string fullPath = ValidateInfPath(infPath);
        _ensureElevated();
        return _api.StageDriver(fullPath);
    }

    /// <summary>Stages an INF package and installs it on matching present devices.</summary>
    public DesktopDeviceOperationResult InstallDriver(string infPath, bool force = false) {
        string fullPath = ValidateInfPath(infPath);
        _ensureElevated();
        return _api.InstallDriver(fullPath, force);
    }

    /// <summary>Updates present devices matching one exact hardware identifier from an INF package.</summary>
    public DesktopDeviceOperationResult UpdateDriver(string infPath, string hardwareId, bool force = false) {
        string fullPath = ValidateInfPath(infPath);
        ValidateHardwareId(hardwareId);
        _ensureElevated();
        return _api.UpdateDriver(fullPath, hardwareId, force);
    }

    /// <summary>Removes a published third-party driver package, optionally reassigning affected devices first.</summary>
    public DesktopDeviceOperationResult DeleteDriver(string publishedInfName, bool uninstallDevices = false, bool force = false) {
        ValidatePublishedInfName(publishedInfName);
        _ensureElevated();
        DesktopDeviceOperationResult result = _api.DeleteDriver(
            publishedInfName,
            uninstallDevices,
            force && !uninstallDevices);
        if (uninstallDevices && force) {
            const string forceNote = "Force is redundant when uninstalling from devices because the native package uninstall reassigns those devices before removing the package.";
            result.Message = string.IsNullOrWhiteSpace(result.Message)
                ? forceNote
                : result.Message + " " + forceNote;
        }
        return result;
    }

    /// <summary>Exports one third-party package from the Driver Store.</summary>
    public DesktopDeviceOperationResult ExportDriver(string publishedInfName, string destinationDirectory, bool overwrite = false) {
        ValidatePublishedInfName(publishedInfName);
        if (string.IsNullOrWhiteSpace(destinationDirectory)) {
            throw new ArgumentException("A destination directory is required.", nameof(destinationDirectory));
        }
        string fullDestination = Path.GetFullPath(destinationDirectory);
        _ensureElevated();
        return _api.ExportDriver(publishedInfName, fullDestination, overwrite);
    }

    /// <summary>Rolls an exact device instance back to its previous installed driver.</summary>
    public DesktopDeviceOperationResult RollbackDriver(string instanceId) {
        ValidateExactInstanceId(instanceId);
        _ensureElevated();
        return _api.RollbackDriver(instanceId);
    }

    /// <summary>Creates a root-enumerated device and installs an INF package for it.</summary>
    public DesktopDeviceOperationResult CreateRootDevice(string infPath, string hardwareId) {
        string fullPath = ValidateInfPath(infPath);
        ValidateHardwareId(hardwareId);
        _ensureElevated();
        return _api.CreateRootDevice(fullPath, hardwareId);
    }

    /// <summary>Replaces the hardware identifier list of a root-enumerated device.</summary>
    public DesktopDeviceOperationResult SetRootHardwareIds(string instanceId, IReadOnlyList<string> hardwareIds) {
        ValidateExactInstanceId(instanceId);
        if (!instanceId.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Hardware identifiers can be changed only for ROOT-enumerated devices.", nameof(instanceId));
        }
        if (hardwareIds == null || hardwareIds.Count == 0) {
            throw new ArgumentException("At least one hardware identifier is required.", nameof(hardwareIds));
        }
        foreach (string hardwareId in hardwareIds) {
            ValidateHardwareId(hardwareId);
        }
        _ensureElevated();
        return _api.SetRootHardwareIds(instanceId, hardwareIds);
    }

    /// <summary>Replaces an upper or lower class-filter chain after verifying every service exists.</summary>
    public DesktopDeviceOperationResult SetClassFilters(
        Guid classGuid,
        DesktopDeviceClassFilterKind kind,
        IReadOnlyList<string> serviceNames) {
        if (classGuid == Guid.Empty) {
            throw new ArgumentException("A non-empty setup class identifier is required.", nameof(classGuid));
        }
        if (serviceNames == null) {
            throw new ArgumentNullException(nameof(serviceNames));
        }
        if (!Enum.IsDefined(typeof(DesktopDeviceClassFilterKind), kind)) {
            throw new ArgumentOutOfRangeException(nameof(kind), "The filter kind must be Upper or Lower.");
        }
        foreach (string serviceName in serviceNames) {
            if (string.IsNullOrWhiteSpace(serviceName) || serviceName.IndexOfAny(new[] { '\\', '/', '*', '?' }) >= 0) {
                throw new ArgumentException($"Filter service name '{serviceName}' is not a valid exact service name.", nameof(serviceNames));
            }
        }
        _ensureElevated();
        return _api.SetClassFilters(classGuid, kind, serviceNames);
    }

    internal static void ValidateExactInstanceId(string instanceId) {
        if (string.IsNullOrWhiteSpace(instanceId)) {
            throw new ArgumentException("An exact device instance identifier is required.", nameof(instanceId));
        }
        if (instanceId.IndexOf('*') >= 0 || instanceId.IndexOf('?') >= 0) {
            throw new ArgumentException("Device instance identifiers cannot contain wildcards.", nameof(instanceId));
        }
    }

    internal static void ValidatePublishedInfName(string publishedInfName) {
        if (string.IsNullOrWhiteSpace(publishedInfName) ||
            publishedInfName.Length <= 7 ||
            !publishedInfName.StartsWith("oem", StringComparison.OrdinalIgnoreCase) ||
            !publishedInfName.EndsWith(".inf", StringComparison.OrdinalIgnoreCase) ||
            publishedInfName.IndexOfAny(new[] { '\\', '/', '*', '?' }) >= 0 ||
            !publishedInfName.Substring(3, publishedInfName.Length - 7).All(character =>
                character >= '0' && character <= '9')) {
            throw new ArgumentException("A published INF name such as 'oem42.inf' is required.", nameof(publishedInfName));
        }
    }

    private static string ValidateInfPath(string infPath) {
        if (string.IsNullOrWhiteSpace(infPath)) {
            throw new ArgumentException("An INF path is required.", nameof(infPath));
        }
        string fullPath = Path.GetFullPath(infPath);
        if (!string.Equals(Path.GetExtension(fullPath), ".inf", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("The driver package path must identify an .inf file.", nameof(infPath));
        }
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException("The driver package INF was not found.", fullPath);
        }
        return fullPath;
    }

    private static void ValidateHardwareId(string hardwareId) {
        if (string.IsNullOrWhiteSpace(hardwareId) || hardwareId.IndexOfAny(new[] { '*', '?' }) >= 0) {
            throw new ArgumentException("An exact hardware identifier without wildcards is required.", nameof(hardwareId));
        }
    }
}
