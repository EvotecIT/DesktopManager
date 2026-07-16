using System;

namespace DesktopManager;

/// <summary>
/// Provides the previous and current interactive-session snapshots.
/// </summary>
public sealed class DesktopSessionChangedEventArgs : EventArgs {
    /// <summary>Initializes session change data.</summary>
    /// <param name="previous">The previous session snapshot.</param>
    /// <param name="current">The current session snapshot.</param>
    public DesktopSessionChangedEventArgs(DesktopSessionInfo previous, DesktopSessionInfo current) {
        Previous = previous ?? throw new ArgumentNullException(nameof(previous));
        Current = current ?? throw new ArgumentNullException(nameof(current));
    }

    /// <summary>Gets the previous session snapshot.</summary>
    public DesktopSessionInfo Previous { get; }

    /// <summary>Gets the current session snapshot.</summary>
    public DesktopSessionInfo Current { get; }
}
