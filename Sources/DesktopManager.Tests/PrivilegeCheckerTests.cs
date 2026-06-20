using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DesktopManager;

namespace DesktopManager.Tests;

[TestClass]
public class PrivilegeCheckerTests {
    [TestMethod]
    public void EnsureElevated_ThrowsWhenNotElevated() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.ThrowsExactly<PlatformNotSupportedException>(() => PrivilegeChecker.EnsureElevated());
        } else {
            if (PrivilegeChecker.IsElevated) {
                Assert.Inconclusive("Test requires non-elevated context");
            }

            Assert.ThrowsExactly<InvalidOperationException>(() => PrivilegeChecker.EnsureElevated());
        }
    }
}
