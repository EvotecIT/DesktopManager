namespace DesktopManager;

/// <summary>
/// Selects workstation profile sections and failure behavior.
/// </summary>
public sealed class WorkstationProfileApplyOptions {
    /// <summary>Gets or sets whether every saved monitor must be connected.</summary>
    public bool RequireAllMonitors { get; set; } = true;

    /// <summary>Gets or sets whether display modes, positions, HDR, brightness, and wallpapers are applied.</summary>
    public bool ApplyDisplays { get; set; } = true;

    /// <summary>Gets or sets whether active endpoint volume, mute, and default roles are applied.</summary>
    public bool ApplyAudio { get; set; } = true;

    /// <summary>Gets or sets whether personalization state is restored.</summary>
    public bool ApplyPersonalization { get; set; } = true;

    /// <summary>Gets or sets whether machine-wide personalization policies are restored.</summary>
    public bool ApplyMachinePolicies { get; set; }

    /// <summary>Gets or sets whether taskbar visibility, edge, and auto-hide are applied.</summary>
    public bool ApplyTaskbars { get; set; } = true;

    /// <summary>Gets or sets whether DesktopManager attempts to restore the pre-apply state after a failure.</summary>
    public bool RollbackOnFailure { get; set; } = true;
}
