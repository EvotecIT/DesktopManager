using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopManager;

public sealed partial class DesktopSessionService {
    /// <summary>Reads the signed-in user of the current process session or an explicitly selected session.</summary>
    /// <param name="sessionId">
    /// The target Windows session identifier, or <c>null</c> to use the current process session.
    /// Only an implicit lookup from session 0 uses the active console session instead.
    /// An explicit target never falls back to another session.
    /// </param>
    /// <returns>The user snapshot, or <c>null</c> when no user is signed in or the session cannot be read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sessionId"/> is negative.</exception>
    /// <remarks>This read-only lookup uses WTS. Callers choose any additional identity source or display placeholder.</remarks>
    public DesktopSessionUser? GetSessionUser(int? sessionId = null) {
        return ResolveSessionUser(sessionId, GetProcessSessionId, GetActiveConsoleSessionId, QueryString);
    }

    /// <summary>Resolves session selection and user identity through the process and WTS read boundaries.</summary>
    internal static DesktopSessionUser? ResolveSessionUser(
        int? sessionId,
        Func<int> getProcessSessionId,
        Func<int> getActiveConsoleSessionId,
        Func<int, int, string?> queryString) {
        if (sessionId < 0) {
            throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "The session identifier cannot be negative.");
        }

        try {
            int targetSessionId = sessionId ?? getProcessSessionId();
            bool isConsoleFallback = !sessionId.HasValue && targetSessionId == 0;
            if (isConsoleFallback) {
                targetSessionId = getActiveConsoleSessionId();
                if (targetSessionId <= 0) {
                    return null;
                }
            }

            if (targetSessionId < 0) {
                return null;
            }

            string? userName = queryString(targetSessionId, WtsUserName);
            if (string.IsNullOrWhiteSpace(userName)) {
                return null;
            }

            string? domainName = queryString(targetSessionId, WtsDomainName);
            return new DesktopSessionUser(
                targetSessionId,
                userName!,
                string.IsNullOrWhiteSpace(domainName) ? string.Empty : domainName!,
                isConsoleFallback);
        } catch (Exception exception) when (exception is Win32Exception
            || exception is InvalidOperationException
            || exception is DllNotFoundException
            || exception is EntryPointNotFoundException
            || exception is BadImageFormatException
            || exception is PlatformNotSupportedException) {
            return null;
        }
    }

    private static int GetProcessSessionId() {
        using (Process process = Process.GetCurrentProcess()) {
            return process.SessionId;
        }
    }

    private static int GetActiveConsoleSessionId() {
        uint sessionId = WTSGetActiveConsoleSessionId();
        return sessionId > int.MaxValue ? -1 : (int)sessionId;
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();
}
