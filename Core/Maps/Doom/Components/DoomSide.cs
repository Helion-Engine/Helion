using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Specials;

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

        public SideScrollData? ScrollData { get; set; }

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