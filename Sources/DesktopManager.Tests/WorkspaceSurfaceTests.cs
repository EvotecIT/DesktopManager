#if NET8_0_OR_GREATER
using System.Text.Json;
using DesktopManager.Cli;

namespace DesktopManager.Tests;

[TestClass]
public sealed class WorkspaceSurfaceTests {
    [TestMethod]
    public void WorkspaceMcp_DefaultSessionDoesNotExposeExperimentalTools() {
        var policy = new McpSafetyPolicy(false, false, false);
        using JsonDocument arguments = JsonDocument.Parse("{}");
        Assert.AreEqual(McpToolSafetyDecisionKind.Deny, policy.EvaluateToolCall("get_workspace_desktops", arguments.RootElement).Kind);
    }

    [TestMethod]
    public void WorkspaceMcp_MutationsRespectReadOnlySettingsAndProcessScope() {
        using JsonDocument arguments = JsonDocument.Parse("{\"path\":\"C:\\\\workspace.json\"}");
        var readOnly = new McpSafetyPolicy(false, false, false, allowExperimental: true);
        Assert.AreEqual(McpToolSafetyDecisionKind.Deny, readOnly.EvaluateToolCall("restore_workspace", arguments.RootElement).Kind);
        var noSettings = new McpSafetyPolicy(true, false, false, allowExperimental: true);
        Assert.AreEqual(McpToolSafetyDecisionKind.Deny, noSettings.EvaluateToolCall("restore_workspace", arguments.RootElement).Kind);
        var processScope = new McpSafetyPolicy(true, false, false, new[] { "notepad" }, allowSystemSettings: true, allowExperimental: true);
        Assert.AreEqual(McpToolSafetyDecisionKind.Deny, processScope.EvaluateToolCall("restore_workspace", arguments.RootElement).Kind);
        var dryRun = new McpSafetyPolicy(true, false, true, allowSystemSettings: true, allowExperimental: true);
        Assert.AreEqual(McpToolSafetyDecisionKind.DryRun, dryRun.EvaluateToolCall("restore_workspace", arguments.RootElement).Kind);
    }

    [TestMethod]
    public void WorkspaceCli_HelpAndMissingPathAreActionableWithoutDesktopChanges() {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.AreEqual(0, CliApplication.Run(new[] { "help", "workspace" }, output, error));
        StringAssert.Contains(output.ToString(), "--virtual-desktops");
        Assert.AreEqual(1, CliApplication.Run(new[] { "workspace", "preview" }, output, error));
        StringAssert.Contains(error.ToString(), "--path");
    }

    [TestMethod]
    public void WorkspaceCli_IncompleteRecoveryCannotBeReportedAsComplete() {
        var result = new WorkspaceRestoreResult();
        result.Windows.Add(new WorkspaceWindowResult { MatchStatus = WindowLayoutMatchStatus.Missing });
        Assert.IsFalse(WorkspaceCommands.IsComplete(result));
        result.Windows[0].PositionApplied = true;
        result.Windows[0].VirtualDesktopApplied = false;
        Assert.IsFalse(WorkspaceCommands.IsComplete(result));
        result.Windows[0].VirtualDesktopApplied = true;
        Assert.IsTrue(WorkspaceCommands.IsComplete(result));
    }
}
#endif
