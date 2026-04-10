using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// End-to-end MCP server tests against a disposable desktop application.
/// </summary>
public class McpServerEndToEndTests {
    private const int TestAppLaunchTimeoutMs = 20000;
    private const int TestAppControlDiscoveryTimeoutMs = 20000;
    private const string TestAppProcessName = "DesktopManager.TestApp";
    private const string TestAppTitlePrefix = "DesktopManager-McpTestApp";
    private const string TestAppCommandBarSurface = "commandbar";
    private const string TestAppCommandBarAutomationId = "CommandBarTextBox";
    private const string TestAppCommandBarControlType = "Edit";

    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can launch the local desktop test app, discover an editable control, set text, assert the value, and capture an artifact.
    /// </summary>
    public void McpServer_TestAppRoundTrip_LaunchesSetsTextAssertsValueAndCapturesArtifact() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        string windowTitle = CreateTestAppWindowTitle("roundtrip");
        string testAppPath = RequireTestAppExecutablePath();
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            JsonElement launchResult = client.CallTool(2, "launch_and_wait_for_window", new {
                filePath = testAppPath,
                arguments = BuildTestAppArguments(windowTitle),
                timeoutMs = TestAppLaunchTimeoutMs,
                intervalMs = 100,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(launchResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(launchResult.GetProperty("WindowWait").GetProperty("Count").GetInt32() > 0);
            resolvedProcessId = ReadResolvedProcessId(launchResult.GetProperty("Launch"));
            Assert.IsTrue(resolvedProcessId > 0);
            Assert.AreEqual("resolved-process-id", launchResult.GetProperty("WaitBinding").GetString());
            Assert.AreEqual(resolvedProcessId, launchResult.GetProperty("BoundProcessId").GetInt32());
            Assert.AreEqual(JsonValueKind.Null, launchResult.GetProperty("BoundProcessName").ValueKind);
            AssertScreenshotPathsExist(launchResult.GetProperty("AfterScreenshots"));
            JsonElement launchedWindow = launchResult.GetProperty("WindowWait").GetProperty("Windows")[0];
            string launchedWindowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(launchedWindowHandle), "Expected the launched test app window to expose a handle.");

            int requestId = 3;
            JsonElement editor = WaitForEditableTextControl(client, ref requestId, launchedWindowHandle, TestAppControlDiscoveryTimeoutMs, 100);
            string editorClassName = editor.GetProperty("ClassName").GetString() ?? string.Empty;
            string editorHandle = editor.GetProperty("Handle").GetString() ?? string.Empty;
            string windowHandle = editor.GetProperty("ParentWindow").GetProperty("Handle").GetString() ?? string.Empty;
            string editorAutomationId = ReadOptionalString(editor, "AutomationId");
            string editorControlType = ReadOptionalString(editor, "ControlType");
            bool useUiAutomationEditor = !string.IsNullOrWhiteSpace(editorAutomationId) && !string.IsNullOrWhiteSpace(editorControlType);
            Assert.IsFalse(string.IsNullOrWhiteSpace(editorClassName), "Expected an editable test app control class.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the resolved editor control to include its parent window handle.");

            string resolvedControlHandle = editorHandle;
            if (!useUiAutomationEditor && string.IsNullOrWhiteSpace(resolvedControlHandle)) {
                JsonElement waitedControl = client.CallTool(requestId++, "wait_for_control", new {
                    windowHandle,
                    controlClassName = editorClassName,
                    timeoutMs = 5000,
                    intervalMs = 100
                });
                Assert.IsTrue(waitedControl.GetProperty("Count").GetInt32() >= 1, "Expected MCP to wait for the test app editor control before mutating it.");
                JsonElement freshEditor = waitedControl.GetProperty("Controls")[0];
                resolvedControlHandle = freshEditor.GetProperty("Handle").GetString() ?? string.Empty;
            }

            if (!useUiAutomationEditor) {
                Assert.IsFalse(string.IsNullOrWhiteSpace(resolvedControlHandle), "Expected the resolved test app editor control to expose a handle.");
            }

            string expectedText = "DesktopManager MCP E2E " + Guid.NewGuid().ToString("N");
            JsonElement setTextResult = client.CallTool(requestId++, "set_control_text", new {
                windowHandle,
                controlHandle = useUiAutomationEditor ? null : resolvedControlHandle,
                controlAutomationId = useUiAutomationEditor ? editorAutomationId : null,
                controlType = useUiAutomationEditor ? editorControlType : null,
                uiAutomation = useUiAutomationEditor,
                ensureForegroundWindow = useUiAutomationEditor,
                text = expectedText,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(setTextResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(setTextResult.GetProperty("Count").GetInt32() >= 1);
            StringAssert.Contains(setTextResult.GetProperty("SafetyMode").GetString() ?? string.Empty, "background");
            AssertScreenshotPathsExist(setTextResult.GetProperty("AfterScreenshots"));

            JsonElement assertionResult = WaitForControlValueMatch(
                client,
                ref requestId,
                windowHandle,
                expectedText,
                5000,
                100,
                controlHandle: useUiAutomationEditor ? null : resolvedControlHandle,
                controlAutomationId: useUiAutomationEditor ? editorAutomationId : null,
                controlType: useUiAutomationEditor ? editorControlType : null,
                uiAutomation: useUiAutomationEditor,
                includeUiAutomation: !useUiAutomationEditor,
                ensureForegroundWindow: useUiAutomationEditor);

            Assert.IsTrue(assertionResult.GetProperty("Matched").GetBoolean(), "Expected MCP to verify the test app editor value after text entry.");
            Assert.IsTrue(assertionResult.GetProperty("MatchedCount").GetInt32() >= 1);
        } finally {
            if (resolvedProcessId > 0) {
                try {
                    using Process process = Process.GetProcessById(resolvedProcessId);
                    TestHelper.SafeKillProcess(process);
                } catch {
                    // Ignore cleanup failures for already exited processes.
                }
            }

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can save a reusable target area, resolve it against the local desktop test app, and capture that exact region.
    /// </summary>
    public void McpServer_TestAppTargetAreaRoundTrip_SavesResolvesAndCapturesTarget() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        string targetName = "McpServerEndToEnd-" + Guid.NewGuid().ToString("N");
        string targetPath = DesktopStateStore.GetTargetPath(targetName);
        string windowTitle = CreateTestAppWindowTitle("target");
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchTestAppProcess(windowTitle, out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "target capture");

            JsonElement saveTargetResult = client.CallTool(requestId++, "save_window_target", new {
                name = targetName,
                description = "Test app center area",
                xRatio = 0.25,
                yRatio = 0.2,
                widthRatio = 0.5,
                heightRatio = 0.5,
                clientArea = true
            });

            Assert.AreEqual(targetName, saveTargetResult.GetProperty("Name").GetString());
            Assert.IsTrue(File.Exists(targetPath));

            JsonElement resolvedTargets = client.CallTool(requestId++, "resolve_window_target", new {
                name = targetName,
                processId = resolvedProcessId
            });

            Assert.IsTrue(resolvedTargets.ValueKind == JsonValueKind.Array && resolvedTargets.GetArrayLength() == 1, "Expected the named target to resolve against exactly one test app window.");
            JsonElement resolvedTarget = resolvedTargets[0];
            Assert.AreEqual(targetName, resolvedTarget.GetProperty("Name").GetString());
            Assert.IsTrue(resolvedTarget.GetProperty("Target").GetProperty("ClientArea").GetBoolean());
            int screenWidth = resolvedTarget.GetProperty("ScreenWidth").GetInt32();
            int screenHeight = resolvedTarget.GetProperty("ScreenHeight").GetInt32();
            Assert.IsTrue(screenWidth > 0);
            Assert.IsTrue(screenHeight > 0);

            string outputPath = Path.Combine(artifactDirectory, "testapp-target.png");
            JsonElement screenshotResult = client.CallTool(requestId++, "screenshot_window", new {
                processId = resolvedProcessId,
                targetName,
                outputPath
            });

            Assert.AreEqual("window-target", screenshotResult.GetProperty("Kind").GetString());
            Assert.AreEqual(screenWidth, screenshotResult.GetProperty("Width").GetInt32());
            Assert.AreEqual(screenHeight, screenshotResult.GetProperty("Height").GetInt32());
            Assert.AreEqual(resolvedProcessId, screenshotResult.GetProperty("Window").GetProperty("ProcessId").GetInt32());
            Assert.IsTrue(File.Exists(screenshotResult.GetProperty("Path").GetString() ?? string.Empty));
            Assert.IsTrue(screenshotResult.TryGetProperty("Geometry", out JsonElement geometry));
            Assert.IsTrue(geometry.GetProperty("ClientWidth").GetInt32() >= screenshotResult.GetProperty("Width").GetInt32());
            Assert.IsTrue(geometry.GetProperty("ClientHeight").GetInt32() >= screenshotResult.GetProperty("Height").GetInt32());
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (File.Exists(targetPath)) {
                File.Delete(targetPath);
            }

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can move a live window, return screenshot artifacts, and verify the new geometry.
    /// </summary>
    public void McpServer_TestAppWindowMutationRoundTrip_MovesWindowAndVerifiesGeometry() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        string windowTitle = CreateTestAppWindowTitle("move");
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchTestAppProcess(windowTitle, out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "window mutation");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched test app window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();
            int targetLeft = initialLeft >= 40 ? initialLeft - 20 : initialLeft + 20;
            int targetTop = initialTop >= 40 ? initialTop - 20 : initialTop + 20;

            JsonElement moveResult = client.CallTool(requestId++, "move_window", new {
                handle = windowHandle,
                x = targetLeft,
                y = targetTop,
                width = initialWidth,
                height = initialHeight,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(moveResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(moveResult.GetProperty("Count").GetInt32() >= 1);
            AssertScreenshotPathsExist(moveResult.GetProperty("AfterScreenshots"));

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(targetLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 10, "Moved window left position");
            AssertIntWithinTolerance(targetTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 10, "Moved window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 10, "Moved window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 10, "Moved window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can run a higher-level workflow against a live local desktop test app window and return structured evidence.
    /// </summary>
    public void McpServer_TestAppWorkflowRoundTrip_PreparesForCodingAndCapturesArtifact() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        string windowTitle = CreateTestAppWindowTitle("workflow");
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchTestAppProcess(windowTitle, out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "workflow");
            string launchedWindowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(launchedWindowHandle), "Expected the launched test app window to expose a handle.");

            JsonElement workflowResult = client.CallTool(requestId++, "prepare_for_coding", new {
                handle = launchedWindowHandle,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(workflowResult.GetProperty("Success").GetBoolean());
            Assert.IsFalse(workflowResult.GetProperty("LayoutApplied").GetBoolean());
            Assert.AreEqual("prepare-for-coding", workflowResult.GetProperty("Action").GetString());
            bool resolvedWindowMatched = workflowResult.TryGetProperty("ResolvedWindow", out JsonElement resolvedWindow) &&
                resolvedWindow.ValueKind == JsonValueKind.Object &&
                resolvedWindow.GetProperty("ProcessId").GetInt32() == resolvedProcessId &&
                string.Equals(launchedWindowHandle, resolvedWindow.GetProperty("Handle").GetString(), StringComparison.OrdinalIgnoreCase);

            bool focusedWindowMatched = false;
            if (workflowResult.TryGetProperty("FocusedWindow", out JsonElement focusedWindow) && focusedWindow.ValueKind == JsonValueKind.Object) {
                focusedWindowMatched =
                    focusedWindow.GetProperty("ProcessId").GetInt32() == resolvedProcessId &&
                    string.Equals(launchedWindowHandle, focusedWindow.GetProperty("Handle").GetString(), StringComparison.OrdinalIgnoreCase);
            }

            JsonElement notes = workflowResult.GetProperty("Notes");
            bool hasNotes = notes.ValueKind == JsonValueKind.Array && notes.GetArrayLength() > 0;
            Assert.IsTrue(resolvedWindowMatched || focusedWindowMatched || hasNotes, "Expected the workflow to return either resolved/focused window evidence or explanatory notes.");
            AssertScreenshotPathsExist(workflowResult.GetProperty("AfterScreenshots"));
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP process allowlists permit a scoped live test app mutation when both process name and exact handle are supplied.
    /// </summary>
    public void McpServer_TestAppAllowedProcessPolicy_AllowsScopedMoveWindowMutation() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        string windowTitle = CreateTestAppWindowTitle("allow");
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process " + TestAppProcessName);
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchTestAppProcess(windowTitle, out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "allowed-process policy");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched test app window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();
            int targetLeft = initialLeft >= 60 ? initialLeft - 30 : initialLeft + 30;
            int targetTop = initialTop >= 60 ? initialTop - 30 : initialTop + 30;

            JsonElement moveResult = client.CallTool(requestId++, "move_window", new {
                processName = TestAppProcessName,
                handle = windowHandle,
                x = targetLeft,
                y = targetTop,
                width = initialWidth,
                height = initialHeight,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(moveResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(moveResult.GetProperty("Count").GetInt32() >= 1);
            AssertScreenshotPathsExist(moveResult.GetProperty("AfterScreenshots"));

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(targetLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 10, "Allowed-policy moved window left position");
            AssertIntWithinTolerance(targetTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 10, "Allowed-policy moved window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 10, "Allowed-policy moved window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 10, "Allowed-policy moved window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP denied-process filters block a scoped live test app window mutation without changing geometry.
    /// </summary>
    public void McpServer_TestAppDeniedProcessPolicy_BlocksScopedMoveWindowMutation() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string windowTitle = CreateTestAppWindowTitle("deny");
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            LaunchTestAppProcess(windowTitle, out launcherProcessId, out resolvedProcessId);

            using var client = McpTestClient.Start("mcp serve --allow-mutations --deny-process " + TestAppProcessName);
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "denied-process policy");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the denied-policy test app window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();

            JsonElement toolError = client.CallToolExpectError(requestId++, "move_window", new {
                processName = TestAppProcessName,
                handle = windowHandle,
                x = initialLeft + 40,
                y = initialTop + 40,
                width = initialWidth,
                height = initialHeight
            });
            StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "denied-process policy");

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(initialLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 5, "Denied-policy blocked window left position");
            AssertIntWithinTolerance(initialTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 5, "Denied-policy blocked window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 5, "Denied-policy blocked window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 5, "Denied-policy blocked window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP dry-run mode previews a scoped live test app window mutation without changing geometry.
    /// </summary>
    public void McpServer_TestAppDryRunPolicy_PreviewsScopedMoveWindowMutationWithoutChangingGeometry() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        string windowTitle = CreateTestAppWindowTitle("dryrun");
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            LaunchTestAppProcess(windowTitle, out launcherProcessId, out resolvedProcessId);

            using var client = McpTestClient.Start("mcp serve --dry-run --allow-process " + TestAppProcessName);
            JsonElement initializeResult = client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });
            Assert.IsTrue(initializeResult.GetProperty("safetyPolicy").GetProperty("dryRun").GetBoolean());

            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "dry-run policy");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the dry-run test app window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();

            JsonElement dryRunResult = client.CallTool(requestId++, "move_window", new {
                processName = TestAppProcessName,
                handle = windowHandle,
                x = initialLeft + 40,
                y = initialTop + 40,
                width = initialWidth,
                height = initialHeight
            });

            Assert.IsTrue(dryRunResult.GetProperty("dryRun").GetBoolean());
            Assert.IsFalse(dryRunResult.GetProperty("applied").GetBoolean());
            Assert.AreEqual("move_window", dryRunResult.GetProperty("toolName").GetString());
            Assert.AreEqual("dry-run", dryRunResult.GetProperty("safetyMode").GetString());
            Assert.AreEqual(TestAppProcessName, dryRunResult.GetProperty("requestedProcesses")[0].GetString());

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(initialLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 5, "Dry-run preserved window left position");
            AssertIntWithinTolerance(initialTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 5, "Dry-run preserved window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 5, "Dry-run preserved window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 5, "Dry-run preserved window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    [TestCategory("ExperimentalUITest")]
    /// <summary>
    /// Ensures the local WPF command bar key path is blocked when foreground-input fallback is requested without server opt-in.
    /// </summary>
    public void McpServer_TestAppForegroundInputPolicy_BlocksCommandBarEnterWithoutServerOptIn() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExperimentalDesktopChanges();
        TestHelper.RequireInteractiveDesktopSession();

        string windowTitle = CreateTestAppWindowTitle("commandbar-blocked");
        string commandText = "blocked-" + Guid.NewGuid().ToString("N");
        string expectedTitle = windowTitle + " - Accepted - " + commandText;
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            LaunchTestAppProcess(windowTitle, TestAppCommandBarSurface, out launcherProcessId, out resolvedProcessId);

            using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process " + TestAppProcessName);
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "foreground-input blocked");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched test app window to expose a handle.");
            WaitForCommandBarControl(client, ref requestId, windowHandle);

            JsonElement setTextResult = client.CallTool(requestId++, "set_control_text", new {
                processName = TestAppProcessName,
                windowHandle,
                controlAutomationId = TestAppCommandBarAutomationId,
                controlType = TestAppCommandBarControlType,
                uiAutomation = true,
                ensureForegroundWindow = true,
                text = commandText
            });
            Assert.IsTrue(setTextResult.GetProperty("Success").GetBoolean());
            Assert.AreEqual("uia-direct-value", setTextResult.GetProperty("SafetyMode").GetString());

            JsonElement toolError = client.CallToolExpectError(requestId++, "send_control_keys", new {
                processName = TestAppProcessName,
                windowHandle,
                controlAutomationId = TestAppCommandBarAutomationId,
                controlType = TestAppCommandBarControlType,
                uiAutomation = true,
                ensureForegroundWindow = true,
                allowForegroundInput = true,
                keys = new[] { "VK_RETURN" }
            });
            StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "--allow-foreground-input");

            JsonElement titleStillOriginal = client.CallTool(requestId++, "window_exists", new {
                processId = resolvedProcessId,
                windowTitle = expectedTitle
            });
            Assert.IsFalse(titleStillOriginal.GetProperty("Matched").GetBoolean(), "Expected the blocked foreground-input path to leave the command bar action unapplied.");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    [TestCategory("ExperimentalUITest")]
    /// <summary>
    /// Ensures the local WPF command bar key path succeeds when both the MCP server and tool call explicitly opt into foreground-input fallback.
    /// </summary>
    public void McpServer_TestAppForegroundInputPolicy_AllowsCommandBarEnterWithServerOptIn() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExperimentalDesktopChanges();
        TestHelper.RequireInteractiveDesktopSession();

        string windowTitle = CreateTestAppWindowTitle("commandbar-allow");
        string commandText = "allow-" + Guid.NewGuid().ToString("N");
        string expectedTitle = windowTitle + " - Accepted - " + commandText;
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            LaunchTestAppProcess(windowTitle, TestAppCommandBarSurface, out launcherProcessId, out resolvedProcessId);
            SetTestAppCommandBarText(resolvedProcessId, windowTitle, commandText);

            using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process " + TestAppProcessName + " --allow-foreground-input");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, TestAppLaunchTimeoutMs, 100, "foreground-input allowed");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched test app window to expose a handle.");

            JsonElement focusResult = client.CallTool(requestId++, "focus_window", new {
                processName = TestAppProcessName,
                handle = windowHandle
            });
            Assert.IsTrue(focusResult.GetProperty("Success").GetBoolean());

            WaitForCommandBarControl(client, ref requestId, windowHandle);

            JsonElement sendKeysResult = client.CallTool(requestId++, "send_control_keys", new {
                processName = TestAppProcessName,
                processId = resolvedProcessId,
                windowTitle,
                controlAutomationId = TestAppCommandBarAutomationId,
                controlType = TestAppCommandBarControlType,
                supportsForegroundInputFallback = true,
                uiAutomation = true,
                ensureForegroundWindow = true,
                allowForegroundInput = true,
                keys = new[] { "VK_RETURN" }
            });
            Assert.IsTrue(sendKeysResult.GetProperty("Success").GetBoolean());
            Assert.AreEqual("foreground-input-fallback", sendKeysResult.GetProperty("SafetyMode").GetString());

            JsonElement acceptedWindow = client.CallTool(requestId++, "wait_for_window", new {
                processId = resolvedProcessId,
                windowTitle = expectedTitle,
                timeoutMs = TestAppLaunchTimeoutMs,
                intervalMs = 100
            });
            Assert.IsTrue(acceptedWindow.GetProperty("Count").GetInt32() >= 1, "Expected the command bar Enter action to update the test app window title.");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);
        }
    }

    private static int ReadResolvedProcessId(JsonElement launchResult) {
        JsonElement resolvedProcessId = launchResult.GetProperty("ResolvedProcessId");
        if (resolvedProcessId.ValueKind == JsonValueKind.Number) {
            return resolvedProcessId.GetInt32();
        }

        return launchResult.GetProperty("ProcessId").GetInt32();
    }

    private static void LaunchTestAppProcess(string windowTitle, out int launcherProcessId, out int resolvedProcessId) {
        LaunchTestAppProcess(windowTitle, surface: null, out launcherProcessId, out resolvedProcessId);
    }

    private static void LaunchTestAppProcess(string windowTitle, string? surface, out int launcherProcessId, out int resolvedProcessId) {
        var automation = new DesktopAutomationService();
        string executablePath = RequireTestAppExecutablePath();
        DesktopProcessLaunchInfo launch = automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = executablePath,
            Arguments = BuildTestAppArguments(windowTitle, surface),
            WaitForInputIdleMilliseconds = 5000,
            WaitForWindowMilliseconds = TestAppLaunchTimeoutMs,
            WaitForWindowIntervalMilliseconds = 100,
            RequireWindow = true
        });

        launcherProcessId = launch.ProcessId;
        resolvedProcessId = launch.ResolvedProcessId ?? launch.ProcessId;
        TestHelper.TrackProcessId(launcherProcessId);
        TestHelper.TrackProcessId(resolvedProcessId);
        Assert.IsTrue(launcherProcessId > 0, "Expected direct test app setup to return a launcher process id.");
        Assert.IsTrue(resolvedProcessId > 0, "Expected direct test app setup to resolve the live window process.");
        Assert.IsNotNull(launch.MainWindow, "Expected direct test app setup to resolve a visible window.");
    }

    private static void SetTestAppCommandBarText(int processId, string windowTitle, string text) {
        var automation = new DesktopAutomationService();
        IReadOnlyList<WindowControlTargetInfo> controls = automation.SetControlText(
            new WindowQueryOptions {
                ProcessId = processId,
                TitlePattern = windowTitle,
                IncludeOwned = true,
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeEmptyTitles = true
            },
            new WindowControlQueryOptions {
                AutomationIdPattern = TestAppCommandBarAutomationId,
                ControlTypePattern = TestAppCommandBarControlType,
                SupportsForegroundInputFallback = true,
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            text);
        Assert.IsTrue(controls.Count >= 1, "Expected direct command bar text setup to resolve the WPF command bar control.");
    }

    private static void RequireNet8McpLiveHarness() {
#if NET472
        Assert.Inconclusive("Live MCP desktop harness runs only under net8.0-windows to avoid driving the same desktop twice through the shared net8 CLI executable.");
#endif
    }

    private static string RequireTestAppExecutablePath() {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null) {
            DirectoryInfo debugDirectory = new(Path.Combine(current.FullName, "Sources", "DesktopManager.TestApp", "bin", "Debug"));
            if (debugDirectory.Exists) {
                FileInfo? debugCandidate = debugDirectory
                    .EnumerateDirectories("net8.0-windows*")
                    .OrderByDescending(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(directory => new FileInfo(Path.Combine(directory.FullName, "DesktopManager.TestApp.exe")))
                    .FirstOrDefault(file => file.Exists);
                if (debugCandidate != null) {
                    return debugCandidate.FullName;
                }
            }

            DirectoryInfo releaseDirectory = new(Path.Combine(current.FullName, "Sources", "DesktopManager.TestApp", "bin", "Release"));
            if (releaseDirectory.Exists) {
                FileInfo? releaseCandidate = releaseDirectory
                    .EnumerateDirectories("net8.0-windows*")
                    .OrderByDescending(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(directory => new FileInfo(Path.Combine(directory.FullName, "DesktopManager.TestApp.exe")))
                    .FirstOrDefault(file => file.Exists);
                if (releaseCandidate != null) {
                    return releaseCandidate.FullName;
                }
            }

            current = current.Parent;
        }

        Assert.Inconclusive("DesktopManager.TestApp.exe was not found. Build the DesktopManager.TestApp project before running the live MCP harness.");
        return string.Empty;
    }

    private static string CreateTestAppWindowTitle(string scenario) {
        return TestAppTitlePrefix + "-" + scenario + "-" + Guid.NewGuid().ToString("N");
    }

    private static string BuildTestAppArguments(string windowTitle, string? surface = null) {
        string arguments = "--title " + windowTitle + " --text seed";
        if (!string.IsNullOrWhiteSpace(surface)) {
            arguments += " --surface " + surface;
        }

        return arguments;
    }

    private static void WaitForCommandBarControl(McpTestClient client, ref int requestId, string windowHandle, int timeoutMilliseconds = TestAppControlDiscoveryTimeoutMs) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            JsonElement controls = client.CallTool(requestId++, "list_window_controls", new {
                processName = TestAppProcessName,
                windowHandle,
                controlAutomationId = TestAppCommandBarAutomationId,
                controlType = TestAppCommandBarControlType,
                supportsForegroundInputFallback = true,
                uiAutomation = true,
                ensureForegroundWindow = true
            });
            if (controls.ValueKind == JsonValueKind.Array && controls.GetArrayLength() > 0) {
                return;
            }

            System.Threading.Thread.Sleep(100);
        }

        Assert.Fail($"Timed out after {timeoutMilliseconds}ms waiting for the test app command bar control to be discoverable through MCP.");
    }

    private static JsonElement WaitForProcessWindow(McpTestClient client, ref int requestId, int processId, int timeoutMilliseconds, int intervalMilliseconds, string scenario) {
        JsonElement windows = client.CallTool(requestId++, "wait_for_window", new {
            processId,
            timeoutMs = timeoutMilliseconds,
            intervalMs = intervalMilliseconds
        });
        Assert.IsTrue(windows.GetProperty("Count").GetInt32() >= 1, $"Expected MCP to resolve a live window for the {scenario} scenario.");
        return windows.GetProperty("Windows")[0];
    }

    private static string ReadOptionalString(JsonElement element, string propertyName) {
        if (element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String) {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static JsonElement WaitForControlValueMatch(
        McpTestClient client,
        ref int requestId,
        string windowHandle,
        string expectedValue,
        int timeoutMilliseconds,
        int intervalMilliseconds,
        string? controlHandle = null,
        string? controlAutomationId = null,
        string? controlType = null,
        bool uiAutomation = false,
        bool includeUiAutomation = true,
        bool ensureForegroundWindow = false) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        JsonElement lastResult = default;
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            lastResult = client.CallTool(requestId++, "assert_control_value", new {
                windowHandle,
                controlHandle,
                controlAutomationId,
                controlType,
                uiAutomation,
                includeUiAutomation,
                ensureForegroundWindow,
                expectedValue
            });
            if (lastResult.GetProperty("Matched").GetBoolean()) {
                return lastResult;
            }

            System.Threading.Thread.Sleep(intervalMilliseconds);
        }

        return lastResult;
    }

    private static void KillProcessById(int processId) {
        if (processId <= 0) {
            return;
        }

        try {
            using Process process = Process.GetProcessById(processId);
            TestHelper.SafeKillProcess(process);
        } catch {
            // Ignore cleanup failures for already exited processes.
        }
    }

    private static void TryDeleteDirectory(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        } catch {
            // Ignore cleanup failures for files still locked by the browser.
        }
    }

    private static void TryDeleteFile(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch {
            // Ignore cleanup failures for already removed files.
        }
    }

    private static JsonElement FindEditableTextControl(JsonElement controls) {
        if (TryFindEditableTextControl(controls, out JsonElement control)) {
            return control;
        }

        Assert.Inconclusive("No editable text control with background text support was exposed through MCP.");
        return default;
    }

    private static JsonElement WaitForEditableTextControl(McpTestClient client, ref int requestId, string windowHandle, int timeoutMilliseconds, int intervalMilliseconds) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            JsonElement controls = client.CallTool(requestId++, "list_window_controls", new {
                windowHandle,
                includeUiAutomation = true,
                ensureForegroundWindow = true
            });
            if (controls.ValueKind == JsonValueKind.Array &&
                controls.GetArrayLength() > 0 &&
                TryFindEditableTextControl(controls, out JsonElement control)) {
                return control;
            }

            System.Threading.Thread.Sleep(intervalMilliseconds);
        }

        Assert.Fail($"Timed out after {timeoutMilliseconds}ms waiting for the test app to expose an editable control through MCP.");
        return default;
    }

