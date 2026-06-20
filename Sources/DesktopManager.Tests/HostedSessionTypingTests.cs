using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Live typing tests for the hosted-session foreground path using the local desktop test app.
/// </summary>
public class HostedSessionTypingTests {
    private const int TextReadTimeoutMilliseconds = 5000;
    private const int FocusTimeoutMilliseconds = 5000;
    private const int FocusRetryCount = 3;
    private const int ForegroundHoldDurationMilliseconds = 4000;

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures hosted-session script typing round-trips a symbol-heavy ASCII payload into the local test app editor.
    /// </summary>
    public void HostedSessionTyping_TestAppEditor_RoundTripsAsciiScriptSymbols() {
        RequireHostedTypingHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        using var session = DesktopManagerTestAppSession.Start("hosted-script");
        var automation = new DesktopAutomationService();
        string expectedText = BuildShortAsciiScriptText();

        ClearEditor(automation, session);
        FocusWindow(automation, session);

        RunHostedTypingWithFocusRetry(
            automation,
            session,
            () => ClearEditor(automation, session),
            () => automation.TypeWindowText(
                session.CreateWindowQuery(),
                expectedText,
                paste: false,
                delayMilliseconds: 25,
                foregroundInput: true,
                physicalKeys: false,
                hostedSession: true,
                script: true,
                scriptChunkLength: 24,
                scriptLineDelayMilliseconds: 60));

        string actualText = WaitForEditorText(session, expectedText);
        Assert.AreEqual(expectedText, actualText);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures hosted-session typing round-trips a broader ASCII symbol matrix that mirrors script punctuation and operator usage.
    /// </summary>
    public void HostedSessionTyping_TestAppEditor_RoundTripsAsciiSymbolMatrix() {
        RequireHostedTypingHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        using var session = DesktopManagerTestAppSession.Start("hosted-symbol-matrix");
        var automation = new DesktopAutomationService();
        string expectedText = BuildAsciiSymbolMatrixText();

        ClearEditor(automation, session);

        RunHostedTypingWithFocusRetry(
            automation,
            session,
            () => ClearEditor(automation, session),
            () => automation.TypeWindowText(
                session.CreateWindowQuery(),
                expectedText,
                paste: false,
                delayMilliseconds: 20,
                foregroundInput: true,
                physicalKeys: false,
                hostedSession: true,
                script: true,
                scriptChunkLength: 18,
                scriptLineDelayMilliseconds: 45));

        string actualText = WaitForEditorText(session, expectedText);
        Assert.AreEqual(expectedText, actualText);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures hosted-session typing aborts instead of continuing into another test-owned window after focus changes.
    /// </summary>
    public void HostedSessionTyping_TestAppEditor_StopsWhenForegroundChanges() {
        RequireHostedTypingHarness();
        TestHelper.RequireExternalDesktopApplicationTests();

        using var session = DesktopManagerTestAppSession.Start("hosted-abort");
        var automation = new DesktopAutomationService();
        string longText = BuildLongProbeText();

        ClearEditor(automation, session);
        FocusWindow(automation, session);

        var focusThread = new Thread(() => {
            Thread.Sleep(400);
            session.RequestFocusSecondary();
        }) {
            IsBackground = true
        };

        focusThread.Start();
        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => automation.TypeWindowText(
            session.CreateWindowQuery(),
            longText,
            paste: false,
            delayMilliseconds: 35,
            foregroundInput: true,
            physicalKeys: false,
            hostedSession: true,
            script: false,
            scriptChunkLength: 120,
            scriptLineDelayMilliseconds: 0));
        focusThread.Join();

        StringAssert.Contains(exception.Message, "Foreground ownership changed while typing");

        DesktopManagerTestAppStatus postStatus = WaitForStatusWithDiagnostics(
            session,
            candidate => candidate.SecondaryWindowHandle != 0 && candidate.SecondaryIsForegroundWindow && string.Equals(candidate.ActiveSurface, "secondary", StringComparison.OrdinalIgnoreCase),
            FocusTimeoutMilliseconds,
            "The interactive session did not report the secondary helper window as the new foreground target.");

        string primaryText = postStatus.EditorText;
        string secondaryText = postStatus.SecondaryText;
        Assert.IsTrue(primaryText.Length > 0, "Expected the primary editor to receive an initial prefix before focus changed.");
        Assert.IsTrue(primaryText.Length < longText.Length, "Expected hosted-session typing to stop before the full payload was delivered.");
        int commonPrefixLength = CountCommonPrefix(longText, primaryText);
        Assert.IsTrue(commonPrefixLength >= Math.Max(1, primaryText.Length - 1), "Expected the partial primary text to remain an exact prefix aside from at most one in-flight boundary character.");
        Assert.AreNotEqual(longText, secondaryText, "Expected the full payload to stop instead of continuing into the secondary helper window.");
        Assert.IsTrue(secondaryText.Length < 4, "Expected focus drift protection to prevent any meaningful suffix from reaching the secondary helper window.");
    }

    private static void RequireHostedTypingHarness() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows.");
        }

#if NET472
        Assert.Inconclusive("Hosted-session live typing tests run only under net8.0-windows to avoid driving the same desktop twice.");
#elif NET10_0
        Assert.Inconclusive("Hosted-session live typing tests run only under net8.0-windows to avoid driving the same desktop twice.");
#endif
    }

    private static void ClearEditor(DesktopAutomationService automation, DesktopManagerTestAppSession session) {
        DesktopManagerTestAppStatus status = WaitForStatusWithDiagnostics(
            session,
            candidate => candidate.EditorHandle != 0,
            TextReadTimeoutMilliseconds,
            "The interactive session did not expose the test app editor handle in time.");

        IReadOnlyList<WindowControlTargetInfo> controls = automation.GetControls(
            session.CreateWindowQuery(),
            new WindowControlQueryOptions {
                Handle = new IntPtr(status.EditorHandle)
            },
            allWindows: false,
            allControls: true);
        WindowControlTargetInfo editor = controls.FirstOrDefault()
            ?? throw new AssertInconclusiveException("The interactive session did not resolve the test app editor control by handle.");
        WindowControlService.SetText(editor.Control, string.Empty);
        WaitForStatusWithDiagnostics(
            session,
            candidate => string.Equals(candidate.EditorText, string.Empty, StringComparison.Ordinal),
            TextReadTimeoutMilliseconds,
            "The interactive session did not confirm the test app editor was cleared.");
    }

    private static void FocusWindow(DesktopAutomationService automation, DesktopManagerTestAppSession session) {
        session.RequestFocusEditor();
        try {
            automation.FocusWindows(session.CreateWindowQuery());
        } catch (InvalidOperationException) {
            // The test app will keep retrying focus from inside its own UI thread.
        }

        WaitForStatusWithDiagnostics(
            session,
            candidate => candidate.IsForegroundWindow && string.Equals(candidate.ActiveSurface, "editor", StringComparison.OrdinalIgnoreCase),
            FocusTimeoutMilliseconds,
            "The interactive session did not allow the test app editor surface to become the foreground target.");
    }

    private static string WaitForEditorText(DesktopManagerTestAppSession session, string expectedText) {
        DesktopManagerTestAppStatus status = WaitForStatusWithDiagnostics(
            session,
            candidate => string.Equals(expectedText, candidate.EditorText, StringComparison.Ordinal),
            TextReadTimeoutMilliseconds,
            "The interactive session did not report the expected test app editor text in time.");
        return status.EditorText;
    }

    private static string BuildLongProbeText() {
        return string.Concat(
            "DesktopManager-hosted-session-",
            "Aa1[]{}|\\\\/-_=+!?",
            "Bb2[]{}|\\\\/-_=+!?",
            "Cc3[]{}|\\\\/-_=+!?",
            "Dd4[]{}|\\\\/-_=+!?",
            "Ee5[]{}|\\\\/-_=+!?",
            "Ff6[]{}|\\\\/-_=+!?",
            "Gg7[]{}|\\\\/-_=+!?",
            "Hh8[]{}|\\\\/-_=+!?",
            "Ii9[]{}|\\\\/-_=+!?",
            "Jj0[]{}|\\\\/-_=+!?");
    }

    private static string BuildShortAsciiScriptText() {
        return
            "function Test-HostedSession {" + Environment.NewLine +
            "    $symbols = \"[]{}()<>|\\\\@#%&!?+-=*/;:,.\\\"~^_\"" + Environment.NewLine +
            "}" + Environment.NewLine;
    }

    private static string BuildAsciiSymbolMatrixText() {
        return string.Join(
            Environment.NewLine,
            new[] {
                "[]{}()<>",
                "\\\\ | @ # % &",
                "! ? + - = * /",
                "; : , . ' \"",
                "~ ^ _ `",
                "path\\\\to\\\\script.ps1",
                "if ($value -eq \"x\") { $items[0] += 1 }"
            }) + Environment.NewLine;
    }

    private static void RunHostedTypingWithFocusRetry(DesktopAutomationService automation, DesktopManagerTestAppSession session, Action prepareAttempt, Action action) {
        string lastExternalForegroundFingerprint = string.Empty;
        int repeatedExternalForegroundCount = 0;
        int repeatedExternalForegroundAbortThreshold = FocusRetryCount;
        string externalForegroundPolicyReport = "category='none', abortThreshold=" + FocusRetryCount + ", fingerprint=''";
        var retryReports = new List<HostedSessionExternalForegroundReport>();

        for (int attempt = 1; attempt <= FocusRetryCount; attempt++) {
            try {
                prepareAttempt();
                FocusWindow(automation, session);
                session.RequestHoldEditorForeground(ForegroundHoldDurationMilliseconds);
                WaitForStatusWithDiagnostics(
                    session,
                    candidate => candidate.ForegroundHoldActive && string.Equals(candidate.ForegroundHoldSurface, "editor", StringComparison.OrdinalIgnoreCase),
                    FocusTimeoutMilliseconds,
                    "The interactive session did not enable the temporary editor foreground hold in time.");
                action();
                return;
            } catch (InvalidOperationException exception) when (exception.Message.Contains("Foreground ownership changed while typing", StringComparison.Ordinal)) {
                string externalForegroundFingerprint = string.Empty;
                string externalForegroundSummary = string.Empty;
                string currentPolicyReport = string.Empty;
                HostedSessionExternalForegroundReport currentReport = HostedSessionExternalForegroundReport.None;
                TryReadExternalForegroundDiagnostics(
                    session,
                    out externalForegroundFingerprint,
                    out externalForegroundSummary,
                    out repeatedExternalForegroundAbortThreshold,
                    out currentPolicyReport,
                    out currentReport);
                externalForegroundPolicyReport = currentPolicyReport;
                if (currentReport.HasExternalForeground) {
                    retryReports.Add(currentReport);
                }
                if (!string.IsNullOrWhiteSpace(externalForegroundFingerprint)) {
                    if (string.Equals(lastExternalForegroundFingerprint, externalForegroundFingerprint, StringComparison.Ordinal)) {
                        repeatedExternalForegroundCount++;
                    } else {
                        lastExternalForegroundFingerprint = externalForegroundFingerprint;
                        repeatedExternalForegroundCount = 1;
                    }
                } else {
                    lastExternalForegroundFingerprint = string.Empty;
                    repeatedExternalForegroundCount = 0;
                }

                if (repeatedExternalForegroundCount >= repeatedExternalForegroundAbortThreshold) {
                    HostedSessionRetryHistoryReport retryHistoryReport = HostedSessionDiagnosticFormatter.CreateRetryHistoryReport(retryReports);
                    EmitStatusSnapshotToTestOutput(
                        session,
                        "Hosted-session typing stopped early after repeated external foreground interruptions",
                        retryHistoryReport,
                        externalForegroundPolicyReport);
                    Assert.Inconclusive(
                        "Repeated external foreground culprit observed across hosted-session retries (threshold " +
                        repeatedExternalForegroundAbortThreshold +
                        "): " +
                        externalForegroundSummary +
                        Environment.NewLine +
                        "RetryHistory: " +
                        retryHistoryReport.Summary +
                        Environment.NewLine +
                        "RetryHistoryReport: " +
                        HostedSessionDiagnosticFormatter.DescribeRetryHistory(retryHistoryReport) +
                        Environment.NewLine +
                        "Policy: " +
                        externalForegroundPolicyReport +
                        Environment.NewLine +
                        FormatStatusDiagnostics(session));
                }

                if (attempt == FocusRetryCount) {
                    HostedSessionRetryHistoryReport retryHistoryReport = HostedSessionDiagnosticFormatter.CreateRetryHistoryReport(retryReports);
                    EmitStatusSnapshotToTestOutput(
                        session,
                        "Hosted-session typing inconclusive after foreground loss",
                        retryHistoryReport,
                        externalForegroundPolicyReport);
                    Assert.Inconclusive(
                        "The hosted-session typing harness could not retain foreground focus long enough to prove the round-trip in this desktop session." +
                        Environment.NewLine +
                        "RetryHistory: " +
                        retryHistoryReport.Summary +
                        Environment.NewLine +
                        "RetryHistoryReport: " +
                        HostedSessionDiagnosticFormatter.DescribeRetryHistory(retryHistoryReport) +
                        Environment.NewLine +
                        "Policy: " +
                        externalForegroundPolicyReport +
                        Environment.NewLine +
                        FormatStatusDiagnostics(session));
                }

                Thread.Sleep(250);
            } finally {
                session.RequestStopForegroundHold();
            }
        }
    }

    private static void TryReadExternalForegroundDiagnostics(DesktopManagerTestAppSession session, out string fingerprint, out string summary, out int abortThreshold, out string policyReport, out HostedSessionExternalForegroundReport report) {
        try {
            DesktopManagerTestAppStatus status = session.ReadStatus();
            report = HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status);
            fingerprint = report.Fingerprint;
            summary = HostedSessionDiagnosticFormatter.Summarize(status);
            abortThreshold = report.AbortThreshold;
            policyReport = report.ToPolicyReport();
        } catch (AssertInconclusiveException) {
            fingerprint = string.Empty;
            summary = "Foreground diagnostics unavailable.";
            abortThreshold = FocusRetryCount;
            policyReport = "category='unknown', abortThreshold=" + FocusRetryCount + ", fingerprint=''";
            report = HostedSessionExternalForegroundReport.None;
        }
    }

    private static int CountCommonPrefix(string expected, string actual) {
        int limit = Math.Min(expected.Length, actual.Length);
        int index = 0;
        while (index < limit && expected[index] == actual[index]) {
            index++;
        }

        return index;
    }

    private static DesktopManagerTestAppStatus WaitForStatusWithDiagnostics(DesktopManagerTestAppSession session, Func<DesktopManagerTestAppStatus, bool> predicate, int timeoutMilliseconds, string failureMessage) {
        try {
            return session.WaitForStatus(predicate, timeoutMilliseconds, failureMessage);
        } catch (AssertInconclusiveException exception) {
            EmitStatusSnapshotToTestOutput(session, failureMessage);
            throw new AssertInconclusiveException(
                exception.Message +
                Environment.NewLine +
                FormatStatusDiagnostics(session));
        }
    }

    private static string FormatStatusDiagnostics(DesktopManagerTestAppSession session) {
        try {
            DesktopManagerTestAppStatus status = session.ReadStatus();
            return
                "Status: " +
                $"Summary='{HostedSessionDiagnosticFormatter.Summarize(status)}', " +
                $"PolicyReport='{HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status).ToPolicyReport()}', " +
                $"ActiveSurface={status.ActiveSurface}, " +
                $"IsForegroundWindow={status.IsForegroundWindow}, " +
                $"SecondaryIsForegroundWindow={status.SecondaryIsForegroundWindow}, " +
                $"ForegroundHoldActive={status.ForegroundHoldActive}, " +
                $"ForegroundHoldSurface={status.ForegroundHoldSurface}, " +
                $"ForegroundHoldRequestCount={status.ForegroundHoldRequestCount}, " +
                $"ForegroundHoldRecoveryCount={status.ForegroundHoldRecoveryCount}, " +
                $"LastObservedForegroundHandle=0x{status.LastObservedForegroundHandle:X}, " +
                $"LastObservedForegroundTitle='{status.LastObservedForegroundTitle}', " +
                $"LastObservedForegroundClass='{status.LastObservedForegroundClass}', " +
                $"LastObservedForegroundChangedUtc='{status.LastObservedForegroundChangedUtc}', " +
                $"LastCommand='{status.LastCommand}', " +
                $"StatusText='{status.StatusText}', " +
                $"ForegroundHistory=[{string.Join(" | ", status.ForegroundHistory)}].";
        } catch (AssertInconclusiveException exception) {
            return "Status diagnostics unavailable: " + exception.Message;
        }
    }

    private static void EmitStatusSnapshotToTestOutput(DesktopManagerTestAppSession session, string reason, HostedSessionRetryHistoryReport? retryHistoryReport = null, string? policyReport = null) {
        try {
            DesktopManagerTestAppStatus status = session.ReadStatus();
            HostedSessionExternalForegroundReport report = HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status);
            HostedSessionRetryHistoryReport resolvedRetryHistoryReport = retryHistoryReport
                ?? HostedSessionDiagnosticFormatter.CreateRetryHistoryReport(new[] { report });
            string resolvedPolicyReport = string.IsNullOrWhiteSpace(policyReport)
                ? report.ToPolicyReport()
                : policyReport ?? string.Empty;
            string summaryText = HostedSessionDiagnosticFormatter.BuildArtifactSummary(
                reason,
                status,
                resolvedRetryHistoryReport,
                resolvedPolicyReport);
            string artifactPath = session.WriteStatusArtifact(reason, resolvedRetryHistoryReport, resolvedPolicyReport, summaryText, resolvedRetryHistoryReport.CategoryHint);
            string summaryArtifactPath = DesktopManagerTestAppSession.GetSummaryArtifactPath(artifactPath, resolvedRetryHistoryReport.CategoryHint);
            string json = JsonSerializer.Serialize(status, new JsonSerializerOptions {
                WriteIndented = true
            });
            Console.WriteLine("Hosted-session diagnostics: " + reason);
            Console.WriteLine("Hosted-session summary: " + HostedSessionDiagnosticFormatter.Summarize(status));
            Console.WriteLine("Hosted-session retry history: " + resolvedRetryHistoryReport.Summary);
            Console.WriteLine("Hosted-session retry history report: " + HostedSessionDiagnosticFormatter.DescribeRetryHistory(resolvedRetryHistoryReport));
            Console.WriteLine("Hosted-session diagnostics artifact: " + artifactPath);
            Console.WriteLine("Hosted-session diagnostics summary artifact: " + summaryArtifactPath);
            Console.WriteLine(summaryText);
            Console.WriteLine(json);
        } catch (AssertInconclusiveException exception) {
            Console.WriteLine("Hosted-session diagnostics unavailable: " + exception.Message);
        }
    }
}
