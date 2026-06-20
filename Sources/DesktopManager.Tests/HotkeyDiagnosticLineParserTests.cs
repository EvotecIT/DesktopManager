#if NET8_0_OR_GREATER
using DesktopManager.App.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests hotkey diagnostic JSONL parsing used by the app diagnose flow.
/// </summary>
public class HotkeyDiagnosticLineParserTests {
    [TestMethod]
    /// <summary>
    /// Completed runtime events should surface backend source, verification, attempts, and diagnostic path.
    /// </summary>
    public void TryParse_CompletedRuntimeEvent_ReturnsOperatorSummary() {
        const string json = """
            {"Timestamp":"2026-06-19T12:00:00+02:00","EventName":"completed","FunctionName":"Move Window","Hotkey":"Ctrl+Alt+Shift+5","Details":{"Source":"keyboard-hook","WindowHandle":"0x1234","Verified":true,"Attempts":1,"DiagnosticPath":"C:\\Diag\\hotkeys.jsonl"}}
            """;

        bool parsed = HotkeyDiagnosticLineParser.TryParse(
            json,
            "Ctrl+Alt+Shift+5",
            "Move Window",
            out HotkeyDiagnosticSummary summary);

        Assert.IsTrue(parsed);
        Assert.IsTrue(summary.Found);
        Assert.AreEqual("completed", summary.EventName);
        StringAssert.Contains(summary.Summary, "completed");
        StringAssert.Contains(summary.Details, "keyboard-hook");
        StringAssert.Contains(summary.Details, "verified: true");
        StringAssert.Contains(summary.Details, "attempts: 1");
        StringAssert.Contains(summary.Details, "C:\\Diag\\hotkeys.jsonl");
    }

    [TestMethod]
    /// <summary>
    /// Runtime events for other hotkeys should be ignored.
    /// </summary>
    public void TryParse_OtherHotkey_ReturnsFalse() {
        const string json = """
            {"EventName":"registered","FunctionName":"Other","Hotkey":"Ctrl+Alt+Shift+9","Details":{"Backend":"LowLevelKeyboardHook"}}
            """;

        bool parsed = HotkeyDiagnosticLineParser.TryParse(
            json,
            "Ctrl+Alt+Shift+5",
            "Move Window",
            out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    /// <summary>
    /// Execution diagnostics without EventName should still summarize verification evidence.
    /// </summary>
    public void TryParse_ExecutionDiagnostic_ReturnsExecutionSummary() {
        const string json = """
            {"Timestamp":"2026-06-19T12:01:00+02:00","FunctionName":"Move Window","Hotkey":"Ctrl+Alt+Shift+5","ResolvedHandle":"0x5678","Attempt":2,"Verified":false,"Error":"Final geometry was not confirmed."}
            """;

        bool parsed = HotkeyDiagnosticLineParser.TryParse(
            json,
            "Ctrl+Alt+Shift+5",
            "Move Window",
            out HotkeyDiagnosticSummary summary);

        Assert.IsTrue(parsed);
        Assert.AreEqual("execution", summary.EventName);
        StringAssert.Contains(summary.Summary, "failed");
        StringAssert.Contains(summary.Details, "verified: false");
        StringAssert.Contains(summary.Details, "attempt: 2");
        StringAssert.Contains(summary.Details, "0x5678");
    }
}
#endif
