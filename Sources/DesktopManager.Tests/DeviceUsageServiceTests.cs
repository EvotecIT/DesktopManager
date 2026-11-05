using System.Security;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace DesktopManager.Tests;

[TestClass]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public sealed class DeviceUsageServiceTests {
    [TestMethod]
    public void DeviceUsers_MixedConsentRecords_ReturnActiveFriendlyNamesOrUnavailableOnReadFailure() {
        string libraryKey = typeof(DeviceUsageService).Assembly.Location.Replace('\\', '#');
        const string browserKey = "C:#DeviceUsageFixture#browser.exe";
        var reader = new FakeReader(new Dictionary<string, DeviceUsageService.Entry[]> {
            ["microphone"] = new[] {
                new DeviceUsageService.Entry("Stopped_package", 100, 100),
                new DeviceUsageService.Entry("LaterStop_package", 100, 101),
                new DeviceUsageService.Entry("NeverStarted_package", 0, -1),
                new DeviceUsageService.Entry("NegativeStart_package", -1, -2),
                new DeviceUsageService.Entry(libraryKey, 100, 0),
                new DeviceUsageService.Entry(browserKey, 100, 99),
                new DeviceUsageService.Entry("C:#OtherDeviceUsageFixture#BROWSER.exe", 100, 0),
                new DeviceUsageService.Entry("MSTeams_8wekyb3d8bbwe", 100, 0),
                new DeviceUsageService.Entry("msteams_8WEKYB3D8BBWE", 100, 0),
                new DeviceUsageService.Entry("Vendor.App.Recorder_family", 100, 1)
            },
            ["webcam"] = new[] {
                new DeviceUsageService.Entry("Vendor.Camera_family", 200, 0),
                new DeviceUsageService.Entry("CameraStopped_family", 200, 201)
            }
        });
        // Every fixture record started after this boot.
        var service = new DeviceUsageService(reader, () => 50);

        DeviceUsageResult microphone = service.GetMicrophoneUsers();
        DeviceUsageResult camera = service.GetCameraUsers();

        Assert.AreEqual(DesktopOperationStatus.Success, microphone.Status);
        Assert.IsTrue(microphone.Succeeded);
        CollectionAssert.AreEqual(
            new[] { "DesktopManager", "browser", "MSTeams", "Recorder" },
            microphone.Applications.Select(application => application.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { libraryKey, browserKey, "MSTeams_8wekyb3d8bbwe", "Vendor.App.Recorder_family" },
            microphone.Applications.Select(application => application.Key).ToArray());
        Assert.AreEqual(DesktopOperationStatus.Success, camera.Status);
        Assert.HasCount(1, camera.Applications);
        Assert.AreEqual("Camera", camera.Applications[0].Name);
        Assert.AreEqual("Vendor.Camera_family", camera.Applications[0].Key);

        // Real registry reads can fail after yielding some records. Never return that partial list as a successful snapshot.
        foreach (Exception failure in new Exception[] {
            new UnauthorizedAccessException("Fixture access denied"),
            new SecurityException("Fixture security denial"),
            new IOException("Fixture registry read failure")
        }) {
            reader.Failure = failure;
            foreach (DeviceUsageResult result in new[] { service.GetMicrophoneUsers(), service.GetCameraUsers() }) {
                Assert.AreEqual(DesktopOperationStatus.Unavailable, result.Status);
                Assert.IsFalse(result.Succeeded);
                Assert.IsEmpty(result.Applications);
                StringAssert.Contains(result.Message, failure.Message);
            }
        }
    }

    [TestMethod]
    public void DeviceUsers_RecordStartedBeforeTheLastBoot_IsNotInUse() {
        // A crash during a Teams call left its microphone record started at 09:29 with no stop; Windows booted at 10:09.
        long boot = new DateTime(2026, 11, 5, 9, 9, 16, DateTimeKind.Utc).ToFileTimeUtc();
        long beforeCrash = new DateTime(2026, 11, 5, 8, 29, 15, DateTimeKind.Utc).ToFileTimeUtc();
        long afterBoot = new DateTime(2026, 11, 5, 9, 15, 0, DateTimeKind.Utc).ToFileTimeUtc();
        var reader = new FakeReader(new Dictionary<string, DeviceUsageService.Entry[]> {
            ["microphone"] = new[] {
                new DeviceUsageService.Entry("MSTeams_8wekyb3d8bbwe", beforeCrash, 0),
                new DeviceUsageService.Entry("Vendor.App.Recorder_family", afterBoot, 0)
            },
            ["webcam"] = new[] { new DeviceUsageService.Entry("Vendor.Camera_family", beforeCrash, 0) }
        });
        var service = new DeviceUsageService(reader, () => boot);

        CollectionAssert.AreEqual(new[] { "Recorder" }, service.GetMicrophoneUsers().Applications.Select(application => application.Name).ToArray());
        Assert.IsEmpty(service.GetCameraUsers().Applications);
    }

    private sealed class FakeReader : DeviceUsageService.IReader {
        private readonly IReadOnlyDictionary<string, DeviceUsageService.Entry[]> _entries;

        internal FakeReader(IReadOnlyDictionary<string, DeviceUsageService.Entry[]> entries) {
            _entries = entries;
        }

        internal Exception? Failure { get; set; }

        public IEnumerable<DeviceUsageService.Entry> Read(string capability) {
            foreach (DeviceUsageService.Entry entry in _entries[capability]) {
                yield return entry;
            }
            if (Failure != null) {
                throw Failure;
            }
        }
    }
}
