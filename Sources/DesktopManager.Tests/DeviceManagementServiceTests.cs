using DesktopManager.Tests.Fakes;
using System.Runtime.Versioning;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows10.0.15063.0")]
public sealed class DeviceManagementServiceTests {
    [TestMethod]
    public void GetDeviceRequestsEveryDetailFamilyForExactInstance() {
        var expected = new DesktopDeviceInfo { InstanceId = @"PCI\VEN_1234&DEV_5678\1", Name = "Test device" };
        var api = new FakeDeviceManagementApi { Devices = new[] { expected } };
        var service = new DeviceManagementService(api);

        DesktopDeviceInfo actual = service.GetDevice(expected.InstanceId);

        Assert.AreSame(expected, actual);
        Assert.IsNotNull(api.LastDeviceQuery);
        Assert.AreEqual(expected.InstanceId, api.LastDeviceQuery.InstanceId);
        Assert.IsTrue(api.LastDeviceQuery.IncludeRelations);
        Assert.IsTrue(api.LastDeviceQuery.IncludeStack);
        Assert.IsTrue(api.LastDeviceQuery.IncludeResources);
        Assert.IsTrue(api.LastDeviceQuery.IncludeInterfaces);
        Assert.IsTrue(api.LastDeviceQuery.IncludeProperties);
    }

    [TestMethod]
    public void GetDeviceContainersGroupsDevicesAndComputesState() {
        Guid containerId = Guid.NewGuid();
        var api = new FakeDeviceManagementApi {
            Devices = new[] {
                new DesktopDeviceInfo { InstanceId = "A", Name = "Second", ContainerId = containerId },
                new DesktopDeviceInfo { InstanceId = "B", Name = "First", ContainerId = containerId, Present = true, HasProblem = true },
                new DesktopDeviceInfo { InstanceId = "C", Name = "Ungrouped" }
            }
        };
        var service = new DeviceManagementService(api);

        DesktopDeviceContainerInfo container = service.GetDeviceContainers().Single();

        Assert.AreEqual(containerId, container.ContainerId);
        Assert.IsTrue(container.Connected);
        Assert.IsTrue(container.HasProblem);
        CollectionAssert.AreEqual(new[] { "First", "Second" }, container.Devices.Select(device => device.Name).ToArray());
    }

    [TestMethod]
    [DataRow(@"PCI\VEN_1234*", "InstanceId")]
    [DataRow(@"PCI\VEN_1234?", "InstanceId")]
    public void DeviceQueriesRejectWildcardInstanceIdentifiers(string instanceId, string _) {
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api);

