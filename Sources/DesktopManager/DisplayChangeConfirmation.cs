namespace DesktopManager;

/// <summary>
/// This enumeration is used during display change confirmation process.
/// </summary>
public enum DisplayChangeConfirmation : int {
    /// <summary>
    /// The display change was successful.
    /// </summary>
    Successful = 0,
    /// <summary>
    /// The display change requires a restart.
    /// </summary>
    Restart = 1,
    /// <summary>
    /// The display change failed.
    /// </summary>
    Failed = -1,
    /// <summary>
    /// The display mode is not supported.
    /// </summary>
    BadMode = -2,
    /// <summary>
    /// The display was not updated.
    /// </summary>
    NotUpdated = -3,
    /// <summary>
    /// The display flags are invalid.
    /// </summary>
    BadFlags = -4,
    /// <summary>
    /// The display parameters are invalid.
    /// </summary>
    BadParam = -5,
    /// <summary>
    /// The dual view mode is invalid.
    /// </summary>
    BadDualView = -6
}
