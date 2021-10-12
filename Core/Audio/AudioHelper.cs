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
            using WaveFileReader wave = new WaveFileReader(new MemoryStream(data));
            using MemoryStream writeStream = new MemoryStream();
            using WaveFileWriter writer = new WaveFileWriter(writeStream, wave.WaveFormat);

            byte[] buffer = new byte[WavBufferSize];
            int readAmount = buffer.Length - (buffer.Length % wave.WaveFormat.BlockAlign);
            int length = wave.Read(buffer, 0, readAmount);
            while (length > 0)
            {
                writer.Write(buffer, 0, length);
                length = wave.Read(buffer, 0, readAmount);
            }

            int mod = (int)writeStream.Length % WavBufferSize;
            if (mod != 0)
                writeStream.Write(new byte[mod], 0, mod);

            format.Channels = wave.WaveFormat.Channels;
            format.SampleRate = wave.WaveFormat.SampleRate;
            format.BitsPerSample = wave.WaveFormat.BitsPerSample;
            sampleData = new Span<byte>(writeStream.ToArray());
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
