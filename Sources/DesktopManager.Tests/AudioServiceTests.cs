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
    /// Protects the native PROPVARIANT layout used to read Core Audio endpoint friendly names.
    /// </summary>
    public void PropVariant_CoreAudioStringLayout_IsWritableAndSixteenBytes() {
        Type valueType = typeof(PropVariant);

        Assert.AreEqual(16, valueType.StructLayoutAttribute?.Size);
        Assert.IsFalse(valueType.GetField("_valueType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.IsInitOnly);
        Assert.IsFalse(valueType.GetField("_pointerValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.IsInitOnly);
    }
}
