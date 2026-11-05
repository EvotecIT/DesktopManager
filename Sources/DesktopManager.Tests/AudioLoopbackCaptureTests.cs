using System.Diagnostics;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public sealed class AudioLoopbackCaptureTests {
    [TestMethod]
    [TestCategory("ReadOnlyAudio")]
    public void Capture_OpensLoopbackOnTheDefaultOutput_AndStopsDeterministically() {
        // Opens a shared-mode loopback stream through each target's COM interop model. Nothing is played or changed.
        bool hasOutput = new AudioService().GetEndpoints(AudioDataFlow.Render, AudioEndpointState.Active)
            .Any(endpoint => endpoint.DefaultRoles.Contains(AudioRole.Multimedia));
        if (!hasOutput) {
            Assert.Inconclusive("No active default output endpoint exists.");
        }

        var capture = new AudioLoopbackCapture();
        int packetsAfterStop = 0;
        int malformedPackets = 0;
        bool stopped = false;
        capture.SamplesCaptured += (_, args) => {
            if (args.Channels <= 0 || args.SampleRate <= 0 || args.SampleCount > args.Samples.Length) {
                Interlocked.Increment(ref malformedPackets);
            }
            if (Volatile.Read(ref stopped)) {
                Interlocked.Increment(ref packetsAfterStop);
            }
        };
        capture.Start();
        capture.Start();
        Stopwatch elapsed = Stopwatch.StartNew();
        while (!capture.IsCapturing && elapsed.Elapsed < TimeSpan.FromSeconds(5)) {
            Thread.Sleep(20);
        }
        Assert.IsTrue(capture.IsCapturing, "Windows must accept the loopback stream on the default output.");

        capture.Stop();
        Volatile.Write(ref stopped, true);
        Assert.IsFalse(capture.IsCapturing);
        Thread.Sleep(100);
        Assert.AreEqual(0, Volatile.Read(ref packetsAfterStop), "No packet may be raised after Stop returns.");
        Assert.AreEqual(0, Volatile.Read(ref malformedPackets), "Every packet must describe its own samples.");

        capture.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => capture.Start());
    }
    [TestMethod]
    [DataRow(false, DisplayName = "Idle output")]
    [DataRow(true, DisplayName = "Continuously queued packets")]
    public void PumpWithoutNotifications_ReturnsToReopenTheDefaultOutput(bool hasPackets) {
        using var capture = new AudioLoopbackCapture();
        var run = new AudioLoopbackCapture.CaptureRun();
        var client = new QueuedCaptureClient(hasPackets);
        var format = new AudioCaptureFormat(AudioSampleEncoding.Pcm16, 2, 48000);
        var pump = System.Threading.Tasks.Task.Run(() => capture.Pump(run, client, format, retryDefaultWatcher: true));
        bool returned;
        try {
            returned = pump.Wait(TimeSpan.FromSeconds(3));
        } finally {
            run.RequestStop();
            pump.GetAwaiter().GetResult();
        }
        Assert.IsTrue(returned, "Without endpoint notifications, the pump must return so the current default and registration are retried.");
    }

    private sealed class QueuedCaptureClient : IAudioCaptureClient {
        private readonly bool _hasPackets;

        internal QueuedCaptureClient(bool hasPackets) {
            _hasPackets = hasPackets;
        }

        public int GetNextPacketSize(out uint frames) {
            // A permanently nonempty queue also exercises the inner drain loop without opening a real device.
            Thread.Sleep(1);
            frames = _hasPackets ? 1u : 0u;
            return 0;
        }

        public int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong performanceCounterPosition) {
            data = IntPtr.Zero;
            frames = 1;
            flags = CoreAudioCaptureNative.SilentBufferFlag;
            devicePosition = 0;
            performanceCounterPosition = 0;
            return 0;
        }

        public int ReleaseBuffer(uint frames) => 0;
    }
}
