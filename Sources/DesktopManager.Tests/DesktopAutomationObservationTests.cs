using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for desktop automation observation and extended wait helpers.
/// </summary>
public class DesktopAutomationObservationTests {
    [TestMethod]
    /// <summary>
    /// Ensures text observation rejects a missing selector.
    /// </summary>
    public void DesktopAutomationService_ObserveWindowText_NullOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ObserveWindowText(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures text observation rejects invalid max lengths.
    /// </summary>
    public void DesktopAutomationService_ObserveWindowText_InvalidMaxLength_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ObserveWindowText(
            new WindowQueryOptions { TitlePattern = "__never__" },
            observationOptions: new DesktopTextObservationOptions {
                MaxObservedTextLength = 0
            }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-based window lookup rejects invalid handles.
    /// </summary>
    public void DesktopAutomationService_GetWindow_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetWindow(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-based control lookup rejects invalid window handles.
    /// </summary>
    public void DesktopAutomationService_GetControl_ZeroWindowHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetControl(IntPtr.Zero, new IntPtr(1)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-based control lookup rejects invalid control handles.
    /// </summary>
    public void DesktopAutomationService_GetControl_ZeroControlHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetControl(new IntPtr(1), IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures focused-control observation rejects invalid window handles.
    /// </summary>
    public void DesktopAutomationService_GetFocusedControlObservation_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetFocusedControlObservation(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-based text observation rejects invalid handles.
    /// </summary>
    public void DesktopAutomationService_ObserveWindowText_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.ObserveWindowText(IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures close waits reject a missing selector.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowToClose_NullOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.WaitForWindowToClose(null!, 1000, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures focus-loss waits reject invalid polling intervals.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowToLoseFocus_ZeroInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.WaitForWindowToLoseFocus(new WindowQueryOptions {
            TitlePattern = "__never__"
        }, 1000, 0));
    }

    [TestMethod]
    /// <summary>
    /// Ensures observed-text waits reject missing expected text.
    /// </summary>
    public void DesktopAutomationService_WaitForObservedText_EmptyExpectedText_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.WaitForObservedText(new WindowQueryOptions {
            TitlePattern = "__never__"
        }, string.Empty, 1000, 10));
    }

    [TestMethod]
    /// <summary>
    /// Ensures observed-text waits reject invalid polling intervals.
    /// </summary>
    public void DesktopAutomationService_WaitForObservedText_ZeroInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.WaitForObservedText(new WindowQueryOptions {
            TitlePattern = "__never__"
        }, "sample", 1000, 0));
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-based observed-text waits reject invalid window handles.
    /// </summary>
    public void DesktopAutomationService_WaitForObservedText_ZeroHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.WaitForObservedText(IntPtr.Zero, "sample", 1000, 10));
    }

    [TestMethod]
    /// <summary>
    /// Ensures close waits complete immediately when nothing matches.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowToClose_MissingWindow_CompletesImmediately() {
        var automation = new DesktopAutomationService();

        DesktopWaitResult result = automation.WaitForWindowToClose(new WindowQueryOptions {
            TitlePattern = "__DesktopManager_NoSuchWindow__"
        }, 1000, 10);

        Assert.IsTrue(result.ElapsedMilliseconds >= 0);
    }

    [TestMethod]
    /// <summary>
    /// Ensures focus-loss waits complete immediately when nothing matches.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowToLoseFocus_MissingWindow_CompletesImmediately() {
        var automation = new DesktopAutomationService();

        DesktopWaitResult result = automation.WaitForWindowToLoseFocus(new WindowQueryOptions {
            TitlePattern = "__DesktopManager_NoSuchWindow__"
        }, 1000, 10);

