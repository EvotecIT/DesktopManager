using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Provides atomic display-mode operations used by workstation profiles.
/// </summary>
public partial class MonitorService {
    private const int DisplayFieldPosition = 0x00000020;
    private const int DisplayFieldOrientation = 0x00000080;
    private const int DisplayFieldWidth = 0x00080000;
    private const int DisplayFieldHeight = 0x00100000;
    private const int DisplayFieldFrequency = 0x00400000;

    /// <summary>Gets the active resolution, refresh rate, and orientation for a monitor.</summary>
    /// <param name="deviceId">The monitor device ID.</param>
    /// <returns>The current display mode.</returns>
    public MonitorDisplayMode GetMonitorDisplayMode(string deviceId) {
        Monitor monitor = ResolveDisplayMonitor(deviceId);
        DEVMODE mode = ReadDisplayMode(monitor.DeviceName);
        return new MonitorDisplayMode {
            Width = mode.dmPelsWidth,
            Height = mode.dmPelsHeight,
            RefreshRate = mode.dmDisplayFrequency,
            Orientation = (DisplayOrientation)mode.dmDisplayOrientation
        };
    }

    /// <summary>Applies resolution, refresh rate, and orientation as one display-mode request.</summary>
    /// <param name="deviceId">The monitor device ID.</param>
    /// <param name="mode">The explicit display mode to apply.</param>
    public void SetMonitorDisplayMode(string deviceId, MonitorDisplayMode mode) {
        ValidateDisplayMode(mode);
        Monitor monitor = ResolveDisplayMonitor(deviceId);
        DEVMODE nativeMode = ReadDisplayMode(monitor.DeviceName);
        ApplyMode(ref nativeMode, mode);
        DisplayChangeConfirmation result = MonitorNativeMethods.ChangeDisplaySettingsEx(
            monitor.DeviceName,
            ref nativeMode,
            IntPtr.Zero,
            ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY,
            IntPtr.Zero);
        EnsureDisplayChangeSucceeded(result, monitor.DeviceName);
    }

    internal void ApplyDisplayProfile(
        IReadOnlyList<WorkstationMonitorProfile> profileMonitors,
        IReadOnlyDictionary<string, Monitor> matches) {
        if (profileMonitors == null) {
            throw new ArgumentNullException(nameof(profileMonitors));
        }
        if (matches == null) {
            throw new ArgumentNullException(nameof(matches));
        }

        foreach (WorkstationMonitorProfile profile in profileMonitors) {
            if (!matches.TryGetValue(profile.StableKey, out Monitor? monitor)) {
                continue;
            }

            ValidateDisplayMode(profile.DisplayMode);
            DEVMODE nativeMode = ReadDisplayMode(monitor.DeviceName);
            ApplyMode(ref nativeMode, profile.DisplayMode);
            nativeMode.dmFields |= DisplayFieldPosition;
            nativeMode.dmPositionX = profile.Left;
            nativeMode.dmPositionY = profile.Top;
            ChangeDisplaySettingsFlags flags = ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY |
                ChangeDisplaySettingsFlags.CDS_NORESET;
            if (profile.IsPrimary) {
                flags |= ChangeDisplaySettingsFlags.CDS_SET_PRIMARY;
            }

            DisplayChangeConfirmation staged = MonitorNativeMethods.ChangeDisplaySettingsEx(
                monitor.DeviceName,
                ref nativeMode,
                IntPtr.Zero,
                flags,
                IntPtr.Zero);
            EnsureDisplayChangeSucceeded(staged, monitor.DeviceName);
        }

        DisplayChangeConfirmation applied = MonitorNativeMethods.ChangeDisplaySettingsEx(
            null,
            IntPtr.Zero,
            IntPtr.Zero,
            ChangeDisplaySettingsFlags.CDS_NONE,
            IntPtr.Zero);
        EnsureDisplayChangeSucceeded(applied, "the staged display profile");
    }

    private Monitor ResolveDisplayMonitor(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }

        Monitor? monitor = GetMonitors().FirstOrDefault(candidate =>
            string.Equals(candidate.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (monitor == null || string.IsNullOrWhiteSpace(monitor.DeviceName)) {
            throw new ArgumentException($"Monitor with device ID '{deviceId}' does not have a resolvable display source.", nameof(deviceId));
        }
        return monitor;
    }

    private static DEVMODE ReadDisplayMode(string deviceName) {
        DEVMODE mode = new() {
            dmSize = (short)Marshal.SizeOf<DEVMODE>()
        };
        if (!MonitorNativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref mode)) {
            throw new InvalidOperationException($"Unable to get display settings for '{deviceName}'.");
        }
        return mode;
    }

    private static void ApplyMode(ref DEVMODE nativeMode, MonitorDisplayMode mode) {
        nativeMode.dmFields = DisplayFieldWidth |
            DisplayFieldHeight |
            DisplayFieldFrequency |
            DisplayFieldOrientation;
        nativeMode.dmPelsWidth = mode.Width;
        nativeMode.dmPelsHeight = mode.Height;
        nativeMode.dmDisplayFrequency = mode.RefreshRate;
        nativeMode.dmDisplayOrientation = (int)mode.Orientation;
    }

    private static void ValidateDisplayMode(MonitorDisplayMode mode) {
        if (mode == null) {
            throw new ArgumentNullException(nameof(mode));
        }
        if (mode.Width <= 0) {
            throw new ArgumentOutOfRangeException(nameof(mode), "Display width must be positive.");
        }
        if (mode.Height <= 0) {
            throw new ArgumentOutOfRangeException(nameof(mode), "Display height must be positive.");
        }
        if (mode.RefreshRate <= 0) {
            throw new ArgumentOutOfRangeException(nameof(mode), "Display refresh rate must be positive.");
        }
        if (!Enum.IsDefined(typeof(DisplayOrientation), mode.Orientation)) {
            throw new ArgumentOutOfRangeException(nameof(mode), "Display orientation is invalid.");
        }
    }

    private static void EnsureDisplayChangeSucceeded(DisplayChangeConfirmation result, string target) {
        if (result != DisplayChangeConfirmation.Successful && result != DisplayChangeConfirmation.Restart) {
            throw new InvalidOperationException($"Unable to apply display settings for {target}. Error: {result}.");
        }
    }
}
