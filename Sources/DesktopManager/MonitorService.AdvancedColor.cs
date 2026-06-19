using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager;

public partial class MonitorService {
    /// <summary>
    /// Gets Advanced Color and HDR state for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <returns>The current Advanced Color and HDR state.</returns>
    public MonitorAdvancedColorInfo GetMonitorAdvancedColor(string deviceId) {
        Monitor monitor = ResolveMonitorForAdvancedColor(deviceId);
        DisplayConfigPathInfo path = ResolveDisplayConfigPathForMonitor(monitor);

        MonitorAdvancedColorInfo result;
        if (TryGetAdvancedColorInfo2(monitor, path, out MonitorAdvancedColorInfo? advancedColorInfo2)) {
            result = advancedColorInfo2!;
        } else {
            result = GetAdvancedColorInfoLegacy(monitor, path);
        }

        if (TryGetSdrWhiteLevel(path, out uint sdrWhiteLevel)) {
            result.SdrWhiteLevel = sdrWhiteLevel;
            result.SdrWhiteLevelNits = sdrWhiteLevel / 1000d * 80d;
        }

        return result;
    }

    /// <summary>
    /// Sets HDR for a monitor, falling back to the legacy Advanced Color packet on older Windows builds.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="enabled">Whether HDR should be enabled.</param>
    public void SetMonitorHdr(string deviceId, bool enabled) {
        Monitor monitor = ResolveMonitorForAdvancedColor(deviceId);
        DisplayConfigPathInfo path = ResolveDisplayConfigPathForMonitor(monitor);

        int hdrError = SetHdrState(path, enabled);
        if (hdrError == MonitorNativeMethods.DisplayConfigErrorSuccess) {
            return;
        }

        if (!IsUnsupportedDeviceInfoPacketError(hdrError)) {
            throw new InvalidOperationException($"Unable to set monitor HDR state. Error: {hdrError}");
        }

        int error = SetAdvancedColorState(path, enabled);
        if (error != MonitorNativeMethods.DisplayConfigErrorSuccess) {
            throw new InvalidOperationException($"Unable to set monitor HDR/Advanced Color state. Error: {error}");
        }
    }

    private Monitor ResolveMonitorForAdvancedColor(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        Monitor? monitor = GetMonitors()
            .FirstOrDefault(m => string.Equals(m.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (monitor == null) {
            throw new ArgumentException($"Monitor with device ID '{deviceId}' not found", nameof(deviceId));
        }

        return monitor;
    }

    private static DisplayConfigPathInfo ResolveDisplayConfigPathForMonitor(Monitor monitor) {
        IReadOnlyList<string> sourceNameCandidates = GetDisplayConfigSourceNameCandidates(monitor);
        if (sourceNameCandidates.Count == 0) {
            throw new InvalidOperationException($"Monitor '{monitor.DeviceId}' does not have a display source name.");
        }

        IReadOnlyList<DisplayConfigPathInfo> paths = QueryActiveDisplayConfigPaths();
        foreach (DisplayConfigPathInfo path in paths) {
            if (TryGetSourceDeviceName(path, out string sourceDeviceName) &&
                sourceNameCandidates.Contains(sourceDeviceName, StringComparer.OrdinalIgnoreCase)) {
                return path;
            }
        }

        throw new InvalidOperationException($"Unable to resolve active DisplayConfig path for monitor '{string.Join("' or '", sourceNameCandidates)}'.");
    }

    internal static IReadOnlyList<string> GetDisplayConfigSourceNameCandidates(Monitor monitor) {
        var candidates = new List<string>();
        AddDisplayConfigSourceNameCandidate(candidates, monitor.DeviceId);
        AddDisplayConfigSourceNameCandidate(candidates, monitor.DeviceName);
        return candidates;
    }

    private static void AddDisplayConfigSourceNameCandidate(List<string> candidates, string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return;
        }

        string candidate = value.Trim();
        if (!candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase)) {
            candidates.Add(candidate);
        }
    }

