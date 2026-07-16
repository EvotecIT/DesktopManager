#if !NET472
using System.Collections.Generic;
using System.Text.Json;

namespace DesktopManager.Tests;

/// <summary>
/// Protects the MCP capability grouping and its system-settings and experimental safety gates.
/// </summary>
[TestClass]
public class McpDesktopStateContractTests {
    private static readonly string[] ExpectedTools = {
        "get_system_state",
        "get_audio_endpoints",
        "configure_audio_endpoint",
        "get_personalization",
        "apply_personalization",
        "get_taskbars",
        "configure_taskbar",
        "get_workstation_profiles",
        "save_workstation_profile",
        "apply_workstation_profile",
        "delete_workstation_profile",
        "list_radios",
        "set_radio_state",
        "get_airplane_mode",
        "set_airplane_mode",
        "get_window_virtual_desktop",
        "move_window_to_virtual_desktop",
        "invoke_system_action",
        "configure_keep_awake"
    };

    [TestMethod]
    public void McpCatalog_DesktopStateCapability_ExposesOneToolPerOperationShape() {
        string[] names = DesktopManager.Cli.McpCatalog.GetTools()
            .Cast<DesktopManager.Cli.McpToolDefinition>()
            .Select(tool => tool.Name)
            .ToArray();

        foreach (string expected in ExpectedTools) {
            CollectionAssert.Contains(names, expected);
        }

        Assert.AreEqual(ExpectedTools.Length, names.Count(ExpectedTools.Contains));
    }

