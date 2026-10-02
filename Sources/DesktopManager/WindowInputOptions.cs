namespace DesktopManager;

/// <summary>
/// Options controlling window input behavior.
/// </summary>
public sealed class WindowInputOptions {
    /// <summary>
    /// Gets or sets whether to activate the target window before sending input.
    /// </summary>
    public bool ActivateWindow { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to restore the previously active window after sending input.
    /// </summary>
    public bool RestoreFocus { get; set; }

    /// <summary>
    /// Gets or sets whether to preserve and restore clipboard text.
    /// </summary>
    public bool PreserveClipboard { get; set; }

    /// <summary>
    /// Gets or sets the number of clipboard open retries.
    /// </summary>
    public int ClipboardRetryCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the delay between clipboard retries in milliseconds.
    /// </summary>
    public int ClipboardRetryDelayMilliseconds { get; set; } = 50;

    /// <summary>
    /// Gets or sets the number of activation retries.
    /// </summary>
    public int ActivationRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between activation retries in milliseconds.
    /// </summary>
    public int ActivationRetryDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of retries when SendInput confirms that no input was delivered.
    /// Paste and native character messages are sent once; partial or timed-out mutations are not replayed.
    /// </summary>
    public int InputRetryCount { get; set; } = 2;

    /// <summary>
    /// Gets or sets the delay between characters in milliseconds.
    /// </summary>
    public int KeyDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets whether to use SendInput for typing (true) or WM_CHAR (false).
    /// </summary>
    public bool UseSendInput { get; set; } = true;

    /// <summary>
    /// Gets or sets whether typing must use real foreground keyboard input and fail instead of falling back to background messaging.
    /// </summary>
    public bool RequireForegroundWindowForTyping { get; set; }

    /// <summary>
    /// Gets or sets whether foreground typing should prefer layout-aware physical key presses over Unicode packet input.
    /// </summary>
    public bool UsePhysicalKeyboardLayout { get; set; }

    /// <summary>
    /// Gets or sets whether foreground typing should prefer a fixed US-style scancode map for hosted remote sessions.
    /// </summary>
    public bool UseHostedSessionScanCodes { get; set; }

    /// <summary>
    /// Gets or sets whether text should be delivered in script mode, preserving line boundaries and chunking long lines safely.
    /// </summary>
    public bool TypeTextAsScript { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of characters to send in each script chunk.
    /// </summary>
    public int ScriptChunkLength { get; set; } = 120;

    /// <summary>
    /// Gets or sets the delay in milliseconds to apply after each script line break.
    /// </summary>
    public int ScriptLineDelayMilliseconds { get; set; }
}
