using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Reports a workstation profile application and any best-effort limitations.
/// </summary>
public sealed class WorkstationProfileApplyResult {
    internal WorkstationProfileApplyResult(bool succeeded, bool rolledBack, string? error, IReadOnlyList<string> warnings) {
        Succeeded = succeeded;
        RolledBack = rolledBack;
        Error = error;
        Warnings = warnings;
    }

    /// <summary>Gets whether all selected profile sections completed.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets whether a pre-apply snapshot was restored after failure.</summary>
    public bool RolledBack { get; }

    /// <summary>Gets the failure message, or <c>null</c> after success.</summary>
    public string? Error { get; }

    /// <summary>Gets non-fatal limitations encountered during matching or optional device operations.</summary>
    public IReadOnlyList<string> Warnings { get; }
}
