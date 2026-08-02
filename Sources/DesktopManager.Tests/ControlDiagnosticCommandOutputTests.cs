#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control diagnostic text output.
/// </summary>
public class ControlDiagnosticCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures the empty diagnostics message stays explicit.
    /// </summary>
    public void WriteNoMatchingDiagnosticWindows_WritesExpectedMessage() {
        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteNoMatchingDiagnosticWindows(writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "No matching windows found for control diagnostics.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control diagnostic text output includes action-probe, root, and sample control details.
    /// </summary>
    public void WriteDiagnosticResults_WritesRichDiagnosticSummary() {
        var diagnostics = new[] {
            new global::DesktopManager.Cli.ControlDiagnosticResult {
                Window = new global::DesktopManager.Cli.WindowResult {
                    Title = "DesktopManager Test App",
                    Handle = "0x00123456"
                },
                EffectiveSource = "uia",
                ElapsedMilliseconds = 87,
                RequiresUiAutomation = true,
                UiAutomationAvailable = true,
                UseUiAutomation = true,
                IncludeUiAutomation = true,
                PreparationAttempted = true,
                PreparationSucceeded = true,
                EnsureForegroundWindow = false,
                UiAutomationFallbackRootCount = 2,
                UsedUiAutomationFallbackRoots = true,
                UsedCachedUiAutomationControls = false,
                PreferredUiAutomationRootHandle = "0x00ABCDEF",
                UsedPreferredUiAutomationRoot = true,
                Win32ControlCount = 1,
                UiAutomationControlCount = 4,
                EffectiveControlCount = 4,
                MatchedControlCount = 2,
                UiAutomationActionProbe = new global::DesktopManager.Cli.UiAutomationActionDiagnosticResult {
                    Attempted = true,
                    TimedOut = false,
                    Resolved = true,
                    UsedCachedActionMatch = false,
                    UsedPreferredRoot = true,
                    RootHandle = "0x00ABCDEF",
                    Score = 92,
                    SearchMode = "preferred-root",
                    ElapsedMilliseconds = 12
                },
                UiAutomationRoots = new[] {
                    new global::DesktopManager.Cli.UiAutomationRootDiagnosticResult {
                        Order = 0,
                        Handle = "0x00ABCDEF",
                        ClassName = "Chrome_RenderWidgetHostHWND",
                        IsPrimaryRoot = true,
                        IsPreferredRoot = true,
                        UsedCachedControls = false,
                        IncludeRoot = true,
                        ElementResolved = true,
                        ControlCount = 4,
                        Error = null,
                        SampleControls = new[] {
                            new global::DesktopManager.Cli.ControlResult {
                                Source = "uia",
                                ControlType = "Edit",
                                AutomationId = "EditorTextBox",
                                Text = "DesktopManager"
                            }
                        }
                    }
                },
                SampleControls = new[] {
                    new global::DesktopManager.Cli.ControlResult {
                        Source = "uia",
                        ControlType = "Edit",
                        AutomationId = "EditorTextBox",
                        Text = "DesktopManager"
                    },
                    new global::DesktopManager.Cli.ControlResult {
                        Source = "win32",
                        ControlType = "Button",
                        AutomationId = string.Empty,
                        Text = "Run"
                    }
                }
            }
        };

        using var writer = new StringWriter();

        global::DesktopManager.Cli.ControlCommands.WriteDiagnosticResults(diagnostics, writer);
        string output = writer.ToString();

        StringAssert.Contains(output, "window: DesktopManager Test App (0x00123456)");
        StringAssert.Contains(output, "effective-source: uia");
        StringAssert.Contains(output, "uia: required=True available=True requested=True include=True");
        StringAssert.Contains(output, "preparation: attempted=True succeeded=True ensureForeground=False");
        StringAssert.Contains(output, "fallback-roots: count=2 used=True cached=False preferred=0x00ABCDEF reused=True");
        StringAssert.Contains(output, "counts: win32=1 uia=4 effective=4 matched=2");
        StringAssert.Contains(output, "action-probe: attempted=True timed-out=False resolved=True cached=False preferred=True root=0x00ABCDEF score=92 mode=preferred-root elapsed-ms=12");
        StringAssert.Contains(output, "root[0]: handle=0x00ABCDEF class=Chrome_RenderWidgetHostHWND primary=True preferred=True cached=False includeRoot=True elementResolved=True count=4 error=");
        StringAssert.Contains(output, "  * uia Edit EditorTextBox DesktopManager");
        StringAssert.Contains(output, "- uia Edit EditorTextBox DesktopManager");
        StringAssert.Contains(output, "- win32 Button  Run");
    }
}
#endif