    [TestMethod]
    public void McpSafetyPolicy_SupportedRadioInventory_RemainsReadOnlyAndSupported() {
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall("list_radios", EmptyArguments());

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Allow, decision.Kind);
        Assert.IsFalse(DesktopManager.Cli.McpCatalog.RequiresExperimentalAccess("list_radios"));
    }

    [TestMethod]
    public void McpSafetyPolicy_ExperimentalAirplaneRead_RequiresExplicitOptIn() {
        var blocked = new DesktopManager.Cli.McpSafetyPolicy(false, false, false);
        var allowed = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false,
            allowExperimental: true);

        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Deny,
            blocked.EvaluateToolCall("get_airplane_mode", EmptyArguments()).Kind);
        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Allow,
            allowed.EvaluateToolCall("get_airplane_mode", EmptyArguments()).Kind);
    }

    [TestMethod]
    public void McpServer_ExperimentalAirplaneTools_AreAdvertisedOnlyAfterExplicitOptIn() {
        using var defaultClient = McpTestClient.Start();
        using var experimentalClient = McpTestClient.Start("mcp serve --allow-experimental");

        HashSet<string> defaultTools = ReadAdvertisedTools(defaultClient, 1);
        HashSet<string> experimentalTools = ReadAdvertisedTools(experimentalClient, 1);

        Assert.IsFalse(defaultTools.Contains("get_airplane_mode"));
        Assert.IsFalse(defaultTools.Contains("set_airplane_mode"));
        Assert.IsTrue(experimentalTools.Contains("get_airplane_mode"));
        Assert.IsTrue(experimentalTools.Contains("set_airplane_mode"));
    }

    [TestMethod]
    public void McpSafetyPolicy_RadioMutation_RequiresSystemSettingsAndMutationOptIns() {
        JsonElement arguments = CreateArguments(new { kind = "WiFi", state = "Off" });
        var noSystemSettings = new DesktopManager.Cli.McpSafetyPolicy(true, false, false);
        var noMutations = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: false,
            allowForegroundInput: false,
            dryRun: false,
            allowSystemSettings: true);
        var allowed = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false,
            allowSystemSettings: true);

        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Deny,
            noSystemSettings.EvaluateToolCall("set_radio_state", arguments).Kind);
        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Deny,
            noMutations.EvaluateToolCall("set_radio_state", arguments).Kind);
        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Allow,
            allowed.EvaluateToolCall("set_radio_state", arguments).Kind);
    }

    [TestMethod]
    public void McpSafetyPolicy_ExperimentalAirplaneMutation_RequiresBothSpecializedGates() {
        JsonElement arguments = CreateArguments(new { state = "Enabled" });
        var missingExperimental = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false,
            allowSystemSettings: true);
        var allowed = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false,
            allowSystemSettings: true,
            allowExperimental: true);

        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Deny,
            missingExperimental.EvaluateToolCall("set_airplane_mode", arguments).Kind);
        Assert.AreEqual(
            DesktopManager.Cli.McpToolSafetyDecisionKind.Allow,
            allowed.EvaluateToolCall("set_airplane_mode", arguments).Kind);
    }

    [TestMethod]
    public void McpSafetyPolicy_SystemSettingsMutation_IsBlockedByProcessFilters() {
        JsonElement arguments = CreateArguments(new { deviceId = "endpoint", muted = true });
        var policy = new DesktopManager.Cli.McpSafetyPolicy(
            allowMutations: true,
            allowForegroundInput: false,
            dryRun: false,
            allowedProcessPatterns: new[] { "notepad" },
            allowSystemSettings: true);

        DesktopManager.Cli.McpToolSafetyDecision decision = policy.EvaluateToolCall("configure_audio_endpoint", arguments);

        Assert.AreEqual(DesktopManager.Cli.McpToolSafetyDecisionKind.Deny, decision.Kind);
        StringAssert.Contains(decision.Message, "global desktop state");
    }

    [TestMethod]
    public void McpCatalog_ProfileApplyResultContract_RejectsFailedMutation() {
        var result = new WorkstationProfileApplyResult(
            succeeded: false,
            rolledBack: true,
            error: "A required monitor is missing.",
            warnings: new[] { "Audio state was not changed." });

        DesktopManager.Cli.CommandLineException exception = Assert.ThrowsExactly<DesktopManager.Cli.CommandLineException>(
            () => DesktopManager.Cli.McpCatalog.RequireSuccessfulWorkstationProfileApply(result));

        StringAssert.Contains(exception.Message, "A required monitor is missing.");
        StringAssert.Contains(exception.Message, "Previous desktop state was restored.");
        StringAssert.Contains(exception.Message, "Audio state was not changed.");
    }

    [TestMethod]
    public void McpCatalog_RadioResultContract_RejectsUnappliedMutation() {
        var result = new DesktopRadioSetResult(
            new DesktopRadioInfo("Wi-Fi", DesktopRadioKind.WiFi, DesktopRadioState.On),
            DesktopRadioAccessStatus.DeniedBySystem,
            accepted: false,
            applied: false);

        DesktopManager.Cli.CommandLineException exception = Assert.ThrowsExactly<DesktopManager.Cli.CommandLineException>(
            () => DesktopManager.Cli.McpCatalog.RequireAppliedRadioResults(new[] { result }));

        StringAssert.Contains(exception.Message, "Wi-Fi");
        StringAssert.Contains(exception.Message, nameof(DesktopRadioAccessStatus.DeniedBySystem));
    }

    [TestMethod]
    public void McpCatalog_ConfigureAudioEndpoint_RejectsAllInvalidRolesBeforeEndpointAccess() {
        JsonElement arguments = CreateArguments(new {
            deviceId = "missing-endpoint",
            volume = 42,
            muted = true,
            defaultRoles = new[] { "Console", "42" }
        });

        bool succeeded = DesktopManager.Cli.McpCatalog.TryCallTool(
            "configure_audio_endpoint",
            arguments,
            out _,
            out string? error);

        Assert.IsFalse(succeeded);
        StringAssert.Contains(error, "defaultRoles");
        StringAssert.Contains(error, "42");
    }

    [TestMethod]
    public void McpCatalog_ApplyPersonalization_RejectsUndefinedEnumsBeforeServiceAccess() {
        JsonElement arguments = CreateArguments(new { systemTheme = "42" });

        bool succeeded = DesktopManager.Cli.McpCatalog.TryCallTool(
            "apply_personalization",
            arguments,
            out _,
            out string? error);

        Assert.IsFalse(succeeded);
        StringAssert.Contains(error, "systemTheme");
        StringAssert.Contains(error, "42");
    }

    private static JsonElement EmptyArguments() {
        return CreateArguments(new { });
    }

    private static HashSet<string> ReadAdvertisedTools(McpTestClient client, int requestId) {
        return client.SendRequest(requestId, "tools/list")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static JsonElement CreateArguments(object value) {
        return JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement.Clone();
    }
}
#endif
