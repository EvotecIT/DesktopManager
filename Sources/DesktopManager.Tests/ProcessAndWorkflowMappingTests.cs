#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI process and workflow result mapping helpers.
/// </summary>
public class ProcessAndWorkflowMappingTests {
    [TestMethod]
    /// <summary>
    /// Ensures CLI process launch mapping preserves resolved process and main window metadata.
    /// </summary>
    public void BuildProcessLaunchResult_MapsResolvedProcessAndMainWindow() {
        var launch = new DesktopProcessLaunchInfo {
            FilePath = "DesktopManager.TestApp.exe",
            Arguments = "--surface editor",
            WorkingDirectory = @"C:\Temp",
            ProcessId = 123,
            ResolvedProcessId = 456,
            HasExited = false,
            MainWindow = new WindowInfo {
                Title = "DesktopManager Test App",
                Handle = new IntPtr(0x1234),
                ProcessId = 456,
                ThreadId = 789,
                IsVisible = true,
                IsTopMost = false,
                State = WindowState.Normal,
                Left = 10,
                Top = 20,
                Right = 810,
                Bottom = 620,
                MonitorIndex = 1,
                MonitorDeviceName = @"\\.\\DISPLAY1"
            }
        };

        global::DesktopManager.Cli.ProcessLaunchResult result = global::DesktopManager.Cli.DesktopOperations.BuildProcessLaunchResult(launch);

        Assert.AreEqual("DesktopManager.TestApp.exe", result.FilePath);
        Assert.AreEqual("--surface editor", result.Arguments);
        Assert.AreEqual(@"C:\Temp", result.WorkingDirectory);
        Assert.AreEqual(123, result.ProcessId);
        Assert.AreEqual(456, result.ResolvedProcessId);
        Assert.IsFalse(result.HasExited);
        Assert.IsNotNull(result.MainWindow);
        Assert.AreEqual("DesktopManager Test App", result.MainWindow.Title);
        Assert.AreEqual("0x1234", result.MainWindow.Handle);
        Assert.AreEqual((uint)456, result.MainWindow.ProcessId);
        Assert.AreEqual((uint)789, result.MainWindow.ThreadId);
        Assert.AreEqual("Normal", result.MainWindow.State);
        Assert.AreEqual(800, result.MainWindow.Width);
        Assert.AreEqual(600, result.MainWindow.Height);
    }

    [TestMethod]
    /// <summary>
    /// Ensures CLI window-process mapping preserves the associated window and process metadata.
    /// </summary>
    public void MapWindowProcessInfo_MapsAssociatedWindowAndProcessMetadata() {
        var window = new WindowInfo {
            Title = "Untitled - Notepad",
            Handle = new IntPtr(0x1234),
            ProcessId = 987,
            ThreadId = 654,
            IsVisible = true,
            IsTopMost = false,
            State = WindowState.Normal,
            Left = 10,
            Top = 20,
            Right = 810,
            Bottom = 620,
            MonitorIndex = 1,
            MonitorDeviceName = @"\\.\\DISPLAY1"
        };
        var info = new WindowProcessInfo {
            ProcessId = 987,
            ThreadId = 654,
            ProcessName = "notepad",
            ProcessPath = @"C:\Windows\System32\notepad.exe",
            IsElevated = false,
            IsWow64 = false
        };

        global::DesktopManager.Cli.WindowProcessInfoResult result = global::DesktopManager.Cli.DesktopOperations.MapWindowProcessInfo(window, info, owner: true);

        Assert.AreEqual("Untitled - Notepad", result.Window.Title);
        Assert.AreEqual("0x1234", result.Window.Handle);
        Assert.IsTrue(result.IsOwnerProcess);
        Assert.AreEqual((uint)987, result.ProcessId);
        Assert.AreEqual((uint)654, result.ThreadId);
        Assert.AreEqual("notepad", result.ProcessName);
        Assert.AreEqual(@"C:\Windows\System32\notepad.exe", result.ProcessPath);
        Assert.AreEqual(false, result.IsElevated);
        Assert.AreEqual(false, result.IsWow64);
    }

    [TestMethod]
    /// <summary>
    /// Ensures CLI workflow result assembly preserves notes, artifacts, and the derived minimized count.
    /// </summary>
    public void BuildWorkflowResult_MapsNotesArtifactsAndMinimizedCount() {
        var minimizedWindows = new[] {
            new global::DesktopManager.Cli.WindowResult {
                Title = "Chat",
                ProcessId = 201
            },
            new global::DesktopManager.Cli.WindowResult {
                Title = "Mail",
                ProcessId = 202
            }
        };
        var resolvedWindow = new global::DesktopManager.Cli.WindowResult {
            Title = "Browser",
            ProcessId = 301
        };
        var focusedWindow = new global::DesktopManager.Cli.WindowResult {
            Title = "Editor",
            ProcessId = 302
        };
        var notes = new[] {
            "Applied named layout 'Pairing'.",
            "Focused requested coding window."
        };
        var beforeScreenshots = new[] {
            new global::DesktopManager.Cli.ScreenshotResult()
        };
        var afterScreenshots = new[] {
            new global::DesktopManager.Cli.ScreenshotResult(),
            new global::DesktopManager.Cli.ScreenshotResult()
        };
        var warnings = new[] {
            "Capture path normalized."
        };

        global::DesktopManager.Cli.WorkflowResult result = global::DesktopManager.Cli.DesktopOperations.BuildWorkflowResult(
            "prepare-for-coding",
            success: true,
            elapsedMilliseconds: 222,
            layoutName: "Pairing",
            layoutApplied: true,
            minimizedWindows,
            resolvedWindow,
            focusedWindow,
            notes,
            beforeScreenshots,
            afterScreenshots,
            warnings);

        Assert.AreEqual("prepare-for-coding", result.Action);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(222, result.ElapsedMilliseconds);
        Assert.AreEqual("Pairing", result.LayoutName);
        Assert.IsTrue(result.LayoutApplied);
        Assert.AreEqual(2, result.MinimizedCount);
        Assert.AreEqual(2, result.MinimizedWindows.Count);
        Assert.AreEqual("Chat", result.MinimizedWindows[0].Title);
        Assert.AreEqual("Browser", result.ResolvedWindow?.Title);
        Assert.AreEqual("Editor", result.FocusedWindow?.Title);
        CollectionAssert.AreEqual(notes, result.Notes.ToArray());
        Assert.AreEqual(1, result.BeforeScreenshots.Count);
        Assert.AreEqual(2, result.AfterScreenshots.Count);
        CollectionAssert.AreEqual(warnings, result.ArtifactWarnings.ToArray());
    }
}
#endif
