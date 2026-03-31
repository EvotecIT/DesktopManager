#if !NET472
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Regression tests for MCP catalog argument handling.
/// </summary>
public class McpCatalogTests {
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

    private static JsonElement CreateArguments(object value) {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }
}
#endif
