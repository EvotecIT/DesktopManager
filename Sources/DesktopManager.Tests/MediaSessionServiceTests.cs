using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DesktopManager.Tests;

[TestClass]
public sealed class MediaSessionServiceTests {
    [TestMethod]
    public async Task Snapshot_MapsMetadataBoundsArtworkAndRoutesTransportWithExplicitUnsupported() {
        byte[] encoded = { 0x89, 0x50, 0x4e, 0x47, 1, 2, 3 };
        var updated = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
        var artwork = new MemoryStream(encoded, writable: false);
        var backend = new FakeBackend {
            Read = () => Task.FromResult<MediaSessionServiceData?>(new MediaSessionServiceData {
                Title = "A track", Artist = " ", AlbumArtist = "Album artist",
                SourceAppId = @"C:\Music\player.exe", Artwork = artwork,
                IsPlaying = true, Position = TimeSpan.FromSeconds(12), Duration = TimeSpan.FromMinutes(3),
                PositionUpdated = updated
            })
        };
        using var service = new MediaSessionService(() => Task.FromResult<IMediaSessionBackend?>(backend));
        MediaSessionResult result = await service.GetCurrentAsync();
        Assert.AreEqual(DesktopOperationStatus.Success, result.Status);
        MediaSessionInfo session = result.Session ?? throw new AssertFailedException("Expected the active session.");
        Assert.AreEqual("A track", session.Title);
        Assert.AreEqual("Album artist", session.Artist);
        Assert.AreEqual(@"C:\Music\player.exe", session.SourceAppId);
        Assert.AreEqual("Player", session.SourceName);
        Assert.IsTrue(session.IsPlaying);
        Assert.AreEqual(TimeSpan.FromSeconds(12), session.Position);
        Assert.AreEqual(TimeSpan.FromMinutes(3), session.Duration);
        Assert.AreEqual(updated, session.PositionUpdated);
        CollectionAssert.AreEqual(encoded, session.ArtworkData);
        Assert.IsFalse(artwork.CanRead, "The snapshot read owns and closes the provider stream.");
        byte[] exposed = session.ArtworkData!;
        exposed[0] = 0;
        CollectionAssert.AreEqual(encoded, session.ArtworkData, "Callers cannot mutate stored artwork.");

        // An unseekable source cannot bypass the encoded-byte limit or be read without bound.
        var oversized = new GeneratedArtworkStream(16 * 1024 * 1024 + 100);
        backend.Read = () => Task.FromResult<MediaSessionServiceData?>(new MediaSessionServiceData {
            Title = "Large artwork", Artist = "Track artist", AlbumArtist = "Unused", Artwork = oversized,
            SourceAppId = "Vendor.App!music", Duration = TimeSpan.FromSeconds(-1)
        });
        var bounded = (await service.GetCurrentAsync()).Session ?? throw new AssertFailedException("Metadata must survive oversized artwork.");
        Assert.AreEqual("Large artwork", bounded.Title);
        Assert.AreEqual("Track artist", bounded.Artist);
        Assert.AreEqual("Music", bounded.SourceName);
        Assert.AreEqual(TimeSpan.Zero, bounded.Duration);
        Assert.IsNull(bounded.ArtworkData);
        Assert.AreEqual(16 * 1024 * 1024 + 1, oversized.BytesRead);
        Assert.IsTrue(oversized.Disposed);

        Assert.IsTrue((await service.TogglePlayPauseAsync()).Succeeded);
        Assert.IsTrue((await service.NextAsync()).Succeeded);
        Assert.IsTrue((await service.PreviousAsync()).Succeeded);
        CollectionAssert.AreEqual(new[] { MediaSessionServiceCommand.TogglePlayPause, MediaSessionServiceCommand.Next,
            MediaSessionServiceCommand.Previous }, backend.Commands);
        backend.Accepted = false;
        Assert.AreEqual(DesktopOperationStatus.Failed, (await service.NextAsync()).Status);
        backend.Accepted = null;
        Assert.AreEqual(DesktopOperationStatus.Unavailable, (await service.PreviousAsync()).Status);
        backend.Read = () => Task.FromResult<MediaSessionServiceData?>(null);
        var empty = await service.GetCurrentAsync();
        Assert.IsTrue(empty.Succeeded);
        Assert.IsNull(empty.Session);

        using var unsupported = new MediaSessionService(() => Task.FromResult<IMediaSessionBackend?>(null));
        Assert.AreEqual(DesktopOperationStatus.Unsupported, (await unsupported.GetCurrentAsync()).Status);
        Assert.AreEqual(DesktopOperationStatus.Unsupported, (await unsupported.TogglePlayPauseAsync()).Status);
        Assert.AreEqual(DesktopOperationStatus.Unsupported, (await unsupported.NextAsync()).Status);
        Assert.AreEqual(DesktopOperationStatus.Unsupported, (await unsupported.PreviousAsync()).Status);
#if NET472 || NETSTANDARD2_0
        using var platform = new MediaSessionService();
        Assert.AreEqual(DesktopOperationStatus.Unsupported, (await platform.GetCurrentAsync()).Status);
        Assert.AreEqual(DesktopOperationStatus.Unsupported, (await platform.NextAsync()).Status);
#endif
    }

