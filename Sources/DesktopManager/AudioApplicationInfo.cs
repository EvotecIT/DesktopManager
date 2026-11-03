namespace DesktopManager;

/// <summary>An immutable mixer reading for all audio sessions belonging to one application.</summary>
public sealed class AudioApplicationInfo {
    internal AudioApplicationInfo(string key, string name, int processId, float volumePercent,
        bool isMuted, float peakValue, bool isActive, bool isSystem) {
        Key = key;
        Name = name;
        ProcessId = processId;
        VolumePercent = volumePercent;
        IsMuted = isMuted;
        PeakValue = peakValue;
        IsActive = isActive;
        IsSystem = isSystem;
    }

    /// <summary>Gets the case-insensitive process-name key, or "system" for system sounds.</summary>
    public string Key { get; }

    /// <summary>Gets a readable session name, falling back to the process name.</summary>
    public string Name { get; }

    /// <summary>Gets a representative process ID, preferring an active session in this application.</summary>
    public int ProcessId { get; }

    /// <summary>Gets the highest session volume in the application, from 0 through 100.</summary>
    public float VolumePercent { get; }

    /// <summary>Gets whether every session in the application is muted.</summary>
    public bool IsMuted { get; }

    /// <summary>Gets the highest session peak, from 0 through 1. Sessions without a meter contribute zero.</summary>
    public float PeakValue { get; }

    /// <summary>Gets whether any session in the application is active.</summary>
    public bool IsActive { get; }

    /// <summary>Gets whether this is the system-sounds group.</summary>
    public bool IsSystem { get; }
}