    private static IReadOnlyList<DisplayConfigPathInfo> QueryActiveDisplayConfigPaths() {
        for (int attempt = 0; attempt < 3; attempt++) {
            int sizeError = MonitorNativeMethods.GetDisplayConfigBufferSizes(
                MonitorNativeMethods.QdcOnlyActivePaths,
                out uint pathCount,
                out uint modeInfoCount);
            if (sizeError != MonitorNativeMethods.DisplayConfigErrorSuccess) {
                throw new InvalidOperationException($"GetDisplayConfigBufferSizes failed. Error: {sizeError}");
            }

            DisplayConfigPathInfo[] paths = new DisplayConfigPathInfo[pathCount];
            DisplayConfigModeInfo[] modes = new DisplayConfigModeInfo[modeInfoCount];
            int queryError = MonitorNativeMethods.QueryDisplayConfig(
                MonitorNativeMethods.QdcOnlyActivePaths,
                ref pathCount,
                paths,
                ref modeInfoCount,
                modes,
                IntPtr.Zero);

            if (queryError == MonitorNativeMethods.DisplayConfigErrorSuccess) {
                return paths.Take((int)pathCount).ToArray();
            }

            if (queryError != MonitorNativeMethods.DisplayConfigErrorInsufficientBuffer) {
                throw new InvalidOperationException($"QueryDisplayConfig failed. Error: {queryError}");
            }
        }

        throw new InvalidOperationException("QueryDisplayConfig failed because the display topology changed while it was being queried.");
    }

    private static bool TryGetSourceDeviceName(DisplayConfigPathInfo path, out string sourceDeviceName) {
        DisplayConfigSourceDeviceName sourceName = new() {
            Header = CreateDeviceInfoHeader(
                DisplayConfigDeviceInfoType.GetSourceName,
                Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                path.SourceInfo.AdapterId,
                path.SourceInfo.Id),
            ViewGdiDeviceName = string.Empty
        };

        int error = MonitorNativeMethods.DisplayConfigGetSourceDeviceName(ref sourceName);
        sourceDeviceName = error == MonitorNativeMethods.DisplayConfigErrorSuccess
            ? sourceName.ViewGdiDeviceName
            : string.Empty;
        return error == MonitorNativeMethods.DisplayConfigErrorSuccess;
    }

    private static bool TryGetAdvancedColorInfo2(Monitor monitor, DisplayConfigPathInfo path, out MonitorAdvancedColorInfo? result) {
        DisplayConfigGetAdvancedColorInfo2 info = new() {
            Header = CreateTargetDeviceInfoHeader(
                DisplayConfigDeviceInfoType.GetAdvancedColorInfo2,
                Marshal.SizeOf<DisplayConfigGetAdvancedColorInfo2>(),
                path)
        };

        int error = MonitorNativeMethods.DisplayConfigGetAdvancedColorInfo2(ref info);
        if (error != MonitorNativeMethods.DisplayConfigErrorSuccess) {
            result = null;
            return false;
        }

        result = CreateBaseAdvancedColorInfo(monitor);
        result.AdvancedColorSupported = info.AdvancedColorSupported;
        result.AdvancedColorEnabled = info.AdvancedColorActive;
        result.HdrSupported = info.HighDynamicRangeSupported;
        result.HdrEnabled = info.HighDynamicRangeUserEnabled;
        result.WideColorSupported = info.WideColorSupported;
        result.WideColorEnabled = info.WideColorUserEnabled;
        result.AdvancedColorLimitedByPolicy = info.AdvancedColorLimitedByPolicy;
        result.ActiveColorMode = info.ActiveColorMode.ToString();
        result.ColorEncoding = info.ColorEncoding.ToString();
        result.BitsPerColorChannel = info.BitsPerColorChannel;
        return true;
    }

    private static MonitorAdvancedColorInfo GetAdvancedColorInfoLegacy(Monitor monitor, DisplayConfigPathInfo path) {
        DisplayConfigGetAdvancedColorInfo info = new() {
            Header = CreateTargetDeviceInfoHeader(
                DisplayConfigDeviceInfoType.GetAdvancedColorInfo,
                Marshal.SizeOf<DisplayConfigGetAdvancedColorInfo>(),
                path)
        };

        int error = MonitorNativeMethods.DisplayConfigGetAdvancedColorInfo(ref info);
        if (error != MonitorNativeMethods.DisplayConfigErrorSuccess) {
            throw new InvalidOperationException($"DisplayConfigGetDeviceInfo advanced color query failed. Error: {error}");
        }

        MonitorAdvancedColorInfo result = CreateLegacyAdvancedColorInfo(monitor, info);
        return result;
    }

