#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window typing option mapping.
/// </summary>
public class WindowTypeCommandOptionsTests {
    [TestMethod]
    /// <summary>
    /// Ensures window type options map explicit foreground-input and delay flags.
    /// </summary>
    public void CreateTypeOptions_MapsForegroundTypingFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "safe probe",
            "--foreground-input",
            "--delay-ms", "25"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("safe probe", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsTrue(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.AreEqual(25, options.DelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures physical-key typing implies the strict foreground-input path.
    /// </summary>
    public void CreateTypeOptions_PhysicalKeys_EnablesForegroundInputToo() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "safe probe",
            "--physical-keys"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("safe probe", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsTrue(options.ForegroundInput);
        Assert.IsTrue(options.PhysicalKeys);
        Assert.AreEqual(0, options.DelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures script typing maps chunking and line pacing options without forcing a different delivery mode.
    /// </summary>
    public void CreateTypeOptions_MapsScriptFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "line1\nline2",
            "--script",
            "--chunk-size", "32",
            "--line-delay-ms", "15"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("line1\nline2", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsFalse(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.IsTrue(options.ScriptMode);
        Assert.AreEqual(32, options.ScriptChunkLength);
        Assert.AreEqual(15, options.ScriptLineDelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session typing enables the physical foreground path and uses safer pacing defaults.
    /// </summary>
    public void CreateTypeOptions_HostedSession_UsesSaferForegroundDefaults() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "safe probe",
            "--hosted-session",
            "--script"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("safe probe", options.Text);
        Assert.IsTrue(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.IsTrue(options.HostedSession);
        Assert.IsTrue(options.ScriptMode);
        Assert.AreEqual(35, options.DelayMilliseconds);
        Assert.AreEqual(120, options.ScriptLineDelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures window type options default to simulated typing without strict foreground requirements.
    /// </summary>
    public void CreateTypeOptions_UsesDefaultsWhenUnset() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "hello"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("hello", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsFalse(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.IsFalse(options.HostedSession);
        Assert.IsFalse(options.ScriptMode);
        Assert.AreEqual(120, options.ScriptChunkLength);
        Assert.AreEqual(0, options.ScriptLineDelayMilliseconds);
        Assert.AreEqual(0, options.DelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures secrets can come from an environment variable or standard input instead of the command line, and that a
    /// piped password does not end with Enter while inner line breaks are kept.
    /// </summary>
    public void ReadText_TakesSecretsFromTheEnvironmentOrStandardInput() {
        var fromEnvironment = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] { "window", "type-session", "--text-env", "SESSION_SECRET" });
        var fromStandardInput = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] { "window", "type-session", "--text-stdin" });

        char[] environmentText = global::DesktopManager.Cli.SessionTypingCommand.ReadText(fromEnvironment, new StringReader("unused"), name => name == "SESSION_SECRET" ? "p@ss" : null);
        char[] pipedText = global::DesktopManager.Cli.SessionTypingCommand.ReadText(fromStandardInput, new StringReader("line1\r\nline2\r\n"), _ => null);

        Assert.AreEqual("p@ss", new string(environmentText));
        Assert.AreEqual("line1\r\nline2", new string(pipedText));
    }

    [TestMethod]
    [DataRow(new[] { "--text", "a", "--text-stdin" })]
    [DataRow(new[] { "--delay-ms", "5" })]
    [DataRow(new[] { "--text-env", "DESKTOPMANAGER_TEST_UNSET_VARIABLE" })]
    /// <summary>
    /// Ensures exactly one text source is required and a missing environment variable is reported instead of typing nothing.
    /// </summary>
    public void ReadText_RequiresExactlyOneAvailableSource(string[] options) {
        var arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] { "window", "type-session" }.Concat(options).ToArray());

        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() =>
            global::DesktopManager.Cli.SessionTypingCommand.ReadText(arguments, new StringReader(string.Empty), _ => null));
    }

    [TestMethod]
    /// <summary>
    /// Ensures session typing options accept the dashed spellings shown in help and reject unknown values.
    /// </summary>
    public void SessionTypingOptions_ParseDashedValuesAndRejectUnknownOnes() {
        var arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window", "type-session", "--profile", "vnc", "--line-break", "shift-enter", "--caps-lock", "turn-off", "--layout", "de-DE", "--foreground-timeout-ms", "0"
        });

        SessionTypingOptions options = global::DesktopManager.Cli.SessionTypingCommand.CreateOptions(arguments);

        Assert.AreEqual(SessionTypingProfile.Vnc, options.Profile);
        Assert.AreEqual(SessionLineBreakMode.ShiftEnter, options.LineBreak);
        Assert.AreEqual(CapsLockPolicy.TurnOff, options.CapsLock);
        Assert.AreEqual("de-DE", options.KeyboardLayout);
        Assert.AreEqual(TimeSpan.Zero, options.ForegroundTimeout);
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() => global::DesktopManager.Cli.SessionTypingCommand.CreateOptions(
            global::DesktopManager.Cli.CommandLineArguments.Parse(new[] { "window", "type-session", "--profile", "telnet" })));
    }
}
#endif
