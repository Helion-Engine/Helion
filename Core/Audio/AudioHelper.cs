using System;

namespace Helion.Audio
{
    public static class AudioHelper
    {
        private const ushort DmxHeader = 3;
        private const int DmxPadding = 16;
        private const int SampleRateArbitraryCutoff = 5000;
        private const int DataBeforeSamplesSize = 2 + 2 + 4 + DmxPadding;
        private const int MinRequiredDmxLength = DataBeforeSamplesSize + DmxPadding;

        public static bool TryReadDoomSound(byte[] data, out int sampleRate, out Span<byte> sampleData)
        {
            sampleRate = 0;
            sampleData = Array.Empty<byte>();
            
            if (data.Length < MinRequiredDmxLength)
                return false;

            int offset = 0;
            if (ReadUInt16(data, ref offset)  != DmxHeader)
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
}