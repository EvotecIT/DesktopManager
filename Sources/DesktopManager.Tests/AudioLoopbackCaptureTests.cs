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
}
