namespace DesktopManager;

/// <summary>
/// Identifies the direction of a Windows audio endpoint.
/// </summary>
public enum AudioDataFlow {
    /// <summary>An audio playback endpoint.</summary>
    Render = 0,
    /// <summary>An audio recording endpoint.</summary>
    Capture = 1,
    /// <summary>Playback and recording endpoints.</summary>
    All = 2
}
