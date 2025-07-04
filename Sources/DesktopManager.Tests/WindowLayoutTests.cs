using System.Linq;
using System.Runtime.InteropServices;
using DesktopManager;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowLayoutTests.
/// </summary>
public class WindowLayoutTests {
    [TestMethod]
    /// <summary>
    /// Test for SaveAndLoadLayout_RoundTrips.
    /// </summary>
    public void SaveAndLoadLayout_RoundTrips() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var original = manager.GetWindowPosition(window);
        var originalState = original.State;
        var path = System.IO.Path.GetTempFileName();

        manager.SaveLayout(path);
        manager.SetWindowPosition(window, original.Left + 20, original.Top + 20);
        if (originalState == WindowState.Maximize) {
            manager.MinimizeWindow(window);
        } else {
            manager.MaximizeWindow(window);
        }
        manager.LoadLayout(path);
        var restored = manager.GetWindowPosition(window);

        Assert.AreEqual(original.Left, restored.Left);
        Assert.AreEqual(original.Top, restored.Top);
        Assert.AreEqual(originalState, restored.State);

        System.IO.File.Delete(path);
    }

    [TestMethod]
    /// <summary>
    /// Test for LoadLayout_InvalidJson_Throws.
    /// </summary>
    public void LoadLayout_InvalidJson_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var path = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllText(path, "{ invalid }");

        Assert.ThrowsException<InvalidOperationException>(() => manager.LoadLayout(path));

        System.IO.File.Delete(path);
    }

    [TestMethod]
    /// <summary>
    /// Test for LoadLayout_ValidateMissingProperties_Throws.
    /// </summary>
    public void LoadLayout_ValidateMissingProperties_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var path = System.IO.Path.GetTempFileName();
        var json = "{\"Windows\":[{\"Title\":\"Test\"}]}";
        System.IO.File.WriteAllText(path, json);

        Assert.ThrowsException<System.IO.InvalidDataException>(() => manager.LoadLayout(path, validate: true));

        System.IO.File.Delete(path);
    }

    [TestMethod]
    /// <summary>
    /// Test for SaveLayout_RelativePath_CreatesFile.
    /// </summary>
    public void SaveLayout_RelativePath_CreatesFile() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(tempDir);
        var originalDir = System.Environment.CurrentDirectory;
        System.Environment.CurrentDirectory = tempDir;
        try {
            var relative = System.IO.Path.Combine("sub", "layout.json");
            manager.SaveLayout(relative);
            var fullPath = System.IO.Path.Combine(tempDir, relative);
            Assert.IsTrue(System.IO.File.Exists(fullPath));
        } finally {
            System.Environment.CurrentDirectory = originalDir;
            System.IO.Directory.Delete(tempDir, true);
        }
    }
}
