using Helion.Util;
using Helion.Util.Geometry;
using static Helion.Util.Assert;

namespace Helion.Maps.Geometry
{
    public class SectorFlat
    {
        public readonly int Id;
        public readonly bool FacingUp;
        public readonly PlaneD Plane;
        public UpperString Texture;
        public byte LightLevel;

        public SectorFlat(int id, bool facingUp, UpperString texture, double z, byte lightLevel)
        {
            Id = id;
            FacingUp = facingUp;
            Texture = texture;
            Plane = new PlaneD(z);
            LightLevel = lightLevel;
        }
    }
}
