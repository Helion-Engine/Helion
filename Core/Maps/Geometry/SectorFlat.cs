using System;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Maps.Geometry
{
    public class SectorFlat
    {
        public readonly int Id;
        public readonly PlaneD Plane;
        public readonly SectorFlatFace Facing;
        public double Z;
        public double PrevZ;
        public CIString Texture;
        public short LightLevel;
        private Sector? m_sector;

        public Sector Sector => m_sector ?? throw new NullReferenceException("Forgot to set sector for sector flat");
        public float UnitLightLevel => LightLevel / 255.0f;

        public SectorFlat(int id, CIString texture, double z, short lightLevel, SectorFlatFace facing)
        {
            Id = id;
            Facing = facing;
            Texture = texture;
            Z = z;
            PrevZ = z;
            Plane = new PlaneD(z);
            LightLevel = lightLevel;
        }

        public void SetSector(Sector sector)
        {
            m_sector = sector;
        }
    }
}