using System.Runtime.InteropServices;
using Windows.Devices.Radios;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace DesktopManager.Tests;

/// <summary>
/// Contract tests for the reusable desktop-state services introduced as one cohesive capability group.
/// </summary>
[TestClass]
public class DesktopStateCapabilityTests {
    [TestMethod]
    public void AudioService_RoleAndFlowMappings_RoundTripPublicValues() {
        Assert.AreEqual(AudioDataFlow.Render, AudioService.FromNativeFlow(AudioService.ToNativeFlow(AudioDataFlow.Render)));
        Assert.AreEqual(AudioDataFlow.Capture, AudioService.FromNativeFlow(AudioService.ToNativeFlow(AudioDataFlow.Capture)));
        Assert.AreEqual(AudioDataFlow.All, AudioService.FromNativeFlow(AudioService.ToNativeFlow(AudioDataFlow.All)));
        Assert.AreEqual(AudioRole.Console, AudioService.FromNativeRole(AudioService.ToNativeRole(AudioRole.Console)));
        Assert.AreEqual(AudioRole.Multimedia, AudioService.FromNativeRole(AudioService.ToNativeRole(AudioRole.Multimedia)));
        Assert.AreEqual(AudioRole.Communications, AudioService.FromNativeRole(AudioService.ToNativeRole(AudioRole.Communications)));
    }

    [TestMethod]
    public void SystemPowerService_ToExecutionState_CombinesRequestedFlagsOnly() {
        uint state = SystemPowerService.ToExecutionState(
            KeepAwakeOptions.System | KeepAwakeOptions.Display | KeepAwakeOptions.AwayMode);

        Assert.AreEqual(
            SystemPowerService.ExecutionStateSystemRequired |
            SystemPowerService.ExecutionStateDisplayRequired |
            SystemPowerService.ExecutionStateAwayModeRequired,
            state);
        Assert.AreEqual(0u, SystemPowerService.ToExecutionState(0));
    }

    [TestMethod]
    public void SystemPowerService_CreateKeepAwakeLease_RejectsUnsupportedOptionsBeforeNativeCall() {
        var service = new SystemPowerService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.CreateKeepAwakeLease(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.CreateKeepAwakeLease((KeepAwakeOptions)128));
        Assert.ThrowsExactly<ArgumentException>(() => service.CreateKeepAwakeLease(KeepAwakeOptions.AwayMode));
    }

    [TestMethod]
    public void SystemPowerStatus_UnknownBatteryFlag_IsNotReportedAsNoBattery() {
        var unknown = new SystemPowerStatus(PowerLineState.Unknown, BatteryChargeState.Unknown, null, null, null);
        var missing = new SystemPowerStatus(PowerLineState.Online, BatteryChargeState.NoBattery, null, null, null);

        Assert.IsTrue(unknown.HasBattery);
        Assert.IsFalse(missing.HasBattery);
    }

    [TestMethod]
    public void DesktopSessionWatcher_IdleTimeAlone_IsNotMeaningfulChange() {
        DesktopSessionInfo before = CreateSession(isLocked: false, TimeSpan.FromSeconds(1));
        DesktopSessionInfo afterIdle = CreateSession(isLocked: false, TimeSpan.FromMinutes(5));
        DesktopSessionInfo afterLock = CreateSession(isLocked: true, TimeSpan.FromMinutes(5));

        Assert.IsFalse(DesktopSessionWatcher.HasMeaningfulChange(before, afterIdle));
        Assert.IsTrue(DesktopSessionWatcher.HasMeaningfulChange(before, afterLock));
    }

