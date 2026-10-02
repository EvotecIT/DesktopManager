using System.Collections.Generic;

namespace DesktopManager;

/// <summary>A snapshot of a process's UI Automation worker and queued work.</summary>
public sealed class UiAutomationProviderHealth {
    internal UiAutomationProviderHealth(uint processId, int pendingCount, bool isBusy, bool isBlocked) {
        ProcessId = processId;
        PendingCount = pendingCount;
        IsBusy = isBusy;
        IsBlocked = isBlocked;
    }
    /// <summary>Gets the target process identifier.</summary>
    public uint ProcessId { get; }
    /// <summary>Gets the number of queued operations.</summary>
    public int PendingCount { get; }
    /// <summary>Gets whether work is executing or queued.</summary>
    public bool IsBusy { get; }
    /// <summary>Gets whether a timed-out native operation is still executing.</summary>
    public bool IsBlocked { get; }
}

/// <summary>Provides read-only health information for the library's provider workers.</summary>
public static class DesktopAutomationDiagnostics {
    /// <summary>Returns the current UI Automation worker states without calling providers.</summary>
    public static IReadOnlyList<UiAutomationProviderHealth> GetUiAutomationHealth() {
        return UiAutomationControlService.Dispatchers.GetHealth();
    }
}
