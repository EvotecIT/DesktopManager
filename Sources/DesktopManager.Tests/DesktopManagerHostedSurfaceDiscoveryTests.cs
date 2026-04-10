using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Live discovery certification for hosted non-classic surfaces inside the DesktopManager test app.
/// </summary>
public class DesktopManagerHostedSurfaceDiscoveryTests {
    private const int StatusTimeoutMilliseconds = 5000;

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the hosted WPF command-bar surface is discoverable as a real editable UI Automation control.
    /// </summary>
    public void DesktopAutomationService_GetControls_ResolvesHostedWpfCommandBarEditor() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start(
            "hosted-commandbar-discovery",
            initialText: "commandbar-discovery",
            surface: "commandbar");
        session.RequestFocusCommandBar();

        session.WaitForStatus(
            candidate => candidate.CommandBarHostBounds.Width > 0 &&
                candidate.CommandBarHostBounds.Height > 0,
            StatusTimeoutMilliseconds,
            "The DesktopManager test app did not publish the hosted command-bar bounds in time.");

        DesktopAutomationService automation = new();
        WindowControlTargetInfo commandBar = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "CommandBarTextBox",
                ControlTypePattern = "Edit",
                SupportsForegroundInputFallback = true,
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The hosted WPF command bar was not discoverable as an editable UI Automation control.");

        Assert.AreEqual("CommandBarTextBox", commandBar.Control.AutomationId);
        Assert.AreEqual("Edit", commandBar.Control.ControlType);
        Assert.AreEqual("WPF", commandBar.Control.FrameworkId);
        Assert.AreEqual(WindowControlSource.UiAutomation, commandBar.Control.Source);
        Assert.IsTrue(commandBar.Control.SupportsForegroundInputFallback);
        Assert.IsTrue(commandBar.Control.IsKeyboardFocusable ?? false);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the hosted WebView2 surface exposes both the WPF host container and the browser-backed child root.
    /// </summary>
    public void DesktopAutomationService_GetControls_ResolvesHostedWebView2ShellAndBrowserRoot() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start(
            "hosted-webview-discovery",
            initialText: "webview-discovery",
            surface: "webview");
        session.RequestFocusWebView();

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.WebViewHostBounds.Width > 0 &&
                candidate.WebViewClientBounds.Width > 0 &&
                (candidate.WebViewReady || candidate.WebViewStatusText.StartsWith("WebView2 initialization failed:", StringComparison.Ordinal)),
            StatusTimeoutMilliseconds * 2,
            "The DesktopManager test app did not publish the hosted WebView2 bounds in time.");
        if (!status.WebViewReady) {
            Assert.Inconclusive(status.WebViewStatusText);
        }

        DesktopAutomationService automation = new();
        WindowQueryOptions windowQuery = CreateCurrentWindowQuery(session);
        DesktopControlDiscoveryDiagnostics diagnostics = automation.GetControlDiagnostics(
            windowQuery,
            new WindowControlQueryOptions {
                UseUiAutomation = true,
                IncludeUiAutomation = true,
                EnsureForegroundWindow = true
            },
            allWindows: false,
            sampleLimit: 50,
            includeActionProbe: false).Single();

        Assert.AreEqual("Merged", diagnostics.EffectiveSource);
        Assert.IsTrue(diagnostics.UiAutomationControlCount > 0);
        Assert.IsTrue(
            diagnostics.SampleControls.Any(control =>
                control != null &&
                string.Equals(control.AutomationId, "WebViewSurface", StringComparison.Ordinal) &&
                string.Equals(control.FrameworkId, "WPF", StringComparison.Ordinal)),
            "Expected discovery diagnostics to include the hosted WPF WebView surface.");
        Assert.IsTrue(
            diagnostics.SampleControls.Any(control =>
                control != null &&
                !string.IsNullOrWhiteSpace(control.ClassName) &&
                control.ClassName.StartsWith("Chrome_WidgetWin", StringComparison.OrdinalIgnoreCase)),
            "Expected discovery diagnostics to expose the browser-backed child root.");

        WindowControlTargetInfo hostSurface = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "WebViewSurface",
                UseUiAutomation = true,
                IncludeUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The hosted WebView2 WPF surface was not discoverable through DesktopManager.");
        Assert.AreEqual("WebViewSurface", hostSurface.Control.AutomationId);
        Assert.AreEqual("WPF", hostSurface.Control.FrameworkId);
        Assert.AreEqual(WindowControlSource.UiAutomation, hostSurface.Control.Source);

        IReadOnlyList<WindowControlTargetInfo> browserRoots = automation.GetControls(
            windowQuery,
            new WindowControlQueryOptions {
                ClassNamePattern = "Chrome_WidgetWin*",
                UseUiAutomation = true,
                IncludeUiAutomation = true,
                EnsureForegroundWindow = true
            },
            allWindows: false,
            allControls: true);

        Assert.IsTrue(browserRoots.Count >= 1, "Expected discovery to expose at least one browser-backed WebView2 root.");
        WindowControlInfo browserRoot = browserRoots[0].Control;
        Assert.IsTrue(browserRoot.ClassName.StartsWith("Chrome_WidgetWin", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(string.IsNullOrWhiteSpace(browserRoot.ControlType));
    }

    private static WindowControlTargetInfo WaitForSingleControl(DesktopAutomationService automation, DesktopManagerTestAppSession session, WindowControlQueryOptions controlQuery, string failureMessage) {
        session.WaitForStatus(
            _ => {
                IReadOnlyList<WindowControlTargetInfo> controls = automation.GetControls(CreateCurrentWindowQuery(session), controlQuery, allWindows: false, allControls: true);
                return controls.Count == 1;
            },
            StatusTimeoutMilliseconds,
            failureMessage);

        IReadOnlyList<WindowControlTargetInfo> resolvedControls = automation.GetControls(CreateCurrentWindowQuery(session), controlQuery, allWindows: false, allControls: true);
        Assert.AreEqual(1, resolvedControls.Count, failureMessage);
        return resolvedControls.Single();
    }

    private static WindowQueryOptions CreateCurrentWindowQuery(DesktopManagerTestAppSession session) {
        DesktopManagerTestAppStatus status = session.ReadStatus();
        return new WindowQueryOptions {
            Handle = status.WindowHandle == 0 ? session.WindowHandle : new IntPtr(status.WindowHandle),
            ProcessId = status.ProcessId,
            TitlePattern = status.WindowTitle,
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    private static void RequireLiveTestAppHarness() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows.");
        }

        TestHelper.RequireExternalDesktopApplicationTests();
    }
}
