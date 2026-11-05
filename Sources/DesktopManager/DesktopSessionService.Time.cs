using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopManager;

public sealed partial class DesktopSessionService {
    /// <summary>Reads the recorded sign-in time, preserved across reconnect/hibernation but renewed after Fast Startup.</summary>
    /// <returns>A UTC FILETIME, or zero when the current session cannot report it.</returns>
    internal static long GetLogonFileTime() {
        using Process process = Process.GetCurrentProcess();
        IntPtr buffer = IntPtr.Zero;
        try {
            // WTSINFOEX has an 8-byte header followed by WTSINFOEX_LEVEL1_W. Its three 32-bit fields and
            // WCHAR arrays (33 station, 21 user, 18 domain) put the aligned LogonTime at byte 168.
            if (!WTSQuerySessionInformation(IntPtr.Zero, process.SessionId, 25, out buffer, out int bytes)
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
}
