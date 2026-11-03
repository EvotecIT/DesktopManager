using System.Collections.Generic;

namespace DesktopManager;

/// <summary>Reports whether device usage could be read and the applications currently recorded as active.</summary>
public sealed class DeviceUsageResult : DesktopOperationResult {
    internal DeviceUsageResult(DesktopOperationStatus status, string message, IReadOnlyList<DeviceUsageInfo> applications)
        : base(status, message) {
        Applications = applications;
    }

    /// <summary>Gets active applications with case-insensitively distinct friendly names; empty when reading is unavailable or unsupported.</summary>
    public IReadOnlyList<DeviceUsageInfo> Applications { get; }
}
