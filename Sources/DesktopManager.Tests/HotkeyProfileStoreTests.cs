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
        Assert.AreEqual(HotkeyBackendKinds.LowLevelKeyboardHook, profile.HotkeyBackend);
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
            Assert.IsTrue(json.Contains("LowLevelKeyboardHook", StringComparison.Ordinal));
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
    /// Profiles without a backend should receive the current single-process default.
    /// </summary>
    [TestMethod]
    public void ApplyRuntimeDefaults_MissingBackend_UsesLowLevelKeyboardHook() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.HotkeyBackend = string.Empty;

        HotkeyProfileDefaults.ApplyRuntimeDefaults(profile);

        Assert.AreEqual(HotkeyBackendKinds.LowLevelKeyboardHook, profile.HotkeyBackend);
    }

    /// <summary>
    /// Explicit advanced host choices should survive runtime default normalization.
    /// </summary>
    [TestMethod]
    public void ApplyRuntimeDefaults_ExplicitNativeHotkeyHost_PreservesAdvancedBackend() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.HotkeyBackend = HotkeyBackendKinds.NativeHotkeyHost;

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
            profile.ApplyRulesOnStartup = true;
            profile.Functions[0].Enabled = false;
            profile.Functions[0].Hotkey = "Ctrl+Alt+Shift+F13";
            profile.Functions[0].WindowAction.MonitorStableKey = "device-id:DISPLAY1";

            HotkeyProfileStore.Save(path, profile);
            HotkeyProfile loaded = HotkeyProfileStore.LoadOrCreate(path);

            Assert.AreEqual("Laptop", loaded.ProfileName);
            Assert.IsTrue(loaded.ApplyRulesOnStartup);
            Assert.IsFalse(loaded.Functions[0].Enabled);
            Assert.AreEqual("Ctrl+Alt+Shift+F13", loaded.Functions[0].Hotkey);
            Assert.AreEqual("device-id:DISPLAY1", loaded.Functions[0].WindowAction.MonitorStableKey);
            Assert.IsTrue(HotkeyProfileValidator.Validate(loaded).IsValid);
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Saved layout profiles and rules should round-trip with the hotkey profile document.
    /// </summary>
    [TestMethod]
    public void SaveAndLoad_RoundTripsLayoutRules() {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "profile.json");

        try {
            HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
            profile.Layouts.Add(CreateLayoutRuleProfile());

            HotkeyProfileStore.Save(path, profile);
            HotkeyProfile loaded = HotkeyProfileStore.LoadOrCreate(path);

            Assert.AreEqual(1, loaded.Layouts.Count);
            Assert.AreEqual("Work layout", loaded.Layouts[0].Name);
            Assert.AreEqual("device-id:DISPLAY1", loaded.Layouts[0].MonitorStableKeys[0]);
            Assert.AreEqual("PowerShell console", loaded.Layouts[0].Rules[0].Name);
            Assert.AreEqual("pwsh*", loaded.Layouts[0].Rules[0].Match.ProcessNamePattern);
            Assert.AreEqual(WindowPlacements.RightHalf, loaded.Layouts[0].Rules[0].Action.Placement);
            Assert.IsTrue(HotkeyProfileValidator.Validate(loaded).IsValid);
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// A failed profile replacement should leave the last readable profile intact.
    /// </summary>
    [TestMethod]
    public void Save_WhenReplaceFails_PreservesExistingProfile() {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "profile.json");

        try {
            HotkeyProfile original = HotkeyProfileDefaults.CreateDefaultProfile();
            original.ProfileName = "Original";
            HotkeyProfileStore.Save(path, original);

            using (FileStream lockedProfile = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                HotkeyProfile changed = HotkeyProfileDefaults.CreateDefaultProfile();
                changed.ProfileName = "Changed";

                Exception? saveException = null;
                try {
                    HotkeyProfileStore.Save(path, changed);
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                    saveException = ex;
                }

                Assert.IsNotNull(saveException);
            }

            HotkeyProfile loaded = HotkeyProfileStore.LoadOrCreate(path);
            Assert.AreEqual("Original", loaded.ProfileName);
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

    /// <summary>
    /// Unparsable enabled hotkeys should be rejected before a runtime silently skips them.
    /// </summary>
    [TestMethod]
    public void Validate_UnparsableEnabledHotkey_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "Ctrl+Alt+Shfit+1";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("invalid hotkey", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Empty hotkey gesture tokens should be rejected instead of ignored.
    /// </summary>
    [TestMethod]
    public void Validate_EmptyHotkeyToken_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "Ctrl++A";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("empty token", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Modifierless gestures should be rejected because the low-level backend would consume normal typing keys.
    /// </summary>
    [TestMethod]
    public void Validate_ModifierlessHotkey_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "A";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("at least one Ctrl, Alt, Shift, or Win modifier", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// NoRepeat is not a real modifier and cannot make a typing key safe to capture globally.
    /// </summary>
    [TestMethod]
    public void Validate_NoRepeatOnlyHotkey_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "NoRepeat+A";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("at least one Ctrl, Alt, Shift, or Win modifier", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Modifier-only gestures should be rejected because they do not provide a real trigger key.
    /// </summary>
    [TestMethod]
    public void Validate_ModifierOnlyHotkey_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "Ctrl+Alt+Shift";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("non-modifier trigger key", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Non-key virtual keys should be rejected before registration reports an impossible shortcut.
    /// </summary>
    [TestMethod]
    public void Validate_MouseVirtualKeyHotkey_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].Hotkey = "Ctrl+Alt+VK_LBUTTON";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("keyboard trigger key", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Unknown hotkey backends should be rejected before runtime registration chooses a fallback backend.
    /// </summary>
    [TestMethod]
    public void Validate_UnknownHotkeyBackend_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.HotkeyBackend = "LowLevelKeyboardHok";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("invalid hotkey backend", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Unsupported action types should be rejected before their hotkeys are registered.
    /// </summary>
    [TestMethod]
    public void Validate_UnknownActionType_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].ActionType = "ManageWindw";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("invalid action type", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Unsupported window targets should be rejected before runtime execution.
    /// </summary>
    [TestMethod]
    public void Validate_UnknownWindowTarget_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].WindowAction.Target = "ForegroundWindw";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("invalid window target", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Exact rectangle actions should contain all required geometry before registration.
    /// </summary>
    [TestMethod]
    public void Validate_IncompleteExactRectangle_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].WindowAction.Placement = WindowPlacements.ExactRectangle;
        profile.Functions[0].WindowAction.ExactLeft = 10;
        profile.Functions[0].WindowAction.ExactTop = 10;
        profile.Functions[0].WindowAction.ExactWidth = 640;
        profile.Functions[0].WindowAction.ExactHeight = null;

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("incomplete exact rectangle", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Null function lists from hand-edited JSON should produce validation errors instead of host crashes.
    /// </summary>
    [TestMethod]
    public void Validate_NullFunctionList_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions = null!;

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("functions list", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Null function entries from hand-edited JSON should produce validation errors instead of host crashes.
    /// </summary>
    [TestMethod]
    public void Validate_NullFunctionEntry_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions.Add(null!);

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("empty", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Layout rules should reuse the same action validation as hotkeys.
    /// </summary>
    [TestMethod]
    public void Validate_LayoutRuleInvalidPlacement_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        WindowLayoutProfileDefinition layout = CreateLayoutRuleProfile();
        layout.Rules[0].Action.Placement = "RigthHalf";
        profile.Layouts.Add(layout);

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("invalid placement", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Matching layout rules should create placement requests for the shared placement engine.
    /// </summary>
    [TestMethod]
    public void WindowRuleEvaluator_MatchingRule_CreatesPlacementRequest() {
        WindowLayoutProfileDefinition layout = CreateLayoutRuleProfile();
        WindowRuleObservation window = new() {
            Handle = new IntPtr(0x1234),
            Title = "Administrator: PowerShell",
            ProcessName = "pwsh",
            ProcessPath = @"C:\Program Files\PowerShell\7\pwsh.exe"
        };

        WindowRuleEvaluation evaluation = WindowRuleEvaluator.Evaluate(new[] { layout }, window);

        Assert.IsTrue(evaluation.Matched);
        Assert.AreEqual("PowerShell console", evaluation.Rule!.Name);
        Assert.IsNotNull(evaluation.Request);
        Assert.AreEqual(new IntPtr(0x1234), evaluation.Request!.TargetWindowHandle);
        Assert.AreEqual(WindowPlacementKind.RightHalf, evaluation.Request.Placement);
        Assert.AreEqual(WindowMonitorTargetKind.TopRight, evaluation.Request.MonitorTarget);
        Assert.AreEqual(0, evaluation.Request.MonitorIndex);
    }

    /// <summary>
    /// Nonmatching windows should not produce placement requests.
    /// </summary>
    [TestMethod]
    public void WindowRuleEvaluator_NonmatchingRule_ReturnsNoMatch() {
        WindowLayoutProfileDefinition layout = CreateLayoutRuleProfile();
        WindowRuleObservation window = new() {
            Title = "Notepad",
            ProcessName = "notepad",
            ProcessPath = @"C:\Windows\notepad.exe"
        };

        WindowRuleEvaluation evaluation = WindowRuleEvaluator.Evaluate(new[] { layout }, window);

        Assert.IsFalse(evaluation.Matched);
        Assert.IsNull(evaluation.Request);
    }

    /// <summary>
    /// Unknown monitor targets should be rejected before a hotkey can move to the current monitor by accident.
    /// </summary>
    [TestMethod]
    public void Validate_UnknownMonitorTarget_ReturnsError() {
        HotkeyProfile profile = HotkeyProfileDefaults.CreateDefaultProfile();
        profile.Functions[0].WindowAction.Monitor = "TopLfet";

        HotkeyProfileValidationResult result = HotkeyProfileValidator.Validate(profile);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("invalid monitor target", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Named monitor-relative placements should not be converted to stale exact rectangles.
    /// </summary>
    [TestMethod]
    public void CreatePlacementRequest_NamedPlacementWithExactRectangle_IgnoresExactRectangle() {
        WindowHotkeyActionDefinition action = new() {
            Monitor = MonitorTargets.TopLeft,
            MonitorIndex = 4,
            Placement = WindowPlacements.LeftHalf,
            ExactLeft = -3840,
            ExactTop = 19,
            ExactWidth = 1920,
            ExactHeight = 2088
        };

        WindowPlacementRequest request = WindowHotkeyPlacementRequestFactory.Create(action, IntPtr.Zero);

        Assert.AreEqual(WindowPlacementKind.LeftHalf, request.Placement);
        Assert.AreEqual(WindowMonitorTargetKind.TopLeft, request.MonitorTarget);
        Assert.AreEqual(4, request.MonitorIndex);
        Assert.IsFalse(request.HasExactRectangle);
    }

    /// <summary>
    /// Placement request creation should accept the same placement casing as profile validation.
    /// </summary>
    [TestMethod]
    public void CreatePlacementRequest_LowercasePlacement_Parses() {
        WindowHotkeyActionDefinition action = new() {
            Monitor = MonitorTargets.Current,
            Placement = "maximize"
        };

        WindowPlacementRequest request = WindowHotkeyPlacementRequestFactory.Create(action, IntPtr.Zero);

        Assert.AreEqual(WindowPlacementKind.Maximize, request.Placement);
    }

    /// <summary>
    /// Unknown monitor targets should fail fast when a profile action is converted to a placement request.
    /// </summary>
    [TestMethod]
    public void CreatePlacementRequest_UnknownMonitorTarget_Throws() {
        WindowHotkeyActionDefinition action = new() {
            Monitor = "TopLfet",
            Placement = WindowPlacements.Maximize
        };

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => WindowHotkeyPlacementRequestFactory.Create(action, IntPtr.Zero));

        StringAssert.Contains(exception.Message, "Unsupported monitor target");
    }

    private static string CreateTemporaryDirectory() {
        string directory = Path.Combine(Path.GetTempPath(), "DesktopManager.App.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static WindowLayoutProfileDefinition CreateLayoutRuleProfile() {
        return new WindowLayoutProfileDefinition {
            Id = "work",
            Name = "Work layout",
            MonitorStableKeys = {
                "device-id:DISPLAY1"
            },
            Rules = {
                new WindowRuleDefinition {
                    Id = "powershell-console",
                    Name = "PowerShell console",
                    Match = new WindowRuleMatchDefinition {
                        TitlePattern = "*PowerShell*",
                        ProcessNamePattern = "pwsh*",
                        ProcessPathPattern = "*PowerShell*"
                    },
                    Action = new WindowHotkeyActionDefinition {
                        Target = WindowTargets.ActiveWindow,
                        Monitor = MonitorTargets.TopRight,
                        MonitorIndex = 0,
                        Placement = WindowPlacements.RightHalf,
                        VerifyAfterAction = true
                    }
                }
            }
        };
    }

    private static void AssertExactRectangle(HotkeyProfile profile, string id, int left, int top, int width, int height) {
        HotkeyFunctionDefinition function = profile.Functions.Single(item => item.Id == id);
        Assert.AreEqual(left, function.WindowAction.ExactLeft);
        Assert.AreEqual(top, function.WindowAction.ExactTop);
        Assert.AreEqual(width, function.WindowAction.ExactWidth);
        Assert.AreEqual(height, function.WindowAction.ExactHeight);
    }
}