    internal static MonitorAdvancedColorInfo CreateLegacyAdvancedColorInfo(Monitor monitor, DisplayConfigGetAdvancedColorInfo info) {
        MonitorAdvancedColorInfo result = CreateBaseAdvancedColorInfo(monitor);
        result.AdvancedColorSupported = info.AdvancedColorSupported;
        result.AdvancedColorEnabled = info.AdvancedColorEnabled;
        result.WideColorEnforced = info.WideColorEnforced;
        result.HdrSupported = IsLegacyHdrAdvancedColorState(info.AdvancedColorSupported, info.WideColorEnforced);
        result.HdrEnabled = IsLegacyHdrAdvancedColorState(info.AdvancedColorEnabled, info.WideColorEnforced);
        result.AdvancedColorLimitedByPolicy = info.AdvancedColorForceDisabled;
        result.ColorEncoding = info.ColorEncoding.ToString();
        result.BitsPerColorChannel = info.BitsPerColorChannel;
        return result;
    }

    private static bool IsLegacyHdrAdvancedColorState(bool advancedColorState, bool wideColorEnforced) {
        // Legacy packets do not split HDR from SDR/WCG Advanced Color. Wide-color enforcement identifies the WCG-only path.
        return advancedColorState && !wideColorEnforced;
    }

    private static bool TryGetSdrWhiteLevel(DisplayConfigPathInfo path, out uint sdrWhiteLevel) {
        DisplayConfigSdrWhiteLevel info = new() {
            Header = CreateTargetDeviceInfoHeader(
                DisplayConfigDeviceInfoType.GetSdrWhiteLevel,
                Marshal.SizeOf<DisplayConfigSdrWhiteLevel>(),
                path)
        };

        int error = MonitorNativeMethods.DisplayConfigGetSdrWhiteLevel(ref info);
        sdrWhiteLevel = info.SdrWhiteLevel;
        return error == MonitorNativeMethods.DisplayConfigErrorSuccess;
    }

    private static int SetHdrState(DisplayConfigPathInfo path, bool enabled) {
        DisplayConfigSetHdrState state = new() {
            Header = CreateTargetDeviceInfoHeader(
                DisplayConfigDeviceInfoType.SetHdrState,
                Marshal.SizeOf<DisplayConfigSetHdrState>(),
                path),
            Value = enabled ? 1u : 0u
        };

        return MonitorNativeMethods.DisplayConfigSetHdrState(ref state);
    }

    private static bool IsUnsupportedDeviceInfoPacketError(int error) {
        return error == MonitorNativeMethods.ErrorInvalidParameter ||
            error == MonitorNativeMethods.ErrorNotSupported;
    }

    private static int SetAdvancedColorState(DisplayConfigPathInfo path, bool enabled) {
        DisplayConfigSetAdvancedColorState state = new() {
            Header = CreateTargetDeviceInfoHeader(
                DisplayConfigDeviceInfoType.SetAdvancedColorState,
                Marshal.SizeOf<DisplayConfigSetAdvancedColorState>(),
                path),
            Value = enabled ? 1u : 0u
        };

        return MonitorNativeMethods.DisplayConfigSetAdvancedColorState(ref state);
    }

    private static MonitorAdvancedColorInfo CreateBaseAdvancedColorInfo(Monitor monitor) {
        return new MonitorAdvancedColorInfo {
            Index = monitor.Index,
            DeviceName = monitor.DeviceName,
            DeviceId = monitor.DeviceId,
            IsPrimary = monitor.IsPrimary
        };
    }

    private static DisplayConfigDeviceInfoHeader CreateTargetDeviceInfoHeader(
        DisplayConfigDeviceInfoType type,
        int size,
        DisplayConfigPathInfo path) {
        return CreateDeviceInfoHeader(type, size, path.TargetInfo.AdapterId, path.TargetInfo.Id);
    }

    private static DisplayConfigDeviceInfoHeader CreateDeviceInfoHeader(
        DisplayConfigDeviceInfoType type,
        int size,
        Luid adapterId,
        uint id) {
        return new DisplayConfigDeviceInfoHeader {
            Type = type,
            Size = (uint)size,
            AdapterId = adapterId,
            Id = id
        };
    }
}
