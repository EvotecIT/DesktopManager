#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared CLI command-line argument parsing.
/// </summary>
public class CommandLineArgumentsTests {
    [TestMethod]
    /// <summary>
    /// Ensures command parts, repeated options, and boolean flags are parsed consistently.
    /// </summary>
    public void Parse_SeparatesCommandPartsOptionsAndFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start",
            "notepad.exe",
            "--arguments", "first",
            "--arguments", "second",
            "--json",
            "--capture-after"
        });

        Assert.IsFalse(arguments.IsEmpty);
        Assert.AreEqual("process", arguments.GetCommandPart(0));
        Assert.AreEqual("start", arguments.GetCommandPart(1));
        Assert.AreEqual("notepad.exe", arguments.GetRequiredCommandPart(2, "process path"));
        Assert.AreEqual("second", arguments.GetOption("arguments"));
        CollectionAssert.AreEqual(new[] { "first", "second" }, arguments.GetOptions("arguments").ToArray());
        Assert.IsTrue(arguments.GetBoolFlag("json"));
        Assert.IsTrue(arguments.GetBoolFlag("capture-after"));
        Assert.IsNull(arguments.GetCommandPart(3));
    }

    [TestMethod]
    /// <summary>
    /// Ensures flags without explicit values are stored as true and missing options return null or empty collections.
    /// </summary>
    public void Parse_UsesTrueForFlagsAndEmptyForMissingOptions() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "--json"
        });

        Assert.IsFalse(arguments.IsEmpty);
        Assert.IsTrue(arguments.HasFlag("json"));
        Assert.IsTrue(arguments.GetBoolFlag("json"));
        Assert.AreEqual("true", arguments.GetOption("json"));
        Assert.IsNull(arguments.GetOption("missing"));
        Assert.AreEqual(0, arguments.GetOptions("missing").Count);
    }

    [TestMethod]
    /// <summary>
    /// Ensures integer and double parsing use invariant culture and required integer lookups work.
    /// </summary>
    public void NumericAccessors_ParseInvariantValues() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "--count", "42",
            "--ratio", "1.5",
            "--large", "1,024"
        });

        Assert.AreEqual(42, arguments.GetIntOption("count"));
        Assert.AreEqual(42, arguments.GetRequiredIntOption("count"));
        Assert.AreEqual(1.5d, arguments.GetDoubleOption("ratio"));
        Assert.AreEqual(1024d, arguments.GetDoubleOption("large"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures required and invalid value paths throw clear command-line exceptions.
    /// </summary>
    public void Accessors_ThrowForMissingOrInvalidValues() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "list",
            "--count", "abc",
            "--ratio", "not-a-number"
        });

        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() => arguments.GetRequiredOption("missing"));
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() => arguments.GetRequiredCommandPart(5, "action"));
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() => arguments.GetIntOption("count"));
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() => arguments.GetRequiredIntOption("missing-int"));
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() => arguments.GetDoubleOption("ratio"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures malformed option names are rejected during parsing.
    /// </summary>
    public void Parse_ThrowsForEmptyOptionName() {
        Assert.ThrowsExactly<global::DesktopManager.Cli.CommandLineException>(() =>
            global::DesktopManager.Cli.CommandLineArguments.Parse(new[] { "--" }));
    }
}
#endif
