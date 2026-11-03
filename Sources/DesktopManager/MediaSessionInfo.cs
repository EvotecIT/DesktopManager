using System;

namespace DesktopManager;

/// <summary>An immutable snapshot of the current Windows media session.</summary>
public sealed class MediaSessionInfo {
    private readonly byte[]? _artworkData;

    internal MediaSessionInfo(string title, string artist, string sourceAppId, string sourceName,
        byte[]? artworkData, bool isPlaying, TimeSpan position, TimeSpan duration, DateTimeOffset positionUpdated) {
        Title = title;
        Artist = artist;
        SourceAppId = sourceAppId;
        SourceName = sourceName;
        _artworkData = artworkData == null ? null : (byte[])artworkData.Clone();
        IsPlaying = isPlaying;
        Position = position;
        Duration = duration;
        PositionUpdated = positionUpdated;
    }

    /// <summary>Gets the media title supplied by the source.</summary>
    public string Title { get; }
    /// <summary>Gets the artist, falling back to the album artist when necessary.</summary>
    public string Artist { get; }
    /// <summary>Gets the source application's Windows app user model identifier.</summary>
    public string SourceAppId { get; }
    /// <summary>Gets a display name derived from the source identifier.</summary>
    public string SourceName { get; }
    /// <summary>Gets a copy of the encoded artwork bytes, or null when absent, unreadable, or larger than 16 MiB.</summary>
    public byte[]? ArtworkData => _artworkData == null ? null : (byte[])_artworkData.Clone();
    /// <summary>Gets whether Windows reports that the source is playing.</summary>
    public bool IsPlaying { get; }
    /// <summary>Gets the position reported by Windows, without extrapolation.</summary>
    public TimeSpan Position { get; }
    /// <summary>Gets the nonnegative length of the reported timeline.</summary>
    public TimeSpan Duration { get; }
    /// <summary>Gets the time at which Windows last updated the position.</summary>
    public DateTimeOffset PositionUpdated { get; }
}
