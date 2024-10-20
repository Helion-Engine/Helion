using NAudio.Wave;
using System;
using System.IO;

namespace Helion.Audio;

public static class AudioHelper
{
    private const ushort DmxHeader = 3;
    private const int DmxPadding = 16;
    private const int SampleRateArbitraryCutoff = 5000;
    private const int DataBeforeSamplesSize = 2 + 2 + 4 + DmxPadding;
    private const int MinRequiredDmxLength = DataBeforeSamplesSize + DmxPadding;
    private const int WavBufferSize = 16384;

    public struct WavFormat
    {
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
    };

    public static bool IsWave(byte[] data) =>
        data.Length >= 4 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F';

    public static bool IsDoomSound(byte[] data)
    {
        if (data.Length < 8)
            return false;

        int offset = 0;
        if (ReadUInt16(data, ref offset) != 3)
            return false;

        return true;
    }

    public static bool TryReadDoomSound(byte[] data, out int sampleRate, out Span<byte> sampleData, out string? error)
    {
        error = "Invalid Doom sound.";
        sampleRate = 0;
        sampleData = Array.Empty<byte>();

        if (data.Length < MinRequiredDmxLength)
            return false;

        int offset = 0;
        if (ReadUInt16(data, ref offset) != DmxHeader)
            return false;

        sampleRate = ReadUInt16(data, ref offset);
        if (sampleRate < SampleRateArbitraryCutoff)
            return false;

        int numSamples = ReadInt32(data, ref offset);
        if (data.Length < DataBeforeSamplesSize + numSamples - DmxPadding)
            return false;

        sampleData = new Span<byte>(data, offset, numSamples);
        return true;
    }

    public static bool TryReadWav(byte[] data, out WavFormat format, out Span<byte> sampleData)
    {
        format = default;

        try
        {
            using MemoryStream ms = new MemoryStream(data);
            using WaveFileReader reader = new WaveFileReader(ms);

            format.Channels = reader.WaveFormat.Channels;
            format.SampleRate = reader.WaveFormat.SampleRate;
            format.BitsPerSample = reader.WaveFormat.BitsPerSample;

            var buffer = new byte[reader.Length];
            reader.Read(buffer);
            sampleData = buffer;
            return true;
        }
        catch
        {
            sampleData = Array.Empty<byte>();
            return false;
        }
    }

    private static ushort ReadUInt16(byte[] data, ref int offset)
    {
        ushort value = (ushort)(data[offset] | data[offset + 1] << 8);
        offset += 2;
        return value;
    }

    private static int ReadInt32(byte[] data, ref int offset)
    {
        int value = data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
        offset += 4;
        return value;
    }
}