    [TestMethod]
    public void DesktopSessionWatcher_DisposeWaitsForAndSuppressesInFlightPoll() {
        DesktopSessionInfo initial = CreateSession(isLocked: false, TimeSpan.Zero);
        DesktopSessionInfo changed = CreateSession(isLocked: true, TimeSpan.Zero);
        using var pollStarted = new ManualResetEventSlim();
        using var releasePoll = new ManualResetEventSlim();
        int readCount = 0;

        DesktopSessionInfo ReadSession() {
            if (Interlocked.Increment(ref readCount) == 1) {
                return initial;
            }

            pollStarted.Set();
            releasePoll.Wait(TimeSpan.FromSeconds(5));
            return changed;
        }

        var watcher = new DesktopSessionWatcher(ReadSession, TimeSpan.FromMilliseconds(10));
        int changedCount = 0;
        watcher.Changed += (_, _) => Interlocked.Increment(ref changedCount);

        try {
            Assert.IsTrue(pollStarted.Wait(TimeSpan.FromSeconds(5)), "The timer did not begin polling.");
            Task disposeTask = Task.Run(watcher.Dispose);
            try {
                Assert.IsFalse(
                    disposeTask.Wait(TimeSpan.FromMilliseconds(100)),
                    "Dispose returned while a poll could still publish a change.");
            } finally {
                releasePoll.Set();
            }

            Assert.IsTrue(disposeTask.Wait(TimeSpan.FromSeconds(5)), "Dispose did not finish after the poll completed.");
            Assert.AreEqual(0, Volatile.Read(ref changedCount));
            Assert.IsFalse(watcher.Current.IsLocked);
            Thread.Sleep(50);
            Assert.AreEqual(0, Volatile.Read(ref changedCount), "A change was raised after Dispose returned.");
        } finally {
            releasePoll.Set();
            watcher.Dispose();
        }
    }

    [TestMethod]
    public void DesktopSessionWatcher_SkipsOverlappingPolls() {
        DesktopSessionInfo initial = CreateSession(isLocked: false, TimeSpan.Zero);
        DesktopSessionInfo changed = CreateSession(isLocked: true, TimeSpan.Zero);
        using var pollStarted = new ManualResetEventSlim();
        using var releasePoll = new ManualResetEventSlim();
        using var changeObserved = new ManualResetEventSlim();
        int readCount = 0;
        int concurrentReads = 0;
        int maximumConcurrentReads = 0;

        DesktopSessionInfo ReadSession() {
            if (Interlocked.Increment(ref readCount) == 1) {
                return initial;
            }

            int active = Interlocked.Increment(ref concurrentReads);
            UpdateMaximum(ref maximumConcurrentReads, active);
            pollStarted.Set();
            releasePoll.Wait(TimeSpan.FromSeconds(5));
            Interlocked.Decrement(ref concurrentReads);
            return changed;
        }

        var watcher = new DesktopSessionWatcher(ReadSession, TimeSpan.FromMilliseconds(5));
        watcher.Changed += (_, _) => changeObserved.Set();

        try {
            Assert.IsTrue(pollStarted.Wait(TimeSpan.FromSeconds(5)), "The timer did not begin polling.");
            Thread.Sleep(100);
            Assert.AreEqual(2, Volatile.Read(ref readCount), "An overlapping timer callback entered the state provider.");
            Assert.AreEqual(1, Volatile.Read(ref maximumConcurrentReads));

            releasePoll.Set();
            Assert.IsTrue(changeObserved.Wait(TimeSpan.FromSeconds(5)), "The serialized poll did not publish its change.");
        } finally {
            releasePoll.Set();
            watcher.Dispose();
        }
    }

