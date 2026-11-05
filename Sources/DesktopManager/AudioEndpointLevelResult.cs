namespace DesktopManager;

/// <summary>Reports an endpoint reading, or why no default endpoint could be read.</summary>
public sealed class AudioEndpointLevelResult : DesktopOperationResult {
    internal AudioEndpointLevelResult(AudioEndpointLevelInfo level)
        : base(DesktopOperationStatus.Success, "The default audio endpoint was read.") {
        Level = level;
    }

    internal AudioEndpointLevelResult(DesktopOperationStatus status, string message)
        : base(status, message) {
    }

    /// <summary>Gets the reading when <see cref="DesktopOperationResult.Succeeded"/> is true; otherwise null.</summary>
    public AudioEndpointLevelInfo? Level { get; }
}
