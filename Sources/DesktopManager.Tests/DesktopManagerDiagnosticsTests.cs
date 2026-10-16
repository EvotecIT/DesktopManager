using System;

namespace DesktopManager.Tests;

[TestClass]
public sealed class DesktopManagerDiagnosticsTests {
    [TestMethod]
    public void Report_ThrowingListenerDoesNotEscape() {
        bool invoked = false;

        DesktopManagerDiagnostics.Report("best effort", _ => {
            invoked = true;
            throw new InvalidOperationException("listener failed");
        });

        Assert.IsTrue(invoked);
    }
}
