namespace DesktopManager.App.Core;

/// <summary>
/// Placement identifiers used by window-management hotkey actions.
/// </summary>
public static class WindowPlacements {
    /// <summary>Maximize the target window.</summary>
    public const string Maximize = "Maximize";

    /// <summary>Place the target window in the left half of the target monitor.</summary>
    public const string LeftHalf = "LeftHalf";

    /// <summary>Place the target window in the right half of the target monitor.</summary>
    public const string RightHalf = "RightHalf";

    /// <summary>Restore the target window without changing monitor placement.</summary>
    public const string Restore = "Restore";

    /// <summary>Move the target window to a fixed rectangle.</summary>
    public const string ExactRectangle = "ExactRectangle";
}
