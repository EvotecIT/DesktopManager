#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI desktop text output.
/// </summary>
public class DesktopCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures background color output renders the expected values.
    /// </summary>
    public void WriteColorResult_WritesColorSummary() {
        var result = new global::DesktopManager.Cli.DesktopColorResult {
            Value = 1056816,
            HexValue = "0x102030"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.DesktopCommands.WriteColorResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "BackgroundColor");
        StringAssert.Contains(output, "0x102030");
        StringAssert.Contains(output, "1056816");
    }

    [TestMethod]
    /// <summary>
    /// Ensures wallpaper position output renders the expected value.
    /// </summary>
    public void WriteWallpaperPositionResult_WritesPositionSummary() {
        var result = new global::DesktopManager.Cli.DesktopWallpaperPositionResult {
            Position = "Fill"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.DesktopCommands.WriteWallpaperPositionResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "WallpaperPosition");
        StringAssert.Contains(output, "Fill");
    }

    [TestMethod]
    /// <summary>
    /// Ensures slideshow output renders the expected action details.
    /// </summary>
    public void WriteSlideshowResult_WritesSlideshowSummary() {
        var result = new global::DesktopManager.Cli.DesktopSlideshowResult {
            Action = "start-desktop-slideshow",
            IsRunning = true,
            ImageCount = 2
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.DesktopCommands.WriteSlideshowResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "start-desktop-slideshow");
        StringAssert.Contains(output, "running=True");
        StringAssert.Contains(output, "ImageCount: 2");
    }
}
#endif
