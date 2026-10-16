using System;
using System.ComponentModel;

namespace DesktopManager.Tests;

/// <summary>Protects session identity selection without changing the desktop or requiring a signed-in test account.</summary>
[TestClass]
public sealed class DesktopSessionUserTests {
    [TestMethod]
    public void GetSessionUser_CurrentInteractiveSession_ReturnsOwnUserAndProvenance() {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null,
            () => 12,
            () => throw new AssertFailedException("An interactive session must not query the console."),
            (sessionId, informationClass) => {
                Assert.AreEqual(12, sessionId);
                return informationClass == 5 ? "alice" : "CONTOSO";
            });

        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(12, result.Value.SessionId);
        Assert.AreEqual("alice", result.Value.UserName);
        Assert.AreEqual("CONTOSO", result.Value.DomainName);
        Assert.AreEqual(@"CONTOSO\alice", result.Value.AccountName);
        Assert.IsFalse(result.Value.IsConsoleFallback);
    }

    [TestMethod]
    public void GetSessionUser_ProcessSessionZero_ReturnsConsoleUserAndProvenance() {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null,
            () => 0,
            () => 7,
            (sessionId, informationClass) => {
                Assert.AreEqual(7, sessionId);
                return informationClass == 5 ? "bob" : "WORKSTATION";
            });

        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(7, result.Value.SessionId);
        Assert.AreEqual(@"WORKSTATION\bob", result.Value.AccountName);
        Assert.IsTrue(result.Value.IsConsoleFallback);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(24)]
    public void GetSessionUser_ExplicitTarget_DoesNotReadProcessOrConsoleSession(int targetSessionId) {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            targetSessionId,
            () => throw new AssertFailedException("An explicit target must not inspect the process session."),
            () => throw new AssertFailedException("An explicit target must not inspect the console session."),
            (sessionId, informationClass) => {
                Assert.AreEqual(targetSessionId, sessionId);
                return informationClass == 5 ? "target-user" : "TARGET";
            });

        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(targetSessionId, result.Value.SessionId);
        Assert.AreEqual(@"TARGET\target-user", result.Value.AccountName);
        Assert.IsFalse(result.Value.IsConsoleFallback);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(24)]
    public void GetSessionUser_EmptyInteractiveOrExplicitTarget_DoesNotBorrowConsoleUser(int? targetSessionId) {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            targetSessionId,
            () => 12,
            () => throw new AssertFailedException("An empty target must not select an unrelated console user."),
            (sessionId, informationClass) => {
                Assert.AreEqual(targetSessionId ?? 12, sessionId);
                Assert.AreEqual(5, informationClass, "A missing user must not query the domain.");
                return string.Empty;
            });

        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetSessionUser_MissingDomain_ReturnsUnqualifiedUser(string? domainName) {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            12, () => 0, () => 7,
            (_, informationClass) => informationClass == 5 ? "alice" : domainName);

        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(string.Empty, result.Value.DomainName);
        Assert.AreEqual("alice", result.Value.AccountName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("   ")]
    public void GetSessionUser_MissingConsoleUser_ReturnsNull(string? userName) {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null, () => 0, () => 7,
            (_, informationClass) => {
                Assert.AreEqual(5, informationClass);
                return userName;
            });

        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void GetSessionUser_NoActiveConsoleSession_ReturnsNull(int consoleSessionId) {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null, () => 0, () => consoleSessionId,
            (_, _) => throw new AssertFailedException("An unavailable console must not be queried."));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetSessionUser_UnavailableProcessSession_DoesNotBorrowConsoleUser() {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null, () => -1,
            () => throw new AssertFailedException("An unknown process session must not select the console."),
            (_, _) => throw new AssertFailedException("An unknown process session must not be queried."));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetSessionUser_UnavailableWts_ReturnsNull() {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            12, () => 0, () => 7,
            (_, _) => throw new DllNotFoundException("WTS is unavailable."));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetSessionUser_ProcessSessionReadFails_ReturnsNull() {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null, () => throw new Win32Exception(5),
            () => throw new AssertFailedException("A failed process lookup must not select the console."),
            (_, _) => throw new AssertFailedException("A failed process lookup must not query WTS."));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetSessionUser_ConsoleSessionReadFails_ReturnsNull() {
        DesktopSessionUser? result = DesktopSessionService.ResolveSessionUser(
            null, () => 0, () => throw new EntryPointNotFoundException("Console lookup is unavailable."),
            (_, _) => throw new AssertFailedException("A failed console lookup must not query WTS."));

        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void GetSessionUser_NegativeTarget_ThrowsArgumentOutOfRange(int sessionId) {
        var service = new DesktopSessionService();

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => service.GetSessionUser(sessionId));

        Assert.AreEqual("sessionId", exception.ParamName);
        Assert.AreEqual(sessionId, exception.ActualValue);
    }
}
