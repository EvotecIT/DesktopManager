using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager;

public sealed partial class DesktopSessionService {
    /// <summary>Reads the recorded sign-in time, preserved across reconnect/hibernation but renewed after Fast Startup.</summary>
    /// <returns>A UTC FILETIME, or zero when the current session cannot report it.</returns>
    /// <remarks>A session's sign-in time is fixed while it exists and this process cannot outlive it, so the first answer is kept.</remarks>
    internal static long GetLogonFileTime() {
        long logonFileTime = Interlocked.Read(ref _logonFileTime);
        if (logonFileTime == 0) {
            logonFileTime = QueryLogonFileTime(CurrentProcessSessionId);
            Interlocked.Exchange(ref _logonFileTime, logonFileTime);
        }
        return logonFileTime;
    }

    private static long _logonFileTime;

    private static long QueryLogonFileTime(int sessionId) {
        IntPtr buffer = IntPtr.Zero;
        try {
            // WTSINFOEX has an 8-byte header followed by WTSINFOEX_LEVEL1_W. Its three 32-bit fields and
            // WCHAR arrays (33 station, 21 user, 18 domain) put the aligned LogonTime at byte 168.
            if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, 25, out buffer, out int bytes)
                || buffer == IntPtr.Zero || bytes < 176 || Marshal.ReadInt32(buffer) != 1) {
                return 0;
            }

            return Marshal.ReadInt64(buffer, 168);
        } finally {
            if (buffer != IntPtr.Zero) {
                WTSFreeMemory(buffer);
            }
        }
    }

    private static int _currentProcessSessionId = -1;

    /// <summary>The session hosting this process, which cannot change for the life of the process.</summary>
    /// <remarks>
    /// <see cref="Process.SessionId"/> snapshots every process on the machine to answer this, which costs
    /// milliseconds per call. <c>ProcessIdToSessionId</c> asks about this process only.
    /// </remarks>
    internal static int CurrentProcessSessionId {
        get {
            int sessionId = Volatile.Read(ref _currentProcessSessionId);
            if (sessionId >= 0) {
                return sessionId;
            }

            if (ProcessIdToSessionId(GetCurrentProcessId(), out uint nativeSessionId) && nativeSessionId <= int.MaxValue) {
                sessionId = (int)nativeSessionId;
            } else {
                using Process process = Process.GetCurrentProcess();
                sessionId = process.SessionId;
            }

            Volatile.Write(ref _currentProcessSessionId, sessionId);
            return sessionId;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();
}
