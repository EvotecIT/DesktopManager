namespace DesktopManager;

internal static class DevicePropertyKeys {
    private static readonly Guid Device = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");
    private static readonly Guid DeviceStatus = new Guid("4340a6c5-93fa-4706-972c-7b648008a5a7");
    private static readonly Guid DeviceState = new Guid("540b947e-8b40-45bc-a8a2-6a0b894cbda2");
    private static readonly Guid DeviceDriver = new Guid("a8b865dd-2e3d-4094-ad97-e593a70c75d6");
    private static readonly Guid DeviceContainer = new Guid("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c");
    private static readonly Guid DeviceClass = new Guid("4321918b-f69e-470d-a5de-4d88c75ad24b");
    private static readonly Guid DeviceClassMetadata = new Guid("259abffc-50a7-47ce-af08-68c9a7d73366");

    internal static readonly DeviceNativeMethods.DevPropKey Description = Key(Device, 2);
    internal static readonly DeviceNativeMethods.DevPropKey HardwareIds = Key(Device, 3);
    internal static readonly DeviceNativeMethods.DevPropKey CompatibleIds = Key(Device, 4);
    internal static readonly DeviceNativeMethods.DevPropKey ClassName = Key(Device, 9);
    internal static readonly DeviceNativeMethods.DevPropKey ClassGuid = Key(Device, 10);
    internal static readonly DeviceNativeMethods.DevPropKey Manufacturer = Key(Device, 13);
    internal static readonly DeviceNativeMethods.DevPropKey FriendlyName = Key(Device, 14);
    internal static readonly DeviceNativeMethods.DevPropKey Location = Key(Device, 15);
    internal static readonly DeviceNativeMethods.DevPropKey Capabilities = Key(Device, 17);
    internal static readonly DeviceNativeMethods.DevPropKey EnumeratorName = Key(Device, 24);
    internal static readonly DeviceNativeMethods.DevPropKey BaseContainerId = Key(Device, 38);

    internal static readonly DeviceNativeMethods.DevPropKey DevNodeStatus = Key(DeviceStatus, 2);
    internal static readonly DeviceNativeMethods.DevPropKey ProblemCode = Key(DeviceStatus, 3);
    internal static readonly DeviceNativeMethods.DevPropKey EjectionRelations = Key(DeviceStatus, 4);
    internal static readonly DeviceNativeMethods.DevPropKey RemovalRelations = Key(DeviceStatus, 5);
    internal static readonly DeviceNativeMethods.DevPropKey PowerRelations = Key(DeviceStatus, 6);
    internal static readonly DeviceNativeMethods.DevPropKey BusRelations = Key(DeviceStatus, 7);
    internal static readonly DeviceNativeMethods.DevPropKey Parent = Key(DeviceStatus, 8);
    internal static readonly DeviceNativeMethods.DevPropKey Children = Key(DeviceStatus, 9);
    internal static readonly DeviceNativeMethods.DevPropKey Siblings = Key(DeviceStatus, 10);

    internal static readonly DeviceNativeMethods.DevPropKey IsPresent = Key(DeviceState, 5);
    internal static readonly DeviceNativeMethods.DevPropKey Stack = Key(DeviceState, 14);

    internal static readonly DeviceNativeMethods.DevPropKey DriverDate = Key(DeviceDriver, 2);
    internal static readonly DeviceNativeMethods.DevPropKey DriverVersion = Key(DeviceDriver, 3);
    internal static readonly DeviceNativeMethods.DevPropKey DriverDescription = Key(DeviceDriver, 4);
    internal static readonly DeviceNativeMethods.DevPropKey DriverInfPath = Key(DeviceDriver, 5);
    internal static readonly DeviceNativeMethods.DevPropKey DriverInfSection = Key(DeviceDriver, 6);
    internal static readonly DeviceNativeMethods.DevPropKey MatchingDeviceId = Key(DeviceDriver, 8);
    internal static readonly DeviceNativeMethods.DevPropKey DriverProvider = Key(DeviceDriver, 9);
    internal static readonly DeviceNativeMethods.DevPropKey DriverRank = Key(DeviceDriver, 14);
    internal static readonly DeviceNativeMethods.DevPropKey ContainerId = Key(DeviceContainer, 2);

    internal static readonly DeviceNativeMethods.DevPropKey ClassUpperFilters = Key(DeviceClass, 19);
    internal static readonly DeviceNativeMethods.DevPropKey ClassLowerFilters = Key(DeviceClass, 20);
    internal static readonly DeviceNativeMethods.DevPropKey ClassDefaultService = Key(DeviceClassMetadata, 11);

    private static DeviceNativeMethods.DevPropKey Key(Guid formatId, uint propertyId) {
        return new DeviceNativeMethods.DevPropKey(formatId, propertyId);
    }
}
