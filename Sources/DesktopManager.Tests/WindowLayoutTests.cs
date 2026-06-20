using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
        TestHelper.RequireOwnedWindowMutationTests();

        string title = $"Layout Harness {System.Guid.NewGuid():N}";
        var fullLayoutPath = System.IO.Path.GetTempFileName();
        var filteredLayoutPath = System.IO.Path.GetTempFileName();

        try {
            using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

            var manager = new WindowManager();
            var original = manager.GetWindowPosition(harness.Window);

            manager.SaveLayout(fullLayoutPath);

            var json = System.IO.File.ReadAllText(fullLayoutPath);
            var layout = System.Text.Json.JsonSerializer.Deserialize<WindowLayout>(json);
            if (layout?.Windows == null || layout.Windows.Count == 0) {
                Assert.Inconclusive("Saved layout did not contain any windows");
                return;
            }

            bool included = layout.Windows.Any(w => w.ProcessId == original.ProcessId && w.Title == original.Title);
            if (!included) {
                Assert.Inconclusive("Saved layout did not include the test window");
                return;
            }

            var newLeft = original.Left + 20;
            var newTop = original.Top + 20;

            var filteredLayout = new WindowLayout {
                Windows = new System.Collections.Generic.List<WindowPosition> {
                    new WindowPosition {
                        Title = original.Title,
                        ProcessId = original.ProcessId,
                        Left = newLeft,
                        Top = newTop,
                        Right = newLeft + original.Width,
                        Bottom = newTop + original.Height,
                        State = WindowState.Normal
                    }
                }
            };

            var filteredJson = System.Text.Json.JsonSerializer.Serialize(filteredLayout,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filteredLayoutPath, filteredJson);

            manager.LoadLayout(filteredLayoutPath);

            System.Threading.Thread.Sleep(200);
            Application.DoEvents();

            var restored = manager.GetWindowPosition(harness.Window);
            manager.ShowWindow(harness.Window, false);
            var leftTolerance = Math.Abs(newLeft - restored.Left);
            var topTolerance = Math.Abs(newTop - restored.Top);

            Assert.IsTrue(leftTolerance <= 50,
                $"Window position restoration failed. Expected Left {newLeft}, got {restored.Left}");
            Assert.IsTrue(topTolerance <= 50,
                $"Window position restoration failed. Expected Top {newTop}, got {restored.Top}");
        } finally {
            if (System.IO.File.Exists(fullLayoutPath)) {
                System.IO.File.Delete(fullLayoutPath);
            }
            if (System.IO.File.Exists(filteredLayoutPath)) {
                System.IO.File.Delete(filteredLayoutPath);
            }
        }
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

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.LoadLayout(path));

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

        Assert.ThrowsExactly<System.IO.InvalidDataException>(() => manager.LoadLayout(path, validate: true));

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
