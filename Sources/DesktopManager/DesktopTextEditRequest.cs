namespace DesktopManager;

/// <summary>Specifies how text should be applied to an observed control.</summary>
public enum DesktopTextEditMode {
    /// <summary>Replace the entire editable value or document.</summary>
    ReplaceDocument,
    /// <summary>Replace the current selected text, or insert at the caret when the selection is empty.</summary>
    ReplaceSelection,
    /// <summary>Insert text at the current caret without selecting the document.</summary>
    InsertAtCaret
}

/// <summary>
/// Defines a safe text edit with an optional optimistic-concurrency precondition.
/// </summary>
public sealed class DesktopTextEditRequest {
    /// <summary>Gets or sets the replacement or inserted text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the edit mode.</summary>
    public DesktopTextEditMode Mode { get; set; } = DesktopTextEditMode.ReplaceDocument;

    /// <summary>Gets or sets an expected complete-content fingerprint. A mismatch prevents the edit.</summary>
    public string? ExpectedFingerprint { get; set; }

    /// <summary>Gets or sets the expected selection/caret context fingerprint for a range edit.</summary>
    public string? ExpectedEditContextFingerprint { get; set; }

    /// <summary>Gets or sets whether the target window should be prepared for foreground input.</summary>
    public bool EnsureForegroundWindow { get; set; }

    /// <summary>Gets or sets whether foreground input fallback is explicitly allowed.</summary>
    public bool AllowForegroundInputFallback { get; set; }

    /// <summary>Gets or sets whether the post-edit text should be observed and verified.</summary>
    public bool VerifyAfterEdit { get; set; } = true;

    /// <summary>Gets or sets the verification timeout in milliseconds.</summary>
    public int VerificationTimeoutMilliseconds { get; set; } = 2000;

    /// <summary>Gets or sets the verification interval in milliseconds.</summary>
    public int VerificationIntervalMilliseconds { get; set; } = 50;
}
