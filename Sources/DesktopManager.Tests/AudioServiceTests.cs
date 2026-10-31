using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for the <see cref="AudioService"/> class.
/// </summary>
public class AudioServiceTests {
    private sealed class FakePolicyConfig : IPolicyConfigClient {
        public readonly List<(string id, ERole role)> Calls = new();
        public void SetDefaultEndpoint(string devID, ERole role) => Calls.Add((devID, role));
    }

    [TestMethod]
    /// <summary>
    /// Verify that setting the default device calls the policy client for all roles.
    /// </summary>
    public void SetDefaultAudioDevice_CallsPolicyForAllRoles() {
        var fake = new FakePolicyConfig();
        var service = new AudioService(fake);
        service.SetDefaultAudioDevice("device1");

        Assert.AreEqual(3, fake.Calls.Count);
        Assert.AreEqual(("device1", ERole.eConsole), fake.Calls[0]);
        Assert.AreEqual(("device1", ERole.eMultimedia), fake.Calls[1]);
        Assert.AreEqual(("device1", ERole.eCommunications), fake.Calls[2]);
    }

    [TestMethod]
    /// <summary>
    /// Verifies callers can update a deliberate subset of default endpoint roles without changing the others.
    /// </summary>
    public void SetDefaultAudioDevice_SpecifiedRoles_OnlyUpdatesThoseRoles() {
        var fake = new FakePolicyConfig();
        var service = new AudioService(fake);

        service.SetDefaultAudioDevice("device1", AudioRole.Multimedia, AudioRole.Communications);

        CollectionAssert.AreEqual(
            new[] {
                ("device1", ERole.eMultimedia),
                ("device1", ERole.eCommunications)
            },
            fake.Calls);
    }

    [TestMethod]
    /// <summary>
    /// Verifies an invalid role rejects the entire request before any default endpoint is changed.
    /// </summary>
    public void SetDefaultAudioDevice_InvalidRole_DoesNotPartiallyUpdateDefaults() {
        var fake = new FakePolicyConfig();
        var service = new AudioService(fake);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            service.SetDefaultAudioDevice("device1", AudioRole.Console, (AudioRole)42));

        Assert.AreEqual(0, fake.Calls.Count);
    }

    [TestMethod]
    public void SetEndpointVolume_NonFiniteValue_IsRejectedBeforeEndpointLookup() {
        var service = new AudioService(new FakePolicyConfig());

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.SetEndpointVolume("device1", float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.SetEndpointVolume("device1", float.PositiveInfinity));
    }

    [TestMethod]
    /// <summary>
    /// Reads live endpoints through each target's COM interop model (source-generated on .NET 8+, built-in on net472).
    /// Read-only: no default device, volume or mute state changes.
    /// </summary>
    public void GetEndpoints_DefaultService_ReadsLiveEndpointsWithoutChangingState() {
        var service = new AudioService();

        IReadOnlyList<AudioEndpointInfo> endpoints = service.GetEndpoints();

        foreach (AudioEndpointInfo endpoint in endpoints) {
            Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.Name));
            Assert.IsTrue(endpoint.DataFlow == AudioDataFlow.Render || endpoint.DataFlow == AudioDataFlow.Capture);
            if (endpoint.VolumePercent.HasValue) {
                Assert.IsTrue(endpoint.VolumePercent.Value >= 0 && endpoint.VolumePercent.Value <= 100.5f);
            }
        }
        Assert.IsTrue(endpoints.Count(endpoint => endpoint.DefaultRoles.Contains(AudioRole.Console) && endpoint.DataFlow == AudioDataFlow.Render) <= 1,
            "At most one render endpoint can be the console default.");
    }

    [TestMethod]
    /// <summary>
    /// Registers the managed notification callback with Core Audio and unregisters the same callback on dispose.
    /// The constructor and Dispose throw when Core Audio rejects the registration or unregistration HRESULT.
    /// </summary>
    public void AudioEndpointWatcher_CreateAndDispose_CoreAudioAcceptsRegistrationAndUnregistration() {
        var watcher = new AudioEndpointWatcher();

        watcher.Dispose();
        watcher.Dispose();
    }

    [TestMethod]
    /// <summary>
    /// Protects the native PROPVARIANT layout used to read Core Audio endpoint friendly names.
    /// </summary>
    public void PropVariant_CoreAudioNativeLayout_MatchesWindowsAbi() {
        Assert.AreEqual(IntPtr.Size == 8 ? 24 : 16, System.Runtime.InteropServices.Marshal.SizeOf<PropVariant>());
        Assert.AreEqual(new IntPtr(8), System.Runtime.InteropServices.Marshal.OffsetOf<PropVariant>("_pointerValue"));
    }
}
