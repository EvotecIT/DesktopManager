#if !NET472
using System.Text.Json;

namespace DesktopManager.Tests;

/// <summary>
/// Protects the process-scoped keep-awake lease replacement contract.
/// </summary>
[TestClass]
public class McpKeepAwakeManagerTests {
    [TestMethod]
    public void Configure_InvalidReplacementPreservesActiveLease() {
        try {
            DesktopManager.Cli.McpKeepAwakeManager.Configure(
                enabled: true,
                KeepAwakeOptions.System,
                durationSeconds: null);

            Assert.ThrowsExactly<DesktopManager.Cli.CommandLineException>(() =>
                DesktopManager.Cli.McpKeepAwakeManager.Configure(
                    enabled: true,
                    KeepAwakeOptions.System,
                    durationSeconds: 0));

            JsonElement state = JsonSerializer.SerializeToElement(DesktopManager.Cli.McpKeepAwakeManager.GetState());
            Assert.IsTrue(state.GetProperty("enabled").GetBoolean());
        } finally {
            DesktopManager.Cli.McpKeepAwakeManager.Configure(
                enabled: false,
                KeepAwakeOptions.System,
                durationSeconds: null);
        }
    }
}
#endif
