#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window process-info and keep-alive text output.
/// </summary>
public class WindowProcessAndKeepAliveCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures window process-info output renders the expected details.
    /// </summary>
    public void WriteWindowProcessInfoResults_WritesDetailedSummary() {
        var results = new List<global::DesktopManager.Cli.WindowProcessInfoResult> {
            new() {
                Window = new global::DesktopManager.Cli.WindowResult {
                    Title = "Untitled - Notepad",
                    Handle = "0x1234"
                },
                IsOwnerProcess = true,
                ProcessId = 456,
                ThreadId = 789,
                ProcessName = "notepad",
                ProcessPath = @"C:\Windows\System32\notepad.exe",
                IsElevated = false,
                IsWow64 = false
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWindowProcessInfoResults(results, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "window: Untitled - Notepad (0x1234)");
        StringAssert.Contains(output, "- Scope: Owner");
        StringAssert.Contains(output, "- ProcessId: 456");
        StringAssert.Contains(output, "- ThreadId: 789");
        StringAssert.Contains(output, "- ProcessName: notepad");
        StringAssert.Contains(output, "- IsElevated: False");
        StringAssert.Contains(output, "- IsWow64: False");
        StringAssert.Contains(output, @"- ProcessPath: C:\Windows\System32\notepad.exe");
    }

    [TestMethod]
    /// <summary>
    /// Ensures keep-alive list output renders the expected summary and window table.
    /// </summary>
    public void WriteWindowKeepAliveResults_WritesSummaryAndTable() {
        var windows = new List<global::DesktopManager.Cli.WindowResult> {
            new() {
                Title = "Untitled - Notepad",
                Handle = "0x1234",
                ProcessId = 456,
                MonitorIndex = 1,
                IsVisible = true,
                State = "Normal"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWindowKeepAliveResults(windows, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "keep-alive: 1 window(s)");
        StringAssert.Contains(output, "PID");
        StringAssert.Contains(output, "Handle");
        StringAssert.Contains(output, "Untitled - Notepad");
        StringAssert.Contains(output, "0x1234".Replace("0x", string.Empty));
    }
}
#endif
