using System;

namespace DesktopManager;

/// <summary>One packet of captured audio as interleaved 32-bit float samples.</summary>
/// <remarks>
/// <see cref="Samples"/> is a buffer the capture reuses for the next packet. Read it inside the handler,
/// or copy the first <see cref="SampleCount"/> values to keep them.
/// </remarks>
public sealed class AudioSamplesCapturedEventArgs : EventArgs {
    internal AudioSamplesCapturedEventArgs(float[] samples, int frameCount, int channels, int sampleRate, bool isSilent) {
        Samples = samples;
        FrameCount = frameCount;
        Channels = channels;
        SampleRate = sampleRate;
        IsSilent = isSilent;
    }

    /// <summary>
    /// Gets the interleaved samples, frame by frame and channel by channel, with full scale at -1.0 and 1.0.
    /// Only the first <see cref="SampleCount"/> values belong to this packet. A floating-point mix can exceed full scale.
    /// </summary>
    public float[] Samples { get; }

    /// <summary>Gets the number of frames in this packet; each frame holds one sample per channel.</summary>
    public int FrameCount { get; }

    /// <summary>Gets the number of interleaved channels.</summary>
    public int Channels { get; }

    /// <summary>Gets the sample rate in frames per second.</summary>
    public int SampleRate { get; }

    /// <summary>Gets the number of valid values in <see cref="Samples"/>.</summary>
    public int SampleCount => FrameCount * Channels;

    /// <summary>Gets whether Windows marked the packet as silence; its samples are all zero.</summary>
    public bool IsSilent { get; }
}
