using System.Diagnostics;

namespace DesktopManager.Tests;

[TestClass]
public sealed class MediaSourceNameResolverTests {
    [TestMethod]
    public void Resolve_RegisteredAppId_UsesStartMenuDisplayName() {
        // Edge registers the AppUserModelID "MSEdge", which is what SMTC reports for its media sessions.
        if (MediaSourceNameResolver.GetAppsFolderName("MSEdge") == null) {
            Assert.Inconclusive("Microsoft Edge is not registered in the Start menu on this machine.");
        }
        Assert.AreEqual("Microsoft Edge", MediaSourceNameResolver.Resolve("MSEdge"));
    }

    [TestMethod]
    public void Resolve_RunningExecutableName_UsesFileDescription() {
        using Process current = Process.GetCurrentProcess();
        string path = current.MainModule?.FileName ?? throw new AssertFailedException("The test host path is unavailable.");
        string? description = FileVersionInfo.GetVersionInfo(path).FileDescription;
        if (string.IsNullOrWhiteSpace(description)) {
            Assert.Inconclusive("The test host executable has no file description.");
        }
        Assert.AreEqual(description!.Trim(), MediaSourceNameResolver.Resolve(Path.GetFileName(path)));
    }
}
