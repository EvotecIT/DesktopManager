using DesktopManager;

namespace DesktopManager.Tests.Fakes;

internal sealed class FakeDeviceManagementApi : IDeviceManagementApi {
    internal IReadOnlyList<DesktopDeviceInfo> Devices { get; set; } = Array.Empty<DesktopDeviceInfo>();
    internal IReadOnlyList<DesktopDeviceDriverInfo> Drivers { get; set; } = Array.Empty<DesktopDeviceDriverInfo>();
    internal IReadOnlyList<DesktopDriverPackageInfo> Packages { get; set; } = Array.Empty<DesktopDriverPackageInfo>();
    internal IReadOnlyList<DesktopDeviceClassInfo> Classes { get; set; } = Array.Empty<DesktopDeviceClassInfo>();
    internal DesktopDeviceQuery? LastDeviceQuery { get; private set; }
    internal string? LastInvocation { get; private set; }
    internal IReadOnlyList<object?> LastArguments { get; private set; } = Array.Empty<object?>();

    public IReadOnlyList<DesktopDeviceInfo> GetDevices(DesktopDeviceQuery query) {
        LastInvocation = nameof(GetDevices);
        LastDeviceQuery = query;
        return Devices;
    }

    public IReadOnlyList<DesktopDeviceDriverInfo> GetCompatibleDrivers(string instanceId) {
        Record(nameof(GetCompatibleDrivers), instanceId);
        return Drivers;
    }

    public IReadOnlyList<DesktopDriverPackageInfo> GetDriverPackages(DesktopDriverPackageQuery query) {
        Record(nameof(GetDriverPackages), query);
        return Packages;
    }

    public IReadOnlyList<DesktopDeviceClassInfo> GetDeviceClasses() {
        Record(nameof(GetDeviceClasses));
        return Classes;
    }

    public DesktopDeviceOperationResult EnableDevice(string instanceId) {
        return Operation(nameof(EnableDevice), instanceId);
    }

    public DesktopDeviceOperationResult DisableDevice(string instanceId, bool force, bool persist) {
        return Operation(nameof(DisableDevice), instanceId, force, persist);
    }

    public DesktopDeviceOperationResult RestartDevice(string instanceId) {
        return Operation(nameof(RestartDevice), instanceId);
    }

    public DesktopDeviceOperationResult RemoveDevice(string instanceId, bool removeSubtree) {
        return Operation(nameof(RemoveDevice), instanceId, removeSubtree);
    }

    public DesktopDeviceOperationResult ScanDevices(string? instanceId, bool asynchronous) {
        return Operation(nameof(ScanDevices), instanceId, asynchronous);
    }

    public DesktopDeviceOperationResult StageDriver(string infPath) {
        return Operation(nameof(StageDriver), infPath);
    }

    public DesktopDeviceOperationResult InstallDriver(string infPath, bool force) {
        return Operation(nameof(InstallDriver), infPath, force);
    }

    public DesktopDeviceOperationResult UpdateDriver(string infPath, string hardwareId, bool force) {
        return Operation(nameof(UpdateDriver), infPath, hardwareId, force);
    }

    public DesktopDeviceOperationResult DeleteDriver(string publishedInfName, bool uninstallDevices, bool force) {
        return Operation(nameof(DeleteDriver), publishedInfName, uninstallDevices, force);
    }

    public DesktopDeviceOperationResult ExportDriver(string publishedInfName, string destinationDirectory, bool overwrite) {
        return Operation(nameof(ExportDriver), publishedInfName, destinationDirectory, overwrite);
    }

    public DesktopDeviceOperationResult RollbackDriver(string instanceId) {
        return Operation(nameof(RollbackDriver), instanceId);
    }

    public DesktopDeviceOperationResult CreateRootDevice(string infPath, string hardwareId) {
        return Operation(nameof(CreateRootDevice), infPath, hardwareId);
    }

    public DesktopDeviceOperationResult SetRootHardwareIds(string instanceId, IReadOnlyList<string> hardwareIds) {
        return Operation(nameof(SetRootHardwareIds), instanceId, hardwareIds);
    }

    public DesktopDeviceOperationResult SetClassFilters(
        Guid classGuid,
        DesktopDeviceClassFilterKind kind,
        IReadOnlyList<string> serviceNames) {
        return Operation(nameof(SetClassFilters), classGuid, kind, serviceNames);
    }

    private DesktopDeviceOperationResult Operation(string name, params object?[] arguments) {
        Record(name, arguments);
        return DesktopDeviceOperationResult.Success(name, arguments.FirstOrDefault()?.ToString() ?? string.Empty);
    }

    private void Record(string name, params object?[] arguments) {
        LastInvocation = name;
        LastArguments = arguments;
    }
}
