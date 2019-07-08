using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.Maps.Geometry
{
    public class Sector
    {
        public readonly int Id;
        public readonly List<Side> Sides = new List<Side>();
        public readonly List<SectorFlat> Flats = new List<SectorFlat>();
        public byte LightLevel;

        public SectorFlat Floor => Flats[0];
        public SectorFlat Ceiling => Flats[1];
        public float UnitLightLevel => LightLevel / 255.0f;

        public Sector(int id, byte lightLevel, SectorFlat floor, SectorFlat ceiling)
        {
            Precondition(floor.Z <= ceiling.Z, "Sector floor is above the ceiling");

            Id = id;
            LightLevel = lightLevel;
            
            Flats.Add(floor);
            Flats.Add(ceiling);
        }

        public void Add(Side side)
        {
            Precondition(!Sides.Any(s => s.Id == side.Id), "Trying to add the same side twice");

            Sides.Add(side);
        }
    }
}
