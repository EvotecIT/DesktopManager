namespace DesktopManager;

internal interface IDeviceManagementApi {
    IReadOnlyList<DesktopDeviceInfo> GetDevices(DesktopDeviceQuery query);

    IReadOnlyList<DesktopDeviceDriverInfo> GetCompatibleDrivers(string instanceId);

    IReadOnlyList<DesktopDriverPackageInfo> GetDriverPackages(DesktopDriverPackageQuery query);

    IReadOnlyList<DesktopDeviceClassInfo> GetDeviceClasses();

    DesktopDeviceOperationResult EnableDevice(string instanceId);

    DesktopDeviceOperationResult DisableDevice(string instanceId, bool force, bool persist);

    DesktopDeviceOperationResult RestartDevice(string instanceId);

    DesktopDeviceOperationResult RemoveDevice(string instanceId, bool removeSubtree);

    DesktopDeviceOperationResult ScanDevices(string? instanceId, bool asynchronous);

    DesktopDeviceOperationResult StageDriver(string infPath);

    DesktopDeviceOperationResult InstallDriver(string infPath, bool force);

    DesktopDeviceOperationResult UpdateDriver(string infPath, string hardwareId, bool force);

    DesktopDeviceOperationResult DeleteDriver(string publishedInfName, bool uninstallDevices, bool force);

    DesktopDeviceOperationResult ExportDriver(string publishedInfName, string destinationDirectory, bool overwrite);

    DesktopDeviceOperationResult RollbackDriver(string instanceId);

    DesktopDeviceOperationResult CreateRootDevice(string infPath, string hardwareId);

    DesktopDeviceOperationResult SetRootHardwareIds(string instanceId, IReadOnlyList<string> hardwareIds);

    DesktopDeviceOperationResult SetClassFilters(Guid classGuid, DesktopDeviceClassFilterKind kind, IReadOnlyList<string> serviceNames);
}
