#if NET8_0_OR_GREATER
using DesktopManager.App.Core;

namespace DesktopManager.Tests;

[TestClass]
public sealed class WorkspaceStartupCommandTests {
    [TestMethod]
    public void StartupCommand_PreservesSelectedProfileAndLegacyDefault() {
        Assert.AreEqual("\"C:\\Apps\\Desktop Manager.exe\" --minimized",
            StartupCommand.Build(@"C:\Apps\Desktop Manager.exe", true));
        Assert.AreEqual("\"C:\\Apps\\Desktop Manager.exe\" --minimized --workspace-directory \"D:\\Workspace Profiles\\Recovery\"",
            StartupCommand.Build(@"C:\Apps\Desktop Manager.exe", true, @"D:\Workspace Profiles\Recovery"));
        Assert.AreEqual("\"C:\\Apps\\Desktop Manager.exe\" --workspace-directory \"" + @"D:\\" + "\"",
            StartupCommand.Build(@"C:\Apps\Desktop Manager.exe", false, @"D:\"));
        Assert.ThrowsExactly<ArgumentException>(() => StartupCommand.Build("relative.exe", true));
        Assert.ThrowsExactly<ArgumentException>(() => StartupCommand.Build("C:\\Apps\\bad\"name.exe", true));
    }
}
#endif
