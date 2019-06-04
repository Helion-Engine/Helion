using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Maps.Geometry
{
    public class Side
    {
        public readonly int Id;
        public Vec2I Offset;
        public string LowerTexture;
        public string MiddleTexture;
        public string UpperTexture;
        public readonly Sector Sector;

        public Line Line {
            get {
                if (line == null)
                    throw new HelionException("Trying to get the line for a side that has not had it set");
                return line;
            }
            set { line = value; }
        }
        private Line? line;

        public bool IsFront => ReferenceEquals(this, Line.Front);
        public bool IsBack => !IsFront;
        public Side? PartnerSide => ReferenceEquals(this, Line.Front) ? Line.Front : (Line.TwoSided ? Line.Back : null);

        public Side(int id, Vec2I offset, string lowerTexture, string middleTexture, string upperTexture, 
            Sector sector)
        {
            Id = id;
            Offset = offset;
            LowerTexture = lowerTexture;
            MiddleTexture = middleTexture;
            UpperTexture = upperTexture;
            Sector = sector;

            sector.Add(this);
        }
    }
}
