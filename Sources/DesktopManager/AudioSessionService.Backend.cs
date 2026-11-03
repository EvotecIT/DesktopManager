using System;
using System.Collections.Generic;

namespace DesktopManager;

internal interface IAudioSessionBackend : IDisposable {
    // Ownership of every returned session transfers to the caller. Failed enumeration releases partial results.
    IReadOnlyList<IAudioApplicationSession> OpenSessions();
}

internal interface IAudioApplicationSession : IDisposable {
    bool IsExpired { get; }
    AudioApplicationSessionState? Read();
    float GetPeak();
    void SetVolume(float scalar);
    void SetMute(bool muted);
}

internal sealed class AudioApplicationSessionState {
    internal AudioApplicationSessionState(int processId, string processName, string? displayName,
        float volumePercent, bool isMuted, float peakValue, bool isActive, bool isSystem) {
        ProcessId = processId;
        ProcessName = processName;
        DisplayName = displayName;
        VolumePercent = volumePercent;
        IsMuted = isMuted;
        PeakValue = peakValue;
        IsActive = isActive;
        IsSystem = isSystem;
    }

    internal int ProcessId { get; }
    internal string ProcessName { get; }
    internal string? DisplayName { get; }
    internal float VolumePercent { get; }
    internal bool IsMuted { get; }
    internal float PeakValue { get; }
    internal bool IsActive { get; }
    internal bool IsSystem { get; }
}