    [TestMethod]
    public void DesktopSessionWatcher_ContainsSubscriberFailureAndNotifiesRemainingSubscribers() {
        DesktopSessionInfo initial = CreateSession(isLocked: false, TimeSpan.Zero);
        DesktopSessionInfo changed = CreateSession(isLocked: true, TimeSpan.Zero);
        using var notificationObserved = new ManualResetEventSlim();
        int readCount = 0;

        DesktopSessionInfo ReadSession() {
            return Interlocked.Increment(ref readCount) == 1 ? initial : changed;
        }

        using var watcher = new DesktopSessionWatcher(ReadSession, TimeSpan.FromMilliseconds(5));
        watcher.Changed += (_, _) => throw new InvalidOperationException("Subscriber failure.");
        watcher.Changed += (_, _) => notificationObserved.Set();

        Assert.IsTrue(
            notificationObserved.Wait(TimeSpan.FromSeconds(5)),
            "A failed subscriber prevented the remaining subscribers from receiving the change.");
        Assert.IsTrue(watcher.Current.IsLocked);
    }

    [TestMethod]
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows10.0.14393.0")]
#endif
    public void RadioService_MapsSupportedWindowsContractValues() {
        Assert.AreEqual(DesktopRadioKind.WiFi, RadioService.ToKind(RadioKind.WiFi));
        Assert.AreEqual(DesktopRadioKind.Bluetooth, RadioService.ToKind(RadioKind.Bluetooth));
        Assert.AreEqual(DesktopRadioState.On, RadioService.ToState(RadioState.On));
        Assert.AreEqual(DesktopRadioState.Disabled, RadioService.ToState(RadioState.Disabled));
        Assert.AreEqual(DesktopRadioAccessStatus.DeniedBySystem, RadioService.ToAccessStatus(RadioAccessStatus.DeniedBySystem));
    }

    [TestMethod]
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows10.0.14393.0")]
#endif
    public void RadioService_AppliedResult_RequiresAllowedAccessAndEffectiveState() {
        Assert.IsTrue(RadioService.IsApplied(RadioAccessStatus.Allowed, RadioState.On, RadioState.On));
        Assert.IsFalse(RadioService.IsApplied(RadioAccessStatus.Allowed, RadioState.Off, RadioState.On));
        Assert.IsFalse(RadioService.IsApplied(RadioAccessStatus.DeniedBySystem, RadioState.On, RadioState.On));

        var acceptedButNotApplied = new DesktopRadioSetResult(
            new DesktopRadioInfo("Wi-Fi", DesktopRadioKind.WiFi, DesktopRadioState.Off),
            DesktopRadioAccessStatus.Allowed,
            accepted: true,
            applied: false);

        Assert.IsTrue(acceptedButNotApplied.Accepted);
        Assert.IsFalse(acceptedButNotApplied.Applied);
    }

    [TestMethod]
    public void ExperimentalAirplaneModeService_RejectsUnknownStateBeforeNativeCall() {
        var service = new ExperimentalAirplaneModeService();

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => service.SetState((AirplaneModeState)42));

