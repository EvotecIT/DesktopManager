using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

public sealed partial class SystemPowerService {
    private const int MaximumFriendlyNameBytes = 4096;
    private static readonly Guid BestPowerEfficiencyOverlay = new("961cc777-2547-4f9d-8174-7d86181b8a7a");
    private static readonly Guid BetterBatteryOverlay = new("3af9b8d9-7c97-431d-ad78-34a8bfea439f");
    private static readonly Guid BestPerformanceOverlay = new("ded574b5-45a0-4f42-8737-46345c09c238");
    private const string PowerSchemesKey = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";
    private static readonly IntPtr HKeyLocalMachine = new(unchecked((int)0x80000002));
    private const int KeyQueryValue = 0x0001;
    private static readonly PowerOverlayCache OverlayCache = new(ReadOverlayStamp, ReadOverlayScheme);

    /// <summary>
    /// Reads the active power plan and the power mode chosen in Settings. This only reads; it never changes either.
    /// </summary>
    /// <returns>The active power plan snapshot.</returns>
    /// <exception cref="Win32Exception">Windows could not report the active power plan.</exception>
    public SystemPowerPlan GetActivePowerPlan() {
        uint error = PowerGetActiveScheme(IntPtr.Zero, out IntPtr schemePointer);
        if (error != 0) {
            throw new Win32Exception(unchecked((int)error));
        }
        if (schemePointer == IntPtr.Zero) {
            throw new InvalidOperationException("Windows did not return the active power plan.");
        }

        Guid schemeId;
        string name;
        try {
            schemeId = Marshal.PtrToStructure<Guid>(schemePointer);
            name = ReadFriendlyName(schemePointer);
        } finally {
            LocalFree(schemePointer);
        }

        return new SystemPowerPlan(schemeId, name, OverlayCache.Get(schemeId));
    }

    internal static SystemPowerMode ToPowerMode(Guid overlay) {
        if (overlay == Guid.Empty) {
            return SystemPowerMode.Balanced;
        }
        if (overlay == BestPowerEfficiencyOverlay) {
            return SystemPowerMode.BestPowerEfficiency;
        }
        if (overlay == BetterBatteryOverlay) {
            return SystemPowerMode.BetterBattery;
        }
        if (overlay == BestPerformanceOverlay) {
            return SystemPowerMode.BestPerformance;
        }
        return SystemPowerMode.Unknown;
    }

    private static string ReadFriendlyName(IntPtr schemePointer) {
        uint size = 0;
        uint error = PowerReadFriendlyName(IntPtr.Zero, schemePointer, IntPtr.Zero, IntPtr.Zero, null, ref size);
        // Asking for the size returns success or ERROR_MORE_DATA depending on the Windows version.
        if ((error != 0 && error != ErrorMoreData) || size == 0) {
            return string.Empty;
        }
        if (size > MaximumFriendlyNameBytes) {
            throw new InvalidOperationException($"Windows reported an invalid power plan name length of {size} bytes.");
        }

        byte[] buffer = new byte[size];
        error = PowerReadFriendlyName(IntPtr.Zero, schemePointer, IntPtr.Zero, IntPtr.Zero, buffer, ref size);
        if (error != 0) {
            throw new Win32Exception(unchecked((int)error));
        }

        int length = (int)Math.Min(size, (uint)buffer.Length) & ~1;
        string name = Encoding.Unicode.GetString(buffer, 0, length);
        int terminator = name.IndexOf('\0');
        return (terminator >= 0 ? name.Substring(0, terminator) : name).Trim();
    }

    private static Guid? ReadOverlayScheme() {
        try {
            return PowerGetEffectiveOverlayScheme(out Guid overlay) == 0 ? overlay : (Guid?)null;
        } catch (EntryPointNotFoundException) {
            // Power mode overlays arrived in Windows 10 version 1709.
            return null;
        }
    }

    /// <summary>
    /// Settings stores the chosen overlays as values of the power schemes key, so its last write time moves
    /// whenever they or the active plan change; the power source decides which overlay applies.
    /// </summary>
    private static PowerOverlayStamp? ReadOverlayStamp() {
        if (!GetSystemPowerStatus(out NativeSystemPowerStatus status)
            || RegOpenKeyEx(HKeyLocalMachine, PowerSchemesKey, 0, KeyQueryValue, out IntPtr key) != 0) {
            return null;
        }

        try {
            return RegQueryInfoKey(key, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out long lastWriteTime) == 0
                ? new PowerOverlayStamp(lastWriteTime, status.ACLineStatus, status.SystemStatusFlag)
                : (PowerOverlayStamp?)null;
        } finally {
            RegCloseKey(key);
        }
    }

    private const uint ErrorMoreData = 234;

    [DllImport("advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode)]
    private static extern int RegOpenKeyEx(IntPtr key, string subKey, int options, int desired, out IntPtr result);

    [DllImport("advapi32.dll", EntryPoint = "RegQueryInfoKeyW")]
    private static extern int RegQueryInfoKey(IntPtr key, IntPtr className, IntPtr classNameLength, IntPtr reserved,
        IntPtr subKeys, IntPtr maxSubKeyLength, IntPtr maxClassLength, IntPtr values, IntPtr maxValueNameLength,
        IntPtr maxValueLength, IntPtr securityDescriptorLength, out long lastWriteTime);

    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr key);

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, IntPtr schemeGuid, IntPtr subGroupOfPowerSettingsGuid,
        IntPtr powerSettingGuid, byte[]? buffer, ref uint bufferSize);

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetEffectiveOverlayScheme(out Guid effectiveOverlayGuid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
