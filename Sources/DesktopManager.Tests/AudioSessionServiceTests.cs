using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Tests;

[TestClass]
public sealed class AudioSessionServiceTests {
    [TestMethod]
    public void Applications_GroupSessionsRouteWritesAndReuseControlsUntilRefreshOrDisposal() {
        var tab1 = new FakeSession(10, "browser", "@resource", 70f, true, 0.2f, false);
        var tab2 = new FakeSession(11, "BROWSER", "Browser call", 30f, false, 0.8f, true);
        var player = new FakeSession(12, "player", "@resource", 40f, true, 0.1f, false);
        var system = new FakeSession(0, "", null, 60f, true, 0.3f, true, isSystem: true);
        var expired = new FakeSession(13, "old", null, 90f, false, 1f, true) { IsExpired = true };
        var backend = new FakeBackend(tab1, tab2, player, system, expired);
        using var service = new AudioSessionService(backend);

        IReadOnlyList<AudioApplicationInfo> applications = service.GetApplications();
        Assert.AreEqual(3, applications.Count);
        AudioApplicationInfo browser = applications[0];
        Assert.AreEqual("browser", browser.Key);
        Assert.AreEqual("Browser call", browser.Name);
        Assert.AreEqual(11, browser.ProcessId);
        Assert.AreEqual(70f, browser.VolumePercent);
        Assert.IsFalse(browser.IsMuted);
        Assert.AreEqual(0.8f, browser.PeakValue);
        Assert.IsTrue(browser.IsActive);
        Assert.IsFalse(browser.IsSystem);
        Assert.AreEqual("Player", applications[1].Name);
        Assert.IsTrue(applications[1].IsMuted);
        Assert.IsFalse(applications[1].IsActive);
        Assert.AreEqual("system", applications[2].Key);
        Assert.AreEqual("System sounds", applications[2].Name);
        Assert.IsTrue(applications[2].IsSystem);
        Assert.AreEqual(1, expired.Disposals);

        service.SetVolume("BrOwSeR", 25f);
        service.SetMute("BROWSER", true);
        Assert.AreEqual(0.25f, tab1.LastVolumeScalar);
        Assert.AreEqual(0.25f, tab2.LastVolumeScalar);
        Assert.IsTrue(tab1.LastMute);
        Assert.IsTrue(tab2.LastMute);
        Assert.AreEqual(0, player.VolumeWrites);
        Assert.AreEqual(0, system.MuteWrites);
        service.SetVolume("system", 50f);
        Assert.AreEqual(0.5f, system.LastVolumeScalar);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.SetVolume("browser", float.NaN));
        Assert.AreEqual(1, tab1.VolumeWrites);
        Assert.AreEqual(70f, browser.VolumePercent, "Snapshots must not track later writes.");

        tab1.Peak = 0.9f;
        tab2.Peak = 0.4f;
        IReadOnlyDictionary<string, float> peaks = service.GetPeaks();
        Assert.AreEqual(0.9f, peaks["BROWSER"]);
        Assert.AreEqual(0.3f, peaks["system"]);
        tab1.IsExpired = true;
        Assert.AreEqual(0.4f, service.GetPeaks()["browser"]);
        Assert.AreEqual(1, tab1.Disposals);
        service.SetMute("browser", false);
        Assert.AreEqual(1, tab1.MuteWrites, "Expired controls must never receive further writes.");
        Assert.IsFalse(tab2.LastMute);
        Assert.AreEqual(1, backend.Opens, "High-rate reads and mutations must use cached controls.");
        Assert.AreEqual(1, tab2.Reads, "Peak reads must not refresh process names or application snapshots.");

        // A later process may reuse an old PID with a different executable name.
        var replacement = new FakeSession(10, "editor", null, 15f, false, 0.5f, true);
        backend.Next = new[] { replacement };
        AudioApplicationInfo refreshed = service.GetApplications().Single();
        Assert.AreEqual("editor", refreshed.Key);
        Assert.AreEqual("Editor", refreshed.Name);
        Assert.AreEqual(10, refreshed.ProcessId);
        Assert.AreEqual(1, tab1.Disposals);
        Assert.AreEqual(1, tab2.Disposals);
        Assert.AreEqual(1, player.Disposals);
        Assert.AreEqual(1, system.Disposals);
        service.SetVolume("browser", 80f);
        Assert.AreEqual(0, replacement.VolumeWrites, "A missing old key must not be redirected to a reused PID.");
        Assert.AreEqual(0.5f, service.GetPeaks()["editor"]);

        service.Dispose();
        service.Dispose();
        Assert.AreEqual(1, replacement.Disposals);
        Assert.AreEqual(1, backend.Disposals);
        Assert.ThrowsExactly<ObjectDisposedException>(() => service.GetApplications());
        Assert.ThrowsExactly<ObjectDisposedException>(() => service.GetPeaks());
        Assert.ThrowsExactly<ObjectDisposedException>(() => service.SetMute("editor", true));
    }

    private sealed class FakeBackend : IAudioSessionBackend {
        internal IReadOnlyList<IAudioApplicationSession> Next;
        internal int Opens;
        internal int Disposals;
        internal FakeBackend(params IAudioApplicationSession[] sessions) => Next = sessions;
        public IReadOnlyList<IAudioApplicationSession> OpenSessions() { Opens++; return Next; }
        public void Dispose() => Disposals++;
    }

    private sealed class FakeSession : IAudioApplicationSession {
        private readonly AudioApplicationSessionState _state;
        internal float Peak;
        internal float LastVolumeScalar;
        internal bool LastMute;
        internal int VolumeWrites;
        internal int MuteWrites;
        internal int Reads;
        internal int Disposals;

        internal FakeSession(int pid, string name, string? displayName, float volumePercent, bool muted,
            float peak, bool active, bool isSystem = false) {
            _state = new AudioApplicationSessionState(pid, name, displayName, volumePercent, muted, peak, active, isSystem);
            Peak = peak;
        }
        public bool IsExpired { get; set; }
        public AudioApplicationSessionState? Read() { Reads++; return IsExpired ? null : _state; }
        public float GetPeak() => Peak;
        public void SetVolume(float scalar) { LastVolumeScalar = scalar; VolumeWrites++; }
        public void SetMute(bool muted) { LastMute = muted; MuteWrites++; }
        public void Dispose() => Disposals++;
    }
}
