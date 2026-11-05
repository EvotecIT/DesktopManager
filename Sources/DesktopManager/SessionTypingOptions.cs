using System;

namespace DesktopManager;

/// <summary>Kind of session that receives typed text, which selects how keys are sent and how fast.</summary>
public enum SessionTypingProfile {
    /// <summary>A local window. Characters are sent as Unicode packets and are independent of keyboard layouts.</summary>
    Local,
    /// <summary>A Remote Desktop (RDP) client. Characters are sent as scan codes for the remote session's layout.</summary>
    Rdp,
    /// <summary>A Citrix session. Characters are sent as scan codes for the remote session's layout, slightly slower than RDP.</summary>
    Citrix,
    /// <summary>A VNC or Apple Remote Desktop viewer. Characters are sent as scan codes for the local layout with longer holds.</summary>
    Vnc
}

/// <summary>How line breaks (CR, LF or CRLF) in the text are typed.</summary>
public enum SessionLineBreakMode {
    /// <summary>Each line break presses Enter once.</summary>
    Enter,
    /// <summary>Each line break presses Shift+Enter, which adds a line without submitting in many chat and form fields.</summary>
    ShiftEnter,
    /// <summary>Line breaks are not typed.</summary>
    None
}

/// <summary>What happens to a character the selected keyboard layout cannot produce.</summary>
public enum UnmappedCharacterPolicy {
    /// <summary>Refuse to start typing.</summary>
    Fail,
    /// <summary>Send the character as a Unicode packet. RDP accepts these; many VNC viewers and some Citrix setups do not.</summary>
    Unicode,
    /// <summary>Leave the character out.</summary>
    Skip
}

/// <summary>What to do when Caps Lock is on before scan-code typing starts.</summary>
public enum CapsLockPolicy {
    /// <summary>Refuse to start typing.</summary>
    Fail,
    /// <summary>Turn Caps Lock off for the duration of typing and turn it back on afterwards.</summary>
    TurnOff,
    /// <summary>Keep Caps Lock on and press Shift the other way for keys Caps Lock affects.</summary>
    Compensate
}

/// <summary>Options for <see cref="SessionTypingService"/>.</summary>
/// <remarks>
/// Pacing values left as <c>null</c> use the defaults of <see cref="Profile"/>. The service never changes this object.
/// </remarks>
public sealed class SessionTypingOptions {
    /// <summary>Gets or sets the kind of session that receives the text.</summary>
    public SessionTypingProfile Profile { get; set; } = SessionTypingProfile.Rdp;

    /// <summary>
    /// Gets or sets the keyboard layout the receiving session uses, as a layout identifier such as <c>00000407</c> or a culture
    /// name such as <c>de-DE</c>. When empty, the layout active on the target window's thread is used.
    /// Ignored by <see cref="SessionTypingProfile.Local"/>, which sends Unicode packets.
    /// </summary>
    public string? KeyboardLayout { get; set; }

    /// <summary>Gets or sets how line breaks are typed.</summary>
    public SessionLineBreakMode LineBreak { get; set; } = SessionLineBreakMode.Enter;

    /// <summary>Gets or sets whether leading spaces and tabs are left out of every line, for editors that indent on their own.</summary>
    public bool StripLeadingIndentation { get; set; }

    /// <summary>Gets or sets what happens to characters the layout cannot produce.</summary>
    public UnmappedCharacterPolicy UnmappedCharacters { get; set; } = UnmappedCharacterPolicy.Fail;

    /// <summary>Gets or sets what happens when Caps Lock is on before scan-code typing starts.</summary>
    public CapsLockPolicy CapsLock { get; set; } = CapsLockPolicy.Fail;

    /// <summary>Gets or sets how long to wait for the target window to become the foreground window. Zero checks once.</summary>
    public TimeSpan ForegroundTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets whether to bring the target to the foreground first. Only allowed with <see cref="SessionTypingProfile.Local"/>;
    /// remote session clients must be focused by the user.
    /// </summary>
    public bool ActivateWindow { get; set; }

    /// <summary>Gets or sets how long to wait for physically held Ctrl, Alt, Shift or Windows keys to be released.</summary>
    public TimeSpan ModifierReleaseTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets whether typing stops when the focused control inside the target window changes.</summary>
    public bool StopWhenFocusChanges { get; set; } = true;

    /// <summary>Gets or sets whether typing stops when the user presses a key, clicks or scrolls.</summary>
    public bool StopOnUserInput { get; set; } = true;

    /// <summary>Gets or sets the delay after each character in milliseconds.</summary>
    public int? KeyDelayMilliseconds { get; set; }

    /// <summary>Gets or sets how long Shift or AltGr is held before and after the key it modifies, in milliseconds.</summary>
    public int? ModifierHoldMilliseconds { get; set; }

    /// <summary>Gets or sets the delay after each typed line break in milliseconds.</summary>
    public int? LineDelayMilliseconds { get; set; }

    /// <summary>Gets or sets how many characters are typed before pausing. Zero types without pausing.</summary>
    public int? ChunkSize { get; set; }

    /// <summary>Gets or sets the pause after each chunk in milliseconds.</summary>
    public int? ChunkPauseMilliseconds { get; set; }
}
