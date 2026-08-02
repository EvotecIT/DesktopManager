namespace DesktopManager;

/// <summary>
/// Reports a safe text edit, its concurrency decision, provider path, and verified observations.
/// </summary>
public sealed class DesktopTextEditResult {
    /// <summary>Gets or sets whether the requested edit was applied and verified as requested.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets whether an edit action was actually attempted.</summary>
    public bool Applied { get; set; }

    /// <summary>Gets or sets the provider or foreground method used.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets a stable failure code.</summary>
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable failure reason.</summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the optimistic-concurrency precondition matched.</summary>
    public bool PreconditionMatched { get; set; } = true;

    /// <summary>Gets or sets the observation captured before the edit.</summary>
    public DesktopControlObservation? Before { get; set; }

    /// <summary>Gets or sets the observation captured after the edit.</summary>
    public DesktopControlObservation? After { get; set; }
}
