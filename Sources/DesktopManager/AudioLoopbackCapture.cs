using System;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>Captures what the PC is playing on its default output through WASAPI loopback.</summary>
/// <remarks>
/// <para>
/// Loopback is the supported way to capture the output mix: it needs no driver and no permission prompt.
/// After <see cref="Start"/>, capture runs on its own multithreaded-apartment thread, so callers never initialize COM.
/// </para>
/// <para>
/// The capture follows the default render endpoint for <see cref="Role"/>: when Windows assigns another default
/// or the endpoint goes away, it reopens the new default. While no output endpoint is available it retries
/// about once a second and <see cref="IsCapturing"/> is false. If endpoint notifications cannot be registered,
/// capture reopens about once a second until registration succeeds; those retries can leave brief gaps.
/// Windows delivers no packets while nothing plays.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class AudioLoopbackCapture : IDisposable {
    private readonly object _gate = new();
    private CaptureRun? _run;
    private bool _disposed;

    /// <summary>Prepares a loopback capture of the default output for <paramref name="role"/>. Call <see cref="Start"/> to begin.</summary>
    /// <param name="role">The default render endpoint role to follow.</param>
    /// <exception cref="ArgumentOutOfRangeException">The role is invalid.</exception>
    public AudioLoopbackCapture(AudioRole role = AudioRole.Multimedia) {
        AudioService.ToNativeRole(role);
        Role = role;
    }

    /// <summary>Gets the default render endpoint role this capture follows.</summary>
    public AudioRole Role { get; }

    /// <summary>Gets whether a loopback stream is open on an output endpoint now.</summary>
    public bool IsCapturing {
        get {
            CaptureRun? run = Volatile.Read(ref _run);
            return run != null && Volatile.Read(ref run.Capturing) != 0;
        }
    }

    /// <summary>Raised on the capture thread for each captured packet.</summary>
    /// <remarks>
    /// Handlers run synchronously on the capture thread and delay the next packet, so keep them short.
    /// The sample buffer is reused; see <see cref="AudioSamplesCapturedEventArgs"/>. Exceptions thrown by
    /// handlers are reported to diagnostics tracing and do not stop the capture.
    /// </remarks>
    public event EventHandler<AudioSamplesCapturedEventArgs>? SamplesCaptured;

    /// <summary>Starts capturing on a new capture thread. Has no effect while already started.</summary>
    /// <exception cref="ObjectDisposedException">The capture has been disposed.</exception>
    public void Start() {
        lock (_gate) {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(AudioLoopbackCapture));
            }
            if (_run != null) {
                return;
            }
            var run = new CaptureRun();
            run.Thread = new Thread(() => {
                try {
                    Run(run);
                } finally {
                    lock (_gate) {
                        if (ReferenceEquals(_run, run)) {
                            Volatile.Write(ref _run, null);
                        }
                    }
                }
            }) { IsBackground = true, Name = "DesktopManager loopback capture" };
            run.Thread.SetApartmentState(ApartmentState.MTA);
            Volatile.Write(ref _run, run);
            run.Thread.Start();
        }
    }

    /// <summary>Stops capturing and waits for the capture thread to finish. Has no effect while stopped.</summary>
    /// <remarks>
    /// After Stop returns no further packets are raised. Do not call Stop from a thread a running handler is waiting on,
    /// such as a UI thread the handler invokes synchronously. Called from a handler, Stop returns at once and
    /// the capture ends after that handler.
    /// </remarks>
    public void Stop() {
        CaptureRun? run;
        lock (_gate) {
            run = _run;
            if (run == null) {
                return;
            }
            run.RequestStop();
        }
        if (run.Thread != Thread.CurrentThread) {
            run.Thread!.Join();
        }
    }

    /// <summary>Stops capturing and prevents restarting. Repeated calls have no effect.</summary>
    public void Dispose() {
        lock (_gate) {
            _disposed = true;
        }
        Stop();
    }

    /// <summary>Owns one capture thread and its stop/default-change signals.</summary>
    internal sealed class CaptureRun {
        private readonly object _signal = new();
        private bool _signalled;
        internal Thread? Thread;
        internal int Capturing;
        internal int DefaultChanged;
        internal volatile bool Stopping;

        internal void RequestStop() {
            Stopping = true;
            Wake();
        }

        internal void Wake() {
            lock (_signal) {
                _signalled = true;
                System.Threading.Monitor.PulseAll(_signal);
            }
        }

        /// <summary>Waits until woken or the timeout passes; a wake that arrived earlier ends the wait at once.</summary>
        internal void Wait(int milliseconds) {
            lock (_signal) {
                if (!_signalled) {
                    System.Threading.Monitor.Wait(_signal, milliseconds);
                }
                _signalled = false;
            }
        }
    }
}
