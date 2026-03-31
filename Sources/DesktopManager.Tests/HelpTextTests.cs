#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI help topic routing and advertised help commands.
/// </summary>
public class HelpTextTests {
    [DataTestMethod]
    [DataRow("desktop", "Desktop commands:")]
    [DataRow("window", "Window commands:")]
    [DataRow("control", "Control commands:")]
    [DataRow("monitor", "Monitor commands:")]
    [DataRow("process", "Process commands:")]
    [DataRow("screenshot", "Screenshot commands:")]
    [DataRow("target", "Target commands:")]
    [DataRow("control-target", "Control-target commands:")]
    [DataRow("layout", "Layout commands:")]
    [DataRow("snapshot", "Snapshot commands:")]
    [DataRow("diagnostic", "Diagnostic commands:")]
    [DataRow("workflow", "Workflow commands:")]
    [DataRow("mcp", "MCP commands:")]
    /// <summary>
    /// Ensures every supported help topic resolves to its specific help block.
    /// </summary>
    public void GetHelpText_KnownTopics_ReturnTopicSpecificHelp(string topic, string expectedHeader) {
        string help = global::DesktopManager.Cli.CliApplication.GetHelpText(topic);

        StringAssert.StartsWith(help, expectedHeader);
        Assert.AreNotEqual(global::DesktopManager.Cli.HelpText.GetGeneralHelp(), help);
    }

    [TestMethod]
    /// <summary>
    /// Ensures unsupported help topics fall back to general help text.
    /// </summary>
    public void GetHelpText_UnknownTopic_ReturnsGeneralHelp() {
        string help = global::DesktopManager.Cli.CliApplication.GetHelpText("unknown-topic");

        Assert.AreEqual(global::DesktopManager.Cli.HelpText.GetGeneralHelp(), help);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the general help text advertises every supported group help command.
    /// </summary>
    public void GetGeneralHelp_ListsAllSupportedHelpTopics() {
        string help = global::DesktopManager.Cli.HelpText.GetGeneralHelp();

        foreach (string topic in new[] {
            "desktop",
            "window",
            "control",
            "monitor",
            "process",
            "screenshot",
            "target",
            "control-target",
            "layout",
            "snapshot",
            "diagnostic",
            "workflow",
            "mcp"
        }) {
            StringAssert.Contains(help, $"desktopmanager help {topic}");
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor help advertises the newer monitor configuration commands.
    /// </summary>
    public void GetMonitorHelp_ListsConfigurationCommands() {
        string help = global::DesktopManager.Cli.HelpText.GetMonitorHelp();

        StringAssert.Contains(help, "desktopmanager monitor brightness");
        StringAssert.Contains(help, "desktopmanager monitor set-position");
        StringAssert.Contains(help, "desktopmanager monitor set-resolution");
        StringAssert.Contains(help, "desktopmanager monitor set-dpi-scaling");
        StringAssert.Contains(help, "desktopmanager monitor set-taskbar");
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop help advertises the personalization commands.
    /// </summary>
    public void GetDesktopHelp_ListsPersonalizationCommands() {
        string help = global::DesktopManager.Cli.HelpText.GetDesktopHelp();

        StringAssert.Contains(help, "desktopmanager desktop background-color");
        StringAssert.Contains(help, "desktopmanager desktop set-background-color");
        StringAssert.Contains(help, "desktopmanager desktop wallpaper-position");
        StringAssert.Contains(help, "desktopmanager desktop set-wallpaper-position");
        StringAssert.Contains(help, "desktopmanager desktop start-slideshow");
        StringAssert.Contains(help, "desktopmanager desktop advance-slideshow");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window help advertises the newer window mutation commands.
    /// </summary>
    public void GetWindowHelp_ListsAdditionalMutationCommands() {
        string help = global::DesktopManager.Cli.HelpText.GetWindowHelp();

        StringAssert.Contains(help, "desktopmanager window process-info");
        StringAssert.Contains(help, "desktopmanager window owner-process-info");
        StringAssert.Contains(help, "desktopmanager window keep-alive-list");
        StringAssert.Contains(help, "desktopmanager window keep-alive-start");
        StringAssert.Contains(help, "desktopmanager window keep-alive-stop");
        StringAssert.Contains(help, "desktopmanager window maximize");
        StringAssert.Contains(help, "desktopmanager window restore");
        StringAssert.Contains(help, "desktopmanager window close");
        StringAssert.Contains(help, "desktopmanager window topmost");
        StringAssert.Contains(help, "desktopmanager window visibility");
        StringAssert.Contains(help, "desktopmanager window transparency");
    }
}
#endif
