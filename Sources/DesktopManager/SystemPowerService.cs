using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Reads power state and performs explicit Windows power/session actions.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SystemPowerService {
    internal const uint ExecutionStateSystemRequired = 0x00000001;
    internal const uint ExecutionStateDisplayRequired = 0x00000002;
    internal const uint ExecutionStateAwayModeRequired = 0x00000040;
    internal const uint ExecutionStateContinuous = 0x80000000;

    /// <summary>Gets the current AC and battery state.</summary>
    /// <returns>The current power snapshot.</returns>
    public SystemPowerStatus GetStatus() {
        if (!GetSystemPowerStatus(out NativeSystemPowerStatus status)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        int? percent = status.BatteryLifePercent == byte.MaxValue ? null : (int?)status.BatteryLifePercent;
        TimeSpan? remaining = ToOptionalDuration(status.BatteryLifeTime);
        TimeSpan? fullLife = ToOptionalDuration(status.BatteryFullLifeTime);
        return new SystemPowerStatus(
            (PowerLineState)status.ACLineStatus,
            (BatteryChargeState)status.BatteryFlag,
            percent,
            remaining,
            fullLife);
    }

    /// <summary>Creates a lease that prevents selected idle power behaviors until it is disposed.</summary>
    /// <param name="options">The behaviors to prevent.</param>
    /// <returns>A disposable keep-awake lease.</returns>
    public KeepAwakeLease CreateKeepAwakeLease(KeepAwakeOptions options = KeepAwakeOptions.System) {
        KeepAwakeOptions supported = KeepAwakeOptions.System | KeepAwakeOptions.Display | KeepAwakeOptions.AwayMode;
        if (options == 0 || (options & ~supported) != 0) {
            throw new ArgumentOutOfRangeException(nameof(options), "At least one supported keep-awake option is required.");
        }
        if ((options & KeepAwakeOptions.AwayMode) != 0 && (options & KeepAwakeOptions.System) == 0) {
            throw new ArgumentException("Away mode requires the system keep-awake option.", nameof(options));
        }
        return new KeepAwakeLease(options);
    }

    /// <summary>Locks the current interactive workstation.</summary>
    public void LockWorkstation() {
        if (!NativeLockWorkStation()) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>Requests system sleep or hibernation.</summary>
    /// <param name="hibernate">When true, requests hibernation; otherwise requests sleep.</param>
    /// <param name="force">When true, requests an immediate forced suspension.</param>
    public void Suspend(bool hibernate = false, bool force = false) {
        if (!SetSuspendState(hibernate, force, false)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>Signs out the current interactive user.</summary>
    /// <param name="force">When true, forces applications to close.</param>
    public void SignOut(bool force = false) {
        uint flags = force ? 0x00000004u : 0u;
        if (!ExitWindowsEx(flags, 0)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    internal static uint ToExecutionState(KeepAwakeOptions options) {
        uint state = 0;
        if ((options & KeepAwakeOptions.System) != 0) {
            state |= ExecutionStateSystemRequired;
        }
        if ((options & KeepAwakeOptions.Display) != 0) {
            state |= ExecutionStateDisplayRequired;
        }
        if ((options & KeepAwakeOptions.AwayMode) != 0) {
            state |= ExecutionStateAwayModeRequired;
        }
        return state;
    }

    private static TimeSpan? ToOptionalDuration(uint seconds) {
        return seconds == uint.MaxValue ? null : (TimeSpan?)TimeSpan.FromSeconds(seconds);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSystemPowerStatus {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemPowerStatus(out NativeSystemPowerStatus systemPowerStatus);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint SetThreadExecutionState(uint executionState);

    [DllImport("user32.dll", EntryPoint = "LockWorkStation", SetLastError = true)]
    private static extern bool NativeLockWorkStation();

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint flags, uint reason);
}