    [TestMethod]
    public async Task ConcurrentReads_InitializeOnceIsolateSubscribersAndRejectStaleSnapshots() {
        var initialization = new TaskCompletionSource<IMediaSessionBackend?>(TaskCreationOptions.RunContinuationsAsynchronously);
        int creations = 0;
        var backend = new FakeBackend();
        using var service = new MediaSessionService(() => { creations++; return initialization.Task; });
        Assert.AreEqual(0, creations, "Construction is lazy.");
        var first = service.GetCurrentAsync();
        var second = service.GetCurrentAsync();
        Assert.AreEqual(1, creations);
        initialization.SetResult(backend);
        Assert.IsTrue((await first).Succeeded);
        Assert.IsTrue((await second).Succeeded);
        Assert.AreEqual(1, creations);
        int notifications = 0;
        service.Changed += (_, _) => throw new InvalidOperationException("Host callback failed.");
        service.Changed += (_, _) => notifications++;
        backend.Signal();
        Assert.AreEqual(1, notifications);

        var artworkRead = new TaskCompletionSource<MediaSessionServiceData?>(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.Read = () => artworkRead.Task;
        var stale = service.GetCurrentAsync();
        backend.Version = 1;
        backend.Signal();
        var artwork = new MemoryStream(new byte[] { 1 });
        artworkRead.SetResult(new MediaSessionServiceData { Version = 0, Artwork = artwork });
        var result = await stale;
        Assert.AreEqual(DesktopOperationStatus.Unavailable, result.Status);
        Assert.IsNull(result.Session);
        Assert.IsFalse(artwork.CanRead);

        service.Dispose();
        Assert.IsTrue(backend.Disposed);
        Assert.IsFalse(backend.HasSubscribers);
        backend.Signal();
        Assert.AreEqual(2, notifications);
        Assert.AreEqual(DesktopOperationStatus.Unavailable, (await service.NextAsync()).Status);
        Assert.AreEqual(0, backend.Commands.Count);
    }

    [TestMethod]
    public async Task Dispose_DuringInitializationOrReadRetiresProviderWithoutPublishingLateData() {
        var initialization = new TaskCompletionSource<IMediaSessionBackend?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var backend = new FakeBackend();
        using var initializing = new MediaSessionService(() => initialization.Task);
        var pending = initializing.GetCurrentAsync();
        initializing.Dispose();
        initialization.SetResult(backend);
        Assert.AreEqual(DesktopOperationStatus.Unavailable, (await pending).Status);
        Assert.IsTrue(backend.Disposed);
        Assert.IsFalse(backend.HasSubscribers);
        Assert.AreEqual(0, backend.Reads);

        var read = new TaskCompletionSource<MediaSessionServiceData?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var readingBackend = new FakeBackend { Read = () => read.Task };
        using var reading = new MediaSessionService(() => Task.FromResult<IMediaSessionBackend?>(readingBackend));
        var snapshot = reading.GetCurrentAsync();
        reading.Dispose();
        Assert.IsFalse(readingBackend.Disposed, "An in-flight read retains the native provider.");
        var stream = new MemoryStream(new byte[] { 2, 3 });
        read.SetResult(new MediaSessionServiceData { Title = "Late", Artwork = stream });
        var disposed = await snapshot;
        Assert.AreEqual(DesktopOperationStatus.Unavailable, disposed.Status);
        Assert.IsNull(disposed.Session);
        Assert.IsTrue(readingBackend.Disposed);
        Assert.IsFalse(readingBackend.HasSubscribers);
        Assert.IsFalse(stream.CanRead);
        reading.Dispose();
    }

    private sealed class FakeBackend : IMediaSessionBackend {
        public event EventHandler? Changed;
        public long Version { get; set; }
        internal Func<Task<MediaSessionServiceData?>> Read { get; set; } = () => Task.FromResult<MediaSessionServiceData?>(null);
        internal List<MediaSessionServiceCommand> Commands { get; } = new();
        internal bool? Accepted { get; set; } = true;
        internal bool Disposed { get; private set; }
        internal bool HasSubscribers => Changed != null;
        internal int Reads { get; private set; }
        public Task<MediaSessionServiceData?> GetCurrentAsync() {
            Reads++;
            return Read();
        }
        public Task<bool?> SendAsync(MediaSessionServiceCommand command) {
            Commands.Add(command);
            return Task.FromResult(Accepted);
        }
        internal void Signal() => Changed?.Invoke(this, EventArgs.Empty);
        public void Dispose() => Disposed = true;
    }

    private sealed class GeneratedArtworkStream : Stream {
        private readonly int _length;
        internal GeneratedArtworkStream(int length) => _length = length;
        internal int BytesRead { get; private set; }
        internal bool Disposed { get; private set; }
        public override bool CanRead => !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) {
            int read = Math.Min(count, _length - BytesRead);
            Array.Clear(buffer, offset, read);
            BytesRead += read;
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => Task.FromResult(Read(buffer, offset, count));
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
