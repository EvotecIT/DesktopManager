#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control query mapping and diagnostic result shaping.
/// </summary>
public class ControlQueryAndDiagnosticsMappingTests {
    [TestMethod]
    /// <summary>
    /// Ensures CLI control selection criteria preserve foreground and UI Automation routing flags.
    /// </summary>
    public void CreateControlQuery_MapsForegroundAndUiAutomationFlags() {
        var criteria = new global::DesktopManager.Cli.ControlSelectionCriteria {
            ClassNamePattern = "WindowsForms10.*",
            TextPattern = "Search*",
            ValuePattern = "DesktopManager*",
            Id = 42,
            Handle = "0x2A",
            AutomationIdPattern = "EditorTextBox",
            ControlTypePattern = "Edit",
            FrameworkIdPattern = "WPF",
            IsEnabled = true,
            IsKeyboardFocusable = true,
            SupportsBackgroundClick = true,
            SupportsBackgroundText = false,
            SupportsBackgroundKeys = true,
            SupportsForegroundInputFallback = true,
            EnsureForegroundWindow = true,
            AllowForegroundInputFallback = true,
            UiAutomation = true,
            IncludeUiAutomation = true
        };

        WindowControlQueryOptions query = global::DesktopManager.Cli.DesktopOperations.CreateControlQuery(criteria);

        Assert.AreEqual("WindowsForms10.*", query.ClassNamePattern);
        Assert.AreEqual("Search*", query.TextPattern);
        Assert.AreEqual("DesktopManager*", query.ValuePattern);
        Assert.AreEqual(42, query.Id);
        Assert.AreEqual(new IntPtr(0x2A), query.Handle);
        Assert.AreEqual("EditorTextBox", query.AutomationIdPattern);
        Assert.AreEqual("Edit", query.ControlTypePattern);
        Assert.AreEqual("WPF", query.FrameworkIdPattern);
        Assert.AreEqual(true, query.IsEnabled);
        Assert.AreEqual(true, query.IsKeyboardFocusable);
        Assert.AreEqual(true, query.SupportsBackgroundClick);
        Assert.AreEqual(false, query.SupportsBackgroundText);
        Assert.AreEqual(true, query.SupportsBackgroundKeys);
        Assert.AreEqual(true, query.SupportsForegroundInputFallback);
        Assert.IsTrue(query.EnsureForegroundWindow);
        Assert.IsTrue(query.AllowForegroundInputFallback);
        Assert.IsTrue(query.UseUiAutomation);
        Assert.IsTrue(query.IncludeUiAutomation);
    }

    [TestMethod]
    /// <summary>
    /// Ensures CLI diagnostic mapping normalizes handles and preserves root, sample, and action-probe metadata.
    /// </summary>
    public void MapControlDiagnostics_NormalizesHandlesAndMapsSamples() {
        var window = new WindowInfo {
            Title = "DesktopManager Test App",
            Handle = new IntPtr(0x1234),
            ProcessId = 321,
            ThreadId = 654,
            IsVisible = true,
            IsTopMost = false,
            Left = 10,
            Top = 20,
            Right = 810,
            Bottom = 620,
            MonitorIndex = 1,
            MonitorDeviceName = @"\\.\\DISPLAY1"
        };
        var diagnostics = new DesktopControlDiscoveryDiagnostics {
            Window = window,
            RequiresUiAutomation = true,
            UseUiAutomation = true,
            IncludeUiAutomation = true,
            EnsureForegroundWindow = false,
            UiAutomationAvailable = true,
            ElapsedMilliseconds = 87,
            PreparationAttempted = true,
            PreparationSucceeded = true,
            UiAutomationFallbackRootCount = 2,
            UsedUiAutomationFallbackRoots = true,
            UsedCachedUiAutomationControls = false,
            UsedPreferredUiAutomationRoot = true,
            PreferredUiAutomationRootHandle = new IntPtr(0xABCDEF),
            EffectiveSource = "uia",
            Win32ControlCount = 1,
            UiAutomationControlCount = 4,
            EffectiveControlCount = 4,
            MatchedControlCount = 2,
            SampleControls = new[] {
                new WindowControlInfo {
                    Source = WindowControlSource.UiAutomation,
                    Handle = IntPtr.Zero,
                    AutomationId = "EditorTextBox",
                    ControlType = "Edit",
                    Text = "DesktopManager",
                    Value = "DesktopManager",
                    SupportsForegroundInputFallback = true,
                    IsPassword = false
                }
            },
            UiAutomationRoots = new[] {
                new DesktopUiAutomationRootDiagnostic {
                    Order = 0,
                    Handle = new IntPtr(0xABCDEF),
                    ClassName = "Chrome_RenderWidgetHostHWND",
                    IsPrimaryRoot = true,
                    IsPreferredRoot = true,
                    UsedCachedControls = false,
                    IncludeRoot = true,
                    ElementResolved = true,
                    ControlCount = 4,
                    SampleControls = new[] {
                        new WindowControlInfo {
                            Source = WindowControlSource.UiAutomation,
                            Handle = IntPtr.Zero,
                            AutomationId = "EditorTextBox",
                            ControlType = "Edit",
                            Text = "DesktopManager",
                            IsPassword = false
                        }
                    }
                }
            },
            UiAutomationActionProbe = new DesktopUiAutomationActionDiagnostic {
                Attempted = true,
                Resolved = true,
                UsedCachedActionMatch = false,
                UsedPreferredRoot = true,
                RootHandle = new IntPtr(0xABCDEF),
                Score = 92,
                SearchMode = "preferred-root",
                ElapsedMilliseconds = 12
            }
        };

        global::DesktopManager.Cli.ControlDiagnosticResult result = global::DesktopManager.Cli.DesktopOperations.MapControlDiagnostics(diagnostics);

        Assert.AreEqual("DesktopManager Test App", result.Window.Title);
        Assert.AreEqual("0x1234", result.Window.Handle);
        Assert.AreEqual("uia", result.EffectiveSource);
        Assert.AreEqual("0xABCDEF", result.PreferredUiAutomationRootHandle);
        Assert.AreEqual(1, result.SampleControls.Count);
        Assert.AreEqual("EditorTextBox", result.SampleControls[0].AutomationId);
        Assert.AreEqual("Edit", result.SampleControls[0].ControlType);
        Assert.AreEqual("DesktopManager", result.SampleControls[0].Text);
        Assert.AreEqual("DesktopManager", result.SampleControls[0].Value);
        Assert.AreEqual("0x0", result.SampleControls[0].Handle);
        Assert.AreEqual("0x1234", result.SampleControls[0].ParentWindow.Handle);
        Assert.AreEqual(1, result.UiAutomationRoots.Count);
        Assert.AreEqual("0xABCDEF", result.UiAutomationRoots[0].Handle);
        Assert.AreEqual("Chrome_RenderWidgetHostHWND", result.UiAutomationRoots[0].ClassName);
        Assert.AreEqual(1, result.UiAutomationRoots[0].SampleControls.Count);
        Assert.AreEqual("EditorTextBox", result.UiAutomationRoots[0].SampleControls[0].AutomationId);
        Assert.IsNotNull(result.UiAutomationActionProbe);
        Assert.AreEqual("0xABCDEF", result.UiAutomationActionProbe.RootHandle);
        Assert.AreEqual("preferred-root", result.UiAutomationActionProbe.SearchMode);
        Assert.AreEqual(92, result.UiAutomationActionProbe.Score);
    }
}
#endif