        Assert.IsTrue(result.ElapsedMilliseconds >= 0);
    }

    [TestMethod]
    /// <summary>
    /// Ensures text observations truncate long values and report expected-text matches.
    /// </summary>
    public void DesktopAutomationService_CreateTextObservation_TruncatesAndTracksExpected() {
        DesktopWindowTextObservation observation = DesktopAutomationService.CreateTextObservation(
            new WindowInfo {
                Handle = new IntPtr(100),
                Title = "Sample"
            },
            new IntPtr(200),
            "Edit",
            "Editor",
            "Edit",
            "abcdef",
            "control.value",
            "cde",
            4);

        Assert.AreEqual("abcd", observation.Value);
        Assert.AreEqual(true, observation.ContainsExpected);
        Assert.IsTrue(observation.IsTruncated);
        Assert.AreEqual(new IntPtr(100), observation.WindowHandle);
        Assert.AreEqual(new IntPtr(200), observation.ControlHandle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures editable-text scoring prefers classic edit surfaces over plain labels.
    /// </summary>
    public void DesktopAutomationService_GetEditableTextCandidateScore_PrefersEditableControls() {
        int richEditScore = DesktopAutomationService.GetEditableTextCandidateScore(new WindowControlInfo {
            ClassName = "RichEditD2DPT",
            ControlType = "Edit",
            SupportsBackgroundText = true,
            IsKeyboardFocusable = true,
            IsEnabled = true
        });
        int labelScore = DesktopAutomationService.GetEditableTextCandidateScore(new WindowControlInfo {
            ClassName = "Static",
            ControlType = "Text"
        });

        Assert.IsTrue(richEditScore > labelScore);
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-based window lookup resolves the live harness window.
    /// </summary>
    public void DesktopAutomationService_GetWindow_ReturnsHarnessWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Handle Lookup Harness");

        WindowInfo? window = new DesktopAutomationService().GetWindow(harness.Window.Handle);

        Assert.IsNotNull(window);
        Assert.AreEqual(harness.Window.Handle, window.Handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures text observation can resolve textbox content from a live harness window.
    /// </summary>
    public void DesktopAutomationService_ObserveWindowText_ReturnsTextboxText() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Observation Harness",
            form => {
                TextBox textBox = new() {
                    Name = "EditorBox",
                    Multiline = true,
                    Width = 220,
                    Height = 120,
                    Left = 12,
                    Top = 12,
                    Text = "DesktopManager observation sample"
                };
                form.Controls.Add(textBox);
                form.Shown += (_, _) => {
                    textBox.Focus();
                };
            });

        new WindowManager().ActivateWindow(harness.Window);
        Application.DoEvents();
        Task.Delay(150).Wait();

        DesktopWindowTextObservation? observation = new DesktopAutomationService().ObserveWindowText(new WindowQueryOptions {
            Handle = harness.Window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, expectedText: "observation sample", observationOptions: new DesktopTextObservationOptions {
            RetryCount = 10,
            RetryDelayMilliseconds = 100
        });

        Assert.IsNotNull(observation);
        Assert.AreEqual(true, observation.ContainsExpected);
        StringAssert.Contains(observation.Value, "DesktopManager");
    }

    [TestMethod]
    /// <summary>
    /// Ensures observed-text waits complete when the target textbox content changes.
    /// </summary>
    public void DesktopAutomationService_WaitForObservedText_CompletesAfterTextboxUpdates() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Text Wait Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Multiline = true,
                    Width = 220,
                    Height = 120,
                    Left = 12,
                    Top = 12,
                    Text = "initial"
                };
                form.Controls.Add(editor);
                form.Shown += (_, _) => {
                    editor.Focus();
                };
            });

        Assert.IsNotNull(editor);
        new WindowManager().ActivateWindow(harness.Window);
        Application.DoEvents();

        Task.Run(async () => {
            await Task.Delay(250).ConfigureAwait(false);
            harness.Form.BeginInvoke(new Action(() => {
                editor!.Text = "DesktopManager text wait complete";
                editor.Focus();
            }));
        });

        DesktopWindowTextObservation observation = new DesktopAutomationService().WaitForObservedText(
            harness.Window.Handle,
            "text wait complete",
            3000,
            50,
            new DesktopTextObservationOptions {
                RetryCount = 2,
                RetryDelayMilliseconds = 25
            });

        Assert.AreEqual(true, observation.ContainsExpected);
        StringAssert.Contains(observation.Value, "DesktopManager");
    }

    [TestMethod]
    /// <summary>
    /// Ensures close waits complete after a live harness window is dismissed.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowToClose_CompletesAfterWindowCloses() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Close Wait Harness");
        Task<DesktopWaitResult> waitTask = Task.Run(() => new DesktopAutomationService().WaitForWindowToClose(new WindowQueryOptions {
            Handle = harness.Window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, 3000, 50));

        Task.Delay(250).Wait();
        harness.Form.Close();
        Application.DoEvents();

        DesktopWaitResult result = waitTask.GetAwaiter().GetResult();

        Assert.IsTrue(result.ElapsedMilliseconds >= 0);
    }

    [TestMethod]
    /// <summary>
    /// Ensures focus-loss waits complete after another window becomes active.
    /// </summary>
    public void DesktopAutomationService_WaitForWindowToLoseFocus_CompletesAfterActivationChanges() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();

        using WinFormsWindowHarness source = WinFormsWindowHarness.Create("DesktopManager Focus Source Harness");
        using WinFormsWindowHarness target = WinFormsWindowHarness.Create("DesktopManager Focus Target Harness");
        WindowManager manager = new();
        manager.ActivateWindow(source.Window);
        Application.DoEvents();
        Task.Delay(150).Wait();

        Task.Run(async () => {
            await Task.Delay(250).ConfigureAwait(false);
            target.Form.BeginInvoke(new Action(() => {
                manager.ActivateWindow(target.Window);
            }));
        });

        DesktopWaitResult result = new DesktopAutomationService().WaitForWindowToLoseFocus(new WindowQueryOptions {
            Handle = source.Window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, 3000, 50);

        Assert.IsTrue(result.ElapsedMilliseconds >= 0);
    }
}
