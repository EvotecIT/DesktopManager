#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI target result mapping helpers.
/// </summary>
public class TargetAndScreenshotMappingTests {
    [TestMethod]
    /// <summary>
    /// Ensures resolved window target mapping preserves geometry, area, and target metadata.
    /// </summary>
    public void MapResolvedWindowTarget_MapsGeometryAndScreenArea() {
        var resolvedTarget = new DesktopResolvedWindowTarget {
            Name = "EditorCenter",
            Definition = new DesktopWindowTargetDefinition {
                Description = "Editor center",
                XRatio = 0.5,
                YRatio = 0.5,
                WidthRatio = 0.4,
                HeightRatio = 0.3,
                ClientArea = true
            },
            Geometry = new DesktopWindowGeometry {
                Window = new WindowInfo {
                    Title = "DesktopManager Test App",
                    Handle = new IntPtr(0x1234),
                    ProcessId = 321,
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
                },
                WindowLeft = 10,
                WindowTop = 20,
                WindowWidth = 800,
                WindowHeight = 600,
                ClientLeft = 18,
                ClientTop = 52,
                ClientWidth = 784,
                ClientHeight = 560,
                ClientOffsetLeft = 8,
                ClientOffsetTop = 32
            },
            RelativeX = 200,
            RelativeY = 150,
            RelativeWidth = 320,
            RelativeHeight = 180,
            ScreenX = 210,
            ScreenY = 170,
            ScreenWidth = 320,
            ScreenHeight = 180
        };

        global::DesktopManager.Cli.ResolvedWindowTargetResult result = global::DesktopManager.Cli.DesktopOperations.MapResolvedWindowTarget(resolvedTarget);

        Assert.AreEqual("EditorCenter", result.Name);
        Assert.AreEqual("Editor center", result.Target.Description);
        Assert.AreEqual(0.5d, result.Target.XRatio);
        Assert.AreEqual(0.4d, result.Target.WidthRatio);
        Assert.IsTrue(result.Target.ClientArea);
        Assert.AreEqual("DesktopManager Test App", result.Window.Title);
        Assert.AreEqual("0x1234", result.Window.Handle);
        Assert.AreEqual(800, result.Geometry.WindowWidth);
        Assert.AreEqual(560, result.Geometry.ClientHeight);
        Assert.AreEqual(200, result.RelativeX);
        Assert.AreEqual(150, result.RelativeY);
        Assert.AreEqual(320, result.RelativeWidth);
        Assert.AreEqual(180, result.RelativeHeight);
        Assert.AreEqual(210, result.ScreenX);
        Assert.AreEqual(170, result.ScreenY);
        Assert.AreEqual(320, result.ScreenWidth);
        Assert.AreEqual(180, result.ScreenHeight);
    }

    [TestMethod]
    /// <summary>
    /// Ensures resolved control target mapping preserves target definition, parent window, and control metadata.
    /// </summary>
    public void MapResolvedControlTarget_MapsTargetWindowAndControl() {
        var resolvedTarget = new DesktopResolvedControlTarget {
            Name = "CommandBar",
            Definition = new DesktopControlTargetDefinition {
                Description = "Command bar",
                AutomationIdPattern = "CommandBarTextBox",
                ControlTypePattern = "Edit",
                SupportsForegroundInputFallback = true,
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            Window = new WindowInfo {
                Title = "DesktopManager Command Surface",
                Handle = new IntPtr(0x2222),
                ProcessId = 444,
                ThreadId = 555,
                Left = 100,
                Top = 200,
                Right = 900,
                Bottom = 700,
                MonitorIndex = 2,
                MonitorDeviceName = @"\\.\\DISPLAY2"
            },
            Control = new WindowControlInfo {
                Handle = IntPtr.Zero,
                ClassName = "HwndWrapper[DesktopManager.TestApp;;123456]",
                Id = 0,
                Source = WindowControlSource.UiAutomation,
                AutomationId = "CommandBarTextBox",
                ControlType = "Edit",
                FrameworkId = "WPF",
                Text = "DesktopManager",
                Value = "DesktopManager",
                SupportsForegroundInputFallback = true,
                IsKeyboardFocusable = true,
                IsEnabled = true,
                IsPassword = false
            }
        };

        global::DesktopManager.Cli.ResolvedControlTargetResult result = global::DesktopManager.Cli.DesktopOperations.MapResolvedControlTarget(resolvedTarget);

        Assert.AreEqual("CommandBar", result.Name);
        Assert.AreEqual("Command bar", result.Target.Description);
        Assert.AreEqual("CommandBarTextBox", result.Target.AutomationIdPattern);
        Assert.AreEqual("Edit", result.Target.ControlTypePattern);
        Assert.AreEqual(true, result.Target.SupportsForegroundInputFallback);
        Assert.IsTrue(result.Target.UseUiAutomation);
        Assert.IsTrue(result.Target.EnsureForegroundWindow);
        Assert.AreEqual("DesktopManager Command Surface", result.Window.Title);
        Assert.AreEqual("0x2222", result.Window.Handle);
        Assert.AreEqual("HwndWrapper[DesktopManager.TestApp;;123456]", result.Control.ClassName);
        Assert.AreEqual("0x0", result.Control.Handle);
        Assert.AreEqual("UiAutomation", result.Control.Source);
        Assert.AreEqual("CommandBarTextBox", result.Control.AutomationId);
        Assert.AreEqual("Edit", result.Control.ControlType);
        Assert.AreEqual("WPF", result.Control.FrameworkId);
        Assert.AreEqual("DesktopManager", result.Control.Value);
        Assert.AreEqual("DesktopManager Command Surface", result.Control.ParentWindow.Title);
    }

    [TestMethod]
    public void MapResolvedControlTarget_UnknownPasswordState_SuppressesEveryTextChannel() {
        var resolvedTarget = new DesktopResolvedControlTarget {
            Definition = new DesktopControlTargetDefinition(),
            Window = new WindowInfo { Handle = new IntPtr(0x2222) },
            Control = new WindowControlInfo {
                Handle = IntPtr.Zero,
                Text = "must-not-map-text",
                Value = "must-not-map-value",
                IsPassword = null
            }
        };

        global::DesktopManager.Cli.ResolvedControlTargetResult result = global::DesktopManager.Cli.DesktopOperations.MapResolvedControlTarget(resolvedTarget);

        Assert.AreEqual(string.Empty, result.Control.Text);
        Assert.AreEqual(string.Empty, result.Control.Value);
        Assert.IsNull(result.Control.IsPassword);
    }

    [TestMethod]
    public void MapResolvedControlTarget_KnownSafeWhitespaceValue_PreservesWhitespace() {
        var resolvedTarget = new DesktopResolvedControlTarget {
            Definition = new DesktopControlTargetDefinition(),
            Window = new WindowInfo { Handle = new IntPtr(0x2222) },
            Control = new WindowControlInfo {
                Handle = IntPtr.Zero,
                Text = "  ",
                Value = "\t",
                IsPassword = false
            }
        };

        global::DesktopManager.Cli.ResolvedControlTargetResult result = global::DesktopManager.Cli.DesktopOperations.MapResolvedControlTarget(resolvedTarget);

        Assert.AreEqual("  ", result.Control.Text);
        Assert.AreEqual("\t", result.Control.Value);
    }
}
#endif
