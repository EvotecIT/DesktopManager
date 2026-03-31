#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI monitor text output.
/// </summary>
public class MonitorCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures monitor output renders the expected table headers and row values.
    /// </summary>
    public void WriteMonitorResults_WritesMonitorTable() {
        var monitors = new List<global::DesktopManager.Cli.MonitorResult> {
            new() {
                Index = 1,
                IsPrimary = true,
                IsConnected = true,
                Left = 0,
                Top = 0,
                Right = 1920,
                Bottom = 1080,
                DeviceName = @"\\.\DISPLAY1",
                DeviceString = "Primary Display"
            },
            new() {
                Index = 2,
                IsPrimary = false,
                IsConnected = false,
                Left = 1920,
                Top = 0,
                Right = 3840,
                Bottom = 1080,
                DeviceName = @"\\.\DISPLAY2",
                DeviceString = "Dock Display"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.MonitorCommands.WriteMonitorResults(monitors, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Idx");
        StringAssert.Contains(output, "Primary");
        StringAssert.Contains(output, "Connected");
        StringAssert.Contains(output, "Bounds");
        StringAssert.Contains(output, "DeviceName");
        StringAssert.Contains(output, "Device");
        StringAssert.Contains(output, "1");
        StringAssert.Contains(output, "Yes");
        StringAssert.Contains(output, "0,0,1920,1080");
        StringAssert.Contains(output, @"\\.\DISPLAY1");
        StringAssert.Contains(output, "Primary Display");
        StringAssert.Contains(output, "2");
        StringAssert.Contains(output, "1920,0,3840,1080");
        StringAssert.Contains(output, @"\\.\DISPLAY2");
        StringAssert.Contains(output, "Dock Display");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor brightness output renders the expected table headers and row values.
    /// </summary>
    public void WriteMonitorBrightnessResults_WritesBrightnessTable() {
        var monitors = new List<global::DesktopManager.Cli.MonitorBrightnessResult> {
            new() {
                Index = 1,
                IsPrimary = true,
                Brightness = 65,
                DeviceName = @"\\.\DISPLAY1",
                DeviceId = "DISPLAY\\ABC123"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.MonitorCommands.WriteMonitorBrightnessResults(monitors, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Brightness");
        StringAssert.Contains(output, @"\\.\DISPLAY1");
        StringAssert.Contains(output, "DISPLAY\\ABC123");
        StringAssert.Contains(output, "65");
    }

    [TestMethod]
    /// <summary>
    /// Ensures monitor wallpaper output renders the expected table headers and row values.
    /// </summary>
    public void WriteMonitorWallpaperResults_WritesWallpaperTable() {
        var monitors = new List<global::DesktopManager.Cli.MonitorWallpaperResult> {
            new() {
                Index = 1,
                IsPrimary = true,
                DeviceName = @"\\.\DISPLAY1",
                DeviceId = "DISPLAY\\ABC123",
                Wallpaper = @"C:\Wallpapers\Aurora.jpg"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.MonitorCommands.WriteMonitorWallpaperResults(monitors, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Wallpaper");
        StringAssert.Contains(output, @"\\.\DISPLAY1");
        StringAssert.Contains(output, "DISPLAY\\ABC123");
        StringAssert.Contains(output, @"C:\Wallpapers\Aurora.jpg");
    }
}
#endif
