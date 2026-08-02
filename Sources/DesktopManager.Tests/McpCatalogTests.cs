#if !NET472
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Regression tests for MCP catalog argument handling.
/// </summary>
public class McpCatalogTests {
    [TestMethod]
    public void McpCatalog_GetTools_ExposesSemanticControlObservationAndSafeEditContracts() {
        JsonElement observe = GetTool("observe_control");
        JsonElement wait = GetTool("wait_for_control_observation");
        JsonElement edit = GetTool("edit_control_text");

        Assert.IsTrue(observe.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        Assert.IsTrue(wait.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        Assert.IsFalse(edit.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        Assert.IsTrue(observe.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("includeTextRanges", out _));
        Assert.IsTrue(wait.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("minimumRangeValue", out _));
        JsonElement waitProperties = wait.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(waitProperties.TryGetProperty("includeTextRanges", out _));
        Assert.IsTrue(waitProperties.TryGetProperty("isTextTruncated", out _));
        Assert.IsFalse(observe.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("realizeVirtualizedItem", out _));
        Assert.IsFalse(waitProperties.TryGetProperty("realizeVirtualizedItem", out _));
        Assert.IsFalse(observe.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("ensureForegroundWindow", out _));
        Assert.IsFalse(waitProperties.TryGetProperty("ensureForegroundWindow", out _));
        JsonElement editProperties = edit.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(editProperties.TryGetProperty("ensureForegroundWindow", out _));
        Assert.IsTrue(editProperties.TryGetProperty("expectedFingerprint", out _));
        Assert.IsTrue(editProperties.TryGetProperty("expectedEditContextFingerprint", out _));
        Assert.IsFalse(editProperties.TryGetProperty("expectedText", out _));
        Assert.IsFalse(editProperties.TryGetProperty("ignoreCase", out _));
        Assert.IsFalse(editProperties.TryGetProperty("includeTextRanges", out _));
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.AffectsLiveDesktop("edit_control_text"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures the server-side mutation policy is derived from the same read-only annotations advertised to MCP clients.
    /// </summary>
    public void McpCatalog_GetTools_SafetyAnnotationsMatchServerClassification() {
        foreach (object entry in DesktopManager.Cli.McpCatalog.GetTools()) {
            using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(entry));
            JsonElement tool = document.RootElement;
            string name = tool.GetProperty("name").GetString() ?? string.Empty;
            bool readOnly = tool.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean();

            Assert.AreEqual(!readOnly, DesktopManager.Cli.McpCatalog.IsMutatingTool(name), $"Tool '{name}' has inconsistent mutation metadata.");
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures the wallpaper URL tool advertises its ability to reach an external HTTP resource.
    /// </summary>
    public void McpCatalog_GetTools_WallpaperUrlToolIsOpenWorld() {
        object tool = DesktopManager.Cli.McpCatalog.GetTools()
            .Single(entry => string.Equals(((DesktopManager.Cli.McpToolDefinition)entry).Name, "set_monitor_wallpaper", StringComparison.Ordinal));

        Assert.IsTrue(((DesktopManager.Cli.McpToolDefinition)tool).Annotations.OpenWorldHint);
    }

    [TestMethod]
    /// <summary>
    /// Ensures keep-alive stop rejects allSessions when window selectors are also supplied.
    /// </summary>
    public void McpCatalog_TryCallTool_StopWindowKeepAliveAllSessionsWithSelectors_ReturnsError() {
        JsonElement arguments = CreateArguments(new {
            allSessions = true,
            processName = "notepad"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryCallTool("stop_window_keep_alive", arguments, out object result, out string? error);

        Assert.IsFalse(success);
        Assert.IsNotNull(result);
        Assert.AreEqual("Cannot combine 'allSessions' with window selectors or 'all'.", error);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesSetControlCheckState() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"set_control_check_state\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesSetMatchingControlCheckState() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"set_matching_control_check_state\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesSetControlSelectedValue() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"set_control_selected_value\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesSetMatchingControlSelectedValue() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"set_matching_control_selected_value\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesWaitForWindowVisualChange() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"wait_for_window_visual_change\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesPlacementAndHdrTools() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();
        string json = JsonSerializer.Serialize(tools);

        StringAssert.Contains(json, "\"name\":\"place_window\"");
        StringAssert.Contains(json, "\"name\":\"get_monitor_advanced_color\"");
        StringAssert.Contains(json, "\"name\":\"set_monitor_hdr\"");
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.IsKnownTool("place_window"));
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.IsKnownTool("get_monitor_advanced_color"));
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.IsKnownTool("set_monitor_hdr"));
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.IsMutatingTool("place_window"));
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.IsMutatingTool("set_monitor_hdr"));
        Assert.IsTrue(DesktopManager.Cli.McpCatalog.AffectsLiveDesktop("place_window"));
    }

    [TestMethod]
    public void McpCatalog_GetTools_PlaceWindow_ExposesPlacementArguments() {
        JsonElement tool = GetTool("place_window");

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("placement", out _));
        Assert.IsTrue(properties.TryGetProperty("monitorTarget", out _));
        Assert.IsTrue(properties.TryGetProperty("monitor", out _));
        Assert.IsTrue(properties.TryGetProperty("x", out _));
        Assert.IsTrue(properties.TryGetProperty("y", out _));
        Assert.IsTrue(properties.TryGetProperty("width", out _));
        Assert.IsTrue(properties.TryGetProperty("height", out _));
        Assert.IsTrue(properties.TryGetProperty("verifyAfter", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_SetMonitorHdr_ExposesEnabledArgument() {
        JsonElement tool = GetTool("set_monitor_hdr");

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("enabled", out _));
        Assert.IsTrue(properties.TryGetProperty("connectedOnly", out _));
        Assert.IsTrue(properties.TryGetProperty("index", out _));
    }

    [TestMethod]
    public void McpCatalog_TryCallTool_PlaceWindowMissingPlacement_ReturnsRequiredPropertyError() {
        JsonElement arguments = CreateArguments(new {
            processName = "DesktopManager.TestApp"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryCallTool("place_window", arguments, out object result, out string? error);

        Assert.IsFalse(success);
        Assert.IsNotNull(result);
        Assert.AreEqual("Property 'placement' is required.", error);
    }

    [TestMethod]
    public void McpCatalog_TryCallTool_SetMonitorHdrMissingEnabled_ReturnsRequiredPropertyError() {
        JsonElement arguments = CreateArguments(new {
            connectedOnly = true
        });

        bool success = DesktopManager.Cli.McpCatalog.TryCallTool("set_monitor_hdr", arguments, out object result, out string? error);

        Assert.IsFalse(success);
        Assert.IsNotNull(result);
        Assert.AreEqual("Property 'enabled' is required.", error);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesSaveVisualBaseline() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"save_visual_baseline\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesAssertVisualBaseline() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"assert_visual_baseline\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesResolveVisualBaseline() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"resolve_visual_baseline\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesReadWindowText() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"read_window_text\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_IncludesResolveWindowText() {
        object[] tools = DesktopManager.Cli.McpCatalog.GetTools();

        bool found = tools.Any(tool => JsonSerializer.Serialize(tool).Contains("\"name\":\"resolve_window_text\"", StringComparison.Ordinal));

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void McpCatalog_GetTools_WaitForWindowVisualChange_ExposesVisualDiffArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "wait_for_window_visual_change", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("targetName", out _));
        Assert.IsTrue(properties.TryGetProperty("clientArea", out _));
        Assert.IsTrue(properties.TryGetProperty("timeoutMs", out _));
        Assert.IsTrue(properties.TryGetProperty("intervalMs", out _));
        Assert.IsTrue(properties.TryGetProperty("minimumChangedRatio", out _));
        Assert.IsTrue(properties.TryGetProperty("differenceThreshold", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_SaveVisualBaseline_ExposesTargetArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "save_visual_baseline", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("windowTitle", out _));
        Assert.IsTrue(properties.TryGetProperty("processName", out _));
        Assert.IsTrue(properties.TryGetProperty("targetName", out _));
        Assert.IsTrue(properties.TryGetProperty("clientArea", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_AssertVisualBaseline_ExposesComparisonArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "assert_visual_baseline", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("targetName", out _));
        Assert.IsTrue(properties.TryGetProperty("clientArea", out _));
        Assert.IsTrue(properties.TryGetProperty("maxChangedRatio", out _));
        Assert.IsTrue(properties.TryGetProperty("differenceThreshold", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_ResolveVisualBaseline_ExposesSearchArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "resolve_visual_baseline", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("clientArea", out _));
        Assert.IsTrue(properties.TryGetProperty("maxAverageDifference", out _));
        Assert.IsTrue(properties.TryGetProperty("differenceThreshold", out _));
        Assert.IsTrue(properties.TryGetProperty("scanStep", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_ReadWindowText_ExposesOcrArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "read_window_text", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("targetName", out _));
        Assert.IsTrue(properties.TryGetProperty("clientArea", out _));
        Assert.IsTrue(properties.TryGetProperty("languageTag", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_ResolveWindowText_ExposesOcrMatchArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "resolve_window_text", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("queryText", out _));
        Assert.IsTrue(properties.TryGetProperty("targetName", out _));
        Assert.IsTrue(properties.TryGetProperty("clientArea", out _));
        Assert.IsTrue(properties.TryGetProperty("contains", out _));
        Assert.IsTrue(properties.TryGetProperty("languageTag", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_ClickWindowPoint_ExposesVisualBaselineArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "click_window_point", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("visualBaselineName", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineMaxAverageDifference", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineDifferenceThreshold", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineScanStep", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrText", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrTargetName", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrContains", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrLanguageTag", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_DragWindowPoints_ExposesVisualBaselineArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "drag_window_points", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("startVisualBaselineName", out _));
        Assert.IsTrue(properties.TryGetProperty("endVisualBaselineName", out _));
        Assert.IsTrue(properties.TryGetProperty("startOcrText", out _));
        Assert.IsTrue(properties.TryGetProperty("endOcrText", out _));
        Assert.IsTrue(properties.TryGetProperty("startOcrTargetName", out _));
        Assert.IsTrue(properties.TryGetProperty("endOcrTargetName", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrContains", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrLanguageTag", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineMaxAverageDifference", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineDifferenceThreshold", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineScanStep", out _));
    }

    [TestMethod]
    public void McpCatalog_GetTools_ScrollWindowPoint_ExposesVisualBaselineArguments() {
        JsonElement tool = DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "scroll_window_point", StringComparison.Ordinal));

        JsonElement properties = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("visualBaselineName", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrText", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrTargetName", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrContains", out _));
        Assert.IsTrue(properties.TryGetProperty("ocrLanguageTag", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineMaxAverageDifference", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineDifferenceThreshold", out _));
        Assert.IsTrue(properties.TryGetProperty("baselineScanStep", out _));
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_SaveVisualBaseline_RequiresExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            windowTitle = "Harness",
            clientArea = true
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "save_visual_baseline",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsFalse(success);
        Assert.AreEqual(0, processPatterns.Length);
        Assert.AreEqual("Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.", error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_SaveVisualBaseline_UsesExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            processName = "DesktopManager.TestApp",
            clientArea = true
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "save_visual_baseline",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(new[] { "DesktopManager.TestApp" }, processPatterns);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_SetMatchingControlCheckState_RequiresExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            windowTitle = "Harness"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "set_matching_control_check_state",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsFalse(success);
        Assert.AreEqual(0, processPatterns.Length);
        Assert.AreEqual("Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.", error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_SetMatchingControlCheckState_UsesExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            processName = "DesktopManager.WinUiHarness",
            automationId = "ModernCheckBox",
            @checked = true
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "set_matching_control_check_state",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(new[] { "DesktopManager.WinUiHarness" }, processPatterns);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_SetMatchingControlSelectedValue_RequiresExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            windowTitle = "Harness"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "set_matching_control_selected_value",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsFalse(success);
        Assert.AreEqual(0, processPatterns.Length);
        Assert.AreEqual("Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.", error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_SetMatchingControlSelectedValue_UsesExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            processName = "DesktopManager.WinUiHarness",
            automationId = "ModernPicker",
            selectedValue = "Beta"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "set_matching_control_selected_value",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(new[] { "DesktopManager.WinUiHarness" }, processPatterns);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_ClickControl_RequiresExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            windowTitle = "Harness",
            automationId = "ModernApplyButton"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "click_control",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsFalse(success);
        Assert.AreEqual(0, processPatterns.Length);
        Assert.AreEqual("Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.", error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_ClickControl_UsesExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            processName = "DesktopManager.WinUiHarness",
            automationId = "ModernApplyButton",
            button = "left"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "click_control",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(new[] { "DesktopManager.WinUiHarness" }, processPatterns);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_PlaceWindow_RequiresExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            windowTitle = "Harness",
            placement = "maximize"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "place_window",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsFalse(success);
        Assert.AreEqual(0, processPatterns.Length);
        Assert.AreEqual("Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.", error);
    }

    [TestMethod]
    public void McpCatalog_TryGetMutatingProcessScope_PlaceWindow_UsesExplicitProcessName() {
        JsonElement arguments = CreateArguments(new {
            processName = "DesktopManager.TestApp",
            placement = "maximize"
        });

        bool success = DesktopManager.Cli.McpCatalog.TryGetMutatingProcessScope(
            "place_window",
            arguments,
            out string[] processPatterns,
            out string? error);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(new[] { "DesktopManager.TestApp" }, processPatterns);
        Assert.IsNull(error);
    }

    private static JsonElement GetTool(string name) {
        return DesktopManager.Cli.McpCatalog
            .GetTools()
            .Select(entry => JsonDocument.Parse(JsonSerializer.Serialize(entry)).RootElement.Clone())
            .Single(element => string.Equals(element.GetProperty("name").GetString(), name, StringComparison.Ordinal));
    }

    private static JsonElement CreateArguments(object value) {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }
}
#endif