        Assert.ThrowsExactly<ArgumentException>(() => service.GetDevices(new DesktopDeviceQuery { InstanceId = instanceId }));
        Assert.IsNull(api.LastInvocation);
    }

    [TestMethod]
    public void DeviceQueriesRejectWildcardHardwareOrCompatibleIdentifiers() {
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api);

        Assert.ThrowsExactly<ArgumentException>(() => service.GetDevices(new DesktopDeviceQuery {
            DeviceId = @"PCI\VEN_1234*"
        }));
        Assert.IsNull(api.LastInvocation);
    }

    [TestMethod]
    public void ReadOnlyOperationsDoNotRequireElevation() {
        int elevationCalls = 0;
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api, () => elevationCalls++);

        service.GetDevices();
        service.GetCompatibleDrivers(@"ROOT\TEST\0000");
        service.GetDriverPackages();
        service.GetDeviceClasses();

        Assert.AreEqual(0, elevationCalls);
    }

    [TestMethod]
    public void DeviceMutationRequiresElevationBeforeNativeCall() {
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api, () => throw new UnauthorizedAccessException("not elevated"));

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => service.DisableDevice(@"ROOT\TEST\0000", force: true));
        Assert.IsNull(api.LastInvocation);
    }

    [TestMethod]
    public void DeviceMutationRoutesExactArguments() {
        int elevationCalls = 0;
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api, () => elevationCalls++);

        service.DisableDevice(@"ROOT\TEST\0000", force: true, persist: false);

        Assert.AreEqual(1, elevationCalls);
        Assert.AreEqual(nameof(IDeviceManagementApi.DisableDevice), api.LastInvocation);
        CollectionAssert.AreEqual(new object?[] { @"ROOT\TEST\0000", true, false }, api.LastArguments.ToArray());
    }

    [TestMethod]
    public void DriverPackageQueriesRequireExactPublishedInfName() {
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api);

        Assert.ThrowsExactly<ArgumentException>(() => service.GetDriverPackages(new DesktopDriverPackageQuery {
            PublishedInfName = @"C:\Windows\INF\oem42.inf"
        }));
        Assert.ThrowsExactly<ArgumentException>(() => service.DeleteDriver("oem*.inf"));
        Assert.ThrowsExactly<ArgumentException>(() => service.DeleteDriver("oem.inf"));
        Assert.IsNull(api.LastInvocation);
    }

    [TestMethod]
    public void DriverUninstallNormalizesRedundantForceAndExplainsTheContract() {
        int elevationCalls = 0;
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api, () => elevationCalls++);

        DesktopDeviceOperationResult result = service.DeleteDriver(
            "oem42.inf",
            uninstallDevices: true,
            force: true);

        Assert.AreEqual(1, elevationCalls);
        Assert.AreEqual(nameof(IDeviceManagementApi.DeleteDriver), api.LastInvocation);
        CollectionAssert.AreEqual(new object?[] { "oem42.inf", true, false }, api.LastArguments.ToArray());
        StringAssert.Contains(result.Message, "Force is redundant");
    }

    [TestMethod]
    public void RootHardwareIdChangeRejectsNonRootDevice() {
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api);

        Assert.ThrowsExactly<ArgumentException>(() => service.SetRootHardwareIds(
            @"PCI\VEN_1234&DEV_5678\1",
            new[] { @"PCI\VEN_1234&DEV_5678" }));
        Assert.IsNull(api.LastInvocation);
    }

    [TestMethod]
    public void DeviceDisableabilityUsesDevNodeStatusInsteadOfSilentInstallCapability() {
        var disableable = new DesktopDeviceInfo {
            StatusFlags = 0x00002000u,
            Capabilities = 0x00000020u
        };
        var notDisableable = new DesktopDeviceInfo();

        Assert.IsFalse(disableable.NotDisableable);
        Assert.IsTrue(disableable.SilentInstall);
        Assert.IsTrue(notDisableable.NotDisableable);
        Assert.IsFalse(notDisableable.SilentInstall);
    }

    [TestMethod]
    public void OperationResultRepresentsUnknownNativeChangeAndRebootState() {
        DesktopDeviceOperationResult result = DesktopDeviceOperationResult.Success(
            "Test",
            "Target",
            changed: null,
            rebootRequired: null);

        Assert.IsNull(result.Changed);
        Assert.IsNull(result.RebootRequired);
    }

    [TestMethod]
    public void ClassFiltersRejectPatternLikeServiceNames() {
        var api = new FakeDeviceManagementApi();
        var service = new DeviceManagementService(api);

        Assert.ThrowsExactly<ArgumentException>(() => service.SetClassFilters(
            Guid.NewGuid(),
            DesktopDeviceClassFilterKind.Upper,
            new[] { "filter*" }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.SetClassFilters(
            Guid.NewGuid(),
            (DesktopDeviceClassFilterKind)42,
            Array.Empty<string>()));
        Assert.IsNull(api.LastInvocation);
    }

    [TestMethod]
    public void InfMutationsNormalizeAndValidateTheFileBeforeElevation() {
        string temporaryFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".inf");
        File.WriteAllText(temporaryFile, "[Version]");
        try {
            int elevationCalls = 0;
            var api = new FakeDeviceManagementApi();
            var service = new DeviceManagementService(api, () => elevationCalls++);

            service.InstallDriver(temporaryFile, force: true);

            Assert.AreEqual(1, elevationCalls);
            Assert.AreEqual(nameof(IDeviceManagementApi.InstallDriver), api.LastInvocation);
            Assert.AreEqual(Path.GetFullPath(temporaryFile), api.LastArguments[0]);
            Assert.AreEqual(true, api.LastArguments[1]);
        } finally {
            File.Delete(temporaryFile);
        }
    }
}
