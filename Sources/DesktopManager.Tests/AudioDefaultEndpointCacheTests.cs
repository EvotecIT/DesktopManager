using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

/// <summary>Protects the default-endpoint cache behind <see cref="AudioService.GetEndpoint"/> without touching real audio devices.</summary>
[TestClass]
public sealed class AudioDefaultEndpointCacheTests {
    private static readonly IReadOnlyDictionary<AudioRole, string> SpeakersDefault =
        new Dictionary<AudioRole, string> { [AudioRole.Multimedia] = "speakers" };

    [TestMethod]
    public void Get_ReusesDefaultsUntilAnEndpointChangeIsReported() {
        Action<AudioEndpointChangeKind>? notify = null;
        var cache = new AudioDefaultEndpointCache(callback => { notify = callback; return new Subscription(); });
        int queries = 0;
        IReadOnlyDictionary<AudioRole, string> Query() { queries++; return SpeakersDefault; }

        cache.Get(AudioDataFlow.Render, Query);
        cache.Get(AudioDataFlow.Render, Query);
        Assert.AreEqual(1, queries);

        notify!(AudioEndpointChangeKind.PropertyChanged);
        cache.Get(AudioDataFlow.Render, Query);
        Assert.AreEqual(1, queries, "A property change cannot move a default endpoint.");

        notify(AudioEndpointChangeKind.DefaultChanged);
        cache.Get(AudioDataFlow.Render, Query);
        Assert.AreEqual(2, queries);

        cache.Get(AudioDataFlow.Capture, Query);
        Assert.AreEqual(3, queries, "Render and capture defaults are cached separately.");
    }

    [TestMethod]
    public void Get_ChangeReportedDuringQuery_DoesNotKeepThatAnswer() {
        Action<AudioEndpointChangeKind>? notify = null;
        var cache = new AudioDefaultEndpointCache(callback => { notify = callback; return new Subscription(); });
        int queries = 0;

        cache.Get(AudioDataFlow.Render, () => {
            queries++;
            notify!(AudioEndpointChangeKind.DefaultChanged);
            return SpeakersDefault;
        });
        cache.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });

        Assert.AreEqual(2, queries);
    }

    [TestMethod]
    public void Get_WithoutNotifications_QueriesEveryTime() {
        int subscribeAttempts = 0;
        var cache = new AudioDefaultEndpointCache(_ => {
            subscribeAttempts++;
            throw new COMException("Audio service unavailable.");
        });
        int queries = 0;

        cache.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });
        cache.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });

        Assert.AreEqual(2, queries);
        Assert.AreEqual(1, subscribeAttempts);
    }

    [TestMethod]
    public void Get_AfterMaximumAge_QueriesAgain() {
        TimeSpan now = TimeSpan.FromHours(1);
        var cache = new AudioDefaultEndpointCache(_ => new Subscription(), () => now);
        int queries = 0;

        cache.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });
        now += AudioDefaultEndpointCache.MaximumAge;
        cache.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });

        Assert.AreEqual(2, queries);
    }

    [TestMethod]
    public void SetDefaultAudioDevice_ForgetsCachedDefaults() {
        var service = new AudioService(new NoOpPolicyConfig());
        int queries = 0;
        try {
            AudioService.DefaultEndpoints.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });

            service.SetDefaultAudioDevice("headset", AudioRole.Multimedia);
            AudioService.DefaultEndpoints.Get(AudioDataFlow.Render, () => { queries++; return SpeakersDefault; });

            Assert.AreEqual(2, queries);
        } finally {
            // Leave no fake defaults behind for other tests in this process.
            AudioService.DefaultEndpoints.OnEndpointChanged(AudioEndpointChangeKind.DefaultChanged);
        }
    }

    private sealed class Subscription : IDisposable {
        public void Dispose() {
        }
    }

    private sealed class NoOpPolicyConfig : IPolicyConfigClient {
        public void SetDefaultEndpoint(string devID, ERole role) {
        }
    }
}
