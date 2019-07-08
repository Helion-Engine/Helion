using Helion.Util;
using static Helion.Util.Assert;

namespace Helion.Maps.Geometry
{
    public class SectorFlat
    {
        public readonly int Id;
        public readonly bool FacingUp;
        public UpperString Texture;
        public int Z;
        public byte LightLevel;

        public SectorFlat(int id, bool facingUp, UpperString texture, int zHeight, byte lightLevel)
        {
            Precondition(zHeight >= short.MinValue && zHeight <= short.MaxValue, $"Floor height out of range: {zHeight}");

            Id = id;
            FacingUp = facingUp;
            Texture = texture;
            Z = zHeight;
            LightLevel = lightLevel;
        }
    }
}
