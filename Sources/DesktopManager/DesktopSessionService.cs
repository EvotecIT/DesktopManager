using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Reads the current Windows interactive-session state.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DesktopSessionService {
    private const int WtsUserName = 5;
    private const int WtsWinStationName = 6;
    private const int WtsDomainName = 7;
    private const int WtsConnectState = 8;
    private const int WtsClientName = 10;
    private const int WtsClientProtocolType = 16;
    private const uint DesktopReadObjects = 0x0001;
    private const uint DesktopSwitchDesktop = 0x0100;

    /// <summary>Gets the session hosting the current DesktopManager process.</summary>
    /// <returns>The current session snapshot.</returns>
    public DesktopSessionInfo GetCurrentSession() {
        int sessionId = Process.GetCurrentProcess().SessionId;
        string userName = QueryString(sessionId, WtsUserName);
        string domainName = QueryString(sessionId, WtsDomainName);
        string clientName = QueryString(sessionId, WtsClientName);
        DesktopSessionConnectState connectState = QueryConnectState(sessionId);
        DesktopSessionProtocol protocol = QueryProtocol(sessionId);
        bool isRemote = protocol == DesktopSessionProtocol.RemoteDesktop || GetSystemMetrics(0x1000) != 0;
        return new DesktopSessionInfo(
            sessionId,
            userName,
            domainName,
            clientName,
            connectState,
            protocol,
            isRemote,
            TryGetLockedState(),
            GetIdleTime());
    }

    private static string QueryString(int sessionId, int informationClass) {
        if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, informationClass, out IntPtr buffer, out int bytes)) {
            return string.Empty;
        }

        try {
            return bytes <= 1 ? string.Empty : Marshal.PtrToStringUni(buffer) ?? string.Empty;
        } finally {
            WTSFreeMemory(buffer);
        }
    }

    private static DesktopSessionConnectState QueryConnectState(int sessionId) {
        if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsConnectState, out IntPtr buffer, out int bytes)) {
            return DesktopSessionConnectState.Unknown;
        }

        try {
            return bytes < sizeof(int)
                ? DesktopSessionConnectState.Unknown
                : (DesktopSessionConnectState)Marshal.ReadInt32(buffer);
        } finally {
            WTSFreeMemory(buffer);
        }
    }

    private static DesktopSessionProtocol QueryProtocol(int sessionId) {
        if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsClientProtocolType, out IntPtr buffer, out int bytes)) {
            return DesktopSessionProtocol.Unknown;
        }

        try {
            if (bytes < sizeof(short)) {
                return DesktopSessionProtocol.Unknown;
            }

            short value = Marshal.ReadInt16(buffer);
            return value >= 0 && value <= 2
                ? (DesktopSessionProtocol)value
                : DesktopSessionProtocol.Unknown;
        } finally {
            WTSFreeMemory(buffer);
        }
    }

    private static bool? TryGetLockedState() {
        IntPtr desktop = OpenInputDesktop(0, false, DesktopReadObjects | DesktopSwitchDesktop);
        if (desktop == IntPtr.Zero) {
            return null;
        }

        try {
            return !SwitchDesktop(desktop);
        } finally {
            CloseDesktop(desktop);
        }
    }

    private static TimeSpan GetIdleTime() {
        var info = new LastInputInfo {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };
        if (!GetLastInputInfo(ref info)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        uint elapsed = unchecked((uint)Environment.TickCount) - info.Time;
        return TimeSpan.FromMilliseconds(elapsed);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo {
        public uint Size;
        public uint Time;
    }

    [DllImport("wtsapi32.dll", EntryPoint = "WTSQuerySessionInformationW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr server,
        int sessionId,
        int informationClass,
        out IntPtr buffer,
        out int bytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr memory);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint desiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SwitchDesktop(IntPtr desktop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
