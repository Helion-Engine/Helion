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

            ByteReader reader = new(data);
            if (reader.UShort() != DmxHeader)
                return false;

            sampleRate = reader.UShort();
            if (sampleRate < SampleRateArbitraryCutoff)
                return false;

            int numSamples = reader.Int();
            if (data.Length < DataBeforeSamplesSize + numSamples - DmxPadding)
                return false;

            reader.Advance(DmxPadding);
            sampleData = reader.Bytes(numSamples);
            return true;
        }
    }
}