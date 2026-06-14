using DesktopManager.App.Core;

namespace DesktopManager.Tests;

/// <summary>
/// Tests for DesktopManager hotkey profile persistence.
/// </summary>
[TestClass]
public class HotkeyProfileStoreTests {
    /// <summary>
    /// The first-run profile should contain the initial DisplayFusion-like movement shortcuts.
    /// </summary>
    [TestMethod]
    public void CreateDefaultProfile_ContainsInitialMovementHotkeys() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();

        Assert.IsTrue(profile.Enabled);
        Assert.IsFalse(profile.StartWithWindows);
        Assert.IsTrue(profile.MinimizeToTray);
        Assert.AreEqual(HotkeyBackendKinds.NativeHotkeyHost, profile.HotkeyBackend);
        CollectionAssert.Contains(profile.LowLevelHookExclusiveProcessNames, "RemoteDesktopManager");
        CollectionAssert.Contains(profile.LowLevelHookExclusiveProcessNames, "mstsc");
        Assert.AreEqual("EVOMAGIC 4 monitors", profile.ProfileName);
        Assert.AreEqual(9, profile.Functions.Count);
        Assert.IsTrue(profile.Functions.Any(function => function.Name == "Move Window to Top Left Monitor" && function.Hotkey == "Ctrl+Alt+Shift+5"));
        Assert.IsTrue(profile.Functions.Any(function => function.Name == "Move Window to Bottom Right Monitor" && function.Hotkey == "Ctrl+Alt+Shift+8"));
        Assert.IsTrue(profile.Functions.Any(function => function.Name == "Maximize Active Window" && function.Hotkey == "Ctrl+Alt+Shift+9"));
        Assert.IsTrue(profile.Functions.Any(function => function.Id == "move-top-left-maximize" && function.WindowAction.MonitorIndex == 1));
        Assert.IsTrue(profile.Functions.Any(function => function.Id == "move-top-right-maximize" && function.WindowAction.MonitorIndex == 0));
        Assert.IsTrue(profile.Functions.Any(function => function.Id == "move-bottom-left-maximize" && function.WindowAction.MonitorIndex == 3));
        Assert.IsTrue(profile.Functions.Any(function => function.Id == "move-bottom-right-maximize" && function.WindowAction.MonitorIndex == 2));
        Assert.IsTrue(HotkeyProfileValidator.Validate(profile).IsValid);
    }

    /// <summary>
    /// The default half-monitor actions should match the DisplayFusion export's exact work-area rectangles.
    /// </summary>
    [TestMethod]
    public void CreateDefaultProfile_ContainsDisplayFusionHalfScreenRectangles() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();

        AssertExactRectangle(profile, "move-top-left-left-half", -3840, 19, 1920, 2088);
        AssertExactRectangle(profile, "move-top-left-right-half", -1920, 19, 1920, 2088);
        AssertExactRectangle(profile, "move-top-right-left-half", 0, 0, 1920, 2088);
        AssertExactRectangle(profile, "move-top-right-right-half", 1920, 0, 1920, 2088);
    }

    /// <summary>
    /// Loading a missing profile should create a readable JSON file and return the same contract.
    /// </summary>
    [TestMethod]
    public void LoadOrCreate_MissingFile_WritesReadableDefaultProfile() {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "profile.json");

        try {
            HotkeyProfile profile = HotkeyProfileStore.LoadOrCreate(path);
            string json = File.ReadAllText(path);

            Assert.AreEqual("EVOMAGIC 4 monitors", profile.ProfileName);
            Assert.IsTrue(File.Exists(path));
            Assert.IsTrue(json.Contains("EVOMAGIC 4 monitors", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("startWithWindows", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("minimizeToTray", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("NativeHotkeyHost", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("RemoteDesktopManager", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("Ctrl+Alt+Shift+5", StringComparison.Ordinal));
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Explicit backend and process-list choices should survive runtime default normalization.
    /// </summary>
    [TestMethod]
    public void LoadOrCreate_ExplicitRegisterHotKeyProfile_PreservesOptOuts() {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "profile.json");

        try {
            HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
            profile.HotkeyBackend = HotkeyBackendKinds.RegisterHotKey;
            profile.LowLevelHookExclusiveProcessNames.Clear();
            HotkeyProfileStore.Save(path, profile);

            HotkeyProfile loaded = HotkeyProfileStore.LoadOrCreate(path);

            Assert.AreEqual(HotkeyBackendKinds.RegisterHotKey, loaded.HotkeyBackend);
            Assert.AreEqual(0, loaded.LowLevelHookExclusiveProcessNames.Count);
            Assert.IsTrue(HotkeyProfileValidator.Validate(loaded).IsValid);
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Profiles without a backend should receive the current native-host default.
    /// </summary>
    [TestMethod]
    public void ApplyRuntimeDefaults_MissingBackend_UsesNativeHotkeyHost() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.HotkeyBackend = string.Empty;

        HotkeyProfileDefaults.ApplyRuntimeDefaults(profile);

        Assert.AreEqual(HotkeyBackendKinds.NativeHotkeyHost, profile.HotkeyBackend);
    }

    /// <summary>
    /// Saved profiles should round-trip through the source-generated JSON path.
    /// </summary>
    [TestMethod]
    public void SaveAndLoad_RoundTripsProfileChanges() {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "profile.json");

        try {
            HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
            profile.ProfileName = "Laptop";
            profile.Functions[0].Enabled = false;
            profile.Functions[0].Hotkey = "Ctrl+Alt+Shift+F13";

            HotkeyProfileStore.Save(path, profile);
            HotkeyProfile loaded = HotkeyProfileStore.LoadOrCreate(path);

            Assert.AreEqual("Laptop", loaded.ProfileName);
            Assert.IsFalse(loaded.Functions[0].Enabled);
            Assert.AreEqual("Ctrl+Alt+Shift+F13", loaded.Functions[0].Hotkey);
            Assert.IsTrue(HotkeyProfileValidator.Validate(loaded).IsValid);
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Duplicate enabled hotkeys should be rejected before registration is attempted.
    /// </summary>
    [TestMethod]
    public void Validate_DuplicateEnabledHotkeys_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[1].Hotkey = profile.Functions[0].Hotkey;

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("duplicate hotkey", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Equivalent enabled hotkeys with reordered modifier aliases should be rejected before registration.
    /// </summary>
    [TestMethod]
    public void Validate_EquivalentDuplicateEnabledHotkeys_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "Ctrl+Alt+Shift+1";
        profile.Functions[1].Hotkey = "Alt+Control+Shift+1";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("duplicate hotkey", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Equivalent key aliases should be rejected before backend registration.
    /// </summary>
    [TestMethod]
    public void Validate_EquivalentDuplicateKeyAliases_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "Ctrl+Alt+1";
        profile.Functions[1].Hotkey = "Alt+Control+VK_1";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("duplicate hotkey", StringComparison.OrdinalIgnoreCase)));
    }

    private static string CreateTemporaryDirectory() {
        string directory = Path.Combine(Path.GetTempPath(), "DesktopManager.App.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void AssertExactRectangle(HotkeyProfile profile, string id, int left, int top, int width, int height) {
        HotkeyFunctionDefinition function = profile.Functions.Single(item => item.Id == id);
        Assert.AreEqual(left, function.WindowAction.ExactLeft);
        Assert.AreEqual(top, function.WindowAction.ExactTop);
        Assert.AreEqual(width, function.WindowAction.ExactWidth);
        Assert.AreEqual(height, function.WindowAction.ExactHeight);
    }
}
