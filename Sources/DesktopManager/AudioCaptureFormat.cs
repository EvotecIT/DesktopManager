using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal enum AudioSampleEncoding {
    Unsupported,
    Float32,
    Pcm16,
    Pcm24,
    Pcm32
}

/// <summary>The parts of a WAVEFORMATEX or WAVEFORMATEXTENSIBLE mix format needed to deliver float samples.</summary>
internal readonly struct AudioCaptureFormat {
    private const ushort PcmTag = 1;
    private const ushort FloatTag = 3;
    private const ushort ExtensibleTag = 0xFFFE;
    // KSDATAFORMAT_SUBTYPE_PCM and _IEEE_FLOAT share this layout with the format tag in their first field.
    private static readonly Guid PcmSubtype = new("00000001-0000-0010-8000-00AA00389B71");
    private static readonly Guid FloatSubtype = new("00000003-0000-0010-8000-00AA00389B71");

    internal AudioCaptureFormat(AudioSampleEncoding encoding, int channels, int sampleRate) {
        Encoding = encoding;
        Channels = channels;
        SampleRate = sampleRate;
    }

    internal AudioSampleEncoding Encoding { get; }
    internal int Channels { get; }
    internal int SampleRate { get; }

    internal int BytesPerSample => Encoding switch {
        AudioSampleEncoding.Pcm16 => 2,
        AudioSampleEncoding.Pcm24 => 3,
        AudioSampleEncoding.Float32 => 4,
        AudioSampleEncoding.Pcm32 => 4,
        _ => 0
    };

    /// <summary>Reads a native wave format. Formats other than 32-bit float or 16/24/32-bit integer PCM are unsupported.</summary>
    internal static AudioCaptureFormat Read(IntPtr waveFormat) {
        if (waveFormat == IntPtr.Zero) {
            throw new ArgumentNullException(nameof(waveFormat));
        }
        ushort tag = (ushort)Marshal.ReadInt16(waveFormat, 0);
        int channels = (ushort)Marshal.ReadInt16(waveFormat, 2);
        int sampleRate = Marshal.ReadInt32(waveFormat, 4);
        int blockAlign = (ushort)Marshal.ReadInt16(waveFormat, 12);
        int bits = (ushort)Marshal.ReadInt16(waveFormat, 14);
        if (tag == ExtensibleTag) {
            int extraSize = (ushort)Marshal.ReadInt16(waveFormat, 16);
            tag = extraSize >= 22 ? SubtypeTag(ReadGuid(waveFormat, 24)) : (ushort)0;
        }
        AudioSampleEncoding encoding = (tag, bits) switch {
            (FloatTag, 32) => AudioSampleEncoding.Float32,
            (PcmTag, 16) => AudioSampleEncoding.Pcm16,
            (PcmTag, 24) => AudioSampleEncoding.Pcm24,
            (PcmTag, 32) => AudioSampleEncoding.Pcm32,
            _ => AudioSampleEncoding.Unsupported
        };
        if (channels <= 0 || sampleRate <= 0 || blockAlign != channels * bits / 8) {
            encoding = AudioSampleEncoding.Unsupported;
        }
        return new AudioCaptureFormat(encoding, channels, sampleRate);
    }

    /// <summary>Converts interleaved native frames into interleaved floats with full scale at 1.0.</summary>
    /// <param name="data">The native packet.</param>
    /// <param name="frames">The number of frames in the packet.</param>
    /// <param name="destination">Receives frames times channels samples.</param>
    /// <param name="scratch">A reusable byte buffer for integer formats; grown when needed.</param>
    internal void ConvertToFloat(IntPtr data, int frames, float[] destination, ref byte[] scratch) {
        int count = frames * Channels;
        if (Encoding == AudioSampleEncoding.Float32) {
            Marshal.Copy(data, destination, 0, count);
            return;
        }
        int bytesPerSample = BytesPerSample;
        if (bytesPerSample == 0) {
            throw new InvalidOperationException("The capture format is not supported.");
        }
        int byteCount = count * bytesPerSample;
        if (scratch.Length < byteCount) {
            scratch = new byte[byteCount];
        }
        Marshal.Copy(data, scratch, 0, byteCount);
        for (int index = 0, offset = 0; index < count; index++, offset += bytesPerSample) {
            destination[index] = Encoding switch {
                AudioSampleEncoding.Pcm16 => BitConverter.ToInt16(scratch, offset) / 32768f,
                AudioSampleEncoding.Pcm24 => (scratch[offset] | (scratch[offset + 1] << 8) | ((sbyte)scratch[offset + 2] << 16)) / 8388608f,
                _ => BitConverter.ToInt32(scratch, offset) / 2147483648f
            };
        }
    }

    private static ushort SubtypeTag(Guid subtype) {
        if (subtype == FloatSubtype) {
            return FloatTag;
        }
        return subtype == PcmSubtype ? PcmTag : (ushort)0;
    }

    private static Guid ReadGuid(IntPtr pointer, int offset) {
        var bytes = new byte[16];
        Marshal.Copy(pointer + offset, bytes, 0, bytes.Length);
        return new Guid(bytes);
    }
}
