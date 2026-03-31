using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Black-box tests for the DesktopManager MCP stdio server.
/// </summary>
public class McpServerTests {
    [TestMethod]
    /// <summary>
    /// Ensures the MCP transport advertises the expected tools, prompts, and resources.
    /// </summary>
    public void McpServer_InitializeAndListCatalog_ReturnsExpectedSurface() {
        using var client = McpTestClient.Start();

        JsonElement initializeResult = client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });
        Assert.AreEqual("2025-06-18", initializeResult.GetProperty("protocolVersion").GetString());
        JsonElement safetyPolicy = initializeResult.GetProperty("safetyPolicy");
        Assert.IsTrue(safetyPolicy.GetProperty("readOnly").GetBoolean());
        Assert.IsFalse(safetyPolicy.GetProperty("allowMutations").GetBoolean());
        Assert.IsFalse(safetyPolicy.GetProperty("allowForegroundInput").GetBoolean());
        Assert.IsFalse(safetyPolicy.GetProperty("dryRun").GetBoolean());
        Assert.AreEqual(0, safetyPolicy.GetProperty("allowedProcesses").GetArrayLength());
        Assert.AreEqual(0, safetyPolicy.GetProperty("deniedProcesses").GetArrayLength());
        StringAssert.Contains(initializeResult.GetProperty("instructions").GetString() ?? string.Empty, "read-only");

        JsonElement toolsResult = client.SendRequest(2, "tools/list");
        HashSet<string> toolNames = toolsResult
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Dictionary<string, JsonElement> toolsByName = toolsResult
            .GetProperty("tools")
            .EnumerateArray()
            .ToDictionary(
                tool => tool.GetProperty("name").GetString() ?? string.Empty,
                tool => tool.Clone(),
                StringComparer.Ordinal);

        Assert.IsTrue(toolNames.Contains("list_named_control_targets"));
        Assert.IsTrue(toolNames.Contains("get_named_control_target"));
        Assert.IsTrue(toolNames.Contains("save_control_target"));
        Assert.IsTrue(toolNames.Contains("resolve_control_target"));
        Assert.IsTrue(toolNames.Contains("diagnose_window_controls"));
        Assert.IsTrue(toolNames.Contains("get_window_geometry"));
        Assert.IsTrue(toolNames.Contains("get_window_process_info"));
        Assert.IsTrue(toolNames.Contains("get_owner_window_process_info"));
        Assert.IsTrue(toolNames.Contains("get_mouse_state"));
        Assert.IsTrue(toolNames.Contains("get_clipboard_text"));
        Assert.IsTrue(toolNames.Contains("get_elevation_status"));
        Assert.IsTrue(toolNames.Contains("get_desktop_background_color"));
        Assert.IsTrue(toolNames.Contains("set_desktop_background_color"));
        Assert.IsTrue(toolNames.Contains("get_desktop_wallpaper_position"));
        Assert.IsTrue(toolNames.Contains("set_desktop_wallpaper_position"));
        Assert.IsTrue(toolNames.Contains("start_desktop_slideshow"));
        Assert.IsTrue(toolNames.Contains("stop_desktop_slideshow"));
        Assert.IsTrue(toolNames.Contains("advance_desktop_slideshow"));
        Assert.IsTrue(toolNames.Contains("wait_for_window_close"));
        Assert.IsTrue(toolNames.Contains("wait_for_window_to_lose_focus"));
        Assert.IsTrue(toolNames.Contains("observe_window_text"));
        Assert.IsTrue(toolNames.Contains("wait_for_observed_text"));
        Assert.IsTrue(toolNames.Contains("get_focused_control"));
        Assert.IsTrue(toolNames.Contains("wait_for_focused_control"));
        Assert.IsTrue(toolNames.Contains("get_control_state"));
        Assert.IsTrue(toolNames.Contains("focus_control"));
        Assert.IsTrue(toolNames.Contains("set_control_enabled"));
        Assert.IsTrue(toolNames.Contains("set_control_visibility"));
        Assert.IsTrue(toolNames.Contains("assert_control_value"));
        Assert.IsTrue(toolNames.Contains("assert_window_layout"));
        Assert.IsTrue(toolNames.Contains("launch_and_wait_for_window"));
        Assert.IsTrue(toolNames.Contains("maximize_windows"));
        Assert.IsTrue(toolNames.Contains("restore_windows"));
        Assert.IsTrue(toolNames.Contains("close_windows"));
        Assert.IsTrue(toolNames.Contains("set_window_topmost"));
        Assert.IsTrue(toolNames.Contains("set_window_visibility"));
        Assert.IsTrue(toolNames.Contains("set_window_transparency"));
        Assert.IsTrue(toolNames.Contains("get_monitor_brightness"));
        Assert.IsTrue(toolNames.Contains("get_monitor_wallpaper"));
        Assert.IsTrue(toolNames.Contains("set_monitor_wallpaper"));
        Assert.IsTrue(toolNames.Contains("set_monitor_brightness"));
        Assert.IsTrue(toolNames.Contains("set_monitor_position"));
        Assert.IsTrue(toolNames.Contains("set_monitor_resolution"));
        Assert.IsTrue(toolNames.Contains("set_monitor_dpi_scaling"));
        Assert.IsTrue(toolNames.Contains("set_taskbar_position"));
        Assert.IsTrue(toolNames.Contains("send_window_keys"));
        Assert.IsTrue(toolNames.Contains("list_window_keep_alive"));
        Assert.IsTrue(toolNames.Contains("start_window_keep_alive"));
        Assert.IsTrue(toolNames.Contains("stop_window_keep_alive"));
        Assert.IsTrue(toolNames.Contains("prepare_for_coding"));
        Assert.IsTrue(toolNames.Contains("prepare_for_screen_sharing"));
        Assert.IsTrue(toolNames.Contains("clean_up_distractions"));
        Assert.IsTrue(toolNames.Contains("set_clipboard_text"));
        AssertToolHasArtifactProperties(toolsByName["move_window"]);
        AssertToolHasArtifactProperties(toolsByName["focus_window"]);
        AssertToolHasArtifactProperties(toolsByName["maximize_windows"]);
        AssertToolHasArtifactProperties(toolsByName["restore_windows"]);
        AssertToolHasArtifactProperties(toolsByName["close_windows"]);
        AssertToolHasArtifactProperties(toolsByName["set_window_topmost"]);
        AssertToolHasArtifactProperties(toolsByName["set_window_visibility"]);
        AssertToolHasArtifactProperties(toolsByName["set_window_transparency"]);
        AssertToolHasArtifactProperties(toolsByName["click_control"]);
        AssertToolHasArtifactProperties(toolsByName["focus_control"]);
        AssertToolHasArtifactProperties(toolsByName["set_control_enabled"]);
        AssertToolHasArtifactProperties(toolsByName["set_control_visibility"]);
        AssertToolHasArtifactProperties(toolsByName["set_control_text"]);
        AssertToolHasArtifactProperties(toolsByName["send_window_keys"]);
        AssertToolHasArtifactProperties(toolsByName["launch_and_wait_for_window"]);
        AssertToolHasArtifactProperties(toolsByName["prepare_for_coding"]);
        AssertToolHasArtifactProperties(toolsByName["prepare_for_screen_sharing"]);
        AssertToolHasArtifactProperties(toolsByName["clean_up_distractions"]);
        AssertToolHasArtifactProperties(toolsByName["assert_window_layout"]);
        AssertToolHasProperty(toolsByName["save_window_target"], "widthRatio");
        AssertToolHasProperty(toolsByName["save_window_target"], "heightRatio");
        AssertToolHasProperty(toolsByName["screenshot_window"], "targetName");
        AssertToolHasProperty(toolsByName["launch_and_wait_for_window"], "timeoutMs");
        AssertToolHasProperty(toolsByName["launch_and_wait_for_window"], "followProcessFamily");
        AssertToolHasProperty(toolsByName["get_clipboard_text"], "retryCount");
        AssertToolHasProperty(toolsByName["get_clipboard_text"], "retryDelayMs");
        AssertToolHasProperty(toolsByName["set_clipboard_text"], "text");
        AssertToolHasProperty(toolsByName["set_desktop_background_color"], "color");
        AssertToolHasProperty(toolsByName["set_desktop_wallpaper_position"], "position");
        AssertToolHasProperty(toolsByName["start_desktop_slideshow"], "imagePaths");
        AssertToolHasProperty(toolsByName["advance_desktop_slideshow"], "direction");
        AssertToolHasProperty(toolsByName["get_monitor_brightness"], "deviceId");
        AssertToolHasProperty(toolsByName["get_monitor_wallpaper"], "deviceName");
        AssertToolHasProperty(toolsByName["set_monitor_wallpaper"], "wallpaperPath");
        AssertToolHasProperty(toolsByName["set_monitor_wallpaper"], "url");
        AssertToolHasProperty(toolsByName["set_monitor_brightness"], "brightness");
        AssertToolHasProperty(toolsByName["set_monitor_position"], "left");
        AssertToolHasProperty(toolsByName["set_monitor_position"], "right");
        AssertToolHasProperty(toolsByName["set_monitor_resolution"], "orientation");
        AssertToolHasProperty(toolsByName["set_monitor_dpi_scaling"], "scalingPercent");
        AssertToolHasProperty(toolsByName["set_taskbar_position"], "position");
        AssertToolHasProperty(toolsByName["set_taskbar_position"], "visible");
        AssertToolHasProperty(toolsByName["observe_window_text"], "expectedText");
        AssertToolHasProperty(toolsByName["wait_for_observed_text"], "expectedText");
        AssertToolHasProperty(toolsByName["wait_for_focused_control"], "timeoutMs");
        AssertToolHasProperty(toolsByName["wait_for_focused_control"], "intervalMs");
        AssertToolHasProperty(toolsByName["get_window_process_info"], "processName");
        AssertToolHasProperty(toolsByName["get_owner_window_process_info"], "handle");
        AssertToolHasProperty(toolsByName["start_window_keep_alive"], "intervalMs");
        AssertToolHasProperty(toolsByName["stop_window_keep_alive"], "allSessions");
        AssertToolHasProperty(toolsByName["get_control_state"], "windowHandle");
        AssertToolHasProperty(toolsByName["get_control_state"], "controlHandle");
        AssertToolHasProperty(toolsByName["focus_control"], "windowHandle");
        AssertToolHasProperty(toolsByName["focus_control"], "controlHandle");
        AssertToolHasProperty(toolsByName["set_control_enabled"], "enabled");
        AssertToolHasProperty(toolsByName["set_control_visibility"], "visible");
        AssertToolHasProperty(toolsByName["send_window_keys"], "keys");
        AssertToolHasProperty(toolsByName["set_window_topmost"], "topMost");
        AssertToolHasProperty(toolsByName["set_window_visibility"], "visible");
        AssertToolHasProperty(toolsByName["set_window_transparency"], "alpha");
        AssertToolHasProperty(toolsByName["assert_window_layout"], "positionTolerancePx");

        JsonElement promptsResult = client.SendRequest(3, "prompts/list");
        HashSet<string> promptNames = promptsResult
            .GetProperty("prompts")
            .EnumerateArray()
            .Select(prompt => prompt.GetProperty("name").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(promptNames.Contains("prepare_for_coding"));
        Assert.IsTrue(promptNames.Contains("prepare_for_screen_sharing"));
        Assert.IsTrue(promptNames.Contains("clean_up_distractions"));

        JsonElement resourcesResult = client.SendRequest(4, "resources/list");
        HashSet<string> resourceUris = resourcesResult
            .GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("uri").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(resourceUris.Contains("desktop://control-targets"));
        Assert.IsTrue(resourceUris.Contains("desktop://targets"));
        Assert.IsTrue(resourceUris.Contains("desktop://snapshot/current"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures named window targets round-trip through the MCP tool surface.
    /// </summary>
    public void McpServer_SaveAndGetWindowTarget_RoundTripsDefinition() {
        string targetName = "McpServerTests-Window-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetTargetPath(targetName);

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new Dictionary<string, object?> {
                ["protocolVersion"] = "2025-06-18"
            });

            JsonElement saveResult = client.CallTool(2, "save_window_target", new Dictionary<string, object?> {
                ["name"] = targetName,
                ["description"] = "Editor center",
                ["xRatio"] = 0.5,
                ["yRatio"] = 0.5,
                ["widthRatio"] = 0.8,
                ["heightRatio"] = 0.6,
                ["clientArea"] = true
            });

            Assert.AreEqual(targetName, saveResult.GetProperty("Name").GetString());
            Assert.IsTrue(File.Exists(path));

            JsonElement getResult = client.CallTool(3, "get_named_target", new Dictionary<string, object?> {
                ["name"] = targetName
            });

            JsonElement target = getResult.GetProperty("Target");
            Assert.AreEqual(targetName, getResult.GetProperty("Name").GetString());
            Assert.AreEqual("Editor center", target.GetProperty("Description").GetString());
            Assert.AreEqual(0.5d, target.GetProperty("XRatio").GetDouble(), 0.0001d);
            Assert.AreEqual(0.5d, target.GetProperty("YRatio").GetDouble(), 0.0001d);
            Assert.AreEqual(0.8d, target.GetProperty("WidthRatio").GetDouble(), 0.0001d);
            Assert.AreEqual(0.6d, target.GetProperty("HeightRatio").GetDouble(), 0.0001d);
            Assert.IsTrue(target.GetProperty("ClientArea").GetBoolean());
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures named control targets round-trip through the MCP tool surface.
    /// </summary>
    public void McpServer_SaveAndGetControlTarget_RoundTripsDefinition() {
        string targetName = "McpServerTests-Control-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetControlTargetPath(targetName);

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new Dictionary<string, object?> {
                ["protocolVersion"] = "2025-06-18"
            });

            JsonElement saveResult = client.CallTool(2, "save_control_target", new Dictionary<string, object?> {
                ["name"] = targetName,
                ["description"] = "Address bar",
                ["controlType"] = "Edit",
                ["supportsBackgroundText"] = true,
                ["uiAutomation"] = true
            });

            Assert.AreEqual(targetName, saveResult.GetProperty("Name").GetString());
            Assert.IsTrue(File.Exists(path));

            JsonElement getResult = client.CallTool(3, "get_named_control_target", new Dictionary<string, object?> {
                ["name"] = targetName
            });

            JsonElement target = getResult.GetProperty("Target");
            Assert.AreEqual(targetName, getResult.GetProperty("Name").GetString());
            Assert.AreEqual("Address bar", target.GetProperty("Description").GetString());
            Assert.AreEqual("Edit", target.GetProperty("ControlTypePattern").GetString());
            Assert.IsTrue(target.GetProperty("SupportsBackgroundText").GetBoolean());
            Assert.IsTrue(target.GetProperty("UseUiAutomation").GetBoolean());
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures MCP prompts resolve through the stdio transport with the expected guidance text.
    /// </summary>
    public void McpServer_GetPrompt_ReturnsExpectedInstructions() {
        using var client = McpTestClient.Start();
        client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });

        JsonElement promptResult = client.SendRequest(2, "prompts/get", new Dictionary<string, object?> {
            ["name"] = "prepare_for_coding",
            ["arguments"] = new Dictionary<string, object?> {
                ["layoutName"] = "PairingLayout"
            }
        });

        Assert.AreEqual("Prepare the desktop for focused coding work.", promptResult.GetProperty("description").GetString());
        JsonElement message = promptResult.GetProperty("messages")[0];
        Assert.AreEqual("user", message.GetProperty("role").GetString());

        JsonElement content = message.GetProperty("content");
        Assert.AreEqual("text", content.GetProperty("type").GetString());
        string text = content.GetProperty("text").GetString() ?? string.Empty;
        StringAssert.Contains(text, "Preferred layout: PairingLayout.");
        StringAssert.Contains(text, "Start by listing named layouts.");
        StringAssert.Contains(text, "focus the main editor or terminal window.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures MCP resources read back the current saved target state and snapshot payloads.
    /// </summary>
    public void McpServer_ReadResources_ReturnsSavedTargetsAndSnapshot() {
        string windowTargetName = "McpServerTests-Resource-Window-" + Guid.NewGuid().ToString("N");
        string controlTargetName = "McpServerTests-Resource-Control-" + Guid.NewGuid().ToString("N");
        string windowTargetPath = DesktopStateStore.GetTargetPath(windowTargetName);
        string controlTargetPath = DesktopStateStore.GetControlTargetPath(controlTargetName);

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new Dictionary<string, object?> {
                ["protocolVersion"] = "2025-06-18"
            });

            client.CallTool(2, "save_window_target", new Dictionary<string, object?> {
                ["name"] = windowTargetName,
                ["description"] = "Resource test target",
                ["xRatio"] = 0.5,
                ["yRatio"] = 0.5,
                ["widthRatio"] = 0.6,
                ["heightRatio"] = 0.4
            });
            client.CallTool(3, "save_control_target", new Dictionary<string, object?> {
                ["name"] = controlTargetName,
                ["description"] = "Resource test control",
                ["controlType"] = "Edit",
                ["uiAutomation"] = true
            });

            JsonElement targetsResource = ReadResourceJson(client.SendRequest(4, "resources/read", new Dictionary<string, object?> {
                ["uri"] = "desktop://targets"
            }));
            Assert.IsTrue(targetsResource.EnumerateArray().Any(item => string.Equals(item.GetString(), windowTargetName, StringComparison.Ordinal)));

            JsonElement controlTargetsResource = ReadResourceJson(client.SendRequest(5, "resources/read", new Dictionary<string, object?> {
                ["uri"] = "desktop://control-targets"
            }));
            Assert.IsTrue(controlTargetsResource.EnumerateArray().Any(item => string.Equals(item.GetString(), controlTargetName, StringComparison.Ordinal)));

            JsonElement snapshotResource = ReadResourceJson(client.SendRequest(6, "resources/read", new Dictionary<string, object?> {
                ["uri"] = "desktop://snapshot/current"
            }));
            Assert.IsTrue(snapshotResource.TryGetProperty("ActiveWindow", out _));
            Assert.IsTrue(snapshotResource.TryGetProperty("Monitors", out _));
            Assert.IsTrue(snapshotResource.TryGetProperty("Windows", out _));
        } finally {
            if (File.Exists(windowTargetPath)) {
                File.Delete(windowTargetPath);
            }

            if (File.Exists(controlTargetPath)) {
                File.Delete(controlTargetPath);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures MCP surfaces transport and tool errors without crashing the stdio server.
    /// </summary>
    public void McpServer_InvalidRequests_ReturnStructuredErrors() {
        using var client = McpTestClient.Start("mcp serve --allow-mutations");
        client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });

        JsonElement resourceError = client.SendRequestExpectError(2, "resources/read", new Dictionary<string, object?> {
            ["uri"] = "desktop://missing"
        });
        Assert.AreEqual(-32602, resourceError.GetProperty("code").GetInt32());
        StringAssert.Contains(resourceError.GetProperty("message").GetString() ?? string.Empty, "Unknown resource");

        JsonElement promptError = client.SendRequestExpectError(3, "prompts/get", new Dictionary<string, object?> {
            ["name"] = "missing_prompt"
        });
        Assert.AreEqual(-32602, promptError.GetProperty("code").GetInt32());
        StringAssert.Contains(promptError.GetProperty("message").GetString() ?? string.Empty, "Unknown prompt");

        JsonElement toolError = client.CallToolExpectError(4, "launch_and_wait_for_window", new Dictionary<string, object?> {
            ["filePath"] = @"C:\__DesktopManagerTests__\missing.exe",
            ["timeoutMs"] = 500,
            ["intervalMs"] = 50
        });
        StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "cannot find the file");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the default MCP server blocks mutating tool calls until mutation mode is explicitly enabled.
    /// </summary>
    public void McpServer_DefaultReadOnly_BlocksMutatingTools() {
        string targetName = "McpServerTests-ReadOnly-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetTargetPath(targetName);

        try {
            using var client = McpTestClient.Start();
            client.SendRequest(1, "initialize", new Dictionary<string, object?> {
                ["protocolVersion"] = "2025-06-18"
            });

            JsonElement toolError = client.CallToolExpectError(2, "save_window_target", new Dictionary<string, object?> {
                ["name"] = targetName,
                ["xRatio"] = 0.5,
                ["yRatio"] = 0.5
            });

            StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "read-only mode");
            Assert.IsFalse(File.Exists(path));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures focused foreground-input fallback requires explicit MCP server opt-in.
    /// </summary>
    public void McpServer_ForegroundInputFallback_RequiresServerOptIn() {
        using var client = McpTestClient.Start("mcp serve --allow-mutations");
        client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });

        JsonElement toolError = client.CallToolExpectError(2, "set_control_text", new Dictionary<string, object?> {
            ["windowTitle"] = "*DesktopManagerTests*",
            ["controlType"] = "Edit",
            ["text"] = "Hello world",
            ["allowForegroundInput"] = true
        });

        StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "--allow-foreground-input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control-key foreground-input fallback requires explicit MCP server opt-in too.
    /// </summary>
    public void McpServer_ForegroundInputFallback_SendControlKeys_RequiresServerOptIn() {
        using var client = McpTestClient.Start("mcp serve --allow-mutations");
        client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });

        JsonElement toolError = client.CallToolExpectError(2, "send_control_keys", new Dictionary<string, object?> {
            ["windowTitle"] = "*DesktopManagerTests*",
            ["controlType"] = "Edit",
            ["keys"] = new[] { "ENTER" },
            ["allowForegroundInput"] = true
        });

        StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "--allow-foreground-input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures dry-run mode returns a structured preview without mutating persistent state.
    /// </summary>
    public void McpServer_DryRun_PreviewsMutatingToolsWithoutWritingState() {
        string targetName = "McpServerTests-DryRun-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetTargetPath(targetName);

        try {
            using var client = McpTestClient.Start("mcp serve --dry-run");
            JsonElement initializeResult = client.SendRequest(1, "initialize", new Dictionary<string, object?> {
                ["protocolVersion"] = "2025-06-18"
            });
            Assert.IsTrue(initializeResult.GetProperty("safetyPolicy").GetProperty("dryRun").GetBoolean());

            JsonElement result = client.CallTool(2, "save_window_target", new Dictionary<string, object?> {
                ["name"] = targetName,
                ["xRatio"] = 0.5,
                ["yRatio"] = 0.5
            });

            Assert.IsTrue(result.GetProperty("dryRun").GetBoolean());
            Assert.IsFalse(result.GetProperty("applied").GetBoolean());
            Assert.AreEqual("save_window_target", result.GetProperty("toolName").GetString());
            Assert.IsFalse(File.Exists(path));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures dry-run foreground-input requests preserve the explicit fallback flag in the preview result.
    /// </summary>
    public void McpServer_DryRun_ForegroundInputPreview_ReportsRequestedFallback() {
        using var client = McpTestClient.Start("mcp serve --dry-run");
        JsonElement initializeResult = client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });
        Assert.IsTrue(initializeResult.GetProperty("safetyPolicy").GetProperty("dryRun").GetBoolean());

        JsonElement result = client.CallTool(2, "send_control_keys", new Dictionary<string, object?> {
            ["processName"] = "DesktopManager.TestApp",
            ["targetName"] = "CommandBar",
            ["keys"] = new[] { "ENTER" },
            ["allowForegroundInput"] = true
        });

        Assert.IsTrue(result.GetProperty("dryRun").GetBoolean());
        Assert.IsTrue(result.GetProperty("requestedForegroundInputFallback").GetBoolean());
        Assert.AreEqual("dry-run", result.GetProperty("safetyMode").GetString());
    }

    [TestMethod]
    /// <summary>
    /// Ensures process-scoped MCP filters reject mutating window tools that do not declare an explicit process target.
    /// </summary>
    public void McpServer_ProcessFilters_BlockUnscopedWindowMutations() {
        using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process notepad");
        JsonElement initializeResult = client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });

        JsonElement safetyPolicy = initializeResult.GetProperty("safetyPolicy");
        Assert.AreEqual(1, safetyPolicy.GetProperty("allowedProcesses").GetArrayLength());
        Assert.AreEqual("notepad", safetyPolicy.GetProperty("allowedProcesses")[0].GetString());

        JsonElement toolError = client.CallToolExpectError(2, "move_window", new Dictionary<string, object?> {
            ["windowTitle"] = "*Notepad*",
            ["x"] = 0,
            ["y"] = 0,
            ["width"] = 640,
            ["height"] = 480
        });

        StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "explicit 'processName' selector");
    }

    [TestMethod]
    /// <summary>
    /// Ensures denied-process filters block launch requests before the executable is started.
    /// </summary>
    public void McpServer_DeniedProcessFilters_BlockLaunchRequests() {
        using var client = McpTestClient.Start("mcp serve --allow-mutations --deny-process notepad");
        client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });

        JsonElement toolError = client.CallToolExpectError(2, "launch_process", new Dictionary<string, object?> {
            ["filePath"] = "notepad.exe"
        });

        StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "denied-process policy");
    }

    [TestMethod]
    /// <summary>
    /// Ensures allowlisted process filters still permit dry-run previews for matching launch requests.
    /// </summary>
    public void McpServer_AllowedProcessFilters_PermitMatchingDryRunLaunchPreview() {
        using var client = McpTestClient.Start("mcp serve --dry-run --allow-process notepad");
        JsonElement initializeResult = client.SendRequest(1, "initialize", new Dictionary<string, object?> {
            ["protocolVersion"] = "2025-06-18"
        });
        Assert.IsTrue(initializeResult.GetProperty("safetyPolicy").GetProperty("dryRun").GetBoolean());

        JsonElement result = client.CallTool(2, "launch_process", new Dictionary<string, object?> {
            ["filePath"] = "notepad.exe"
        });

        Assert.IsTrue(result.GetProperty("dryRun").GetBoolean());
        Assert.AreEqual("launch_process", result.GetProperty("toolName").GetString());
        Assert.AreEqual(2, result.GetProperty("requestedProcesses").GetArrayLength());
    }

    private static JsonElement ReadResourceJson(JsonElement resourceResult) {
        JsonElement contents = resourceResult.GetProperty("contents");
        Assert.AreEqual(1, contents.GetArrayLength());

        string text = contents[0].GetProperty("text").GetString() ?? string.Empty;
        using JsonDocument document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static void AssertToolHasArtifactProperties(JsonElement tool) {
        JsonElement properties = tool
            .GetProperty("inputSchema")
            .GetProperty("properties");

        Assert.IsTrue(properties.TryGetProperty("captureBefore", out _));
        Assert.IsTrue(properties.TryGetProperty("captureAfter", out _));
        Assert.IsTrue(properties.TryGetProperty("artifactDirectory", out _));
        Assert.IsTrue(properties.TryGetProperty("verifyAfter", out _));
        Assert.IsTrue(properties.TryGetProperty("verificationTolerancePixels", out _));
    }

    private static void AssertToolHasProperty(JsonElement tool, string propertyName) {
        JsonElement properties = tool
            .GetProperty("inputSchema")
            .GetProperty("properties");

        Assert.IsTrue(properties.TryGetProperty(propertyName, out _));
    }
}
