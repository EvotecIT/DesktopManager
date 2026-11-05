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

        return new SystemPowerPlan(schemeId, name, ReadOverlayScheme());
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

    private const uint ErrorMoreData = 234;

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
