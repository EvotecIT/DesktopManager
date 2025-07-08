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
}