    private static bool TryFindEditableTextControl(JsonElement controls, out JsonElement control) {
        JsonElement? richEdit = null;
        JsonElement? fallback = null;

        foreach (JsonElement candidate in controls.EnumerateArray()) {
            string className = candidate.GetProperty("ClassName").GetString() ?? string.Empty;
            bool supportsBackgroundText = candidate.GetProperty("SupportsBackgroundText").GetBoolean();
            if (!supportsBackgroundText) {
                continue;
            }

            if (string.Equals(className, "RichEditD2DPT", StringComparison.OrdinalIgnoreCase)) {
                richEdit = candidate.Clone();
                break;
            }

            if (fallback == null &&
                (className.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 className.IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0)) {
                fallback = candidate.Clone();
            }
            }

            if (richEdit.HasValue) {
                control = richEdit.Value;
                return true;
        }

        if (fallback.HasValue) {
            control = fallback.Value;
            return true;
        }

        control = default;
        return false;
    }

    private static void AssertScreenshotPathsExist(JsonElement screenshots) {
        if (screenshots.ValueKind != JsonValueKind.Array) {
            Assert.Fail("Expected screenshots to be returned as an array.");
        }

        Assert.IsTrue(screenshots.GetArrayLength() > 0, "Expected at least one screenshot artifact.");
        foreach (JsonElement screenshot in screenshots.EnumerateArray()) {
            string? path = screenshot.GetProperty("Path").GetString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(path), "Expected a non-empty artifact path.");
            Assert.IsTrue(File.Exists(path), $"Expected artifact file to exist: {path}");
        }
    }

    private static void AssertIntWithinTolerance(int expected, int actual, int tolerance, string label) {
        int delta = Math.Abs(expected - actual);
        Assert.IsTrue(delta <= tolerance, $"{label} expected {expected} but was {actual} (delta {delta}, tolerance {tolerance}).");
    }
}
