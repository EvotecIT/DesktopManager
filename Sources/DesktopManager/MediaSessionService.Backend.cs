using System;
using System.IO;
using System.Threading.Tasks;

namespace DesktopManager;

// The native provider owns subscriptions; each read transfers ownership of its artwork stream.
internal interface IMediaSessionBackend : IDisposable {
    event EventHandler? Changed;
    long Version { get; }
    Task<MediaSessionServiceData?> GetCurrentAsync();
    Task<bool?> SendAsync(MediaSessionServiceCommand command);
}

internal enum MediaSessionServiceCommand { TogglePlayPause, Next, Previous }

internal sealed class MediaSessionServiceData : IDisposable {
    internal string Title { get; set; } = string.Empty;
    internal string Artist { get; set; } = string.Empty;
    internal string AlbumArtist { get; set; } = string.Empty;
    internal string SourceAppId { get; set; } = string.Empty;
    internal Stream? Artwork { get; set; }
    internal bool IsPlaying { get; set; }
    internal TimeSpan Position { get; set; }
    internal TimeSpan Duration { get; set; }
    internal DateTimeOffset PositionUpdated { get; set; }
    internal long Version { get; set; }

    public void Dispose() => Artwork?.Dispose();
}
