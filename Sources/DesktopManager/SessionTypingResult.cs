namespace DesktopManager;

/// <summary>Why session typing stopped before the end of the text.</summary>
public enum SessionTypingAbortReason {
    /// <summary>All text was typed.</summary>
    None,
    /// <summary>The caller cancelled.</summary>
    Cancelled,
    /// <summary>Another window became the foreground window.</summary>
    ForegroundChanged,
    /// <summary>The focused control inside the target window changed.</summary>
    FocusChanged,
    /// <summary>The user pressed a key, clicked or scrolled.</summary>
    UserInput,
    /// <summary>Windows did not accept the input, for example because the desktop was locked or a secure prompt was showing.</summary>
    InputRejected
}

/// <summary>Outcome of a <see cref="SessionTypingService"/> call. It never contains the typed text.</summary>
public sealed class SessionTypingResult {
    internal SessionTypingResult(SessionTypingProfile profile, string keyboardLayout, int charactersTotal, int charactersSent, SessionTypingAbortReason abortReason) {
        Profile = profile;
        KeyboardLayout = keyboardLayout;
        CharactersTotal = charactersTotal;
        CharactersSent = charactersSent;
        AbortReason = abortReason;
    }

    /// <summary>Gets the profile used.</summary>
    public SessionTypingProfile Profile { get; }

    /// <summary>Gets the keyboard layout the keys were chosen for, or <c>Unicode</c> for the local profile.</summary>
    public string KeyboardLayout { get; }

    /// <summary>Gets the length of the text in UTF-16 code units.</summary>
    public int CharactersTotal { get; }

    /// <summary>
    /// Gets how many UTF-16 code units of the text were completely typed. When typing stops early, the text from this
    /// position on was not completely sent; the character at this position may have been partly sent before a stop
    /// condition or rejected input. Check the target before retrying a partial result.
    /// </summary>
    public int CharactersSent { get; }

    /// <summary>Gets why typing stopped early, or <see cref="SessionTypingAbortReason.None"/>.</summary>
    public SessionTypingAbortReason AbortReason { get; }

    /// <summary>Gets whether the whole text was typed.</summary>
    public bool Completed => AbortReason == SessionTypingAbortReason.None;
}
