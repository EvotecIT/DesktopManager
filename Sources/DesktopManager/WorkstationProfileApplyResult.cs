using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Reports a workstation profile application and any best-effort limitations.
/// </summary>
public sealed class WorkstationProfileApplyResult {
    internal WorkstationProfileApplyResult(bool succeeded, bool rolledBack, string? error, IReadOnlyList<string> warnings, bool restartRequired = false) {
        Succeeded = succeeded;
        RolledBack = rolledBack;
        Error = error;
        Warnings = warnings;
        RestartRequired = restartRequired;
    }

    /// <summary>Gets whether selected sections completed without a required restart. Inspect warnings for best-effort limitations.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets whether accepted display settings require a system restart before application completes.</summary>
    public bool RestartRequired { get; }

    /// <summary>Gets whether a pre-apply snapshot was restored after failure.</summary>
    public bool RolledBack { get; }

    /// <summary>Gets the failure message, or <c>null</c> after success.</summary>
    public string? Error { get; }

    /// <summary>Gets non-fatal limitations encountered during matching or optional device operations.</summary>
    public IReadOnlyList<string> Warnings { get; }
}
