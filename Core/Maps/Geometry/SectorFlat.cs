using Helion.Util;
using static Helion.Util.Assert;

namespace Helion.Maps.Geometry
{
    public class SectorFlat
    {
        public readonly int Id;
        public CiString Texture;
        public int Z;
        public byte LightLevel;

        public SectorFlat(int id, CiString texture, int zHeight, byte lightLevel)
        {
            Precondition(zHeight >= short.MinValue && zHeight <= short.MaxValue, $"Floor height out of range: {zHeight}");

            Id = id;
            Texture = texture;
            Z = zHeight;
            LightLevel = lightLevel;
        }
    }
}
