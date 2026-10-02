using System;

namespace DesktopManager;

/// <summary>Reports whether Windows applied a display request immediately or requires a restart.</summary>
public sealed class DisplayModeApplyResult {
    internal DisplayModeApplyResult(bool restartRequired) { RestartRequired = restartRequired; }
    /// <summary>Gets whether Windows accepted the request but requires a system restart.</summary>
    public bool RestartRequired { get; }
    /// <summary>Gets whether Windows reported immediate application.</summary>
    public bool Applied => !RestartRequired;
}

/// <summary>Indicates that a display request was accepted but requires a system restart.</summary>
public sealed class DisplayRestartRequiredException : InvalidOperationException {
    /// <summary>Creates an exception for an accepted request that cannot be reported as immediately applied.</summary>
    public DisplayRestartRequiredException() : base("Windows accepted the display settings, but a system restart is required to apply them.") { }
}
