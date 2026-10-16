namespace DesktopManager;

/// <summary>The Windows mechanism used to set a lock screen image.</summary>
public enum LogonWallpaperMechanism {
    /// <summary>No image or policy value was set.</summary>
    None,
    /// <summary>The current account's Windows Runtime lock screen API completed.</summary>
    WindowsRuntime,
    /// <summary>The machine's LockScreenImage policy value was written.</summary>
    PersonalizationPolicy
}

/// <summary>Reports a lock screen operation without confusing a policy write with a visible change.</summary>
public sealed class LogonWallpaperResult {
    /// <summary>Whether the API completed or the policy value was written. Windows may still enforce another policy.</summary>
    public bool Succeeded { get; }

    /// <summary>The mechanism that completed, or None when the operation failed.</summary>
    public LogonWallpaperMechanism Mechanism { get; }

    /// <summary>The failure message, or null on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Why the per-user API failed, including when a subsequent policy write succeeded.</summary>
    public string? WindowsRuntimeError { get; }

    internal LogonWallpaperResult(bool succeeded, LogonWallpaperMechanism mechanism, string? errorMessage, string? windowsRuntimeError) {
        Succeeded = succeeded;
        Mechanism = mechanism;
        ErrorMessage = errorMessage;
        WindowsRuntimeError = windowsRuntimeError;
    }
}
