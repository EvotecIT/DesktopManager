namespace DesktopManager;

/// <summary>An immutable reading of an endpoint's master volume, mute state, and signal level.</summary>
public sealed class AudioEndpointLevelInfo {
    internal AudioEndpointLevelInfo(float volumePercent, bool isMuted, float? peakValue) {
        VolumePercent = volumePercent;
        IsMuted = isMuted;
        PeakValue = peakValue;
    }

    /// <summary>Gets master volume from 0 through 100.</summary>
    public float VolumePercent { get; }

    /// <summary>Gets whether the endpoint is muted.</summary>
    public bool IsMuted { get; }

    /// <summary>Gets the signal peak from 0 through 1, or null when the endpoint has no meter.</summary>
    public float? PeakValue { get; }
}
