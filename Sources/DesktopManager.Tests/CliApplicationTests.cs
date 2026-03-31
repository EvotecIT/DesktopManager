#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI entrypoint help and dispatch behavior.
/// </summary>
public class CliApplicationTests {
    [TestMethod]
    /// <summary>
    /// Ensures the CLI prints general help when invoked without arguments.
    /// </summary>
    public void Run_WithNoArguments_WritesGeneralHelpToStandardOutput() {
        (int exitCode, string standardOutput, string standardError) = RunCli();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(standardOutput, "desktopmanager - Windows desktop automation CLI");
        StringAssert.Contains(standardOutput, "Groups:");
        Assert.AreEqual(string.Empty, standardError);
    }

    [TestMethod]
    /// <summary>
    /// Ensures group help routes to the requested topic instead of the general help text.
    /// </summary>
    public void Run_WithHelpTopic_WritesGroupHelpToStandardOutput() {
        (int exitCode, string standardOutput, string standardError) = RunCli("help", "window");

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(standardOutput, "Window commands:");
        Assert.AreEqual(string.Empty, standardError);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the shared help flag returns the general help text before command dispatch.
    /// </summary>
    public void Run_WithHelpFlag_WritesGeneralHelpToStandardOutput() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "list", "--help");

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(standardOutput, "desktopmanager - Windows desktop automation CLI");
        Assert.AreEqual(string.Empty, standardError);
    }

    [TestMethod]
    /// <summary>
    /// Ensures unknown command groups fail with an error and include the general help text on standard error.
    /// </summary>
    public void Run_WithUnknownGroup_WritesErrorAndGeneralHelpToStandardError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("unknown-group");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Unknown command group 'unknown-group'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [DataTestMethod]
    [DataRow("window")]
    [DataRow("control")]
    [DataRow("desktop")]
    [DataRow("monitor")]
    [DataRow("process")]
    [DataRow("screenshot")]
    [DataRow("target")]
    [DataRow("control-target")]
    [DataRow("layout")]
    [DataRow("snapshot")]
    [DataRow("diagnostic")]
    [DataRow("workflow")]
    [DataRow("mcp")]
    /// <summary>
    /// Ensures known groups return a consistent command-line error when the action is unknown.
    /// </summary>
    public void Run_WithUnknownActionForKnownGroup_WritesGroupSpecificErrorAndGeneralHelp(string group) {
        (int exitCode, string standardOutput, string standardError) = RunCli(group, "unknown-action");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, $"Error: Unknown {group} command 'unknown-action'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [DataTestMethod]
    [DataRow("window")]
    [DataRow("control")]
    [DataRow("desktop")]
    [DataRow("monitor")]
    [DataRow("process")]
    [DataRow("screenshot")]
    [DataRow("target")]
    [DataRow("control-target")]
    [DataRow("layout")]
    [DataRow("snapshot")]
    [DataRow("diagnostic")]
    [DataRow("workflow")]
    [DataRow("mcp")]
    /// <summary>
    /// Ensures known groups fail consistently when the action is omitted entirely.
    /// </summary>
    public void Run_WithMissingActionForKnownGroup_WritesEmptyActionErrorAndGeneralHelp(string group) {
        (int exitCode, string standardOutput, string standardError) = RunCli(group);

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, $"Error: Missing required {group} command.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures process start reports a missing process path through the CLI entrypoint.
    /// </summary>
    public void Run_ProcessStartWithoutPath_WritesMissingProcessPathError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("process", "start");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required process path.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures target get reports a missing target name through the CLI entrypoint.
    /// </summary>
    public void Run_TargetGetWithoutName_WritesMissingTargetNameError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("target", "get");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required target name.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window snap reports a missing required option through the CLI entrypoint.
    /// </summary>
    public void Run_WindowSnapWithoutPosition_WritesMissingRequiredOptionError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "snap");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required option '--position'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing rejects contradictory paste and strict foreground-input flags through the CLI entrypoint.
    /// </summary>
    public void Run_WindowTypeWithPasteAndForegroundInput_WritesCombinationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "type", "--text", "hello", "--paste", "--foreground-input");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Cannot combine '--paste' with '--foreground-input'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing rejects contradictory paste and physical-key flags through the CLI entrypoint.
    /// </summary>
    public void Run_WindowTypeWithPasteAndPhysicalKeys_WritesCombinationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "type", "--text", "hello", "--paste", "--physical-keys");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Cannot combine '--paste' with '--foreground-input'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing rejects contradictory paste and script flags through the CLI entrypoint.
    /// </summary>
    public void Run_WindowTypeWithPasteAndScript_WritesCombinationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "type", "--text", "hello", "--paste", "--script");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Cannot combine '--paste' with '--script'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures keep-alive start rejects non-positive interval values through the CLI entrypoint.
    /// </summary>
    public void Run_WindowKeepAliveStartWithZeroInterval_WritesPositiveIntervalError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "keep-alive-start", "--process", "notepad", "--interval-ms", "0");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--interval-ms' expects a value greater than 0.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures keep-alive stop rejects conflicting global-stop and selector options through the CLI entrypoint.
    /// </summary>
    public void Run_WindowKeepAliveStopWithAllSessionsAndSelector_WritesCombinationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "keep-alive-stop", "--all-sessions", "--process", "notepad");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Cannot combine '--all-sessions' with window selectors or '--all'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures malformed empty option names are reported through the CLI entrypoint.
    /// </summary>
    public void Run_WithMalformedEmptyOptionName_WritesParseError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("--");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Encountered an empty option name.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid integer values are reported through the CLI entrypoint before command execution.
    /// </summary>
    public void Run_ProcessStartWithInvalidIntegerOption_WritesIntegerError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("process", "start", "notepad.exe", "--wait-for-input-idle-ms", "abc");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--wait-for-input-idle-ms' expects an integer value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid numeric values are reported through the CLI entrypoint before command execution.
    /// </summary>
    public void Run_TargetSaveWithInvalidNumericOption_WritesNumericError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("target", "save", "editor-center", "--x-ratio", "not-a-number", "--y-ratio", "0.5");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--x-ratio' expects a numeric value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid assertion tolerances are reported through the CLI entrypoint before layout checks run.
    /// </summary>
    public void Run_LayoutAssertWithInvalidTolerance_WritesIntegerError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("layout", "assert", "coding", "--position-tolerance-px", "oops");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--position-tolerance-px' expects an integer value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor set-position reports missing required bounds through the CLI entrypoint.
    /// </summary>
    public void Run_MonitorSetPositionWithoutRight_WritesMissingRequiredOptionError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("monitor", "set-position", "--left", "0", "--top", "0", "--bottom", "1080");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required option '--right'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor set-resolution reports invalid integer values through the CLI entrypoint.
    /// </summary>
    public void Run_MonitorSetResolutionWithInvalidWidth_WritesIntegerError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("monitor", "set-resolution", "--width", "wide", "--height", "1080");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--width' expects an integer value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor set-taskbar rejects contradictory visibility flags through the CLI entrypoint.
    /// </summary>
    public void Run_MonitorSetTaskbarWithShowAndHide_WritesCombinationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("monitor", "set-taskbar", "--show", "--hide");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Cannot combine '--show' with '--hide'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop set-background-color reports a missing color through the CLI entrypoint.
    /// </summary>
    public void Run_DesktopSetBackgroundColorWithoutColor_WritesMissingRequiredOptionError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("desktop", "set-background-color");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--color' is required.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop set-wallpaper-position rejects unsupported position values through the CLI entrypoint.
    /// </summary>
    public void Run_DesktopSetWallpaperPositionWithInvalidValue_WritesValidationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("desktop", "set-wallpaper-position", "--position", "sideways");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--position' expects one of: center, tile, stretch, fit, fill, span.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor set-wallpaper requires a path or URL through the CLI entrypoint.
    /// </summary>
    public void Run_MonitorSetWallpaperWithoutSource_WritesValidationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("monitor", "set-wallpaper", "--primary");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Either wallpaperPath or url must be provided.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window topmost rejects contradictory on and off flags through the CLI entrypoint.
    /// </summary>
    public void Run_WindowTopMostWithOnAndOff_WritesCombinationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "topmost", "--on", "--off");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Specify exactly one of '--on' or '--off'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window transparency validates alpha bounds through the CLI entrypoint.
    /// </summary>
    public void Run_WindowTransparencyWithOutOfRangeAlpha_WritesValidationError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "transparency", "--alpha", "300");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--alpha' expects a value from 0 to 255.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the hosted-session diagnostic CLI command reads the newest artifact via repository-root resolution.
    /// </summary>
    public void Run_DiagnosticHostedSessionSummaryOnly_WritesSummaryToStandardOutput() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "CliDiagnostic", Guid.NewGuid().ToString("N"));
        string artifactDirectory = Path.Combine(repositoryRoot, "Artifacts", "HostedSessionTyping");
        Directory.CreateDirectory(artifactDirectory);
        File.WriteAllText(Path.Combine(repositoryRoot, "DesktopManager.sln"), string.Empty);

        string artifactPath = Path.Combine(artifactDirectory, "sample.json");
        File.WriteAllText(artifactPath, """
{"FormatVersion":1,"Reason":"Test artifact","CreatedUtc":"2026-03-24T08:00:00Z","Summary":"summary from json","PolicyReport":"category='none'","RetryHistoryReport":{"CategoryHint":"none","Summary":"no retained external foreground interruption","ExternalCount":0,"DistinctFingerprintCount":0},"Status":{"WindowTitle":"DesktopManager Test App","StatusText":"Waiting for focus","ForegroundHistory":[]}}
""");
        File.WriteAllText(Path.Combine(artifactDirectory, "sample.summary.txt"), "summary from sidecar");

        string originalCurrentDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = repositoryRoot;
        try {
            (int exitCode, string standardOutput, string standardError) = RunCli("diagnostic", "hosted-session", "--summary-only");

            Assert.AreEqual(0, exitCode);
            StringAssert.Contains(standardOutput, "summary from sidecar");
            Assert.AreEqual(string.Empty, standardError);
        } finally {
            Environment.CurrentDirectory = originalCurrentDirectory;
            Directory.Delete(repositoryRoot, true);
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunCli(params string[] args) {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        int exitCode = global::DesktopManager.Cli.CliApplication.Run(args, standardOutput, standardError);
        return (exitCode, standardOutput.ToString(), standardError.ToString());
    }
}
#endif
