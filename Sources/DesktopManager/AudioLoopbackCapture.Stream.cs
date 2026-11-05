using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager;

// The capture thread. Every Core Audio reference below is created, used and released on that thread.
public sealed partial class AudioLoopbackCapture {
    private const int PollMilliseconds = 10;
    private const int UnavailableRetryMilliseconds = 1000;
    private const int FailureRetryMilliseconds = 5000;

    private void Run(CaptureRun run) {
        try {
            AudioComThread.EnterApartment();
        } catch (Exception exception) {
            DesktopManagerDiagnostics.Report($"Loopback capture could not enter a COM apartment: {exception.Message}");
            return;
        }
        IDisposable? watcher = null;
        try {
            while (!run.Stopping) {
                watcher ??= TryWatchDefault(run);
                Volatile.Write(ref run.DefaultChanged, 0);
                int retry = CaptureUntilInterrupted(run);
                if (!run.Stopping && retry > 0) {
                    run.Wait(retry);
                }
            }
        } finally {
            try {
                watcher?.Dispose();
            } catch (Exception exception) when (exception is COMException || exception is UnauthorizedAccessException) {
                DesktopManagerDiagnostics.Report($"Loopback capture could not stop following the default endpoint: {exception.Message}");
            }
            AudioComThread.LeaveApartment();
        }
    }

    private IDisposable? TryWatchDefault(CaptureRun run) {
        try {
            var watcher = new AudioEndpointWatcher();
            watcher.Changed += (_, change) => {
                if (AudioEndpointAvailability.IsDefaultChange(change, AudioDataFlow.Render, Role)) {
                    Volatile.Write(ref run.DefaultChanged, 1);
                    run.Wake();
                }
            };
            return watcher;
        } catch (Exception exception) when (exception is COMException || exception is UnauthorizedAccessException) {
            // Without notifications the capture still reopens when its endpoint is invalidated.
            DesktopManagerDiagnostics.Report($"Loopback capture cannot follow default endpoint changes: {exception.Message}");
            return null;
        }
    }

    /// <summary>Captures until stopped, the default changes, or the stream fails; returns the delay before reopening.</summary>
    private int CaptureUntilInterrupted(CaptureRun run) {
        IMMDevice? device = null;
        IAudioClient? client = null;
        IAudioCaptureClient? capture = null;
        bool started = false;
        try {
            device = CoreAudioSessionsNative.OpenDefaultDevice(AudioDataFlow.Render, Role);
            client = Take<IAudioClient>(CoreAudioSessionsNative.Activate(device, CoreAudioCaptureNative.AudioClientInterfaceId, out IntPtr clientPointer), clientPointer);
            if (!TryInitialize(client, out AudioCaptureFormat format)) {
                return FailureRetryMilliseconds;
            }
            Guid captureInterface = CoreAudioCaptureNative.CaptureClientInterfaceId;
            int result = client.GetService(ref captureInterface, out IntPtr capturePointer);
            capture = Take<IAudioCaptureClient>(result, capturePointer);
            Marshal.ThrowExceptionForHR(client.Start());
            started = true;
            Volatile.Write(ref run.Capturing, 1);
            Pump(run, capture, format);
            return 0;
        } catch (Exception exception) when (AudioEndpointAvailability.IsUnavailable(exception)) {
            return UnavailableRetryMilliseconds;
        } catch (Exception exception) {
            // This runs on the capture thread, where an escaping exception would end the host process. HRESULTs
            // also surface as ArgumentException, OutOfMemoryException and others, so every failure retries.
            DesktopManagerDiagnostics.Report($"Loopback capture failed and will retry: {exception.Message} (0x{exception.HResult:X8})");
            return FailureRetryMilliseconds;
        } finally {
            Volatile.Write(ref run.Capturing, 0);
            if (started) {
                client!.Stop();
            }
            NativeComActivation.Release(capture);
            NativeComActivation.Release(client);
            NativeComActivation.Release(device);
        }
    }

    private static bool TryInitialize(IAudioClient client, out AudioCaptureFormat format) {
        Marshal.ThrowExceptionForHR(client.GetMixFormat(out IntPtr mix));
        try {
            format = AudioCaptureFormat.Read(mix);
            if (format.Encoding == AudioSampleEncoding.Unsupported) {
                DesktopManagerDiagnostics.Report("Loopback capture does not support the output endpoint's mix format.");
                return false;
            }
            Marshal.ThrowExceptionForHR(client.Initialize(
                CoreAudioCaptureNative.SharedMode,
                CoreAudioCaptureNative.LoopbackStreamFlag,
                CoreAudioCaptureNative.BufferDuration,
                0,
                mix,
                IntPtr.Zero));
            return true;
        } finally {
            Marshal.FreeCoTaskMem(mix);
        }
    }

    private void Pump(CaptureRun run, IAudioCaptureClient capture, AudioCaptureFormat format) {
        float[] samples = new float[0];
        byte[] scratch = new byte[0];
        while (!run.Stopping && Volatile.Read(ref run.DefaultChanged) == 0) {
            Marshal.ThrowExceptionForHR(capture.GetNextPacketSize(out uint packet));
            while (packet > 0 && !run.Stopping) {
                Marshal.ThrowExceptionForHR(capture.GetBuffer(out IntPtr data, out uint frames, out uint flags, out _, out _));
                EventHandler<AudioSamplesCapturedEventArgs>? handlers = SamplesCaptured;
                int count = (int)frames * format.Channels;
                bool silent = (flags & CoreAudioCaptureNative.SilentBufferFlag) != 0;
                int released;
                try {
                    if (handlers != null) {
                        if (samples.Length < count) {
                            samples = new float[count];
                        }
                        if (silent) {
                            Array.Clear(samples, 0, count);
                        } else {
                            format.ConvertToFloat(data, (int)frames, samples, ref scratch);
                        }
                    }
                } finally {
                    released = capture.ReleaseBuffer(frames);
                }
                Marshal.ThrowExceptionForHR(released);
                if (handlers != null) {
                    Raise(handlers, new AudioSamplesCapturedEventArgs(samples, (int)frames, format.Channels, format.SampleRate, silent));
                }
                Marshal.ThrowExceptionForHR(capture.GetNextPacketSize(out packet));
            }
            run.Wait(PollMilliseconds);
        }
    }

    private void Raise(EventHandler<AudioSamplesCapturedEventArgs> handlers, AudioSamplesCapturedEventArgs args) {
        foreach (EventHandler<AudioSamplesCapturedEventArgs> handler in handlers.GetInvocationList()) {
            try {
                handler(this, args);
            } catch (Exception exception) {
                DesktopManagerDiagnostics.Report($"Loopback capture handler failed: {exception.Message}");
            }
        }
    }

    private static T Take<T>(int result, IntPtr pointer) where T : class {
        object instance = CoreAudioSessionsNative.TakeReference(result, pointer);
        try {
            return (T)instance;
        } catch {
            NativeComActivation.Release(instance);
            throw;
        }
    }
}
