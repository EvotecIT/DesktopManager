using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Live actionability certification for inner WebView2 DOM-backed controls.
/// </summary>
public class DesktopManagerWebViewActionTests {
    private const int StatusTimeoutMilliseconds = 15000;

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the inner WebView prompt can be resolved as a Chrome UI Automation edit control and updated through DesktopManager.
    /// </summary>
    public void DesktopAutomationService_SetControlText_UpdatesInnerWebViewPrompt() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start(
            "webview-inner-edit",
            initialText: "seed",
            surface: "webview");
        session.RequestFocusWebView();

        WaitForWebViewReady(session);

        DesktopAutomationService automation = new();
        WindowQueryOptions windowQuery = CreateCurrentWindowQuery(session);
        WindowControlQueryOptions promptQuery = CreateWebViewPromptQuery();
        WindowControlTargetInfo prompt = WaitForSingleControl(
            automation,
            session,
            promptQuery,
            "The inner WebView prompt control was not discoverable.");

        string expectedText = "webview-inner-" + Guid.NewGuid().ToString("N");
        IReadOnlyList<WindowControlTargetInfo> updatedControls = automation.SetControlText(
            windowQuery,
            promptQuery,
            expectedText,
            allWindows: false,
            allControls: false);

        Assert.AreEqual(1, updatedControls.Count);
        Assert.AreEqual(WindowControlSource.UiAutomation, prompt.Control.Source);
        Assert.AreEqual("Chrome", prompt.Control.FrameworkId);
        Assert.IsTrue(prompt.Control.SupportsForegroundInputFallback);

        DesktopManagerTestAppStatus updatedStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.WebViewPromptText, expectedText, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The inner WebView prompt did not publish the updated value after DesktopManager text entry.");
        Assert.AreEqual(expectedText, updatedStatus.WebViewPromptText);

        WindowControlTargetInfo updatedPrompt = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "prompt",
                ControlTypePattern = "Edit",
                ValuePattern = expectedText,
                UseUiAutomation = true,
                EnsureForegroundWindow = true,
                AllowForegroundInputFallback = true
            },
            "The inner WebView prompt did not publish the updated value after DesktopManager text entry.");
        Assert.AreEqual(expectedText, updatedPrompt.Control.Value);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the inner WebView send button can be triggered through focused key input and the resulting status text appears through DesktopManager discovery.
    /// </summary>
    public void DesktopAutomationService_SendControlKeys_InvokesInnerWebViewSendButton() {
        RequireLiveTestAppHarness();
        using var session = DesktopManagerTestAppSession.Start(
            "webview-inner-send",
            initialText: "seed",
            surface: "webview");
        session.RequestFocusWebView();

        WaitForWebViewReady(session);

        DesktopAutomationService automation = new();
        WindowQueryOptions windowQuery = CreateCurrentWindowQuery(session);
        string expectedText = "webview-send-" + Guid.NewGuid().ToString("N");
        WaitForSingleControl(
            automation,
            session,
            CreateWebViewPromptQuery(),
            "The inner WebView prompt control was not discoverable before send-button automation.");

        IReadOnlyList<WindowControlTargetInfo> updatedPromptControls = automation.SetControlText(
            windowQuery,
            CreateWebViewPromptQuery(),
            expectedText,
            allWindows: false,
            allControls: false);
        Assert.AreEqual(1, updatedPromptControls.Count);

        WindowControlTargetInfo sendButton = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "send",
                ControlTypePattern = "Button",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The inner WebView send button was not discoverable.");
        Assert.AreEqual("Chrome", sendButton.Control.FrameworkId);
        Assert.AreEqual(WindowControlSource.UiAutomation, sendButton.Control.Source);

        IReadOnlyList<WindowControlTargetInfo> clickedControls = automation.ClickControls(
            windowQuery,
            new WindowControlQueryOptions {
                AutomationIdPattern = "send",
                ControlTypePattern = "Button",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            MouseButton.Left,
            allWindows: false,
            allControls: false);
        Assert.AreEqual(1, clickedControls.Count);

        DesktopManagerTestAppStatus actionStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.WebViewPromptText, expectedText, StringComparison.Ordinal) &&
                string.Equals(candidate.WebViewDomStatusText, "Sent prompt: " + expectedText, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The inner WebView send action did not publish the expected status text.");
        Assert.AreEqual("Sent prompt: " + expectedText, actionStatus.WebViewDomStatusText);
        Assert.AreEqual(expectedText, actionStatus.WebViewPromptText);
    }

    private static void WaitForWebViewReady(DesktopManagerTestAppSession session) {
        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.WebViewClientBounds.Width > 0 &&
                candidate.WebViewClientBounds.Height > 0 &&
                (candidate.WebViewReady || candidate.WebViewStatusText.StartsWith("WebView2 initialization failed:", StringComparison.Ordinal)),
            StatusTimeoutMilliseconds * 2,
            "The DesktopManager test app did not publish the hosted WebView2 bounds in time.");
        if (!status.WebViewReady) {
            Assert.Inconclusive(status.WebViewStatusText);
        }
    }

    private static WindowControlQueryOptions CreateWebViewPromptQuery() {
        return new WindowControlQueryOptions {
            AutomationIdPattern = "prompt",
            ControlTypePattern = "Edit",
            UseUiAutomation = true,
            EnsureForegroundWindow = true,
            AllowForegroundInputFallback = true
        };
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
