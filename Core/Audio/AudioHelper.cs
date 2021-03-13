using System.Diagnostics.CodeAnalysis;
using Helion.Util;
using Helion.Util.Bytes;

namespace Helion.Audio
{
    public static class AudioHelper
    {
        private const ushort DmxHeader = 3;
        private const int DmxPadding = 16;
        private const int SampleRateArbitraryCutoff = 5000;
        private const int DataBeforeSamplesSize = 2 + 2 + 4 + DmxPadding;
        private const int MinRequiredDmxLength = DataBeforeSamplesSize + DmxPadding;

        public static bool TryReadDoomSound(byte[] data, out int sampleRate, out byte[] sampleData)
        {
            sampleRate = 0;
            sampleData = new byte[0];
            
            if (data.Length < MinRequiredDmxLength)
                return false;
            
            ByteReader reader = new ByteReader(data);
            if (reader.ReadUInt16() != DmxHeader)
                return false;

            sampleRate = reader.ReadUInt16();
            if (sampleRate < SampleRateArbitraryCutoff)
                return false;

            int numSamples = reader.ReadInt32();
            if (data.Length < DataBeforeSamplesSize + numSamples - DmxPadding)
                return false;

            reader.Advance(DmxPadding);
            sampleData = reader.ReadBytes(numSamples);
            return true;
        }
    }
}