        Assert.AreEqual("state", exception.ParamName);
    }

    [TestMethod]
    public void WorkstationProfileStore_RoundTripsCohesiveProfile() {
        string name = "test-" + Guid.NewGuid().ToString("N");
        var profile = new WorkstationProfile {
            CapturedAt = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero),
            TaskbarAutoHide = true,
            Monitors = {
                new WorkstationMonitorProfile {
                    StableKey = "display-1",
                    DeviceId = "device-id",
                    DeviceName = @"\\.\DISPLAY1",
                    IsPrimary = true,
                    DisplayMode = new MonitorDisplayMode {
                        Width = 2560,
                        Height = 1440,
                        RefreshRate = 120,
                        Orientation = DisplayOrientation.Default
                    }
                }
            },
            AudioEndpoints = {
                new WorkstationAudioEndpointProfile {
                    Id = "endpoint-id",
                    Name = "Speakers",
                    DataFlow = AudioDataFlow.Render,
                    VolumePercent = 42,
                    IsMuted = false,
                    DefaultRoles = { AudioRole.Multimedia }
                }
            },
            Taskbars = {
                new WorkstationTaskbarProfile {
                    MonitorStableKey = "display-1",
                    IsVisible = true,
                    Position = TaskbarPosition.Bottom
                }
            }
        };

        try {
            WorkstationProfileStore.Save(name, profile);
            WorkstationProfile loaded = WorkstationProfileStore.Load(name);

            Assert.AreEqual(1, loaded.SchemaVersion);
            Assert.AreEqual(2560, loaded.Monitors.Single().DisplayMode.Width);
            Assert.AreEqual(120, loaded.Monitors.Single().DisplayMode.RefreshRate);
            Assert.AreEqual(AudioRole.Multimedia, loaded.AudioEndpoints.Single().DefaultRoles.Single());
            Assert.IsTrue(loaded.TaskbarAutoHide);
            CollectionAssert.Contains(WorkstationProfileStore.List().ToList(), name);
        } finally {
            WorkstationProfileStore.Delete(name);
        }
    }

    [TestMethod]
    public void WorkstationProfileStore_UnsupportedSchema_IsRejected() {
        string name = "test-" + Guid.NewGuid().ToString("N");
        var profile = new WorkstationProfile { SchemaVersion = 99 };

        Assert.ThrowsExactly<NotSupportedException>(() => WorkstationProfileStore.Save(name, profile));
        Assert.IsFalse(File.Exists(DesktopStateStore.GetWorkstationProfilePath(name)));
    }

    [TestMethod]
    public void WorkstationProfileStore_DuplicateMonitorIdentity_IsRejectedBeforePersistence() {
        string name = "test-" + Guid.NewGuid().ToString("N");
        var profile = new WorkstationProfile {
            Monitors = {
                CreateWorkstationMonitor("display-1"),
                CreateWorkstationMonitor("DISPLAY-1")
            }
        };

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => WorkstationProfileStore.Save(name, profile));

        StringAssert.Contains(exception.Message, "duplicated");
        Assert.IsFalse(File.Exists(DesktopStateStore.GetWorkstationProfilePath(name)));
    }

    [TestMethod]
    public void WorkstationMonitorKeyResolver_DuplicateRawIdentity_UsesUniqueDisplaySource() {
        var service = new MonitorService(new FakeDesktopManager());
        var first = new Monitor(service) {
            Index = 0,
            DeviceId = "duplicated-device",
            DeviceName = @"\\.\DISPLAY1"
        };
        var second = new Monitor(service) {
            Index = 1,
            DeviceId = "duplicated-device",
            DeviceName = @"\\.\DISPLAY2"
        };

        IReadOnlyDictionary<Monitor, string> keys = WorkstationMonitorKeyResolver.Resolve(new[] { first, second });

        Assert.AreNotEqual(keys[first], keys[second]);
        StringAssert.Contains(keys[first], "device-name:");
        StringAssert.Contains(keys[second], "device-name:");
    }

    [TestMethod]
    public void PersonalizationService_RejectsUndefinedEnumsBeforeMutation() {
        var invalidSettings = new[] {
            new PersonalizationSettings { SystemTheme = (SystemTheme)42 },
            new PersonalizationSettings { AppsTheme = (SystemTheme)42 },
            new PersonalizationSettings { StartLayout = (StartLayoutPreference)42 },
            new PersonalizationSettings { TaskbarAlignment = (TaskbarAlignmentPreference)42 },
            new PersonalizationSettings { TaskbarGrouping = (TaskbarGroupingPreference)42 },
            new PersonalizationSettings { DesktopWallpaperPosition = (DesktopWallpaperPosition)42 }
        };

        foreach (PersonalizationSettings settings in invalidSettings) {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PersonalizationService.ValidateSettings(settings));
        }
    }

    [TestMethod]
    public void PersonalizationStateStore_RoundTripsInCurrentUserStateDirectory() {
        string name = "test-" + Guid.NewGuid().ToString("N");
        var snapshot = new PersonalizationSnapshot {
            DesktopBackgroundColor = 0x123456,
            WallpaperPosition = DesktopWallpaperPosition.Fill
        };

        try {
            PersonalizationStateStore.SaveSnapshot(name, snapshot);
            PersonalizationSnapshot loaded = PersonalizationStateStore.LoadSnapshot(name);

            Assert.AreEqual(0x123456u, loaded.DesktopBackgroundColor);
            Assert.AreEqual(DesktopWallpaperPosition.Fill, loaded.WallpaperPosition);
            Assert.AreEqual(
                Path.GetDirectoryName(DesktopStateStore.GetPersonalizationSnapshotPath(name)),
                PersonalizationStateStore.GetSnapshotsDirectory());
        } finally {
            PersonalizationStateStore.DeleteSnapshot(name);
        }
    }

    [TestMethod]
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows10.0.10240.0")]
#endif
    public void VirtualDesktopService_DelegatesSupportedOperations() {
        Guid expectedDesktopId = Guid.NewGuid();
        var api = new FakeVirtualDesktopManagerApi(expectedDesktopId);
        var service = new VirtualDesktopService(api);
        var handle = new IntPtr(1234);

        Assert.IsTrue(service.IsWindowOnCurrentDesktop(handle));
        Assert.AreEqual(expectedDesktopId, service.GetWindowDesktopId(handle));
        service.MoveWindowToDesktop(handle, expectedDesktopId);

        Assert.AreEqual(handle, api.LastHandle);
        Assert.AreEqual(expectedDesktopId, api.LastMovedDesktopId);
        Assert.ThrowsExactly<ArgumentException>(() => service.GetWindowDesktopId(IntPtr.Zero));
        service.Dispose();
        Assert.IsTrue(api.IsDisposed);
    }

    [TestMethod]
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows10.0.10240.0")]
#endif
    public void VirtualDesktopService_PropagatesWindowsFailureHResult() {
        var api = new FakeVirtualDesktopManagerApi(Guid.Empty) { Result = unchecked((int)0x80004005) };
        var service = new VirtualDesktopService(api);

        Assert.ThrowsExactly<COMException>(() => service.IsWindowOnCurrentDesktop(new IntPtr(1234)));
    }

    private static DesktopSessionInfo CreateSession(bool isLocked, TimeSpan idleTime) {
        return new DesktopSessionInfo(
            1,
            "user",
            "domain",
            string.Empty,
            DesktopSessionConnectState.Active,
            DesktopSessionProtocol.Console,
            false,
            isLocked,
            idleTime);
    }

    private static void UpdateMaximum(ref int maximum, int candidate) {
        int observed;
        do {
            observed = Volatile.Read(ref maximum);
            if (candidate <= observed) {
                return;
            }
        } while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
    }

    private static WorkstationMonitorProfile CreateWorkstationMonitor(string stableKey) {
        return new WorkstationMonitorProfile {
            StableKey = stableKey,
            DisplayMode = new MonitorDisplayMode {
                Width = 1920,
                Height = 1080,
                RefreshRate = 60,
                Orientation = DisplayOrientation.Default
            }
        };
    }

    private sealed class FakeVirtualDesktopManagerApi : IVirtualDesktopManagerApi {
        private readonly Guid _desktopId;

        public FakeVirtualDesktopManagerApi(Guid desktopId) {
            _desktopId = desktopId;
        }

        public int Result { get; set; }
        public IntPtr LastHandle { get; private set; }
        public Guid LastMovedDesktopId { get; private set; }
        public bool IsDisposed { get; private set; }

        public int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out int onCurrentDesktop) {
            LastHandle = topLevelWindow;
            onCurrentDesktop = 1;
            return Result;
        }

        public int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId) {
            LastHandle = topLevelWindow;
            desktopId = _desktopId;
            return Result;
        }

        public int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId) {
            LastHandle = topLevelWindow;
            LastMovedDesktopId = desktopId;
            return Result;
        }

        public void Dispose() {
            IsDisposed = true;
        }
    }
}
