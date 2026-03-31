using System.Text.Json;

namespace DesktopManager.Tests;

#if NET8_0_OR_GREATER
[TestClass]
/// <summary>
/// Tests for MCP safety-policy decisions and operator guidance text.
/// </summary>
public class McpSafetyPolicyTests {
    [TestMethod]
    /// <summary>
    /// Ensures foreground-input guidance is explicit when the server blocks zero-handle UI Automation fallback.
    /// </summary>
    public void McpSafetyPolicy_BuildInstructions_ForegroundInputDisabled_MentionsServerOptIn() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false);

        string instructions = policy.BuildInstructions();

        StringAssert.Contains(instructions, "Focused foreground input fallback is blocked");
        StringAssert.Contains(instructions, "--allow-foreground-input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground-input guidance is explicit when the server allows zero-handle UI Automation fallback.
    /// </summary>
    public void McpSafetyPolicy_BuildInstructions_ForegroundInputEnabled_MentionsEnabledMode() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: true,
            dryRun: false);

        string instructions = policy.BuildInstructions();

        StringAssert.Contains(instructions, "Focused foreground input fallback is enabled");
    }

    [TestMethod]
    /// <summary>
    /// Ensures set-control-text requests that ask for foreground fallback are denied without server opt-in.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetControlTextForegroundFallbackWithoutOptIn_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_control_text",
            CreateArguments(new {
                processName = "DesktopManager.TestApp",
                targetName = "Editor",
                text = "Hello world",
                allowForegroundInput = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "--allow-foreground-input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures send-control-keys requests that ask for foreground fallback are denied without server opt-in.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SendControlKeysForegroundFallbackWithoutOptIn_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "send_control_keys",
            CreateArguments(new {
                processName = "DesktopManager.TestApp",
                targetName = "CommandBar",
                keys = new[] { "ENTER" },
                allowForegroundInput = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "--allow-foreground-input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures dry-run previews preserve the requested foreground-fallback flag for operator visibility.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_DryRunForegroundFallbackPreview_ReportsRequestedFlag() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: true);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "send_control_keys",
            CreateArguments(new {
                processName = "DesktopManager.TestApp",
                targetName = "CommandBar",
                keys = new[] { "ENTER" },
                allowForegroundInput = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.DryRun, decision.Kind);
        using JsonDocument result = JsonDocument.Parse(JsonSerializer.Serialize(decision.Result));
        Assert.IsTrue(result.RootElement.GetProperty("requestedForegroundInputFallback").GetBoolean());
        Assert.AreEqual("dry-run", result.RootElement.GetProperty("safetyMode").GetString());
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground-fallback requests are allowed when the server opt-in is enabled.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_ForegroundFallbackWithServerOptIn_AllowsRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: true,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "send_control_keys",
            CreateArguments(new {
                processName = "DesktopManager.TestApp",
                targetName = "CommandBar",
                keys = new[] { "ENTER" },
                allowForegroundInput = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Allow, decision.Kind);
        Assert.IsNull(decision.Message);
    }

    [TestMethod]
    /// <summary>
    /// Ensures clipboard mutations are still blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetClipboardTextInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_clipboard_text",
            CreateArguments(new {
                text = "Hello from DesktopManager"
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor brightness mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetMonitorBrightnessInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_monitor_brightness",
            CreateArguments(new {
                brightness = 60,
                primaryOnly = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor position mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetMonitorPositionInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_monitor_position",
            CreateArguments(new {
                left = 0,
                top = 0,
                right = 1920,
                bottom = 1080,
                primaryOnly = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures taskbar mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetTaskbarPositionInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_taskbar_position",
            CreateArguments(new {
                position = "bottom",
                visible = true,
                primaryOnly = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop background color mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetDesktopBackgroundColorInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_desktop_background_color",
            CreateArguments(new {
                color = "0x102040"
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor wallpaper mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetMonitorWallpaperInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_monitor_wallpaper",
            CreateArguments(new {
                wallpaperPath = @"C:\Wallpapers\Aurora.jpg",
                primaryOnly = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures topmost window mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_SetWindowTopMostInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "set_window_topmost",
            CreateArguments(new {
                processName = "notepad",
                topMost = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures close-window mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_CloseWindowsInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "close_windows",
            CreateArguments(new {
                processName = "notepad"
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures keep-alive mutations are blocked when the server is read-only.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_StartWindowKeepAliveInReadOnlyMode_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "start_window_keep_alive",
            CreateArguments(new {
                processName = "notepad",
                intervalMs = 30000
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "read-only mode");
    }

    [TestMethod]
    /// <summary>
    /// Ensures global keep-alive stop requests are blocked while process filters are active.
    /// </summary>
    public void McpSafetyPolicy_EvaluateToolCall_StopWindowKeepAliveAllSessionsWithProcessFilters_DeniesRequest() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false,
            allowedProcessPatterns: new[] { "notepad" });

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall(
            "stop_window_keep_alive",
            CreateArguments(new {
                allSessions = true
            }));

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "multiple applications");
    }

    private static JsonElement CreateArguments(object value) {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }
}
#endif
