using System;
using System.IO;
using System.Runtime.Versioning;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared desktop automation helpers.
/// </summary>
public class DesktopAutomationCoreTests {
    [TestMethod]
    /// <summary>
    /// Ensures hexadecimal handles parse correctly.
    /// </summary>
    public void DesktopHandleParser_ParseHexValue_ReturnsHandle() {
        IntPtr handle = DesktopHandleParser.Parse("0x1A2B");

        Assert.AreEqual(new IntPtr(0x1A2B), handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures decimal handles parse correctly.
    /// </summary>
    public void DesktopHandleParser_ParseDecimalValue_ReturnsHandle() {
        IntPtr handle = DesktopHandleParser.Parse("6699");

        Assert.AreEqual(new IntPtr(6699), handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid handles are rejected.
    /// </summary>
    public void DesktopHandleParser_ParseInvalidValue_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => DesktopHandleParser.Parse("not-a-handle"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop automation reports the same elevation state as the shared privilege helper.
    /// </summary>
    public void DesktopAutomationService_IsElevated_MatchesPrivilegeChecker() {
        var automation = new DesktopAutomationService();

        Assert.AreEqual(PrivilegeChecker.IsElevated, automation.IsElevated());
    }

    [TestMethod]
    /// <summary>
    /// Ensures clipboard writes reject null text.
    /// </summary>
    public void DesktopAutomationService_SetClipboardText_NullText_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.SetClipboardText(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures logon wallpaper updates reject a missing image path.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public void DesktopAutomationService_SetLogonWallpaper_NullPath_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetLogonWallpaper(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop slideshow start rejects missing image paths.
    /// </summary>
    public void DesktopAutomationService_StartDesktopSlideshow_NullImagePaths_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.StartDesktopSlideshow(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor wallpaper queries reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_GetMonitorWallpaper_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetMonitorWallpaper(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor wallpaper updates reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_SetMonitorWallpaper_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorWallpaper(null!, "wallpaper.jpg"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor wallpaper stream updates reject a missing stream.
    /// </summary>
    public void DesktopAutomationService_SetMonitorWallpaperStream_NullStream_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.SetMonitorWallpaper("DISPLAY1", (Stream)null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor wallpaper URL updates reject a missing URL.
    /// </summary>
    public void DesktopAutomationService_SetMonitorWallpaperFromUrl_NullUrl_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorWallpaperFromUrl("DISPLAY1", null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures all-user wallpaper updates reject a missing file path.
    /// </summary>
    public void DesktopAutomationService_SetDesktopWallpaperForAllUsers_NullPath_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetDesktopWallpaperForAllUsers(null!, DesktopWallpaperPosition.Fill));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor brightness queries reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_GetMonitorBrightness_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetMonitorBrightness(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor brightness updates reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_SetMonitorBrightness_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorBrightness(null!, 50));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor position queries reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_GetMonitorPosition_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetMonitorPosition(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor position updates reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_SetMonitorPosition_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorPosition(null!, new MonitorPosition(0, 0, 100, 100)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor position updates reject a missing position object.
    /// </summary>
    public void DesktopAutomationService_SetMonitorPosition_NullPosition_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.SetMonitorPosition("DISPLAY1", null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor resolution updates reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_SetMonitorResolution_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorResolution(null!, 1920, 1080));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor orientation updates reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_SetMonitorOrientation_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorOrientation(null!, DisplayOrientation.Default));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor DPI scaling updates reject a missing device identifier.
    /// </summary>
    public void DesktopAutomationService_SetMonitorDpiScaling_NullDeviceId_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetMonitorDpiScaling(null!, 150));
    }

    [TestMethod]
    /// <summary>
    /// Ensures taskbar position updates reject negative monitor indexes.
    /// </summary>
    public void DesktopAutomationService_SetTaskbarPosition_NegativeMonitorIndex_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.SetTaskbarPosition(-1, TaskbarPosition.Bottom));
    }

    [TestMethod]
    /// <summary>
    /// Ensures taskbar visibility updates reject negative monitor indexes.
    /// </summary>
    public void DesktopAutomationService_SetTaskbarVisibility_NegativeMonitorIndex_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.SetTaskbarVisibility(-1, visible: true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window activation rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_ActivateWindow_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.ActivateWindow(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first topmost updates reject a zero handle.
    /// </summary>
    public void DesktopAutomationService_SetWindowTopMost_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetWindowTopMost(IntPtr.Zero, true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first visibility updates reject a zero handle.
    /// </summary>
    public void DesktopAutomationService_SetWindowVisibility_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetWindowVisibility(IntPtr.Zero, true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first transparency updates reject a zero handle.
    /// </summary>
    public void DesktopAutomationService_SetWindowTransparency_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetWindowTransparency(IntPtr.Zero, 128));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first style updates reject a zero handle.
    /// </summary>
    public void DesktopAutomationService_SetWindowStyle_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetWindowStyle(IntPtr.Zero, (long)WindowStyleFlags.SysMenu, enable: true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window typing rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_TypeWindowText_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.TypeWindowText(IntPtr.Zero, "sample", paste: false, delayMilliseconds: 0, foregroundInput: false, physicalKeys: false, hostedSession: false, script: false, scriptChunkLength: 120, scriptLineDelayMilliseconds: 0));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window typing with explicit options rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_TypeWindowTextWithOptions_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.TypeWindowText(IntPtr.Zero, "sample", new WindowInputOptions()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window paste rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_PasteWindowText_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.PasteWindowText(IntPtr.Zero, "sample"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window key sending rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_SendWindowKeys_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SendWindowKeys(IntPtr.Zero, [VirtualKey.VK_RETURN], activate: true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window process queries reject a zero handle.
    /// </summary>
    public void DesktopAutomationService_GetWindowProcessInfo_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetWindowProcessInfo(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first owner window process queries reject a zero handle.
    /// </summary>
    public void DesktopAutomationService_GetOwnerWindowProcessInfo_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetOwnerWindowProcessInfo(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first keep-alive start rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_StartWindowKeepAlive_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.StartWindowKeepAlive(IntPtr.Zero, TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first keep-alive stop rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_StopWindowKeepAlive_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.StopWindowKeepAlive(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor lookups return null when the selector cannot match.
    /// </summary>
    public void DesktopAutomationService_GetMonitor_ImpossibleDeviceName_ReturnsNull() {
        var automation = new DesktopAutomationService();

        Monitor? monitor = automation.GetMonitor(deviceName: "__DesktopManager_NoSuchMonitor__");

        Assert.IsNull(monitor);
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor capture rejects missing monitor selectors.
    /// </summary>
    public void DesktopAutomationService_CaptureMonitor_ImpossibleDeviceName_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.CaptureMonitor(deviceName: "__DesktopManager_NoSuchMonitor__"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window geometry rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_GetWindowGeometry_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetWindowGeometry(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window capture rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_CaptureWindow_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.CaptureWindow(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first control capture rejects a zero window handle.
    /// </summary>
    public void DesktopAutomationService_CaptureControl_ZeroWindowHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.CaptureControl(IntPtr.Zero, new IntPtr(1)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first client-area capture rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_CaptureWindowClientArea_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.CaptureWindowClientArea(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for a focused control rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_WaitForFocusedControlObservation_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.WaitForFocusedControlObservation(IntPtr.Zero, 1000, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first control check-state queries reject a zero window handle.
    /// </summary>
    public void DesktopAutomationService_GetControlCheckState_ZeroWindowHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetControlCheckState(IntPtr.Zero, new IntPtr(1)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first control check-state updates reject a zero window handle.
    /// </summary>
    public void DesktopAutomationService_SetControlCheckState_ZeroWindowHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SetControlCheckState(IntPtr.Zero, new IntPtr(1), check: true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-first window process termination rejects a zero handle.
    /// </summary>
    public void DesktopAutomationService_TerminateWindowProcess_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.TerminateWindowProcess(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window process termination rejects a negative exit wait.
    /// </summary>
    public void DesktopAutomationService_TerminateWindowProcess_NegativeWait_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();
        var window = new WindowInfo {
            Handle = new IntPtr(0x1234),
            ProcessId = 123
        };

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.TerminateWindowProcess(window, waitForExitMilliseconds: -1));
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait binding planner rejects null launch metadata.
    /// </summary>
    public void DesktopAutomationService_CreateLaunchWaitBindingPlan_NullLaunch_ThrowsArgumentNullException() {
        Assert.ThrowsException<ArgumentNullException>(() => DesktopAutomationService.CreateLaunchWaitBindingPlan(
            null!,
            launchWindowTitlePattern: null,
            launchWindowClassNamePattern: null,
            windowTitlePattern: null,
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmptyTitles: false,
            all: false,
            followProcessFamily: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot paths are normalized and get a png extension.
    /// </summary>
    public void DesktopStateStore_ResolveCapturePath_AppendsPngExtension() {
        string testRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string requestedPath = Path.Combine(testRoot, "capture-output");

        try {
            string resolvedPath = DesktopStateStore.ResolveCapturePath("desktop", requestedPath);
            string expectedPath = Path.GetFullPath(requestedPath + ".png");

            Assert.AreEqual(expectedPath, resolvedPath, true);
            Assert.AreEqual(".png", Path.GetExtension(resolvedPath), true);
            Assert.IsTrue(Directory.Exists(testRoot));
        } finally {
            if (Directory.Exists(testRoot)) {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot paths normalize non-PNG extensions to PNG.
    /// </summary>
    public void DesktopStateStore_ResolveCapturePath_ReplacesNonPngExtension() {
        string testRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string requestedPath = Path.Combine(testRoot, "capture-output.jpg");

        try {
            string resolvedPath = DesktopStateStore.ResolveCapturePath("desktop", requestedPath);
            string expectedPath = Path.GetFullPath(Path.Combine(testRoot, "capture-output.png"));

            Assert.AreEqual(expectedPath, resolvedPath, true);
            Assert.AreEqual(".png", Path.GetExtension(resolvedPath), true);
            Assert.IsTrue(Directory.Exists(testRoot));
        } finally {
            if (Directory.Exists(testRoot)) {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures target paths use json storage.
    /// </summary>
    public void DesktopStateStore_GetTargetPath_UsesJsonExtension() {
        string path = DesktopStateStore.GetTargetPath("editor-center");

        Assert.AreEqual(".json", Path.GetExtension(path), true);
        StringAssert.Contains(path, Path.DirectorySeparatorChar + "targets" + Path.DirectorySeparatorChar);
    }

    [TestMethod]
    /// <summary>
    /// Ensures reserved Windows device names are rejected for saved state.
    /// </summary>
    public void DesktopStateStore_GetTargetPath_ReservedDeviceName_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => DesktopStateStore.GetTargetPath("CON"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures trailing spaces and dots are normalized away from saved state names.
    /// </summary>
    public void DesktopStateStore_GetTargetPath_TrailingDotsAndSpaces_AreTrimmed() {
        string path = DesktopStateStore.GetTargetPath("editor-center. ");

        Assert.AreEqual("editor-center.json", Path.GetFileName(path), true);
    }

    [TestMethod]
    /// <summary>
    /// Ensures saved window targets round-trip through the shared core.
    /// </summary>
    public void DesktopAutomationService_SaveWindowTarget_RoundTripsDefinition() {
        string targetName = "DesktopAutomationCoreTests-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetTargetPath(targetName);
        var automation = new DesktopAutomationService();

        try {
            DesktopWindowTargetDefinition saved = automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
                Description = "Editor center",
                XRatio = 0.5,
                YRatio = 0.5,
                WidthRatio = 0.8,
                HeightRatio = 0.6,
                ClientArea = true
            });

            DesktopWindowTargetDefinition loaded = automation.GetWindowTarget(targetName);

            Assert.AreEqual("Editor center", saved.Description);
            Assert.AreEqual(saved.Description, loaded.Description);
            Assert.AreEqual(saved.XRatio, loaded.XRatio);
            Assert.AreEqual(saved.YRatio, loaded.YRatio);
            Assert.AreEqual(saved.WidthRatio, loaded.WidthRatio);
            Assert.AreEqual(saved.HeightRatio, loaded.HeightRatio);
            Assert.AreEqual(saved.ClientArea, loaded.ClientArea);
            Assert.IsTrue(File.Exists(path));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures saved control targets round-trip through the shared core.
    /// </summary>
    public void DesktopAutomationService_SaveControlTarget_RoundTripsDefinition() {
        string targetName = "DesktopAutomationCoreTests-Control-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetControlTargetPath(targetName);
        var automation = new DesktopAutomationService();

        try {
            DesktopControlTargetDefinition saved = automation.SaveControlTarget(targetName, new DesktopControlTargetDefinition {
                Description = "Address bar",
                ControlTypePattern = "Edit",
                SupportsBackgroundText = true,
                UseUiAutomation = true
            });

            DesktopControlTargetDefinition loaded = automation.GetControlTarget(targetName);

            Assert.AreEqual("Address bar", saved.Description);
            Assert.AreEqual(saved.Description, loaded.Description);
            Assert.AreEqual(saved.ControlTypePattern, loaded.ControlTypePattern);
            Assert.AreEqual(saved.SupportsBackgroundText, loaded.SupportsBackgroundText);
            Assert.AreEqual(saved.UseUiAutomation, loaded.UseUiAutomation);
            Assert.IsTrue(File.Exists(path));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures control queries reject a missing window selector.
    /// </summary>
    public void DesktopAutomationService_ControlExists_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ControlExists(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control queries reject a missing window selector.
    /// </summary>
    public void DesktopAutomationService_GetControls_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.GetControls(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for controls rejects a missing window selector.
    /// </summary>
    public void DesktopAutomationService_WaitForControls_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.WaitForControls(null!, null, 1000, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for controls rejects negative timeouts.
    /// </summary>
    public void DesktopAutomationService_WaitForControls_NegativeTimeout_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.WaitForControls(new WindowQueryOptions { TitlePattern = "*" }, null, -1, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for controls rejects non-positive polling intervals.
    /// </summary>
    public void DesktopAutomationService_WaitForControls_ZeroInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.WaitForControls(new WindowQueryOptions { TitlePattern = "*" }, null, 1000, 0));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control diagnostics reject a missing window selector.
    /// </summary>
    public void DesktopAutomationService_GetControlDiagnostics_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.GetControlDiagnostics(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control diagnostics reject negative sample sizes.
    /// </summary>
    public void DesktopAutomationService_GetControlDiagnostics_NegativeSampleLimit_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.GetControlDiagnostics(new WindowQueryOptions { TitlePattern = "*" }, sampleLimit: -1));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window-relative clicks reject a negative X coordinate.
    /// </summary>
    public void DesktopAutomationService_ClickWindowPoint_NegativeX_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ClickWindowPoint(new WindowQueryOptions { TitlePattern = "*" }, -1, 0, MouseButton.Left, activate: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window-relative clicks reject a negative Y coordinate.
    /// </summary>
    public void DesktopAutomationService_ClickWindowPoint_NegativeY_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ClickWindowPoint(new WindowQueryOptions { TitlePattern = "*" }, 0, -1, MouseButton.Left, activate: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window-relative drags reject a negative step delay.
    /// </summary>
    public void DesktopAutomationService_DragWindowPoints_NegativeStepDelay_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.DragWindowPoints(new WindowQueryOptions { TitlePattern = "*" }, 0, 0, 1, 1, MouseButton.Left, -1, activate: false, clientArea: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures normalized clicks reject ratios outside the 0..1 range.
    /// </summary>
    public void DesktopAutomationService_ClickWindowPoint_InvalidRatio_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ClickWindowPoint(new WindowQueryOptions { TitlePattern = "*" }, null, null, 1.5, 0.5, MouseButton.Left, activate: false, clientArea: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window geometry rejects a missing selector.
    /// </summary>
    public void DesktopAutomationService_GetWindowGeometry_NullOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.GetWindowGeometry(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch rejects negative window wait values.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_NegativeWindowWait_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "notepad.exe",
            WaitForWindowMilliseconds = -1
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch rejects non-positive window polling intervals.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_ZeroWindowInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "notepad.exe",
            WaitForWindowIntervalMilliseconds = 0
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch can require a real user-facing window when none appears.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_RequireWindowWithoutUserFacingWindow_ThrowsTimeoutException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<TimeoutException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "cmd.exe",
            Arguments = "/c exit",
            WaitForWindowMilliseconds = 1,
            WaitForWindowIntervalMilliseconds = 1,
            RequireWindow = true
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch can require a matching window selector when no match appears.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_RequireWindowWithImpossibleSelector_ThrowsTimeoutException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<TimeoutException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "cmd.exe",
            Arguments = "/c exit",
            WindowTitlePattern = "__DesktopManager_NoSuchWindow__",
            WaitForWindowMilliseconds = 1,
            WaitForWindowIntervalMilliseconds = 1,
            RequireWindow = true
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures target definitions require one horizontal selector.
    /// </summary>
    public void DesktopAutomationService_SaveWindowTarget_MissingHorizontalSelector_ThrowsArgumentException() {
        string targetName = "DesktopAutomationCoreTests-" + Guid.NewGuid().ToString("N");
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
            YRatio = 0.5
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures target area definitions reject zero width.
    /// </summary>
    public void DesktopAutomationService_SaveWindowTarget_ZeroWidth_ThrowsArgumentOutOfRangeException() {
        string targetName = "DesktopAutomationCoreTests-" + Guid.NewGuid().ToString("N");
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
            XRatio = 0.5,
            YRatio = 0.5,
            Width = 0,
            Height = 100
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control target definitions require at least one selector or capability.
    /// </summary>
    public void DesktopAutomationService_SaveControlTarget_WithoutSelectors_ThrowsArgumentException() {
        string targetName = "DesktopAutomationCoreTests-Control-" + Guid.NewGuid().ToString("N");
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SaveControlTarget(targetName, new DesktopControlTargetDefinition()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control-target lookups reject null window selectors.
    /// </summary>
    public void DesktopAutomationService_ControlTargetExists_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ControlTargetExists(null!, "sample"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for a control target rejects a missing window selector.
    /// </summary>
    public void DesktopAutomationService_WaitForControlTarget_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.WaitForControlTarget(null!, "sample", 1000, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control clicks reject null controls.
    /// </summary>
    public void DesktopAutomationService_ClickControl_NullControl_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ClickControl(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control clicks require parent window metadata.
    /// </summary>
    public void DesktopAutomationService_ClickControl_MissingParentWindow_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.ClickControl(new WindowControlInfo {
            Handle = new IntPtr(1)
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control text updates require parent window metadata.
    /// </summary>
    public void DesktopAutomationService_SetControlText_MissingParentWindow_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.SetControlText(new WindowControlInfo {
            Handle = new IntPtr(1)
        }, "hello"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control key sends require parent window metadata.
    /// </summary>
    public void DesktopAutomationService_SendControlKeys_MissingParentWindow_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.SendControlKeys(new WindowControlInfo {
            Handle = new IntPtr(1)
        }, new[] { VirtualKey.VK_A }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation text routing requires an explicitly fallback-capable control.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_UnsupportedControl_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = false
        }, allowForegroundInputFallback: true);

        StringAssert.Contains(error, "does not expose direct value setting");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation text routing requires an explicit foreground-input opt-in.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_MissingOptIn_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: false);

        StringAssert.Contains(error, "Enable foreground input fallback");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation text routing allows explicitly approved foreground fallback.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_AllowedControl_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: true);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-backed controls bypass the zero-handle UI Automation text fallback checks.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_HandleBackedControl_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = new IntPtr(123),
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: false);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation key routing requires a fallback-capable control.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_UnsupportedControl_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = false
        }, allowForegroundInputFallback: true);

        StringAssert.Contains(error, "cannot receive foreground fallback key input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation key routing requires an explicit foreground-input opt-in.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_MissingOptIn_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: false);

        StringAssert.Contains(error, "does not expose a Win32 handle");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation key routing allows explicitly approved foreground fallback.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_AllowedControl_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: true);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures Win32 controls bypass the zero-handle UI Automation key fallback checks.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_Win32Control_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.Win32,
            Handle = IntPtr.Zero,
            SupportsForegroundInputFallback = false
        }, allowForegroundInputFallback: false);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures direct control text updates reject missing Win32 handles.
    /// </summary>
    public void WindowControlService_SetText_InvalidHandle_ThrowsArgumentException() {
        var control = new WindowControlInfo();

        Assert.ThrowsException<ArgumentException>(() => WindowControlService.SetText(control, "hello"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures direct control key sends reject missing Win32 handles.
    /// </summary>
    public void WindowControlService_SendKeys_InvalidHandle_ThrowsArgumentException() {
        var control = new WindowControlInfo();

        Assert.ThrowsException<ArgumentException>(() => WindowControlService.SendKeys(control, VirtualKey.VK_A));
    }

    [TestMethod]
    /// <summary>
    /// Ensures direct control key sends require at least one key.
    /// </summary>
    public void WindowControlService_SendKeys_NoKeys_ThrowsArgumentException() {
        var control = new WindowControlInfo {
            Handle = new IntPtr(1)
        };

        Assert.ThrowsException<ArgumentException>(() => WindowControlService.SendKeys(control, Array.Empty<VirtualKey>()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground key sending requires at least one key.
    /// </summary>
    public void KeyboardInputService_SendToForeground_NoKeys_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => KeyboardInputService.SendToForeground(Array.Empty<VirtualKey>()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground text sending rejects null text.
    /// </summary>
    public void KeyboardInputService_SendTextToForeground_NullText_ThrowsArgumentNullException() {
        Assert.ThrowsException<ArgumentNullException>(() => KeyboardInputService.SendTextToForeground(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground text sending rejects negative delays.
    /// </summary>
    public void KeyboardInputService_SendTextToForeground_NegativeDelay_ThrowsArgumentOutOfRangeException() {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => KeyboardInputService.SendTextToForeground("test", -1));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing prefers real foreground input when the target already owns focus.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_WithForegroundTarget_UsesForegroundInput() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true
        }, targetOwnsForeground: true);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.ForegroundInput, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing falls back to message delivery when foreground ownership is unavailable and strict typing is not required.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_WithoutForegroundTarget_UsesWindowMessageFallback() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true
        }, targetOwnsForeground: false);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.WindowMessage, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures strict foreground typing fails when the target window does not own the foreground.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_RequireForegroundWithoutForeground_ThrowsInvalidOperationException() {
        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            RequireForegroundWindowForTyping = true
        }, targetOwnsForeground: false));

        StringAssert.Contains(exception.Message, "Current:");
    }

    [TestMethod]
    /// <summary>
    /// Ensures strict foreground typing rejects message-only routing.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_RequireForegroundWithMessageOnly_ThrowsInvalidOperationException() {
        Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = false,
            RequireForegroundWindowForTyping = true
        }, targetOwnsForeground: true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures physical-key typing requires the target window to own the foreground.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_PhysicalKeysWithoutForeground_ThrowsInvalidOperationException() {
        Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UsePhysicalKeyboardLayout = true
        }, targetOwnsForeground: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures physical-key typing still routes through the foreground delivery path when focus ownership is available.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_PhysicalKeysWithForeground_UsesForegroundInput() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UsePhysicalKeyboardLayout = true
        }, targetOwnsForeground: true);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.ForegroundInput, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session scan code typing requires the target window to own the foreground.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_HostedSessionWithoutForeground_ThrowsInvalidOperationException() {
        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UseHostedSessionScanCodes = true
        }, targetOwnsForeground: false));

        StringAssert.Contains(exception.Message, "Current:");
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session scan code typing routes through foreground delivery when the target owns focus.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_HostedSessionWithForeground_UsesForegroundInput() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UseHostedSessionScanCodes = true
        }, targetOwnsForeground: true);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.ForegroundInput, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures script chunking preserves blank lines and line breaks.
    /// </summary>
    public void WindowInputService_CreateScriptChunks_PreservesBlankLines() {
        IReadOnlyList<WindowInputService.WindowScriptChunk> chunks = WindowInputService.CreateScriptChunks("line1\n\nline3", 120);

        Assert.AreEqual(3, chunks.Count);
        Assert.AreEqual("line1", chunks[0].Text);
        Assert.IsTrue(chunks[0].SendLineBreak);
        Assert.AreEqual(string.Empty, chunks[1].Text);
        Assert.IsTrue(chunks[1].SendLineBreak);
        Assert.AreEqual("line3", chunks[2].Text);
        Assert.IsFalse(chunks[2].SendLineBreak);
    }

    [TestMethod]
    /// <summary>
    /// Ensures script chunking splits long lines into smaller segments and only marks the final chunk with a line break.
    /// </summary>
    public void WindowInputService_CreateScriptChunks_SplitsLongLinesSafely() {
        IReadOnlyList<WindowInputService.WindowScriptChunk> chunks = WindowInputService.CreateScriptChunks("abcdef\ngh", 3);

        Assert.AreEqual(3, chunks.Count);
        Assert.AreEqual("abc", chunks[0].Text);
        Assert.IsFalse(chunks[0].SendLineBreak);
        Assert.AreEqual("def", chunks[1].Text);
        Assert.IsTrue(chunks[1].SendLineBreak);
        Assert.AreEqual("gh", chunks[2].Text);
        Assert.IsFalse(chunks[2].SendLineBreak);
    }
}
