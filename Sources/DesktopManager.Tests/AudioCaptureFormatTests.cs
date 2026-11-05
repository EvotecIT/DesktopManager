using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public sealed class AudioCaptureFormatTests {
    private static readonly Guid PcmSubtype = new("00000001-0000-0010-8000-00AA00389B71");
    private static readonly Guid FloatSubtype = new("00000003-0000-0010-8000-00AA00389B71");

    [TestMethod]
    public void Read_RecognizesSharedModeMixFormats_AndRejectsOthers() {
        AssertFormat(Extensible(2, 48000, 32, FloatSubtype), AudioSampleEncoding.Float32, 2, 48000);
        // 24 valid bits in a 32-bit container is left-justified, so it reads as 32-bit PCM.
        AssertFormat(Extensible(6, 44100, 32, PcmSubtype, validBits: 24), AudioSampleEncoding.Pcm32, 6, 44100);
        AssertFormat(Extensible(2, 96000, 24, PcmSubtype), AudioSampleEncoding.Pcm24, 2, 96000);
        AssertFormat(Plain(1, 1, 16000, 16), AudioSampleEncoding.Pcm16, 1, 16000);
        AssertFormat(Plain(3, 2, 48000, 32), AudioSampleEncoding.Float32, 2, 48000);
        AssertFormat(Plain(3, 2, 48000, 64), AudioSampleEncoding.Unsupported, 2, 48000);
        AssertFormat(Extensible(2, 48000, 32, Guid.NewGuid()), AudioSampleEncoding.Unsupported, 2, 48000);
    }

    [TestMethod]
    public void ConvertToFloat_ScalesEachIntegerEncodingToFullScale_AndKeepsFloatSamples() {
        byte[] pcm16 = Bytes(BitConverter.GetBytes((short)-32768), BitConverter.GetBytes((short)16384), BitConverter.GetBytes((short)0), BitConverter.GetBytes((short)-16384));
        CollectionAssert.AreEqual(new[] { -1f, 0.5f, 0f, -0.5f }, Convert(AudioSampleEncoding.Pcm16, pcm16, frames: 2));

        // Little-endian 24-bit: -8388608, 4194304, -4194304 and 0; the top byte carries the sign.
        byte[] pcm24 = { 0x00, 0x00, 0x80, 0x00, 0x00, 0x40, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00 };
        CollectionAssert.AreEqual(new[] { -1f, 0.5f, -0.5f, 0f }, Convert(AudioSampleEncoding.Pcm24, pcm24, frames: 2));

        byte[] pcm32 = Bytes(BitConverter.GetBytes(int.MinValue), BitConverter.GetBytes(1 << 30), BitConverter.GetBytes(-(1 << 30)), BitConverter.GetBytes(0));
        CollectionAssert.AreEqual(new[] { -1f, 0.5f, -0.5f, 0f }, Convert(AudioSampleEncoding.Pcm32, pcm32, frames: 2));

        byte[] float32 = Bytes(BitConverter.GetBytes(0.25f), BitConverter.GetBytes(-1.5f), BitConverter.GetBytes(1f), BitConverter.GetBytes(0f));
        CollectionAssert.AreEqual(new[] { 0.25f, -1.5f, 1f, 0f }, Convert(AudioSampleEncoding.Float32, float32, frames: 2));
    }

    private static float[] Convert(AudioSampleEncoding encoding, byte[] native, int frames) {
        var format = new AudioCaptureFormat(encoding, 2, 48000);
        var destination = new float[frames * 2];
        var scratch = new byte[0];
        WithPointer(native, pointer => format.ConvertToFloat(pointer, frames, destination, ref scratch));
        return destination;
    }

    private static void AssertFormat(byte[] waveFormat, AudioSampleEncoding encoding, int channels, int sampleRate) {
        AudioCaptureFormat format = default;
        WithPointer(waveFormat, pointer => format = AudioCaptureFormat.Read(pointer));
        Assert.AreEqual(encoding, format.Encoding);
        Assert.AreEqual(channels, format.Channels);
        Assert.AreEqual(sampleRate, format.SampleRate);
    }

    private static byte[] Plain(ushort tag, int channels, int sampleRate, int bits) {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        WriteHeader(writer, tag, channels, sampleRate, bits, extraSize: 0);
        return stream.ToArray();
    }

    private static byte[] Extensible(int channels, int sampleRate, int bits, Guid subtype, int? validBits = null) {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        WriteHeader(writer, 0xFFFE, channels, sampleRate, bits, extraSize: 22);
        writer.Write((ushort)(validBits ?? bits));
        writer.Write(0x3u);
        writer.Write(subtype.ToByteArray());
        return stream.ToArray();
    }

    private static void WriteHeader(BinaryWriter writer, ushort tag, int channels, int sampleRate, int bits, ushort extraSize) {
        int blockAlign = channels * bits / 8;
        writer.Write(tag);
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * blockAlign);
        writer.Write((ushort)blockAlign);
        writer.Write((ushort)bits);
        writer.Write(extraSize);
    }

    private static byte[] Bytes(params byte[][] parts) => parts.SelectMany(part => part).ToArray();

    private static void WithPointer(byte[] data, Action<IntPtr> use) {
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try {
            use(handle.AddrOfPinnedObject());
        } finally {
            handle.Free();
        }
    }
}
