using System.Diagnostics;

namespace DesktopManager.Tests;

[TestClass]
public sealed class MediaSourceNameResolverTests {
    [TestMethod]
    public void Resolve_RegisteredAppId_UsesStartMenuDisplayName() {
        // Edge registers the AppUserModelID "MSEdge", which is what SMTC reports for its media sessions.
        string? displayName = MediaSourceNameResolver.GetAppsFolderName("MSEdge");
        if (displayName == null) {
            Assert.Inconclusive("Microsoft Edge is not registered in the Start menu on this machine.");
        }
        Assert.AreEqual(displayName.Trim(), MediaSourceNameResolver.Resolve("MSEdge"));
    }

    [TestMethod]
    public void Resolve_ExecutablePath_UsesFileDescriptionAndRetriesMissingFiles() {
        using Process current = Process.GetCurrentProcess();
        string path = current.MainModule?.FileName ?? throw new AssertFailedException("The test host path is unavailable.");
        string? description = FileVersionInfo.GetVersionInfo(path).FileDescription;
        if (string.IsNullOrWhiteSpace(description)) {
            Assert.Inconclusive("The test host executable has no file description.");
        }
        string directory = Path.Combine(Path.GetTempPath(), "DesktopManager-MediaName-" + Guid.NewGuid().ToString("N"));
        string target = Path.Combine(directory, "player.exe");
        Directory.CreateDirectory(directory);
        try {
            Assert.AreEqual("Player", MediaSourceNameResolver.Resolve(target));
            File.Copy(path, target);
            Assert.AreEqual(description!.Trim(), MediaSourceNameResolver.Resolve(target));
            File.Delete(target);
            Assert.AreEqual("Player", MediaSourceNameResolver.Resolve(target), "Executable metadata must not survive the executable it describes.");
        } finally {
            Directory.Delete(directory, true);
        }
    }
}
