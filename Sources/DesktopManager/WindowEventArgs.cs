using System;

namespace DesktopManager;

/// <summary>
/// Describes one window change reported by <see cref="WindowEventWatcher"/>.
/// </summary>
public sealed class WindowEventArgs : EventArgs {
    /// <summary>Initializes a new instance of the <see cref="WindowEventArgs"/> class.</summary>
    /// <param name="kind">The single event that occurred.</param>
    /// <param name="handle">The window the event refers to.</param>
    public WindowEventArgs(WindowEventKinds kind, IntPtr handle) {
        Kind = kind;
        Handle = handle;
    }

    /// <summary>Gets the single event that occurred.</summary>
    public WindowEventKinds Kind { get; }

    /// <summary>
    /// Gets the window the event refers to. For <see cref="WindowEventKinds.Destroyed"/> the handle
    /// is no longer valid and serves only to forget state kept for it.
    /// </summary>
    public IntPtr Handle { get; }
}
