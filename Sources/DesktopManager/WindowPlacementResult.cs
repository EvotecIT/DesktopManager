using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Describes the observed result of a window-placement operation.
/// </summary>
public sealed class WindowPlacementResult {
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowPlacementResult"/> class.
    /// </summary>
    /// <param name="requestedHandle">The originally requested handle.</param>
    /// <param name="resolvedHandle">The root top-level handle used for movement.</param>
    /// <param name="window">The final observed window.</param>
    /// <param name="verified">Whether the final observed state matched the request.</param>
    /// <param name="attempts">The number of attempts performed.</param>
    /// <param name="snapshots">Execution snapshots captured during movement.</param>
    public WindowPlacementResult(
        IntPtr requestedHandle,
        IntPtr resolvedHandle,
        WindowInfo window,
        bool verified,
        int attempts,
        IReadOnlyList<WindowPlacementSnapshot> snapshots) {
        RequestedHandle = requestedHandle;
        ResolvedHandle = resolvedHandle;
        Window = window ?? throw new ArgumentNullException(nameof(window));
        Verified = verified;
        Attempts = attempts;
        Snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
    }

    /// <summary>Gets the originally requested handle.</summary>
    public IntPtr RequestedHandle { get; }

    /// <summary>Gets the root top-level handle used for movement.</summary>
    public IntPtr ResolvedHandle { get; }

    /// <summary>Gets the final observed window.</summary>
    public WindowInfo Window { get; }

    /// <summary>Gets whether the final observed state matched the request.</summary>
    public bool Verified { get; }

    /// <summary>Gets the number of attempts performed.</summary>
    public int Attempts { get; }

    /// <summary>Gets execution snapshots captured during movement.</summary>
    public IReadOnlyList<WindowPlacementSnapshot> Snapshots { get; }
}
