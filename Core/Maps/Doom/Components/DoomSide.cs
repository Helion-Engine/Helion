using Helion.Maps.Components;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Maps.Doom.Components
{
    public class DoomSide : ISide
    {
        public int Id { get; }
        public Vec2I Offset { get; set; }
        public string UpperTexture { get; set; }
        public string MiddleTexture { get; set; }
        public string LowerTexture { get; set; }
        public readonly DoomSector Sector;

        public DoomSide(int id, Vec2I offset, string upperTexture, string middleTexture, string lowerTexture, 
            DoomSector sector)
        {
            Id = id;
            Offset = offset;
            UpperTexture = upperTexture;
            MiddleTexture = middleTexture;
            LowerTexture = lowerTexture;
            Sector = sector;
        }

        public ISector GetSector() => Sector;
    }
}