using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for MouseInputService that only read state. Moving, clicking, scrolling or dragging here would act on the
/// real desktop under the cursor (a click at 0,0 un-maximises whatever window's title bar is there), so those
/// operations are not exercised against the live session.
/// </summary>
public class MouseInputServiceTests {
    [TestMethod]
    /// <summary>
    /// Test for GetState_DoesNotThrow.
    /// </summary>
    public void GetState_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        DesktopMouseState state = MouseInputService.GetState();

        Assert.IsNotNull(state);
    }
}